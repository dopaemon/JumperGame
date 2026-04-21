using UnityEngine;

public sealed class FlightPowerUp : MonoBehaviour
{
	public const float BoostVelocity = 18f;
	public const float BoostDuration = 1.35f;

	private SpriteRenderer spriteRenderer;

	private Vector3 startWorldPosition;

	[SerializeField] private float bobAmplitude = 0.12f;
	[SerializeField] private float bobSpeed = 2f;

	private void Awake()
	{
		spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
		spriteRenderer.sprite = RuntimeSpriteFactory.WhiteSprite;
		spriteRenderer.color = new Color(0.98f, 0.56f, 0.16f);
		spriteRenderer.sortingOrder = 3;

		transform.localScale = new Vector3(0.48f, 0.7f, 1f);

		BoxCollider2D triggerCollider = gameObject.AddComponent<BoxCollider2D>();
		triggerCollider.isTrigger = true;
		triggerCollider.size = new Vector2(1.0f, 1.4f);
	}

	private void OnEnable()
	{
		startWorldPosition = transform.position;
	}

	private void Update()
	{
		float yOffset =
			Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;

		transform.position =
			startWorldPosition + Vector3.up * yOffset;
	}

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
		}
	}
}