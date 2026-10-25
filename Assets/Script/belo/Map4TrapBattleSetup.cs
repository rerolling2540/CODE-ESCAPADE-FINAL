using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

/// <summary>Places animated trap-pack hazards in Map 4 and creates the exploration health HUD.</summary>
public sealed class Map4TrapBattleSetup : MonoBehaviour
{
    private const int MaxHealth = 100;
    private static readonly HashSet<string> HazardTileNames = new HashSet<string>
    {
        "main_lev_buildA_2", "main_lev_buildA_3",
        "main_lev_buildA_557", "main_lev_buildA_558",
        "main_lev_buildA_1112", "main_lev_buildA_1113"
    };

    private static readonly TrapSheet[] TrapSheets =
    {
        new TrapSheet("TrapPack/Arrow_Trap_Level_1", 32),
        new TrapSheet("TrapPack/Fire_Trap_Level_1", 64),
        new TrapSheet("TrapPack/Saw_Trap_Level_1", 64)
    };

    private static Map4TrapBattleSetup active;
    private readonly List<Sprite[]> frameSets = new List<Sprite[]>();
    private Slider healthSlider;
    private TextMeshProUGUI healthLabel;
    private TextMeshProUGUI headerLabel;
    private int currentHealth = MaxHealth;
    private float nextDamageTime;
    private int placedTrapCount;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string normalizedName = scene.name.Replace(" ", string.Empty).ToLowerInvariant();
        if (!normalizedName.Contains("map4")) return;
        if (active != null) return;

