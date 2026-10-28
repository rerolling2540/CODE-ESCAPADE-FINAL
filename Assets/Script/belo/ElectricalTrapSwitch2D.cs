using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>A scene-authored lever controlling its explicitly linked electrical traps.</summary>
public sealed class ElectricalTrapSwitch2D : MonoBehaviour
{
    [SerializeField] private Map4TrapDamageTrigger[] controlledTraps;
    [SerializeField] private bool startsPowered = true;
    [SerializeField, Min(0.5f)] private float interactionDistance = 2f;
    [SerializeField] private Transform leverPivot;
    [SerializeField] private SpriteRenderer indicator;
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private TMP_Text interactionPrompt;

    private playerMovement player;
    private float readyAt;
    public bool IsPowered { get; private set; }

    void Start() { SetPowered(startsPowered); }

    private bool CanInteract()
    {
        if (GameControlsGuide.MapHidden || Time.timeScale <= 0f || Time.unscaledTime < readyAt) return false;
        if (player == null) player = FindFirstObjectByType<playerMovement>();
        if (player == null) return false;
        var body = player.GetComponent<Collider2D>();
        Vector2 center = body != null ? body.bounds.center : player.transform.position;
        return Vector2.Distance(center, (Vector2)transform.position + Vector2.up * 0.65f) <= interactionDistance;
    }

    void Update()
    {
        bool nearby = CanInteract();
        if (interactionPrompt != null)
        {
            interactionPrompt.gameObject.SetActive(nearby);
            interactionPrompt.text = IsPowered ? "[E] Disable electricity" : "[E] Enable electricity";
        }
        bool pressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        pressed |= Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;
        if (nearby && pressed) TryInteract();
    }

    public bool TryInteract()
    {
        if (!CanInteract()) return false;
        SetPowered(!IsPowered);
        readyAt = Time.unscaledTime + 0.3f;
        return true;
    }

    public void SetPowered(bool powered)
    {
        IsPowered = powered;
        if (controlledTraps != null)
            foreach (var trap in controlledTraps)
            {
                if (trap == null) continue;
                trap.enabled = powered;
                foreach (var hitbox in trap.GetComponents<Collider2D>()) hitbox.enabled = powered;
                var animation = trap.GetComponent<TrapPackAnimation>();
                if (animation != null)
                {
                    animation.enabled = powered;
                    var renderer = trap.GetComponent<SpriteRenderer>();
                    if (!powered && renderer != null && animation.IdleSprite != null)
                        renderer.sprite = animation.IdleSprite;
                }
            }
        if (leverPivot != null) leverPivot.localRotation = Quaternion.Euler(0f, 0f, powered ? -35f : 35f);
        if (indicator != null) indicator.color = powered ? new Color(1f, 0.2f, 0.1f) : new Color(0.3f, 1f, 0.4f);
        if (statusLabel != null) statusLabel.text = powered ? "ON" : "OFF";
    }
}
