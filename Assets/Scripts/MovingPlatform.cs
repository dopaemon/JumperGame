using UnityEngine;

public sealed class MovingPlatform : PlatformBase
{
    private float anchorX;
    private float moveAmplitude;
    private float moveSpeed;
    private float phaseOffset;

    public void Configure(float amplitude, float speed, float phase)
    {
        anchorX = transform.position.x;
        moveAmplitude = amplitude;
        moveSpeed = speed;
        phaseOffset = phase;
    }

    private void Update()
    {
        float x = anchorX + Mathf.Sin((Time.time + phaseOffset) * moveSpeed) * moveAmplitude;
        transform.position = new Vector3(x, transform.position.y, transform.position.z);
    }

    public override void HandleBounce(PlayerController player)
    {
        player.Bounce();
    }
}
