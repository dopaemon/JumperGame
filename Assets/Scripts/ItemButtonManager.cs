using TMPro;
using UnityEngine;

public class ItemButtonManager : MonoBehaviour
{
	[Header("Buttons")]
	public GameObject rocketButton;
	public GameObject highJumpButton;
	public GameObject doubleCoinButton;

	[Header("Count Text")]
	public TMP_Text rocketCountText;
	public TMP_Text highJumpCountText;
	public TMP_Text doubleCoinCountText;

	private void Update()
	{
		UpdateButtons();
	}

	private void UpdateButtons()
	{
		UpdateButton(
			BuffType.RocketBoost,
			rocketButton,
			rocketCountText
		);

		UpdateButton(
			BuffType.HighJump,
			highJumpButton,
			highJumpCountText
		);

		UpdateButton(
			BuffType.DoubleCoin,
			doubleCoinButton,
			doubleCoinCountText
		);
	}

	private void UpdateButton(
		BuffType type,
		GameObject button,
		TMP_Text countText)
	{
		int count =
			PlayerInventory.Instance.GetItemCount(type);

		bool hasItem = count > 0;

		button.SetActive(hasItem);

		if (hasItem)
		{
			countText.text = count.ToString();
		}
	}
}