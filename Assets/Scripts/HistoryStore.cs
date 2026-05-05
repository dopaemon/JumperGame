using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ScoreList
{
	public List<int> scores = new List<int>();
}

public static class HistoryStore
{
	private const string PrefsKey = "LeaderboardV1";
	private const int MaxScores = 20;

	public static void AddScore(int score)
	{
		ScoreList list = LoadList();

		list.scores.Add(score);

		list.scores.Sort((a, b) => b.CompareTo(a));

		if (list.scores.Count > MaxScores)
		{
			list.scores.RemoveRange(MaxScores, list.scores.Count - MaxScores);
		}

		SaveList(list);
	}

	public static List<int> GetScores()
	{
		return LoadList().scores;
	}

	public static void Clear()
	{
		PlayerPrefs.DeleteKey(PrefsKey);
		PlayerPrefs.Save();
	}

	private static ScoreList LoadList()
	{
		string json = PlayerPrefs.GetString(PrefsKey, "");

		if (string.IsNullOrEmpty(json))
			return new ScoreList();

		try
		{
			return JsonUtility.FromJson<ScoreList>(json) ?? new ScoreList();
		}
		catch
		{
			return new ScoreList();
		}
	}

	private static void SaveList(ScoreList list)
	{
		string json = JsonUtility.ToJson(list);

		PlayerPrefs.SetString(PrefsKey, json);
		PlayerPrefs.Save();
	}
}