using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class VolumeSetting : MonoBehaviour
{
    [Header("Music")]
    [SerializeField] private Slider musicSlider;

    [Header("SFX")]
    [SerializeField] private Slider sfxSlider;

    private readonly List<Slider> musicSliders = new List<Slider>();
    private readonly List<Slider> sfxSliders = new List<Slider>();

    private void OnEnable()
    {
        musicSliders.Clear();
        sfxSliders.Clear();
        // Include hidden settings panels and recover missing Inspector references.
        foreach (var slider in FindObjectsByType<Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (slider.gameObject.scene != gameObject.scene) continue;
            if (slider == musicSlider || string.Equals(slider.name, "music", System.StringComparison.OrdinalIgnoreCase))
                musicSliders.Add(slider);
            else if (slider == sfxSlider || string.Equals(slider.name, "SFX", System.StringComparison.OrdinalIgnoreCase))
                sfxSliders.Add(slider);
        }
        foreach (var slider in musicSliders) slider.onValueChanged.AddListener(SetMusicVolume);
        foreach (var slider in sfxSliders) slider.onValueChanged.AddListener(SetSFXVolume);
        GameAudio.VolumesChanged += RefreshSliders;
        RefreshSliders();
    }

    private void Start() { GameAudio.ApplySavedVolumes(); }

    private void OnDisable()
    {
        foreach (var slider in musicSliders) if (slider != null) slider.onValueChanged.RemoveListener(SetMusicVolume);
        foreach (var slider in sfxSliders) if (slider != null) slider.onValueChanged.RemoveListener(SetSFXVolume);
        GameAudio.VolumesChanged -= RefreshSliders;
    }

    private void RefreshSliders()
    {
        foreach (var slider in musicSliders) RefreshSlider(slider, GameAudio.MusicVolume);
        foreach (var slider in sfxSliders) RefreshSlider(slider, GameAudio.SfxVolume);
    }

    private static void RefreshSlider(Slider slider, float volume)
    {
        if (slider == null) return;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.SetValueWithoutNotify(volume);
    }

    public void SetMusicVolume(float volume) { GameAudio.SetMusicVolume(volume); }
    public void SetSFXVolume(float volume) { GameAudio.SetSfxVolume(volume); }
}
