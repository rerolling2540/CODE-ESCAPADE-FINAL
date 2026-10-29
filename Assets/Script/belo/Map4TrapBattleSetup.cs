using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>Places animated trap-pack hazards that damage the shared player health pool.</summary>
public sealed class Map4TrapBattleSetup : MonoBehaviour
{
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
    }

    private void OnDestroy()
    {
        if (active == this) active = null;
    }

    public void DamagePlayer(int amount)
    {
        var player = FindFirstObjectByType<playerMovement>();
        if (player != null) player.TryTakeDamage(amount);
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
            damageTrigger.Initialize(18);
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
