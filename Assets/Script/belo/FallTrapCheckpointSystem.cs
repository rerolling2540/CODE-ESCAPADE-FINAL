using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

/// <summary>
/// Map 4 fall recovery. The first player position becomes the start point;
/// reaching marked progress thresholds while standing on Ground activates checkpoints.
/// </summary>
public sealed class FallTrapCheckpointSystem : MonoBehaviour
{
    private static readonly HashSet<string> HazardTileNames = new HashSet<string>
    {
        "main_lev_buildA_2", "main_lev_buildA_3",
        "main_lev_buildA_557", "main_lev_buildA_558",
        "main_lev_buildA_1112", "main_lev_buildA_1113"
    };

    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap trapTilemap;
    [SerializeField, Min(1f)] private float extraFallDistance = 4f;
    [SerializeField, Range(0.1f, 0.9f)] private float firstCheckpointProgress = 0.38f;
    [SerializeField, Range(0.2f, 0.98f)] private float secondCheckpointProgress = 0.72f;
    [SerializeField, Min(0.05f)] private float respawnDelay = 0.35f;

    private playerMovement player;
    private Vector2 startPosition;
    private Vector2 checkpointPosition;
    private float startX;
    private float endX;
    private float fallY;
    private bool hasStart;
    private bool firstCheckpointReached;
    private bool secondCheckpointReached;
    private float respawnAt = -1f;
    private GameObject checkpointMarker;
    private static Sprite markerSprite;

    private void Awake()
    {
        foreach (Tilemap map in GetComponentsInChildren<Tilemap>(true))
        {
            if (groundTilemap == null && (map.name == "gound" || map.name == "ground"))
                groundTilemap = map;
            if (trapTilemap == null && map.name == "trap")
                trapTilemap = map;
        }

        BuildTrapTriggers();

        if (groundTilemap != null)
        {
            Bounds bounds = groundTilemap.localBounds;
            Vector3 min = groundTilemap.transform.TransformPoint(bounds.min);
            Vector3 max = groundTilemap.transform.TransformPoint(bounds.max);
            startX = min.x;
            endX = max.x;
            fallY = min.y - extraFallDistance;
        }
        else
        {
            // Map 4's ground currently extends to around y = -196.
            fallY = -205f;
            Debug.LogWarning("FallTrapCheckpointSystem could not find the Ground Tilemap; using its fallback fall limit.", this);
        }
    }

    private void BuildTrapTriggers()
    {
        if (trapTilemap == null)
        {
            Debug.LogWarning("Map 4 trap Tilemap was not found; falling below the level will still respawn the player.", this);
            return;
        }

        BoundsInt cells = trapTilemap.cellBounds;
        Vector3 cellSize = trapTilemap.cellSize;
        int triggerCount = 0;
        foreach (Vector3Int cell in cells.allPositionsWithin)
        {
            TileBase tile = trapTilemap.GetTile(cell);
            if (tile == null || !HazardTileNames.Contains(tile.name)) continue;

            GameObject trigger = new GameObject("Trap Trigger");
            trigger.transform.SetParent(transform, true);
            trigger.transform.position = trapTilemap.GetCellCenterWorld(cell);
            BoxCollider2D box = trigger.AddComponent<BoxCollider2D>();
            box.size = new Vector2(cellSize.x, cellSize.y);
            // Extend each sensor slightly below the tile so feet standing on the spike row overlap it.
            box.offset = new Vector2(0f, -cellSize.y * 0.25f);
            box.isTrigger = true;
            TrapContact2D contact = trigger.AddComponent<TrapContact2D>();
            contact.Initialize(this);
            triggerCount++;
        }

        if (triggerCount == 0)
            Debug.LogWarning("Map 4 trap Tilemap has no occupied tiles to use as hazards.", trapTilemap);
        else
            Debug.Log($"Map 4 trap is active ({triggerCount} tile triggers).", trapTilemap);
    }

