using UnityEngine;

public sealed class BreakablePlatform : PlatformBase
{
    private const float DisableDelay = 0.35f;

    private bool broken;
    private float disableAtTime;
    private bool pendingDeactivate;

	public override void Initialize(Sprite sprite, Vector2 size)
	{
		base.Initialize(sprite, size);
	}

	private void Update()
    {
        if (pendingDeactivate && Time.time >= disableAtTime)
        {
            pendingDeactivate = false;
            Deactivate();
        }
    }

	protected override void ResetState()
	{
		broken = false;
		pendingDeactivate = false;
		disableAtTime = 0f;

		if (PlatformCollider != null)
		{
			PlatformCollider.enabled = true;
		}

		if (PlatformRenderer != null)
		{
			Color c = PlatformRenderer.color;
			c.a = 1f;
			PlatformRenderer.color = c;
		}
	}


	public override void HandleBounce(PlayerController player)
    {
        if (broken)
        {
            return;
        }

        player.Bounce();
        broken = true;
        PlatformCollider.enabled = false;

        if (PlatformRenderer != null)
        {
            Color color = PlatformRenderer.color;
            PlatformRenderer.color = new Color(color.r, color.g, color.b, 0.3f);
        }

        pendingDeactivate = true;
        disableAtTime = Time.time + DisableDelay;
    }
}
