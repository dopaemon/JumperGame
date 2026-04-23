using UnityEngine;

public class Coin : MonoBehaviour
{
	private Vector3 startLocalPos;

	[SerializeField] private int value = 1;

	private void Start()
	{
		startLocalPos = transform.localPosition;
	}

	private void Update()
	{
		float y = Mathf.Sin(Time.time * 3f) * 0.08f;

		transform.localPosition =
			startLocalPos + new Vector3(0f, y, 0f);
	}

	private void OnTriggerEnter2D(Collider2D collision)
	{
		if (collision.GetComponent<PlayerController>() == null)
			return;

		Debug.Log("Coin Picked");

		int amount = value;

		if (PlayerInventory.Instance != null &&
			PlayerInventory.Instance.HasItem(BuffType.DoubleCoin))
		{
			amount *= 2;
		}

		CoinManager.Instance.AddCoin(amount);

		AudioManager.Instance.PlayCoinCollect();

		Destroy(gameObject);
	}
}