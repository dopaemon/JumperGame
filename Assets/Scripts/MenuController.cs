using TMPro;
using UnityEngine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class MenuController : MonoBehaviour
{
	public GameObject menuPanel;
	public GameObject aboutPanel;
	public GameObject levelCard;
	public GameObject historyPanel;
	public Image playerPreview;
	public TMP_Text levelName;

	[Header("Sliding Background")]
	[SerializeField] private RectTransform currentBackground;
	[SerializeField] private RectTransform nextBackground;

	[SerializeField] private float slideDuration = 0.35f;

	private bool isSliding;
	[SerializeField] private Canvas canvas;

	[Header("Menu Visuals")]
	[SerializeField] private Sprite fixedMenuBackground;

	[SerializeField] private RectTransform frameOverlay;
	[SerializeField] private RectTransform frameIn;
	[SerializeField] private RectTransform frameOut;

	private int currentIndex = 0;

	[Header("Level Options")]
	// Assign LevelData assets (3 items) in the inspector for your level buttons
	public LevelData[] levelOptions;

	private void Start()
	{
		if (!SceneManager.GetSceneByName("AudioScene").isLoaded)
		{
			SceneManager.LoadSceneAsync("AudioScene", LoadSceneMode.Additive);
		}

		ResetMenu();

		UpdateLevelCard();
		ApplySelectedBackground();

		currentBackground.localScale = Vector3.one;
		nextBackground.localScale = Vector3.one;
	}

	private void OnEnable()
	{
		// Apply any previously selected level background when menu becomes active
		ApplySelectedBackground();
	}

	// Called by the Play button. Loads the GameScene using the currently selected level (if any).
	public void PlayGame()
	{
		AudioManager.Instance.PlayUIClick();
		// If no level selected, default to the first option if available
		if (LevelSelectionStore.SelectedLevel == null && levelOptions != null && levelOptions.Length > 0)
		{
			LevelSelectionStore.SelectedLevel = levelOptions[0];
		}

		SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
	}

	public void ResetMenu()
	{
		menuPanel.SetActive(true);
		aboutPanel.SetActive(false);
		historyPanel.SetActive(false);
	}

	public void OpenAbout()
	{
		AudioManager.Instance.PlayUIClick();
		menuPanel.SetActive(false);
		aboutPanel.SetActive(true);
		historyPanel.SetActive(false);
	}

	public void OpenHistory()
	{
		AudioManager.Instance.PlayUIClick();
		menuPanel.SetActive(false);
		aboutPanel.SetActive(false);
		historyPanel.SetActive(true);
	}

	public void BackToMenu()
	{
		AudioManager.Instance.PlayUIClick();
		ResetMenu();
	}

	public void SelectLevelIndex(int index)
	{
		if (levelOptions == null || index < 0 || index >= levelOptions.Length)
		{
			LevelSelectionStore.SelectedLevel = null;
			return;
		}

		currentIndex = index;

		UpdateLevelCard();

		//ResetMenu();

		ApplySelectedBackground();
	}

	private void ApplySelectedBackground()
	{
        // Keep the menu background fixed. If a fixedMenuBackground sprite is assigned, use it.
		GameObject bgObj = GameObject.Find("Background");
		if (bgObj != null)
		{
			SpriteRenderer sr = bgObj.GetComponent<SpriteRenderer>();
			if (sr != null)
			{
				sr.sortingOrder = -10;
				if (fixedMenuBackground != null)
				{
					sr.sprite = fixedMenuBackground;
				}
			}
		}
	}
	private void UpdateLevelCard()
	{
		if (levelOptions == null || levelOptions.Length == 0)
			return;

		LevelData data = levelOptions[currentIndex];

		if (currentBackground != null)
		{
			Image img = currentBackground.GetComponent<Image>();
			img.sprite = data.background;
		}

		if (playerPreview != null)
			playerPreview.sprite = data.playerSprite;

		if (levelName != null)
			levelName.text = data.name;

		LevelSelectionStore.SelectedLevel = data;
	}

	private IEnumerator SlideBackground(Sprite newSprite, bool moveRight, System.Action onComplete)
	{
		isSliding = true;

		float width = ((RectTransform)canvas.transform).rect.width;

		Vector2 currentStart = Vector2.zero;

		Vector2 currentEnd = moveRight
			? new Vector2(-width, 0f)
			: new Vector2(width, 0f);

		Vector2 nextStart = moveRight
			? new Vector2(width, 0f)
			: new Vector2(-width, 0f);

		nextBackground.anchoredPosition = nextStart;

		Image nextImage = nextBackground.GetComponent<Image>();
		nextImage.sprite = newSprite;

		float time = 0f;

		while (time < slideDuration)
		{
			time += Time.deltaTime;
			float t = time / slideDuration;

			currentBackground.anchoredPosition =
				Vector2.Lerp(currentStart, currentEnd, t);

			nextBackground.anchoredPosition =
				Vector2.Lerp(nextStart, Vector2.zero, t);

			frameOut.anchoredPosition = currentBackground.anchoredPosition;
			frameIn.anchoredPosition = nextBackground.anchoredPosition;

			yield return null;
		}

		currentBackground.GetComponent<Image>().sprite = newSprite;

		currentBackground.anchoredPosition = Vector2.zero;
		nextBackground.anchoredPosition = nextStart;

		frameOut.anchoredPosition = Vector2.zero;
		frameIn.anchoredPosition = Vector2.zero;

		isSliding = false;

		onComplete?.Invoke();
	}

	public void NextLevel()
	{
		if (isSliding) return;

		AudioManager.Instance.PlayUIClick();

		int nextIndex = (currentIndex + 1) % levelOptions.Length;
		LevelData data = levelOptions[nextIndex];

		currentIndex = nextIndex;

		StartCoroutine(
			SlideBackground(data.background, true, () =>
			{
				UpdateLevelCard();
			})
		);
	}

	public void PreviousLevel()
	{
		if (isSliding) return;

		AudioManager.Instance.PlayUIClick();

		int nextIndex = currentIndex - 1;
		if (nextIndex < 0)
			nextIndex = levelOptions.Length - 1;

		LevelData data = levelOptions[nextIndex];

		currentIndex = nextIndex;

		StartCoroutine(
			SlideBackground(data.background, false, () =>
			{
				UpdateLevelCard();
			})
		);
	}
}
