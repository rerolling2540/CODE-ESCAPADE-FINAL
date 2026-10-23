using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PortalInteraction : MonoBehaviour
{
    public string entryPrompt = "[E] Enter portal";
    public string unavailablePrompt;
    public bool playEntrySound = true;
    private sealed class Route
    {
        public Func<bool> available;
        public Action<Transform> enter;
    }
    private readonly List<Route> routes = new List<Route>();
    private readonly HashSet<Collider2D> players = new HashSet<Collider2D>();
    private static readonly HashSet<PortalInteraction> portals = new HashSet<PortalInteraction>();
    private static float readyAt;
    private static int enteredFrame = -1;
    public event Action Entered;
    private GUIStyle buttonStyle;

    void Awake() { PortalEntrySound.Preload(); }

    public static PortalInteraction For(GameObject portal)
    {
        var interaction = portal.GetComponent<PortalInteraction>();
        return interaction != null ? interaction : portal.AddComponent<PortalInteraction>();
    }

    public void AddRoute(Func<bool> available, Action<Transform> enter)
    {
        routes.Add(new Route { available = available, enter = enter });
    }

    void OnEnable() { portals.Add(this); }
    void OnDisable() { portals.Remove(this); players.Clear(); }
    void OnTriggerEnter2D(Collider2D other) { Track(other); }
    void OnTriggerStay2D(Collider2D other) { Track(other); }
    void OnTriggerExit2D(Collider2D other) { players.Remove(other); }

    void Track(Collider2D other)
    {
        if (other != null && (other.GetComponentInParent<playerMovement>() != null || other.CompareTag("Player")))
            players.Add(other);
    }

    Transform NearbyPlayer()
    {
        players.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
        foreach (var collider in players)
        {
            var movement = collider.GetComponentInParent<playerMovement>();
            return movement != null ? movement.transform : collider.transform;
        }
        return null;
    }

    bool HasRoute() => routes.Exists(r => r.available());

    bool CanEnter()
    {
        if (Time.timeScale <= 0f || Time.unscaledTime < readyAt || enteredFrame == Time.frameCount ||
            (DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive) || !HasRoute()) return false;
        var player = NearbyPlayer();
        if (player == null) return false;
        // If trigger volumes overlap, show and activate only the nearest portal.
        float distance = (player.position - transform.position).sqrMagnitude;
        foreach (var portal in portals)
            if (portal != this && portal.NearbyPlayer() != null && portal.HasRoute() &&
                (player.position - portal.transform.position).sqrMagnitude < distance) return false;
        return true;
    }

    void Update()
    {
        bool pressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        pressed |= Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;
        if (pressed) TryEnter();
    }

    void TryEnter()
    {
        if (!CanEnter()) return;
        var route = routes.Find(r => r.available());
        var player = NearbyPlayer();
        enteredFrame = Time.frameCount;
        readyAt = Time.unscaledTime + 0.75f;
        if (playEntrySound) PortalEntrySound.PlayFor(gameObject);
        Entered?.Invoke();
        route.enter(player);
    }

    void OnGUI()
    {
        bool canEnter = CanEnter();
        bool showLocked = !HasRoute() && NearbyPlayer() != null && !string.IsNullOrEmpty(unavailablePrompt) &&
            Time.timeScale > 0f && (DialogueManager.Instance == null || !DialogueManager.Instance.isDialogueActive);
        if ((!canEnter && !showLocked) || Camera.main == null) return;
        var bounds = GetComponent<Collider2D>();
        Vector3 anchor = bounds != null ? new Vector3(bounds.bounds.center.x, bounds.bounds.max.y + 0.4f, transform.position.z) : transform.position + Vector3.up * 2f;
        Vector3 screen = Camera.main.WorldToScreenPoint(anchor);
        if (screen.z <= 0f) return;
        if (buttonStyle == null)
        {
            buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18, fontStyle = FontStyle.Bold };
            buttonStyle.normal.textColor = new Color(1f, 0.85f, 0.35f);
        }
        var oldMatrix = GUI.matrix;
        var oldColor = GUI.color;
        GUI.matrix = Matrix4x4.identity;
        GUI.color = Color.white;
        float width = showLocked ? 300f : 170f;
        Rect button = new Rect(Mathf.Clamp(screen.x - width * 0.5f, 4f, Mathf.Max(4f, Screen.width - width - 4f)),
            Mathf.Clamp(Screen.height - screen.y - 20f, 4f, Mathf.Max(4f, Screen.height - 44f)), width, 40f);
        bool oldEnabled = GUI.enabled;
        GUI.enabled = oldEnabled && canEnter;
        if (GUI.Button(button, canEnter ? entryPrompt : unavailablePrompt, buttonStyle)) TryEnter();
        GUI.enabled = oldEnabled;
        GUI.color = oldColor;
        GUI.matrix = oldMatrix;
    }
}
