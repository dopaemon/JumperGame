using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
public sealed class PlayerController : MonoBehaviour
{
    private static PhysicsMaterial2D sharedPlayerMaterial;
    private const float InputThreshold = 0.01f;
	private Vector3 baseScale;
	private Quaternion baseRotation;

	[SerializeField] private float startMoveSpeed = 2.9f;
    [SerializeField] private float maxMoveSpeed = 8.5f;
    [SerializeField] private float moveSpeedGainStartY = 8f;
    [SerializeField] private float moveSpeedGainEndY = 70f;
    [SerializeField] private float horizontalAcceleration = 38f;
    [SerializeField] private float horizontalDeceleration = 52f;
    [SerializeField] private float jumpVelocity = 15f;
    [SerializeField] private float wrapPadding = 0.35f;
    [SerializeField] private float topBounceTolerance = 0.1f;
    [SerializeField] private float bounceProbeDepth = 0.38f;
    [SerializeField] private float shootCooldown = 0.2f;
	[SerializeField] private Sprite projectileSprite;

	private float originalJumpVelocity;
	private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private IHorizontalInputSource inputSource;
    private Camera targetCamera;
    private Transform projectileParent;
    private readonly System.Collections.Generic.List<PlayerProjectile> projectiles = new();
    private bool controlsEnabled = true;
    private bool canBounce = true;
    private bool isDead;
    private float flightBoostTimer;
    private float cachedHorizontal;
    private float previousBottomY;
    private float nextShootTime;
	private Coroutine highJumpCoroutine;

	public event Action<float> HighestPointChanged;

    public CapsuleCollider2D PlayerCollider { get; private set; }
    public float VerticalVelocity => rb.linearVelocity.y;
    public float HighestY { get; private set; }
    public bool IsDead => isDead;

	[SerializeField] private GameObject dustPrefab;
	private bool wasGrounded;

	public bool IsFalling => rb.linearVelocity.y < -0.1f;
	public void Initialize(IHorizontalInputSource horizontalInputSource, Camera gameplayCamera, Transform projectileRoot = null)
    {
        inputSource = horizontalInputSource;
        targetCamera = gameplayCamera;
        projectileParent = projectileRoot;
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        PlayerCollider = GetComponent<CapsuleCollider2D>();

        spriteRenderer = GetComponent<SpriteRenderer>();
        Sprite playerSprite = RuntimeSpriteFactory.PlayerSprite;
        spriteRenderer.sprite = playerSprite != null ? playerSprite : RuntimeSpriteFactory.WhiteSprite;
        spriteRenderer.color = playerSprite != null ? Color.white : new Color(0.98f, 0.89f, 0.35f);
        spriteRenderer.sortingOrder = 2;

        transform.localScale = playerSprite != null ? new Vector3(1.2f, 1.2f, 1f) : new Vector3(0.85f, 1.1f, 1f);

        rb.gravityScale = 3.1f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        PlayerCollider.direction = CapsuleDirection2D.Vertical;
        PlayerCollider.size = RuntimeSpriteFactory.PlayerSprite != null ? new Vector2(0.48f, 0.74f) : new Vector2(0.72f, 1f);
        PlayerCollider.offset = RuntimeSpriteFactory.PlayerSprite != null ? new Vector2(0f, -0.08f) : Vector2.zero;
        PlayerCollider.sharedMaterial = GetPlayerMaterial();
        HighestY = transform.position.y;
		originalJumpVelocity = jumpVelocity;
		previousBottomY = PlayerCollider.bounds.min.y;

        SetControlsEnabled(false);
        rb.simulated = false;
		baseScale = transform.localScale;
		baseRotation = transform.rotation;
	}

	private void Start()
	{
		if (PlayerInventory.Instance == null)
			return;
	}

	public void ActivateHighJump(float multiplier, float duration)
	{
		jumpVelocity = originalJumpVelocity * multiplier;

		if (highJumpCoroutine != null)
			StopCoroutine(highJumpCoroutine);

		highJumpCoroutine = StartCoroutine(ResetHighJump(duration));
	}

	private System.Collections.IEnumerator ResetHighJump(float duration)
	{
		yield return new WaitForSeconds(duration);
		jumpVelocity = originalJumpVelocity;
	}

