#nullable enable
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.EventSystems;
#endif

public sealed class GameManager : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private PlayerController player;
	[SerializeField] private Camera gameplayCamera;

	[Header("UI")]
	[SerializeField] private CanvasGroup gameOverGroup;
	[SerializeField] private Text gameOverText;

	[SerializeField] private CanvasGroup pauseGroup;
	[SerializeField] private Text pauseText;

	[Header("Settings")]
	[SerializeField] private float deathOffset = 0.35f;

	private bool isGameOver;
	private bool isPaused;
	private float startTime;

	private void Start()
	{
		Time.timeScale = 1f;
		startTime = Time.time;
		SetGameOverVisible(false);
		SetPauseVisible(false);
        // Wire up a UI button named "ShootButton" (or variants) to fire player projectiles
		TryWireShootButton();
	}

	private void TryWireShootButton()
	{
		if (player == null) return;
		GameObject? btnObj = GameObject.Find("ShootButton");
		if (btnObj == null) btnObj = GameObject.Find("shootbutton");
		if (btnObj == null) btnObj = GameObject.Find("shootButton");
		if (btnObj == null) return;

		Button? btn = btnObj.GetComponent<Button>();
		if (btn == null) return;

        // remove previous listeners then add our handler
		btn.onClick.RemoveAllListeners();
		btn.onClick.AddListener(() => OnShootButtonPressed());
	}

    // Public handler that can be wired from UI to make the player shoot
	public void OnShootButtonPressed()
	{
		if (player == null) return;
		player.TryShoot();
	}

	private void Update()
	{
		if (Time.time - startTime < 0.5f)
		{
			return;
		}

		if (player == null || gameplayCamera == null)
		{
			return;
		}

		if (ShouldTogglePause())
		{
			TogglePause();
		}

		if (isPaused)
		{
			return;
		}

		if (!isGameOver)
		{
			float cameraBottom = gameplayCamera.transform.position.y - gameplayCamera.orthographicSize;

			if (player.IsDead || player.transform.position.y < cameraBottom - deathOffset)
			{
				TriggerGameOver();
			}
			return;
		}

		if (isGameOver && ShouldRestart())
		{
			Restart();
		}

	}

	private void TriggerGameOver()
	{
		isGameOver = true;
		player.SetControlsEnabled(false);

		SetGameOverVisible(true);

		if (gameOverText != null)
		{
			gameOverText.text = "Game Over\nTap / Click / Space to Restart";
		}

		// Save play history: score and selected level
		try
		{
			var scoreSys = FindObjectOfType<ScoreSystem>();
			int score = scoreSys != null ? scoreSys.CurrentScore : 0;
			string levelName = LevelSelectionStore.SelectedLevel != null ? LevelSelectionStore.SelectedLevel.name : "Default";
			HistoryStore.AddScore(score);
		}
		catch
		{
		
		}
	}

	public void TogglePause()
	{
		AudioManager.Instance.PlayUIClick();
		if (isGameOver)
		{
			return;
		}

		isPaused = !isPaused;
		Time.timeScale = isPaused ? 0f : 1f;
		SetPauseVisible(isPaused);
	}

	public void Resume()
	{
		AudioManager.Instance.PlayUIClick();
		isPaused = false;
		Time.timeScale = 1f;
		SetPauseVisible(false);
	}

	public void Restart()
	{
		AudioManager.Instance.PlayUIClick();
		Time.timeScale = 1f;
		SceneManager.LoadScene(SceneManager.GetActiveScene().name);
	}

	private void SetGameOverVisible(bool visible)
	{
		if (gameOverGroup == null) return;

		gameOverGroup.alpha = visible ? 1f : 0f;
		gameOverGroup.interactable = visible;
		gameOverGroup.blocksRaycasts = visible;
	}

	private void SetPauseVisible(bool visible)
	{
		if (pauseGroup == null) return;

		pauseGroup.alpha = visible ? 1f : 0f;
		pauseGroup.interactable = visible;
		pauseGroup.blocksRaycasts = visible;

		if (visible && pauseText != null)
		{
			pauseText.text = "Paused";
		}
	}

	private static bool ShouldTogglePause()
	{
#if ENABLE_INPUT_SYSTEM
		if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
			return true;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
		return false;
#endif
	}

	private static bool ShouldRestart()
	{
		if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
			return false;
#if ENABLE_INPUT_SYSTEM
		if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
			return true;

		if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
			return true;

		if (Touchscreen.current != null)
		{
			foreach (TouchControl touch in Touchscreen.current.touches)
			{
				if (touch.press.wasPressedThisFrame)
					return true;
			}
		}
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || Input.touchCount > 0;
#else
		return false;
#endif
	}

	public void BackToMenu()
	{
		AudioManager.Instance.PlayUIClick();
		Time.timeScale = 1f;
		SceneManager.LoadScene("MenuScene");
	}
}
