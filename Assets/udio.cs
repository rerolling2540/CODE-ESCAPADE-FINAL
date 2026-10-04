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
        musicSource.clip = background;
        musicSource.volume = musicVolume;
        musicSource.Play();

        SFXSource.volume = sfxVolume;
    }

    public void PlayButtonSound()
    {
        SFXSource.PlayOneShot(buttonClick);
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        musicSource.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        SFXSource.volume = volume;
    }

    public void PlayButtonSoundAndLoadScene(string sceneName)
    {
        SFXSource.PlayOneShot(buttonClick);
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