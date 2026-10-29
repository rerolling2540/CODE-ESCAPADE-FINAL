using UnityEngine;

/// <summary>Local trap sounds follow the same activation state as contact damage.</summary>
[DefaultExecutionOrder(100)]
[RequireComponent(typeof(AudioSource), typeof(TrapPackAnimation))]
public sealed class TrapSoundEffects : MonoBehaviour
{
    [SerializeField] private AudioClip[] activationSounds;
    [SerializeField, Range(0f, 1f)] private float volume = 0.55f;
    [SerializeField, Min(0f), Tooltip("Repeat interval for a continuously spinning mechanism. Zero plays once per attack cycle.")]
    private float repeatInterval;
    [SerializeField, Min(0f)] private float fullVolumeDistance = 1.25f;
    [SerializeField, Min(0.1f), Tooltip("Silent beyond this distance from the player to the trap's visible bounds.")]
    private float hearingDistance = 4f;

    private AudioSource source;
    private TrapPackAnimation animation;
    private Map4TrapDamageTrigger damageTrigger;
    private playerMovement player;
    private Collider2D playerBody;
    private SpriteRenderer trapRenderer;
    private bool wasActive;
    private int previousFrame = -1;
    private float nextSoundTime;
    private int nextClip;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        animation = GetComponent<TrapPackAnimation>();
        damageTrigger = GetComponent<Map4TrapDamageTrigger>();
        trapRenderer = GetComponent<SpriteRenderer>();
        source.playOnAwake = false;
        source.loop = false;
        // Distance is measured in the 2D game world, independent of camera zoom and Z offset.
        source.spatialBlend = 0f;
        GameAudio.Route(source, false);
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;
        float proximityVolume = GetProximityVolume();
        source.volume = volume * proximityVolume;
        bool active = animation.IsDamageActive && (damageTrigger == null || damageTrigger.CanDealDamage) &&
            proximityVolume > 0f;
        bool wrapped = animation.CurrentFrame < previousFrame;
        if (active && (!wasActive || (repeatInterval > 0f ? Time.time >= nextSoundTime : wrapped)))
        {
            if (activationSounds != null && activationSounds.Length > 0)
            {
                var clip = activationSounds[nextClip++ % activationSounds.Length];
                if (clip != null)
                {
                    source.clip = clip;
                    source.Play();
                    nextSoundTime = Time.time + repeatInterval;
                }
            }
        }
        if (!active && wasActive) source.Stop();
        wasActive = active;
        previousFrame = animation.CurrentFrame;
    }

    private float GetProximityVolume()
    {
        if (player == null || !player.isActiveAndEnabled)
        {
            player = FindFirstObjectByType<playerMovement>();
            playerBody = player != null ? player.GetComponent<Collider2D>() : null;
        }
        if (player == null || !player.isActiveAndEnabled) return 0f;

        Vector3 position = playerBody != null ? playerBody.bounds.center : player.transform.position;
        Vector3 nearest = transform.position;
        if (trapRenderer != null)
        {
            Bounds bounds = trapRenderer.bounds;
            position.z = bounds.center.z;
            nearest = bounds.ClosestPoint(position);
        }
        float distance = Vector2.Distance(position, nearest);
        float maximum = Mathf.Max(0.1f, hearingDistance);
        float minimum = Mathf.Clamp(fullVolumeDistance, 0f, maximum - 0.01f);
        return 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(minimum, maximum, distance));
    }

    private void OnDisable()
    {
        if (source != null) source.Stop();
        wasActive = false;
        previousFrame = -1;
    }
}