        GameObject host = new GameObject("Map 4 Trap Battle HUD");
        active = host.AddComponent<Map4TrapBattleSetup>();
    }

    private void Awake()
    {
        if (active != null && active != this)
        {
            Destroy(gameObject);
            return;
        }

        active = this;
        BuildTrapFrames();
        PlaceTraps();
        BuildHealthHud();
        RefreshHealthHud();
    }

    private void OnDestroy()
    {
        if (active == this) active = null;
    }

    public void DamagePlayer(int amount)
    {
        if (Time.unscaledTime < nextDamageTime || currentHealth <= 0) return;
        nextDamageTime = Time.unscaledTime + 0.8f;
        currentHealth = Mathf.Max(0, currentHealth - Mathf.Max(1, amount));
        RefreshHealthHud();
    }

    private void BuildTrapFrames()
    {
        frameSets.Clear();
        foreach (TrapSheet sheet in TrapSheets)
        {
            Texture2D texture = Resources.Load<Texture2D>(sheet.ResourcePath);
            if (texture == null || !texture.isReadable)
            {
                Debug.LogError($"Map 4 could not load readable trap sheet '{sheet.ResourcePath}'. Check its Resources import settings.", this);
                frameSets.Add(Array.Empty<Sprite>());
                continue;
            }

            int frameCount = Mathf.Max(1, texture.width / sheet.FrameSize);
            Sprite[] frames = new Sprite[frameCount];
            for (int frame = 0; frame < frameCount; frame++)
            {
                Rect rect = new Rect(frame * sheet.FrameSize, 0, sheet.FrameSize, sheet.FrameSize);
                frames[frame] = Sprite.Create(texture, rect, new Vector2(0.5f, 0f), 64f);
                frames[frame].name = $"{texture.name}_frame_{frame:00}";
            }
            frameSets.Add(frames);
        }
    }

    private void PlaceTraps()
    {
        List<TrapPlacement> placements = FindExistingHazardCells();
        if (placements.Count == 0) placements = FindGroundSurfacePlacements();
        if (placements.Count == 0)
        {
            Debug.LogWarning("Map 4 could not find hazard or ground cells for the trap-pack sprites.", this);
            return;
        }

        placements.Sort((a, b) => a.Position.x.CompareTo(b.Position.x));
        int count = Mathf.Min(6, placements.Count);
        for (int i = 0; i < count; i++)
        {
            int placementIndex = count == 1 ? 0 : Mathf.RoundToInt(i * (placements.Count - 1f) / (count - 1f));
            TrapPlacement placement = placements[placementIndex];
            Sprite[] frames = frameSets[i % frameSets.Count];
            if (frames == null || frames.Length == 0) continue;

            GameObject trap = new GameObject(TrapSheets[i % TrapSheets.Length].DisplayName);
            trap.transform.SetParent(transform, true);
            trap.transform.position = placement.Position;

            SpriteRenderer renderer = trap.AddComponent<SpriteRenderer>();
            renderer.sprite = frames[0];
            renderer.sortingOrder = 12;

            BoxCollider2D hitbox = trap.AddComponent<BoxCollider2D>();
            hitbox.isTrigger = true;
            hitbox.size = new Vector2(Mathf.Max(0.45f, placement.CellSize.x * 0.9f), Mathf.Max(0.45f, placement.CellSize.y * 0.65f));
            hitbox.offset = new Vector2(0f, placement.CellSize.y * 0.12f);

            Map4TrapDamageTrigger damageTrigger = trap.AddComponent<Map4TrapDamageTrigger>();
            damageTrigger.Initialize(this, 18);
            TrapPackAnimation animation = trap.AddComponent<TrapPackAnimation>();
            animation.Initialize(frames, 12f);
            placedTrapCount++;
        }

        Debug.Log($"Map 4 trap pack is active ({placedTrapCount} visible damage traps).", this);
    }

    private List<TrapPlacement> FindExistingHazardCells()
    {
        List<TrapPlacement> placements = new List<TrapPlacement>();
        Tilemap trapMap = FindTilemap("trap");
        if (trapMap == null) return placements;

        BoundsInt bounds = trapMap.cellBounds;
        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            TileBase tile = trapMap.GetTile(cell);
            if (tile == null || !HazardTileNames.Contains(tile.name)) continue;
            placements.Add(new TrapPlacement(trapMap.GetCellCenterWorld(cell), trapMap.cellSize));
        }
        return placements;
    }

    private List<TrapPlacement> FindGroundSurfacePlacements()
    {
        List<TrapPlacement> placements = new List<TrapPlacement>();
        Tilemap ground = FindTilemap("gound") ?? FindTilemap("ground");
        if (ground == null) return placements;

        BoundsInt bounds = ground.cellBounds;
        foreach (Vector3Int cell in bounds.allPositionsWithin)
        {
            if (ground.GetTile(cell) == null || ground.GetTile(cell + Vector3Int.up) != null) continue;
            Vector3 position = ground.CellToWorld(cell) + new Vector3(ground.cellSize.x * 0.5f, ground.cellSize.y, 0f);
            placements.Add(new TrapPlacement(position, ground.cellSize));
        }
        return placements;
    }

    private Tilemap FindTilemap(string exactName)
    {
        foreach (Tilemap map in FindObjectsByType<Tilemap>(FindObjectsSortMode.None))
            if (string.Equals(map.name, exactName, StringComparison.OrdinalIgnoreCase)) return map;
        return null;
    }

    private void BuildHealthHud()
    {
        GameObject canvasObject = new GameObject("Trap Health HUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 120;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform panel = CreateRect("Health Panel", canvasObject.transform);
        panel.anchorMin = panel.anchorMax = new Vector2(1f, 0.52f);
        panel.pivot = new Vector2(1f, 0.5f);
        panel.anchoredPosition = new Vector2(-28f, 0f);
        panel.sizeDelta = new Vector2(310f, 126f);
        Image panelImage = panel.gameObject.AddComponent<Image>();
        panelImage.color = new Color(0.055f, 0.075f, 0.09f, 0.92f);
        panelImage.raycastTarget = false;

        headerLabel = CreateText("BATTLE VITALS", panel, 22f, FontStyles.Bold, new Color(1f, 0.83f, 0.5f, 1f));
        headerLabel.rectTransform.anchorMin = new Vector2(0f, 0.65f);
        headerLabel.rectTransform.anchorMax = new Vector2(1f, 1f);
        headerLabel.rectTransform.offsetMin = new Vector2(18f, 0f);
        headerLabel.rectTransform.offsetMax = new Vector2(-18f, -8f);
        headerLabel.alignment = TextAlignmentOptions.Left;

        RectTransform sliderRect = CreateRect("Health Bar", panel);
        sliderRect.anchorMin = new Vector2(0.06f, 0.39f);
        sliderRect.anchorMax = new Vector2(0.94f, 0.61f);
        sliderRect.offsetMin = Vector2.zero;
        sliderRect.offsetMax = Vector2.zero;
        Image barBackground = sliderRect.gameObject.AddComponent<Image>();
        barBackground.color = new Color(0.16f, 0.12f, 0.13f, 1f);
        barBackground.raycastTarget = false;
        healthSlider = sliderRect.gameObject.AddComponent<Slider>();
        healthSlider.minValue = 0f;
        healthSlider.maxValue = MaxHealth;
        healthSlider.value = MaxHealth;
        healthSlider.wholeNumbers = true;
        healthSlider.interactable = false;
        healthSlider.transition = Selectable.Transition.None;
        healthSlider.navigation = new Navigation { mode = Navigation.Mode.None };
        healthSlider.targetGraphic = barBackground;
        healthSlider.direction = Slider.Direction.LeftToRight;

        RectTransform fillArea = CreateRect("Fill Area", sliderRect);
        fillArea.anchorMin = Vector2.zero;
        fillArea.anchorMax = Vector2.one;
        fillArea.offsetMin = new Vector2(3f, 3f);
        fillArea.offsetMax = new Vector2(-3f, -3f);
        RectTransform fill = CreateRect("Fill", fillArea);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        Image fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = new Color(0.72f, 0.13f, 0.13f, 1f);
        fillImage.raycastTarget = false;
        healthSlider.fillRect = fill;

        healthLabel = CreateText("HP 100 / 100", panel, 18f, FontStyles.Bold, Color.white);
        healthLabel.rectTransform.anchorMin = new Vector2(0f, 0f);
        healthLabel.rectTransform.anchorMax = new Vector2(1f, 0.36f);
        healthLabel.rectTransform.offsetMin = new Vector2(12f, 4f);
        healthLabel.rectTransform.offsetMax = new Vector2(-12f, 0f);
        healthLabel.alignment = TextAlignmentOptions.Center;
    }

    private void RefreshHealthHud()
    {
        if (healthSlider != null) healthSlider.value = currentHealth;
        if (healthLabel != null) healthLabel.text = currentHealth > 0 ? $"HP {currentHealth} / {MaxHealth}" : "HP 0 / 100  •  DOWN";
        if (headerLabel != null) headerLabel.text = currentHealth > 0 ? "BATTLE VITALS" : "BATTLE VITALS  •  DOWN";
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        GameObject child = new GameObject(name, typeof(RectTransform));
        child.transform.SetParent(parent, false);
        return child.GetComponent<RectTransform>();
    }

    private static TextMeshProUGUI CreateText(string value, RectTransform parent, float size, FontStyles style, Color color)
    {
        GameObject child = new GameObject(value, typeof(RectTransform), typeof(TextMeshProUGUI));
        child.transform.SetParent(parent, false);
        TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.raycastTarget = false;
        text.enableWordWrapping = false;
        return text;
    }

    private readonly struct TrapPlacement
    {
        public readonly Vector3 Position;
        public readonly Vector3 CellSize;

        public TrapPlacement(Vector3 position, Vector3 cellSize)
        {
            Position = position;
            CellSize = cellSize;
        }
    }

    private readonly struct TrapSheet
    {
        public readonly string ResourcePath;
        public readonly int FrameSize;
        public readonly string DisplayName;

        public TrapSheet(string resourcePath, int frameSize)
        {
            ResourcePath = resourcePath;
            FrameSize = frameSize;
            DisplayName = resourcePath.Substring(resourcePath.LastIndexOf('/') + 1).Replace('_', ' ');
        }
    }
}
