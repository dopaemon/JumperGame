using UnityEngine;

public sealed class EnemyHazard : MonoBehaviour
{
    [SerializeField] private float moveAmplitude = 0.55f;
    [SerializeField] private float moveSpeed = 0.9f;

    private float anchorX;
    private float phaseOffset;
    private SpriteRenderer spriteRenderer;
	private Rigidbody2D rb;

	private void Awake()
    {
        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = RuntimeSpriteFactory.WhiteSprite;
        spriteRenderer.color = new Color(0.19f, 0.18f, 0.24f);
        spriteRenderer.sortingOrder = 2;

        transform.localScale = new Vector3(0.9f, 0.6f, 1f);

        BoxCollider2D triggerCollider = gameObject.AddComponent<BoxCollider2D>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = Vector2.one;

		rb = gameObject.AddComponent<Rigidbody2D>();
		rb.gravityScale = 0f;
		rb.bodyType = RigidbodyType2D.Kinematic;
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
            // Reset local scale baseline so sprite displays at expected size; actual size will be handled by spawner/platform sizing
            transform.localScale = new Vector3(0.9f, 0.6f, 1f);
        }
    }

    private void OnEnable()
    {
        anchorX = transform.position.x;
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        float x = anchorX + Mathf.Sin((Time.time + phaseOffset) * moveSpeed) * moveAmplitude;
        transform.position = new Vector3(x, transform.position.y, transform.position.z);
    }
}
