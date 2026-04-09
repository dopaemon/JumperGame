using UnityEngine;

public sealed class PlayerProjectile : MonoBehaviour
{
	private const float Speed = 18f;
	private const float Lifetime = 1.8f;
	private Vector3 direction;
	private SpriteRenderer renderer;
	private Sprite projectileSprite;
	private float despawnAtTime;

	public void Init(Sprite sprite)
	{
		projectileSprite = sprite;

		if (renderer != null)
			renderer.sprite = projectileSprite;
	}

	private void Awake()
	{
		renderer = gameObject.AddComponent<SpriteRenderer>();
		renderer.sortingOrder = 4;

		transform.localScale = Vector3.one * 0.2f;

		BoxCollider2D triggerCollider = gameObject.AddComponent<BoxCollider2D>();
		triggerCollider.isTrigger = true;
		triggerCollider.size = Vector2.one;
	}

	private void OnEnable()
	{
		despawnAtTime = Time.time + Lifetime;
		direction = Vector3.up;
	}

	private void Update()
	{
		transform.position += direction * Speed * Time.deltaTime;

		if (Time.time >= despawnAtTime)
		{
			gameObject.SetActive(false);
		}
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		EnemyHazard enemy = collision.GetComponent<EnemyHazard>();
		if (enemy == null)
		{
			return;
		}

		enemy.gameObject.SetActive(false);
		gameObject.SetActive(false);
	}
}