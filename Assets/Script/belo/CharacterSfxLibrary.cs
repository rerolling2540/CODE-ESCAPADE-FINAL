using System;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/Character SFX Library")]
public sealed class CharacterSfxLibrary : ScriptableObject
{
    [Serializable]
    public sealed class Profile
    {
        public string character;
        public AudioClip[] footsteps;
        public AudioClip jump;
        public AudioClip dash;
        [Range(0f, 1f)] public float stepVolume = 0.3f;
        [Range(0f, 1f)] public float actionVolume = 0.5f;
        [Min(0.1f)] public float stepInterval = 0.32f;
    }

    // Uses the same stable order as SelectedHero and saved character selection.
    public Profile[] characters = new Profile[3];
}
