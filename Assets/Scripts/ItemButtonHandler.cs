using UnityEngine;

public class ItemButtonHandler : MonoBehaviour
{
	public void UseRocket()
	{
		PlayerInventory.Instance.UseItem(BuffType.RocketBoost);
	}

	public void UseHighJump()
	{
		PlayerInventory.Instance.UseItem(BuffType.HighJump);
	}

	public void UseDoubleCoin()
	{
		PlayerInventory.Instance.UseItem(BuffType.DoubleCoin);
	}
}