using System;
using UnityEngine;
using UnityEngine.Audio;

public static class GameAudio
{
    private static AudioMixer mixer;
    public static event Action VolumesChanged;
    public static float MusicVolume => Mathf.Clamp01(PlayerPrefs.GetFloat("musicVolume", 0.75f));
    public static float SfxVolume => Mathf.Clamp01(PlayerPrefs.GetFloat("sfxVolume", 0.75f));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Reset()
    {
        mixer = null;
        VolumesChanged = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    public static void ApplySavedVolumes()
    {
        var output = Mixer;
        if (output == null) return;
        output.SetFloat("music", ToDecibels(MusicVolume));
        output.SetFloat("sfx", ToDecibels(SfxVolume));
    }

    private static AudioMixer Mixer
    {
        get
        {
            if (mixer == null) mixer = Resources.Load<AudioMixer>("NewAudioMixer");
            return mixer;
        }
    }

    public static float ToDecibels(float volume) => volume <= 0.0001f ? -80f : Mathf.Log10(Mathf.Clamp01(volume)) * 20f;

    public static void SetMusicVolume(float volume) { SetVolume("musicVolume", volume); }
    public static void SetSfxVolume(float volume) { SetVolume("sfxVolume", volume); }

    private static void SetVolume(string key, float volume)
    {
        PlayerPrefs.SetFloat(key, Mathf.Clamp01(volume));
        ApplySavedVolumes();
        PlayerPrefs.Save();
        VolumesChanged?.Invoke();
    }

    public static void Route(AudioSource source, bool music)
    {
        if (source == null || Mixer == null) return;
        var groups = Mixer.FindMatchingGroups(music ? "music" : "SFX");
        if (groups.Length > 0) source.outputAudioMixerGroup = groups[0];
    }
}
