using UnityEngine;

/// <summary>Damages the player on contact with a painted hazard or animated trap.</summary>
[RequireComponent(typeof(Collider2D))]
public sealed class Map4TrapDamageTrigger : MonoBehaviour
{
    [SerializeField, Min(1)] private int damage = 18;
    [SerializeField, Tooltip("Powered contact removes a full health bar.")]
    private bool lethalContact;
    private TrapPackAnimation trapAnimation;
    [SerializeField, Min(0f), Tooltip("Required overlap in world units before contact hurts. Zero accepts any overlap.")]
    private float minimumOverlapDepth;
    private Collider2D[] damageHitboxes;
    [SerializeField] private AudioClip damageSound;
    [SerializeField, Range(0f, 1f)] private float damageSoundVolume = 0.65f;
    [SerializeField] private AudioSource damageAudioSource;

    public bool CanDealDamage
    {
        get
        {
            if (!isActiveAndEnabled) return false;
            // Also handles generated traps that add the animation after this component.
            if (trapAnimation == null) trapAnimation = GetComponent<TrapPackAnimation>();
            return trapAnimation == null || trapAnimation.IsDamageActive;
        }
    }

    public void Initialize(int damageAmount)
    {
        damage = Mathf.Max(1, damageAmount);
    }

    private void OnTriggerEnter2D(Collider2D other) => DamagePlayer(other);
    private void OnTriggerStay2D(Collider2D other) => DamagePlayer(other);

    private void DamagePlayer(Collider2D other)
    {
        if (!CanDealDamage || other == null) return;
        if (minimumOverlapDepth > 0f && !HasDamagingOverlap(other)) return;
        var player = other.GetComponentInParent<playerMovement>();
        if (player != null && player.TryTakeDamage(lethalContact ? Mathf.Max(damage, PlayerHealth.MaxHealth) : damage))
            PlayDamageSound();
    }

    private void PlayDamageSound()
    {
        if (damageSound == null) return;
        if (damageAudioSource == null) damageAudioSource = gameObject.AddComponent<AudioSource>();
        damageAudioSource.playOnAwake = false;
        damageAudioSource.loop = false;
        // A painted tilemap can span the level; the hit happens at the player, not its origin.
        damageAudioSource.spatialBlend = 0f;
        damageAudioSource.volume = damageSoundVolume;
        damageAudioSource.priority = 64;
        GameAudio.Route(damageAudioSource, false);
        damageAudioSource.clip = damageSound;
        damageAudioSource.Play();
    }

    private bool HasDamagingOverlap(Collider2D other)
    {
        if (damageHitboxes == null) damageHitboxes = GetComponents<Collider2D>();
        foreach (var hitbox in damageHitboxes)
        {
            if (hitbox == null || !hitbox.enabled || !hitbox.isTrigger) continue;
            ColliderDistance2D contact = hitbox.Distance(other);
            if (contact.isValid && contact.isOverlapped && contact.distance <= -minimumOverlapDepth)
                return true;
        }
        return false;
    }
}
