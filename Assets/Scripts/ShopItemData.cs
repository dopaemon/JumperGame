using UnityEngine;

[CreateAssetMenu(menuName = "Game/Shop Item")]
public class ShopItemData : ScriptableObject
{
	public string itemName;

	public Sprite icon;

	[TextArea]
	public string description;

	public int price;

	public BuffType buffType;

	public float value;

	[Header("Buff Settings")]

	public bool isTimedBuff;

	public float duration;
}

public enum BuffType
{
	RocketBoost,
	HighJump,
	DoubleCoin
}