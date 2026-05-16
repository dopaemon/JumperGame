using UnityEngine;

public class ShopManager : MonoBehaviour
{
	public ShopItemData[] items;

	public ShopItemUI itemPrefab;

	public Transform container;

	[SerializeField]
	private GameObject shopPanel;

	private void Start()
	{
		shopPanel.SetActive(false);

		foreach (var item in items)
		{
			ShopItemUI ui =
				Instantiate(itemPrefab, container);

			ui.Setup(item);
		}
	}

	public void OpenShop()
	{
		shopPanel.SetActive(true);
		CoinManager.Instance.RefreshCoinText();
		Time.timeScale = 0f;
	}

	public void CloseShop()
	{
		shopPanel.SetActive(false);

		Time.timeScale = 1f;
	}
}