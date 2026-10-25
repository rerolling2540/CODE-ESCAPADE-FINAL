using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
public class CardRewardChest : MonoBehaviour
{
    public string chestId;
    public CardData reward;
    public Texture2D spriteSheet;
    [Range(0, 2)] public int variant;
    public float interactionDistance = 2.5f;
    private readonly Sprite[] frames = new Sprite[10];
    private SpriteRenderer visual;
    private CharacterInventory inventory;
    private playerMovement player;
    private bool opened;
    private float messageUntil;
    private GUIStyle promptStyle, rewardStyle;

    void Awake()
    {
        visual = GetComponent<SpriteRenderer>();
        if (spriteSheet == null) return;
        // Fixed cells preserve the chest's feet and lid alignment throughout the animation.
        for (int i = 0; i < frames.Length; i++)
        {
            int row = variant * 2 + i / 5;
            frames[i] = Sprite.Create(spriteSheet,
                new Rect(i % 5 * 48, spriteSheet.height - 33 - row * 32, 48, 32),
                new Vector2(1f / 3f, 11f / 32f), 100f);
        }
        visual.sprite = frames[0];
    }

    void Start()
    {
        inventory = FindFirstObjectByType<CharacterInventory>();
        opened = inventory != null && inventory.IsChestOpened(chestId);
        if (opened && frames[9] != null) visual.sprite = frames[9];
    }

    bool CanInteract()
    {
        return !opened && reward != null && spriteSheet != null && inventory != null && player != null &&
            Time.timeScale > 0f &&
            (DialogueManager.Instance == null || !DialogueManager.Instance.isDialogueActive) &&
            Vector2.Distance(player.transform.position, visual.bounds.center) <= interactionDistance;
    }

    void Update()
    {
        if (player == null) player = FindFirstObjectByType<playerMovement>();
        if (inventory == null) inventory = FindFirstObjectByType<CharacterInventory>();
        if (!opened && inventory != null && inventory.IsChestOpened(chestId))
        {
            opened = true;
            if (frames[9] != null) visual.sprite = frames[9];
        }
        if (opened) return;
        if (frames[0] != null) visual.sprite = frames[Mathf.FloorToInt(Time.time * 6f) % 5];
        if (!CanInteract()) return;
        bool interact = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        interact |= Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;
        if (!interact || !inventory.ClaimChest(chestId, reward)) return;
        opened = true;
        StartCoroutine(Open());
    }

    IEnumerator Open()
    {
        for (int i = 5; i < frames.Length; i++)
        {
            visual.sprite = frames[i];
            yield return new WaitForSeconds(0.12f);
        }
        messageUntil = Time.unscaledTime + 6f;
    }

    void OnGUI()
    {
        if (GameControlsGuide.SettingsOpen) return;
        if (!CanInteract() && Time.unscaledTime >= messageUntil) return;
        if (Camera.main == null) return;
        if (promptStyle == null)
        {
            promptStyle = new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
            promptStyle.normal.textColor = new Color(1f, 0.85f, 0.35f);
            rewardStyle = new GUIStyle(promptStyle) { wordWrap = true, fontSize = 20, padding = new RectOffset(16, 16, 10, 10) };
        }
        var oldMatrix = GUI.matrix;
        var oldColor = GUI.color;
        GUI.matrix = Matrix4x4.identity;
        GUI.color = Color.white;
        if (Time.unscaledTime < messageUntil)
        {
            float width = Mathf.Min(520f, Screen.width - 32f);
            GUI.Box(new Rect((Screen.width - width) / 2f, 30f, width, 130f),
                "New card: " + reward.name + "\n" + reward.Description + "\nAdded to your collection and battle deck.", rewardStyle);
        }
        else
        {
            Vector3 screen = Camera.main.WorldToScreenPoint(visual.bounds.center + Vector3.up * 1.1f);
            if (screen.z > 0)
                GUI.Box(new Rect(screen.x - 95f, Screen.height - screen.y - 18f, 190f, 36f), "[E] Open chest", promptStyle);
        }
        GUI.color = oldColor;
        GUI.matrix = oldMatrix;
    }

    void OnDestroy()
    {
        foreach (var frame in frames) if (frame != null) Destroy(frame);
    }
}
