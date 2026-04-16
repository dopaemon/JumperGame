using UnityEngine;

public sealed class SpringPlatform : PlatformBase
{
    private const float SpringBoostVelocity = 21f;

	private SpriteRenderer springRenderer;
	private Vector3 springBaseScale = Vector3.one;
	private const float SpringPressScale = 1.18f;

	public void Initialize(Sprite platformSprite, Sprite springSprite, Vector2 size)
	{
		base.Initialize(platformSprite, size);

		if (springRenderer == null)
		{
			Transform springTransform = transform.Find("Spring");
			GameObject springObject = springTransform != null ? springTransform.gameObject : new GameObject("Spring");

			springObject.transform.SetParent(transform, false);
            // Position the spring closer to the platform top so it sits just above the platform graphic
			springObject.transform.localPosition = new Vector3(0f, 0.26f, 0f);

			// create or get renderer before sizing so we can measure sprite bounds
			springRenderer = springObject.GetComponent<SpriteRenderer>();
			if (springRenderer == null)
			{
				springRenderer = springObject.AddComponent<SpriteRenderer>();
			}

			// set a default scale; actual scale will be computed after assigning the sprite
			springObject.transform.localScale = Vector3.one;


			springRenderer.sortingOrder = 2;
		}

		// assign sprite and size the spring so its world size is stable (not stretched by parent scale)
        springRenderer.sprite = springSprite;
		springRenderer.color = Color.white;

		if (springSprite != null)
		{
			// desired world dimensions for the spring visual
			float desiredWorldWidth = 0.34f;
			float desiredWorldHeight = 0.42f;
			Vector2 spriteSize = springSprite.bounds.size;
			Vector3 parentScale = transform.localScale;
            float scaleX = desiredWorldWidth / (Mathf.Max(0.0001f, spriteSize.x) * Mathf.Max(0.0001f, parentScale.x));
			float scaleY = desiredWorldHeight / (Mathf.Max(0.0001f, spriteSize.y) * Mathf.Max(0.0001f, parentScale.y));
			springBaseScale = new Vector3(scaleX, scaleY, 1f);
			springRenderer.transform.localScale = springBaseScale;
		}
		ResetState();
	}


	protected override void ResetState()
    {
        if (springRenderer != null)
		{
			springRenderer.color = Color.white;
			springRenderer.transform.localScale = springBaseScale;
		}
    }

    public override void HandleBounce(PlayerController player)
    {
        if (springRenderer != null)
		{
			// Briefly scale the spring to show activation rather than tinting (avoids tint artifacts)
			springRenderer.transform.localScale = springBaseScale * SpringPressScale;
		}

		AudioManager.Instance.PlaySpring();

		player.Bounce(SpringBoostVelocity);

		// Reset scale after a short delay to simulate spring recovery
		if (springRenderer != null)
		{
			// schedule reset using coroutine on the platform GameObject
			StartCoroutine(ResetSpringCoroutine());
		}
    }

	private System.Collections.IEnumerator ResetSpringCoroutine()
	{
		yield return new WaitForSeconds(0.12f);
		if (springRenderer != null)
		{
			springRenderer.transform.localScale = springBaseScale;
		}
	}
}
