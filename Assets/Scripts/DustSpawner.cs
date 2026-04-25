using UnityEngine;

public class DustSpawner : MonoBehaviour
{
	public GameObject dustPrefab;
	private PlayerController player;

	void Start()
	{
		player = FindFirstObjectByType<PlayerController>();
	}

	private void OnCollisionEnter2D(Collision2D col)
	{
		if (!col.collider.CompareTag("Platform")) return;
		if (player == null) return;

		ContactPoint2D contact = col.GetContact(0);

		if (contact.normal.y < 0.5f)
			return;

		Vector3 hitPos = contact.point;
		Instantiate(dustPrefab, hitPos, Quaternion.identity);
	}
}