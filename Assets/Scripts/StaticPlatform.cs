using UnityEngine;

public sealed class StaticPlatform : PlatformBase
{
    public override void HandleBounce(PlayerController player)
    {
        player.Bounce();
    }
}
