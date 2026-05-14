using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemUI : MonoBehaviour
{
	public Image icon;
	public TMP_Text itemName;
	public TMP_Text priceText;
	public Button buyButton;

	private ShopItemData data;

	public void Setup(ShopItemData itemData)
	{
		data = itemData;

		icon.sprite = data.icon;
		itemName.text = data.itemName;
		priceText.text = data.price.ToString();

		buyButton.onClick.RemoveAllListeners();
		buyButton.onClick.AddListener(BuyItem);
	}

	private void BuyItem()
	{
		Debug.Log("DATA = " + data);
		Debug.Log("COIN = " + CoinManager.Instance);
		Debug.Log("INVENTORY = " + PlayerInventory.Instance);

		if (data == null)
		{
			Debug.LogError("Data NULL");
			return;
		}

		if (CoinManager.Instance == null)
		{
			Debug.LogError("CoinManager NULL");
			return;
		}

		if (PlayerInventory.Instance == null)
		{
			Debug.LogError("Inventory NULL");
			return;
		}

		bool success =
			CoinManager.Instance.SpendCoins(data.price);

		if (!success)
		{
			Debug.Log("Not enough coins");
			return;
		}

		PlayerInventory.Instance.AddItem(data);

		Debug.Log("Bought: " + data.itemName);
	}
}