using UnityEngine;

/// <summary>Plays jump and dash cues, independent of hero artwork.</summary>
[DisallowMultipleComponent]
public sealed class CharacterSoundEffects : MonoBehaviour
{
    private playerMovement movement;
    private CharacterSfxLibrary.Profile profile;
    private AudioSource actions;

    public void Initialize(playerMovement owner)
    {
        movement = owner;
        var library = Resources.Load<CharacterSfxLibrary>("CharacterSfxLibrary");
        int character = owner.mageAppearance || (!owner.archerAppearance && SelectedHero.IsMage)
            ? SelectedHero.MageIndex : owner.archerAppearance || SelectedHero.IsArcher ? SelectedHero.ArcherIndex : 0;
        if (library == null || library.characters == null || character >= library.characters.Length) return;
        profile = library.characters[character];
        if (profile == null) return;
        if (actions == null) actions = CreateSource();
    }

    private AudioSource CreateSource()
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 0f;
        GameAudio.Route(source, false);
        return source;
    }

    private bool CanPlay => profile != null && movement != null && movement.isActiveAndEnabled &&
        Time.timeScale > 0f && !PlayerDeathFlow.IsActive &&
        (DialogueManager.Instance == null || !DialogueManager.Instance.isDialogueActive);

    private void Update()
    {
        if (!CanPlay) StopSounds();
    }

    public void Jump() => Play(profile != null ? profile.jump : null);
    public void Dash() => Play(profile != null ? profile.dash : null);

    private void Play(AudioClip clip)
    {
        if (!CanPlay || clip == null || actions == null) return;
        actions.PlayOneShot(clip, profile.actionVolume);
    }

    private void StopSounds()
    {
        if (actions != null) actions.Stop();
    }

    private void OnDisable() => StopSounds();
}
