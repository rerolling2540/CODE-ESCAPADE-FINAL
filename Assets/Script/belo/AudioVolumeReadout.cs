using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Displays the same saved audio percentage as the main menu.</summary>
[RequireComponent(typeof(Slider))]
public sealed class AudioVolumeReadout : MonoBehaviour
{
    [SerializeField] private TMP_Text percentage;
    [SerializeField] private bool music;
    private Slider slider;

    private void OnEnable()
    {
        slider = GetComponent<Slider>();
        slider.onValueChanged.AddListener(ShowValue);
        GameAudio.VolumesChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (slider != null) slider.onValueChanged.RemoveListener(ShowValue);
        GameAudio.VolumesChanged -= Refresh;
    }

    private void Refresh() => ShowValue(music ? GameAudio.MusicVolume : GameAudio.SfxVolume);

    private void ShowValue(float value)
    {
        if (percentage != null) percentage.text = Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
    }
}
