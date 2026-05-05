using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LeaderboardUI : MonoBehaviour
{
	public TMP_Text historyText;

	private void OnEnable()
	{
		RefreshLeaderboard();
	}

	public void RefreshLeaderboard()
	{
		List<int> scores = HistoryStore.GetScores();

		historyText.text = "";

		for (int i = 0; i < scores.Count; i++)
		{
			string rank = "";

			if (i == 0)
			{
				rank = "<size=250%><voffset=0.1em><sprite=0></voffset></size>";
				historyText.text += $"<indent=5px>{rank}<space=2px>SCORE : {scores[i]}\n\n";
			}
			else if (i == 1)
			{
				rank = "<size=250%><voffset=0.1em><sprite=1></voffset></size>";
				historyText.text += $"<indent=5px>{rank}<space=2px>SCORE : {scores[i]}\n\n";
			}
			else if (i == 2)
			{
				rank = "<size=250%><voffset=0.1em><sprite=2></voffset></size>";
				historyText.text += $"<indent=5px>{rank}<space=2px>SCORE : {scores[i]}\n\n";
			}
			else
			{
				rank = (i + 1).ToString();

				historyText.text += $"<indent=22px>{rank}   SCORE : {scores[i]}\n";
			}
		}
	}
}