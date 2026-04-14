using UnityEngine;

public abstract class PlatformBase : MonoBehaviour
{
    private const float ColliderTopStripHeight = 0.18f;
    private const float ColliderTopStripOffset = 0.42f;

    private SpriteRenderer platformRenderer;

    protected BoxCollider2D PlatformCollider { get; private set; }
    protected SpriteRenderer PlatformRenderer => platformRenderer;

	public virtual void Initialize(Sprite sprite, Vector2 size)
	{

        // Create or reuse a Visual child to render the platform sprite so we can size the sprite
        // independently of the platform GameObject's transform (which controls collider placement).
        Transform visualT = transform.Find("Visual");
        GameObject visualObj = visualT != null ? visualT.gameObject : new GameObject("Visual");
        visualObj.transform.SetParent(transform, false);
        visualObj.transform.localPosition = Vector3.zero;
        visualObj.transform.localRotation = Quaternion.identity;

        SpriteRenderer visRenderer = visualObj.GetComponent<SpriteRenderer>();
        if (visRenderer == null)
        {
            visRenderer = visualObj.AddComponent<SpriteRenderer>();
        }

        visRenderer.sortingOrder = 1;
        visRenderer.sprite = sprite;
        visRenderer.color = Color.white;

        // Keep platform root transform at scale 1; we'll size the visual to match requested world size
        transform.localScale = Vector3.one;

        // Compute scale for visual so sprite occupies desired world size
        if (sprite != null)
        {
            Vector2 spriteSize = sprite.bounds.size;
            if (spriteSize.x > 0.0001f && spriteSize.y > 0.0001f)
            {
                visualObj.transform.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);
            }
            else
            {
                visualObj.transform.localScale = new Vector3(size.x, size.y, 1f);
            }
        }
        else
        {
            visualObj.transform.localScale = new Vector3(size.x, size.y, 1f);
        }

        // Use the visual renderer as the platform renderer reference
        platformRenderer = visRenderer;

        // Configure collider in world units relative to requested size
        PlatformCollider = GetComponent<BoxCollider2D>();
        if (PlatformCollider == null)
        {
            PlatformCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        PlatformCollider.size = new Vector2(size.x, ColliderTopStripHeight * size.y);
        PlatformCollider.offset = new Vector2(0f, ColliderTopStripOffset * size.y);
        PlatformCollider.isTrigger = true;
        PlatformCollider.enabled = true;

        ResetState();
    }

    public bool CanBounce(PlayerController player, float topTolerance)
    {
        if (player.VerticalVelocity > 0f)
        {
            return false;
        }

        return true;
    }

    public float TopY => PlatformCollider.bounds.max.y;

    protected virtual void ResetState()
    {
    }

    public virtual void Deactivate()
    {
        gameObject.SetActive(false);
    }

    public abstract void HandleBounce(PlayerController player);
}
