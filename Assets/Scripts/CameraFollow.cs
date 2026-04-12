using UnityEngine;

public sealed class CameraFollow : MonoBehaviour
{
	[SerializeField] private float verticalOffset = 1.75f;

	private Transform target;

	public void Initialize(Transform followTarget)
	{
		target = followTarget;
	}

	private void LateUpdate()
	{
		if (target == null)
		{
			return;
		}

		Vector3 position = transform.position;
		float desiredY = target.position.y + verticalOffset;
		if (desiredY > position.y)
		{
			position.y = desiredY;
			transform.position = position;
		}
	}
}