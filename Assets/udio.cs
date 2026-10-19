using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class AudioManager : MonoBehaviour
{
    [Header("---------- Audio Source ----------")]
    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioSource SFXSource;

    [Header("---------- Audio Clip ----------")]
    public AudioClip background;
    public AudioClip death;
    public AudioClip checkpoint;
    public AudioClip wallTouch;
    public AudioClip portalIn;
    public AudioClip portalOut;
    public AudioClip buttonClick;

    [Header("---------- Volume ----------")]
    [Range(0f, 1f)]
    public float musicVolume = 1f;

    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    private void Start()
    {
        GameAudio.Route(musicSource, true);
        GameAudio.Route(SFXSource, false);
        GameAudio.ApplySavedVolumes();
        if (musicSource != null)
        {
            musicSource.clip = background;
            musicSource.volume = Mathf.Clamp01(musicVolume);
            musicSource.loop = true;
            if (background != null) musicSource.Play();
        }
        if (SFXSource != null)
        {
            SFXSource.volume = Mathf.Clamp01(sfxVolume);
            SFXSource.loop = false;
        }
    }

    public void PlayButtonSound()
    {
        if (SFXSource != null && buttonClick != null) SFXSource.PlayOneShot(buttonClick);
    }

    public void SetMusicVolume(float volume)
    {
        GameAudio.SetMusicVolume(volume);
    }

    public void SetSFXVolume(float volume)
    {
        GameAudio.SetSfxVolume(volume);
    }

    public void PlayButtonSoundAndLoadScene(string sceneName)
    {
        PlayButtonSound();
        StartCoroutine(LoadSceneAfterSound(sceneName));
    }

    private IEnumerator LoadSceneAfterSound(string sceneName)
    {
        yield return new WaitForSecondsRealtime(0.2f);
        SceneManager.LoadScene(sceneName);
    }

    public void LoadScene2()
    {
        PlayButtonSoundAndLoadScene("Scene 2");
    }
}
