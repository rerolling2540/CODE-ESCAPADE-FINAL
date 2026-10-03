using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class VolumeSetting : MonoBehaviour
{
    [SerializeField] private AudioMixer myMixer;
    [SerializeField] private Slider musicSlider;

    private void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat("musicVolume", 0.75f);

        musicSlider.value = savedVolume;
        SetMusicVolume(savedVolume);

        // Siguraduhing nag-aadjust habang ginagalaw ang slider
        musicSlider.onValueChanged.AddListener(SetMusicVolume);
    }

    public void SetMusicVolume(float volume)
    {
        // Iwasan ang Log10(0)
        volume = Mathf.Clamp(volume, 0.0001f, 1f);

        // 0.0001 = -80 dB, 1 = 0 dB
        float dB = Mathf.Log10(volume) * 20f;

        myMixer.SetFloat("music", dB);

        PlayerPrefs.SetFloat("musicVolume", volume);
        PlayerPrefs.Save();
    }
}
