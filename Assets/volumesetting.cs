using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSetting : MonoBehaviour
{
    [SerializeField] private AudioMixer myMixer;

    [Header("Music")]
    [SerializeField] private Slider musicSlider;

    [Header("SFX")]
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        // MUSIC
        float savedMusicVolume = PlayerPrefs.GetFloat("musicVolume", 0.75f);
        musicSlider.value = savedMusicVolume;
        SetMusicVolume(savedMusicVolume);
        musicSlider.onValueChanged.AddListener(SetMusicVolume);

        // SFX
        float savedSFXVolume = PlayerPrefs.GetFloat("sfxVolume", 0.75f);
        sfxSlider.value = savedSFXVolume;
        SetSFXVolume(savedSFXVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
    }

    public void SetMusicVolume(float volume)
    {
        volume = Mathf.Clamp(volume, 0.0001f, 1f);

        float dB = Mathf.Log10(volume) * 20f;

        myMixer.SetFloat("music", dB);

        PlayerPrefs.SetFloat("musicVolume", volume);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float volume)
    {
        volume = Mathf.Clamp(volume, 0.0001f, 1f);

        float dB = Mathf.Log10(volume) * 20f;

        myMixer.SetFloat("sfx", dB);

        PlayerPrefs.SetFloat("sfxVolume", volume);
        PlayerPrefs.Save();
    }
}