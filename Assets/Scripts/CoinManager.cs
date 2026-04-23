using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CoinManager : MonoBehaviour
{
	public static CoinManager Instance;

	private TMP_Text coinText;

	private int coins;

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

	private void OnEnable()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	private void Start()
	{
		coins = PlayerPrefs.GetInt("Coins", 0);

		FindCoinText();
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		FindCoinText();

		UpdateUI();
	}

	private void FindCoinText()
	{
		GameObject obj =
			GameObject.FindWithTag("CoinText");

		if (obj != null)
		{
			coinText =
				obj.GetComponent<TMP_Text>();
		}
	}

	public void AddCoin(int amount)
	{
		coins += amount;

		PlayerPrefs.SetInt("Coins", coins);

		UpdateUI();
	}

	public bool SpendCoins(int amount)
	{
		if (coins < amount)
			return false;

		coins -= amount;

		PlayerPrefs.SetInt("Coins", coins);

		UpdateUI();

		return true;
	}

	private void UpdateUI()
	{
		if (coinText != null)
		{
			coinText.text = coins.ToString();
		}
	}

	public void RefreshCoinText()
	{
		FindCoinText();

		UpdateUI();
	}
}