using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Plays UI clicks only while the main menu is active.</summary>
public sealed class BattleButtonClickSound : MonoBehaviour, IPointerClickHandler, ISubmitHandler
{
    private AudioSource source;
    private AudioClip clip;
    private float volume = 1f;

    public void Configure(AudioSource audioSource, AudioClip audioClip, float clickVolume)
    {
        source = audioSource;
        clip = audioClip;
        volume = Mathf.Clamp01(clickVolume);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData != null && eventData.button == PointerEventData.InputButton.Left)
            Play();
    }

    public void OnSubmit(BaseEventData eventData)
    {
        Play();
    }

    private void Play()
    {
        if (!GameAudio.UiClickSoundsEnabled) return;
        if (!isActiveAndEnabled || source == null || clip == null) return;
        Button button = GetComponent<Button>();
        if (button != null && !button.IsInteractable()) return;
        source.PlayOneShot(clip, volume);
    }
}
