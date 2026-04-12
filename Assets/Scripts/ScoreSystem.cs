using UnityEngine;
using TMPro;

public sealed class ScoreSystem : MonoBehaviour
{
	[SerializeField] private TMP_Text scoreText;

	private int currentScore;

	public int CurrentScore => currentScore;

	public void RegisterPlayer(PlayerController player)
	{
		player.HighestPointChanged += HandleHighestPointChanged;
		HandleHighestPointChanged(player.HighestY);
	}

	private void HandleHighestPointChanged(float highestY)
	{
		currentScore = Mathf.Max(currentScore, Mathf.FloorToInt(highestY));
		Refresh();
	}

	private void Refresh()
	{
		if (scoreText != null)
		{
			scoreText.text = "Score: " + currentScore;
		}
	}
}
