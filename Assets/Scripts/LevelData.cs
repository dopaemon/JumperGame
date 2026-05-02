using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/Level Data")]
public class LevelData : ScriptableObject
{
	public Sprite staticPlatform;
	public Sprite movingPlatform;
	public Sprite breakablePlatform;
	public Sprite springPlatform;

	public Sprite enemySprite;
	public Sprite playerSprite;
	public Sprite background;
    public Sprite springTop;

	// Sprite for the flight / rocket power-up
	public Sprite powerUpSprite;

}
