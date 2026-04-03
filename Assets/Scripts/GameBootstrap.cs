using UnityEngine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public sealed class GameBootstrap : MonoBehaviour
{
	private const int PortraitWidth = 1080;
	private const int PortraitHeight = 1920;
	private GameObject startOverlayObject;
	private PlayerController startOverlayPlayer;
	private bool worldInitialized;

	private void Awake()
	{
		ConfigureDisplay();
		ConfigureFramePacing();
		SetupCamera();
		EnableMobileSensors();
	}

	private void Start()
	{
		var myScene = this.gameObject.scene;

		if (myScene.name != "GameScene")
			return;

		if (SceneManager.sceneCount > 1 &&
			SceneManager.GetActiveScene().name != "GameScene")
		{
			Debug.Log("GameBootstrap: waiting for GameScene to become active.");

			foreach (GameObject root in myScene.GetRootGameObjects())
			{
				if (root == this.gameObject)
					continue;

				root.SetActive(false);
			}

			SceneManager.activeSceneChanged += OnActiveSceneChanged;
			return;
		}

		StartCoroutine(InitDelayed());
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (this == null || !this) return;
		if (scene.name != "GameScene") return;

		if (SceneManager.sceneCount > 1 &&
			SceneManager.GetActiveScene().name != "GameScene")
		{
			return;
		}

		StartCoroutine(InitDelayed());
	}

	private void OnEnable()
	{
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void OnDisable()
	{
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	private void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene previous, UnityEngine.SceneManagement.Scene next)
	{
        if (this == null || this.gameObject == null) return;
		if (next.name != "GameScene") return;

		var gameScene = SceneManager.GetSceneByName("GameScene");
		if (gameScene.IsValid())
		{
			foreach (GameObject root in gameScene.GetRootGameObjects())
			{
				if (root == this.gameObject) continue;
				root.SetActive(true);
			}
		}

		SceneManager.activeSceneChanged -= OnActiveSceneChanged;
		StartCoroutine(InitDelayed());
	}

	private void OnDestroy()
	{
		SceneManager.activeSceneChanged -= OnActiveSceneChanged;
	}

	private System.Collections.IEnumerator InitDelayed()
	{
		yield return null;

		SetupWorld();
	}


	private void SetupCamera()
	{
		Screen.orientation = ScreenOrientation.Portrait;

		Camera cam = Camera.main;

		if (cam == null)
		{
			GameObject camObj = new GameObject("Main Camera");
			camObj.tag = "MainCamera";

			cam = camObj.AddComponent<Camera>();
			if (FindFirstObjectByType<AudioListener>() == null)
			{
				camObj.AddComponent<AudioListener>();
			}
			cam.transform.position = new Vector3(0, 0, -10);
		}

		cam.orthographic = true;
		cam.orthographicSize = 7f;

		// Camera Follow
		CameraFollow follow = cam.GetComponent<CameraFollow>();
		if (follow == null)
			follow = cam.gameObject.AddComponent<CameraFollow>();
	}


	private void SetupWorld()
	{
		if (worldInitialized)
			return;

		worldInitialized = true;

		Camera cam = Camera.main;


		PlayerController player = FindFirstObjectByType<PlayerController>();

        if (player == null)
		{
			Debug.LogError("❌ Không có Player trong scene!");
			return;
		}

		// Lower the player's starting Y so the player does not appear too high at game start
		// Align roughly with the platform spawner's starting platform (which is at y = -3.4)
		if (player.transform != null)
		{
			player.transform.position = new Vector3(0f, -2.8f, 0f);
		}


		GameObject projectileRoot = GameObject.Find("Projectile Root");
		if (projectileRoot == null)
		{
         projectileRoot = new GameObject("Projectile Root");

			UnityEngine.SceneManagement.Scene gameScene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("GameScene");
			if (gameScene.IsValid()) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(projectileRoot, gameScene);
		}


		bool preferTilt = Application.isMobilePlatform && SystemInfo.supportsAccelerometer;
		IHorizontalInputSource input = new CompositeHorizontalInputSource(preferTilt, 2.2f, 0.06f);

		player.Initialize(input, cam, projectileRoot.transform);


		CameraFollow follow = cam.GetComponent<CameraFollow>();
		follow.Initialize(player.transform);


		PlatformSpawner spawner = FindFirstObjectByType<PlatformSpawner>();

		if (spawner == null)
		{
			GameObject obj = new GameObject("Platform Spawner");
			spawner = obj.AddComponent<PlatformSpawner>();
			UnityEngine.SceneManagement.Scene gs = UnityEngine.SceneManagement.SceneManager.GetSceneByName("GameScene");
			if (gs.IsValid()) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(obj, gs);
		}

        spawner.Initialize(cam);

		// If a level was selected in the menu, assign it to the spawner before seeding platforms
		if (LevelSelectionStore.SelectedLevel != null)
		{
			spawner.SetLevelData(LevelSelectionStore.SelectedLevel);
		}

		spawner.SeedInitialPlatforms();

		// If LevelData provides a player sprite, assign it to the player so the player image matches level art
		if (LevelSelectionStore.SelectedLevel != null && LevelSelectionStore.SelectedLevel.playerSprite != null)
		{
			player.SetSprite(LevelSelectionStore.SelectedLevel.playerSprite);
		}

		// Ensure any Background GameObject(s) render behind gameplay (prevent a fixed background from covering level sprites)
		GameObject bgObj = GameObject.Find("Background");
		if (bgObj != null)
		{
			var sr = bgObj.GetComponent<SpriteRenderer>();
			if (sr != null)
			{
				sr.sortingOrder = -10;
				if (LevelSelectionStore.SelectedLevel != null && LevelSelectionStore.SelectedLevel.background != null)
				{
					sr.sprite = LevelSelectionStore.SelectedLevel.background;
				}
			}
			// ensure it's centered on the camera and placed behind (z can be positive, sortingOrder controls render order)
			bgObj.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 5f);
		}

		ScoreSystem score = FindFirstObjectByType<ScoreSystem>();

		if (score != null)
		{
			score.RegisterPlayer(player);
		}
		else
		{
			Debug.LogWarning("⚠️ Không có ScoreSystem trong scene");
		}

		ShowStartOverlay(player);
	}

    private void ShowStartOverlay(PlayerController player)
	{
        if (player == null) return;

		if (startOverlayObject != null)
			return;

		startOverlayPlayer = player;

		if (FindFirstObjectByType<EventSystem>() == null)
		{
			GameObject es = new GameObject("EventSystem");
			es.AddComponent<EventSystem>();
			es.AddComponent<StandaloneInputModule>();
		}

		GameObject overlay = new GameObject("StartOverlay");
		Canvas canvas = overlay.AddComponent<Canvas>();
		canvas.renderMode = RenderMode.ScreenSpaceOverlay;
		overlay.AddComponent<CanvasScaler>();
		overlay.AddComponent<GraphicRaycaster>();

		GameObject panel = new GameObject("Panel");
		panel.transform.SetParent(overlay.transform, false);
		RectTransform panelRect = panel.AddComponent<RectTransform>();
		panelRect.anchorMin = Vector2.zero;
		panelRect.anchorMax = Vector2.one;
		panelRect.offsetMin = Vector2.zero;
		panelRect.offsetMax = Vector2.zero;

		Image img = panel.AddComponent<Image>();
		img.color = new Color(0f, 0f, 0f, 0.35f);

		Button btn = panel.AddComponent<Button>();
		btn.targetGraphic = img;

		GameObject labelObj = new GameObject("StartLabel");
		labelObj.transform.SetParent(panel.transform, false);
		RectTransform labelRect = labelObj.AddComponent<RectTransform>();
		labelRect.anchorMin = new Vector2(0.5f, 0.5f);
		labelRect.anchorMax = new Vector2(0.5f, 0.5f);
		labelRect.sizeDelta = new Vector2(600f, 160f);

		Text label = labelObj.AddComponent<Text>();
		label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		label.fontSize = 40;
		label.alignment = TextAnchor.MiddleCenter;
		label.color = Color.white;
		label.text = "Tap to Start";

		btn.onClick = new Button.ButtonClickedEvent();
		btn.onClick.AddListener(() => BeginGame(player, overlay));

		startOverlayObject = overlay;

		UnityEngine.SceneManagement.Scene gameSceneForOverlay = UnityEngine.SceneManagement.SceneManager.GetSceneByName("GameScene");
		if (gameSceneForOverlay.IsValid()) UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(overlay, gameSceneForOverlay);
	}

	private void BeginGame(PlayerController player, GameObject overlay)
	{
		if (overlay != null)
		{
			Destroy(overlay);
		}
		player.BeginRun();
	}

	private static void EnableMobileSensors()
	{
#if ENABLE_INPUT_SYSTEM
		if (!Application.isMobilePlatform) return;

		if (Accelerometer.current != null && !Accelerometer.current.enabled)
		{
			InputSystem.EnableDevice(Accelerometer.current);
		}
#endif
	}

	private static void ConfigureFramePacing()
	{
		QualitySettings.vSyncCount = 0;
		Application.targetFrameRate = 60;
	}

	private void Update()
	{
		if (startOverlayObject == null) return;
#if ENABLE_LEGACY_INPUT_MANAGER
		if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || Input.touchCount > 0)
		{
			BeginGame(startOverlayPlayer, startOverlayObject);
			return;
		}
#endif

#if ENABLE_INPUT_SYSTEM
		if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
		{
			BeginGame(startOverlayPlayer, startOverlayObject);
			return;
		}

		if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
		{
			BeginGame(startOverlayPlayer, startOverlayObject);
			return;
		}

		if (UnityEngine.InputSystem.Touchscreen.current != null)
		{
			foreach (var t in UnityEngine.InputSystem.Touchscreen.current.touches)
			{
				if (t.press.wasPressedThisFrame)
				{
					BeginGame(startOverlayPlayer, startOverlayObject);
					return;
				}
			}
		}
#endif
	}

	private static void ConfigureDisplay()
	{
#if UNITY_STANDALONE
		Screen.SetResolution(PortraitWidth, PortraitHeight, FullScreenMode.Windowed);
#endif
	}
}