	// Allow external code to set the player's sprite (used by bootstrap when LevelData provides a sprite)
	public void SetSprite(Sprite sprite)
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
            {
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            }
        }

        if (sprite != null)
        {
            spriteRenderer.sprite = sprite;
            spriteRenderer.color = Color.white;

            // Normalize scale then size sprite to a consistent player height in world units
            transform.localScale = Vector3.one;
            // Force a renderer update and measure bounds
            float spriteWorldHeight = spriteRenderer.bounds.size.y;
            if (spriteWorldHeight <= 0f)
            {
                spriteWorldHeight = 1f; // fallback
            }
            const float desiredPlayerHeight = 1.2f;
            float scaleFactor = desiredPlayerHeight / spriteWorldHeight;
            transform.localScale = new Vector3(scaleFactor, scaleFactor, 1f);
            baseScale = transform.localScale;
        }
    }

    public void BeginRun()
    {
        rb.simulated = true;
        SetControlsEnabled(true);
        Bounce();
    }

    private void Update()
    {
        if (!controlsEnabled)
        {
            cachedHorizontal = 0f;
            return;
        }

        cachedHorizontal = inputSource != null ? inputSource.GetHorizontal() : 0f;
        UpdateFacingDirection();
		UpdateJumpAnimation();

		HandleShootInput();

        if (transform.position.y > HighestY)
        {
            HighestY = transform.position.y;
            HighestPointChanged?.Invoke(HighestY);
        }
    }

    private void FixedUpdate()
    {
        Vector2 velocity = rb.linearVelocity;
        float currentMoveSpeed = GetCurrentMoveSpeed();
        float targetVelocityX = cachedHorizontal * currentMoveSpeed;
        float acceleration = Mathf.Abs(cachedHorizontal) > InputThreshold ? horizontalAcceleration : horizontalDeceleration;
        velocity.x = Mathf.MoveTowards(velocity.x, targetVelocityX, acceleration * Time.fixedDeltaTime);

        if (flightBoostTimer > 0f)
        {
            flightBoostTimer = Mathf.Max(0f, flightBoostTimer - Time.fixedDeltaTime);
            velocity.y = FlightPowerUp.BoostVelocity;
        }

        rb.linearVelocity = velocity;

        WrapAcrossScreen();
        TryProbeBounce();

        previousBottomY = PlayerCollider.bounds.min.y;
    }

    private float GetCurrentMoveSpeed()
    {
        if (Mathf.Abs(cachedHorizontal) <= InputThreshold)
        {
            return 0f;
        }

        float paceT = Mathf.InverseLerp(moveSpeedGainStartY, moveSpeedGainEndY, HighestY);
        return Mathf.Lerp(startMoveSpeed, maxMoveSpeed, paceT);
    }

    public void Bounce()
    {
        Bounce(jumpVelocity);
    }

    public void Bounce(float bounceVelocity)
    {
        if (!canBounce)
        {
            return;
        }

        Vector2 velocity = rb.linearVelocity;
        velocity.y = bounceVelocity;
        rb.linearVelocity = velocity;

		AudioManager.Instance.PlayJump();
	}

    public void SetControlsEnabled(bool isEnabled)
    {
        controlsEnabled = isEnabled;
        canBounce = isEnabled;

        if (!controlsEnabled)
        {
            rb.linearVelocity = Vector2.zero;
        }
    }

    public void Kill()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        SetControlsEnabled(false);
    }

    public void ActivateFlightBoost(float duration)
    {
        if (isDead)
        {
            return;
        }
		AudioManager.Instance.PlayRocket();
		flightBoostTimer = Mathf.Max(flightBoostTimer, duration);
        Bounce(FlightPowerUp.BoostVelocity);
    }

    public void TryShoot()
    {
        if (!controlsEnabled || isDead || Time.time < nextShootTime)
        {
            return;
        }

        nextShootTime = Time.time + shootCooldown;
		AudioManager.Instance.PlayShoot();
		PlayerProjectile projectile = RentProjectile();

		projectile.Init(projectileSprite);

		projectile.transform.SetParent(projectileParent, false);
		projectile.transform.position = transform.position + new Vector3(0f, 0.72f, 0f);
		projectile.gameObject.SetActive(true);
	}

    private void TryProbeBounce()
    {
        if (!canBounce)
        {
            return;
        }

        if (flightBoostTimer > 0f)
        {
            return;
        }

        if (VerticalVelocity > 0f)
        {
            return;
        }

        if (targetCamera != null)
        {
            float cameraBottom = targetCamera.transform.position.y - targetCamera.orthographicSize;

			if (PlayerCollider.bounds.max.y < cameraBottom)
            {
                return;
            }
        }

        Bounds bounds = PlayerCollider.bounds;
        float currentBottomY = bounds.min.y;
        float sweepBottomY = Mathf.Min(previousBottomY, currentBottomY) - bounceProbeDepth;
        float sweepTopY = Mathf.Max(previousBottomY, currentBottomY) + topBounceTolerance;

        if (targetCamera != null)
        {
            float cameraBottom = targetCamera.transform.position.y - targetCamera.orthographicSize;
            sweepBottomY = Mathf.Max(sweepBottomY, cameraBottom);
        }

        float sweepHeight = Mathf.Max(bounceProbeDepth, sweepTopY - sweepBottomY);
        Vector2 probeCenter = new Vector2(bounds.center.x, (sweepBottomY + sweepTopY) * 0.5f);
        Vector2 probeSize = new Vector2(bounds.size.x * 0.92f, sweepHeight);
		Collider2D[] hits = Physics2D.OverlapBoxAll(probeCenter,probeSize,0f);

		int hitCount = hits.Length;

		PlatformBase bestPlatform = null;
        float bestPlatformTop = float.MinValue;

        for (int i = 0; i < hitCount; i++)
        {
			Collider2D hit = hits[i];
			if (hit == null)
            {
                continue;
            }

            PlatformBase platform = hit.GetComponent<PlatformBase>();
            if (platform == null)
            {
                continue;
            }

            if (!platform.CanBounce(this, topBounceTolerance))
            {
                continue;
            }

            float platformTop = platform.TopY;
            bool crossedPlatformTop = sweepTopY >= platformTop - topBounceTolerance
                && sweepBottomY <= platformTop + topBounceTolerance;

            if (!crossedPlatformTop)
            {
                continue;
            }

            if (platformTop > bestPlatformTop)
            {
                bestPlatformTop = platformTop;
                bestPlatform = platform;
            }
        }

		if (bestPlatform != null)
		{
			if (!wasGrounded)
			{
				Vector3 hitPos = PlayerCollider.bounds.min;
				Instantiate(dustPrefab, hitPos, Quaternion.identity);
			}

			wasGrounded = true;

			bestPlatform.HandleBounce(this);
		}
		else
		{
			wasGrounded = false;
		}
	}

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<EnemyHazard>() != null)
        {
            if (flightBoostTimer <= 0f)
            {
                Kill();
            }

            return;
        }

        FlightPowerUp powerUp = collision.GetComponent<FlightPowerUp>();
        if (powerUp != null)
        {
            ActivateFlightBoost(FlightPowerUp.BoostDuration);
            powerUp.gameObject.SetActive(false);
        }
    }

    private void HandleShootInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            TryShoot();
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Space))
        {
            TryShoot();
        }
