using UnityEngine;

public class AudioManager : MonoBehaviour
{
	public static AudioManager Instance;

	[Header("Audio Sources")]
	public AudioSource musicSource;
	public AudioSource sfxSource;

	[Header("Master Volume")]
	[Range(0f, 1f)]
	public float masterMusicVolume = 1f;

	[Range(0f, 1f)]
	public float masterSFXVolume = 1f;

	private bool isMuted = false;

	[Header("Sounds")]

	public AudioClip jump;
	[Range(0f, 1f)]
	public float jumpVolume = 1f;

	public AudioClip spring;
	[Range(0f, 1f)]
	public float springVolume = 1f;

	public AudioClip shoot;
	[Range(0f, 1f)]
	public float shootVolume = 1f;

	public AudioClip rocket;
	[Range(0f, 1f)]
	public float rocketVolume = 1f;

	public AudioClip uiClick;
	[Range(0f, 1f)]
	public float uiClickVolume = 1f;

	public AudioClip coinCollect;
	[Range(0f, 1f)]
	public float coinCollectVolume = 1f;

	[Header("Music")]

	public AudioClip music;

	[Range(0f, 1f)]
	public float musicVolume = 0.5f;

	private void Awake()
	{
		if (Instance != null)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

	private void Start()
	{
		PlayMusic();
	}

	// =========================
	// MUSIC
	// =========================

	public void PlayMusic()
	{
		if (music == null)
			return;

		musicSource.clip = music;

		musicSource.volume =
			musicVolume * masterMusicVolume;

		musicSource.loop = true;

		musicSource.Play();
	}

	// =========================
	// SETTINGS
	// =========================

	public void SetMusicVolume(float value)
	{
		masterMusicVolume = value;

		musicSource.volume =
			musicVolume * masterMusicVolume;
	}

	public void SetSFXVolume(float value)
	{
		masterSFXVolume = value;
	}

	public void ToggleMute(bool mute)
	{
		isMuted = mute;

		musicSource.mute = mute;
		sfxSource.mute = mute;
	}

	// =========================
	// SFX
	// =========================

	public void PlayJump()
	{
		sfxSource.PlayOneShot(
			jump,
			jumpVolume * masterSFXVolume
		);
	}

	public void PlaySpring()
	{
		sfxSource.PlayOneShot(
			spring,
			springVolume * masterSFXVolume
		);
	}

	public void PlayShoot()
	{
		sfxSource.PlayOneShot(
			shoot,
			shootVolume * masterSFXVolume
		);
	}

	public void PlayRocket()
	{
		sfxSource.PlayOneShot(
			rocket,
			rocketVolume * masterSFXVolume
		);
	}

	public void PlayUIClick()
	{
		sfxSource.PlayOneShot(
			uiClick,
			uiClickVolume * masterSFXVolume
		);
	}

	public void PlayCoinCollect()
	{
		sfxSource.PlayOneShot(
			coinCollect,
			coinCollectVolume * masterSFXVolume
		);
	}
}