    public void HandleTrapContact(Collider2D other)
    {
        if (PlayerDeathFlow.IsActive) return;
        if (respawnAt >= 0f) return;
        playerMovement victim = other != null ? other.GetComponentInParent<playerMovement>() : null;
        if (victim == null) return;

        if (player == null) RegisterStart(victim);
        if (victim != player) return;
        respawnAt = Time.unscaledTime + respawnDelay;
        Debug.Log("Player touched a Map 4 trap; returning to the latest checkpoint.", player);
    }

    private void Update()
    {
        if (PlayerDeathFlow.IsActive) return;
        if (player == null)
        {
            player = FindFirstObjectByType<playerMovement>();
            if (player != null) RegisterStart(player);
        }
        if (player == null || !hasStart) return;

        if (respawnAt >= 0f)
        {
            if (Time.unscaledTime >= respawnAt) Respawn();
            return;
        }

        float firstX = Mathf.Lerp(Mathf.Max(startX, startPosition.x), endX, firstCheckpointProgress);
        float secondX = Mathf.Lerp(Mathf.Max(startX, startPosition.x), endX, secondCheckpointProgress);

        if (!firstCheckpointReached && player.transform.position.x >= firstX && IsStandingOnGround())
            ActivateCheckpoint(1);
        if (firstCheckpointReached && !secondCheckpointReached &&
            player.transform.position.x >= secondX && IsStandingOnGround())
            ActivateCheckpoint(2);

        if (player.transform.position.y < fallY && respawnAt < 0f)
            respawnAt = Time.unscaledTime + respawnDelay;
    }

    private void RegisterStart(playerMovement movement)
    {
        player = movement;
        startPosition = movement.transform.position;
        checkpointPosition = startPosition;
        var saved = SaveSlots.Read(SaveSlots.Active);
        if (saved != null && saved.hasCheckpoint && saved.checkpointScene == gameObject.scene.name)
            checkpointPosition = saved.checkpointPosition;
        hasStart = true;
    }

    private bool IsStandingOnGround()
    {
        Collider2D body = player.GetComponent<Collider2D>();
        if (body == null) return false;
        Vector2 origin = new Vector2(body.bounds.center.x, body.bounds.min.y + 0.08f);
        RaycastHit2D[] hits = Physics2D.RaycastAll(origin, Vector2.down, 0.22f);
        foreach (RaycastHit2D hit in hits)
        {
            if (hit.collider != null && hit.collider.gameObject.CompareTag("Ground")) return true;
        }
        return false;
    }

    private void ActivateCheckpoint(int number)
    {
        checkpointPosition = player.transform.position;
        PlayerDeathFlow.RecordCheckpoint(player.transform.position);
        if (number == 1) firstCheckpointReached = true;
        else secondCheckpointReached = true;
        ShowCheckpointMarker(checkpointPosition);
        Debug.Log($"Map 4 checkpoint {number} activated.", player);
    }

    private void ShowCheckpointMarker(Vector2 position)
    {
        if (checkpointMarker != null) Destroy(checkpointMarker);
        checkpointMarker = new GameObject("Active Checkpoint");
        checkpointMarker.transform.position = position + Vector2.up * 1.55f;
        SpriteRenderer flag = checkpointMarker.AddComponent<SpriteRenderer>();
        flag.sprite = GetMarkerSprite();
        flag.color = new Color(0.2f, 0.95f, 1f, 0.95f);
        flag.sortingOrder = 20;
        checkpointMarker.transform.localScale = new Vector3(0.55f, 0.4f, 1f);
    }

    private static Sprite GetMarkerSprite()
    {
        if (markerSprite != null) return markerSprite;
        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "Checkpoint Marker Pixel";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        markerSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return markerSprite;
    }

    private void Respawn()
    {
        if (player == null) return;
        Vector2 destination = checkpointPosition;
        Rigidbody2D body = player.rb != null ? player.rb : player.GetComponent<Rigidbody2D>();
        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.position = destination;
        }
        player.transform.position = destination;
        respawnAt = -1f;
    }
}
