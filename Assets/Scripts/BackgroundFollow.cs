using UnityEngine;

public class BackgroundFollow : MonoBehaviour
{
	private Transform cam;

	private void Start()
	{
		cam = Camera.main.transform;
	}

	private void LateUpdate()
	{
		transform.position = new Vector3(
			cam.position.x,
			cam.position.y,
			0f
		);
	}
}
