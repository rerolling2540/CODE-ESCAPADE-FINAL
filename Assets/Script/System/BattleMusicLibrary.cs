using UnityEngine;

/// <summary>Audio assets used by the battle music flow.</summary>
[CreateAssetMenu(menuName = "Code Escapade/Audio/Battle Music Library")]
public sealed class BattleMusicLibrary : ScriptableObject
{
    [Header("Track 18 — Finals game of a tournament")]
    public AudioClip transitionAndVictory;

    [Header("Track 7 — Opening of mass combat")]
    public AudioClip battleIntro;
    public AudioClip battleLoop;

    [Header("Track 13 — Evil forces")]
    public AudioClip defeat;
}
