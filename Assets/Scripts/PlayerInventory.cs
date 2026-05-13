using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
	public static PlayerInventory Instance;

	private Dictionary<BuffType, int> itemCounts =
		new Dictionary<BuffType, int>();

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);
	}
	public void AddItem(ShopItemData item)
	{
		if (item == null) return;

		BuffType type = item.buffType;

		if (!itemCounts.ContainsKey(type))
			itemCounts[type] = 0;

		itemCounts[type]++;
	}

	public int GetItemCount(BuffType type)
	{
		if (itemCounts.TryGetValue(type, out int count))
			return count;

		return 0;
	}

	public bool HasItem(BuffType type)
	{
		return GetItemCount(type) > 0;
	}

	public bool UseItem(BuffType type)
	{
		if (!itemCounts.ContainsKey(type))
			return false;

		if (itemCounts[type] <= 0)
			return false;

		itemCounts[type]--;

		if (itemCounts[type] <= 0)
			itemCounts.Remove(type);

		ActivateBuff(type);
		return true;
	}

	public void Clear()
	{
		itemCounts.Clear();
	}

	private void ActivateBuff(BuffType type)
	{
		PlayerController player = FindAnyObjectByType<PlayerController>();

		if (player == null)
			return;

		ShopItemData item = GetItemData(type);
		float duration = item != null ? item.duration : 5f;

		switch (type)
		{
			case BuffType.RocketBoost:
				player.ActivateFlightBoost(duration);
				break;

			case BuffType.HighJump:
				player.ActivateHighJump(1.5f, duration);
				break;

			case BuffType.DoubleCoin:
				Debug.Log("Double coin activated");
				break;
		}
	}

	private ShopItemData GetItemData(BuffType type)
	{
		ShopItemData[] items =
			Resources.FindObjectsOfTypeAll<ShopItemData>();

		foreach (var item in items)
		{
			if (item.buffType == type)
				return item;
		}

		return null;
	}
}