using UnityEngine;

/// <summary>Exploration cues follow successful movement actions, independent of hero artwork.</summary>
[DisallowMultipleComponent]
public sealed class CharacterSoundEffects : MonoBehaviour
{
    private playerMovement movement;
    private CharacterSfxLibrary.Profile profile;
    private AudioSource steps;
    private AudioSource actions;
    private float nextStep;
    private int stepIndex;

    public void Initialize(playerMovement owner)
    {
        movement = owner;
        var library = Resources.Load<CharacterSfxLibrary>("CharacterSfxLibrary");
        int character = owner.mageAppearance || (!owner.archerAppearance && SelectedHero.IsMage)
            ? SelectedHero.MageIndex : owner.archerAppearance || SelectedHero.IsArcher ? SelectedHero.ArcherIndex : 0;
        if (library == null || library.characters == null || character >= library.characters.Length) return;
        profile = library.characters[character];
        if (profile == null) return;
        if (steps == null) steps = CreateSource();
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

    public void UpdateRunning(bool running, bool grounded, float speed)
    {
        if (!CanPlay || !running || !grounded || movement.IsHurt)
        {
            nextStep = Time.time;
            if (steps != null) steps.Stop();
            return;
        }
        if (Time.time < nextStep || profile.footsteps == null || profile.footsteps.Length == 0) return;
        var clip = profile.footsteps[stepIndex++ % profile.footsteps.Length];
        if (clip != null)
        {
            steps.clip = clip;
            steps.volume = profile.stepVolume;
            steps.Play();
        }
        nextStep = Time.time + profile.stepInterval *
            Mathf.Clamp(movement.moveSpeed / Mathf.Max(speed, 0.1f), 0.7f, 1.5f);
    }

    public void Jump() => Play(profile != null ? profile.jump : null);
    public void Dash() => Play(profile != null ? profile.dash : null);

    private void Play(AudioClip clip)
    {
        if (!CanPlay || clip == null || actions == null) return;
        if (steps != null) steps.Stop();
        actions.PlayOneShot(clip, profile.actionVolume);
    }

    private void StopSounds()
    {
        if (steps != null) steps.Stop();
        if (actions != null) actions.Stop();
        nextStep = Time.time;
    }

    private void OnDisable() => StopSounds();
}