#endif
    }

    private void WrapAcrossScreen()
    {
        if (targetCamera == null)
        {
            return;
        }

        float halfWidth = targetCamera.orthographicSize * targetCamera.aspect;
        float left = targetCamera.transform.position.x - halfWidth - wrapPadding;
        float right = targetCamera.transform.position.x + halfWidth + wrapPadding;
        Vector3 position = transform.position;

        if (position.x < left)
        {
            position.x = right;
            transform.position = position;
        }
        else if (position.x > right)
        {
            position.x = left;
            transform.position = position;
        }
    }

    private void UpdateFacingDirection()
    {
        if (spriteRenderer == null || Mathf.Abs(cachedHorizontal) < 0.01f)
        {
            return;
        }

        spriteRenderer.flipX = cachedHorizontal < 0f;
    }

    private static PhysicsMaterial2D GetPlayerMaterial()
    {
        if (sharedPlayerMaterial == null)
        {
            sharedPlayerMaterial = new PhysicsMaterial2D("PlayerMaterial")
            {
                friction = 0f,
                bounciness = 0f
            };
        }

        return sharedPlayerMaterial;
    }

	private PlayerProjectile RentProjectile()
	{
		for (int i = 0; i < projectiles.Count; i++)
		{
			PlayerProjectile projectile = projectiles[i];
			if (projectile != null && !projectile.gameObject.activeSelf)
			{
				return projectile;
			}
		}

		GameObject projectileObject = new GameObject("Player Projectile");

		projectileObject.transform.localScale = Vector3.one * 0.2f;

		PlayerProjectile createdProjectile = projectileObject.AddComponent<PlayerProjectile>();
		projectiles.Add(createdProjectile);

		return createdProjectile;
	}

	private void UpdateJumpAnimation()
	{
		Vector3 targetScale = baseScale;

		if (rb.linearVelocity.y > 1f)
		{
			targetScale = new Vector3(
				baseScale.x * 0.9f,
				baseScale.y * 1.1f,
				1f);
		}

		else if (rb.linearVelocity.y < -1f)
		{
			targetScale = new Vector3(
				baseScale.x * 1.08f,
				baseScale.y * 0.92f,
				1f);
		}

		transform.localScale = Vector3.Lerp(
			transform.localScale,
			targetScale,
			Time.deltaTime * 8f);
	}

}
