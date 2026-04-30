using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
	[SerializeField] private GameObject settingsPanel;

	[SerializeField] private Slider musicSlider;
	[SerializeField] private Slider sfxSlider;

	[SerializeField] private Toggle muteToggle;

	private void Start()
	{
		settingsPanel.SetActive(false);

		musicSlider.value =
			AudioManager.Instance.masterMusicVolume;

		sfxSlider.value =
			AudioManager.Instance.masterSFXVolume;

		musicSlider.onValueChanged.AddListener(
			AudioManager.Instance.SetMusicVolume
		);

		sfxSlider.onValueChanged.AddListener(
			AudioManager.Instance.SetSFXVolume
		);

		muteToggle.onValueChanged.AddListener(
			AudioManager.Instance.ToggleMute
		);
	}

	public void OpenSettings()
	{
		settingsPanel.SetActive(true);

		AudioManager.Instance.PlayUIClick();
	}

	public void CloseSettings()
	{
		settingsPanel.SetActive(false);

		AudioManager.Instance.PlayUIClick();
	}
}