using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

public class ExplorationHUD : MonoBehaviour
{
    public CharacterInventory inventory;
    public Vector2 minimapWorldSize = new Vector2(64f, 38f);
    private playerMovement player;
    private TutorialSignboard[] signs;
    private Texture2D terrain;
    private Bounds worldBounds;
    private bool showMap = true;
    private bool inventoryOpen;
    private float previousTimeScale;
    private int selected = -1;
    private Vector2 scroll;
    private GUIStyle headingStyle, textStyle, smallStyle, slotStyle;

    void Start()
    {
        GameSave.EnsureInstance();
        player = FindFirstObjectByType<playerMovement>();
        signs = FindObjectsByType<TutorialSignboard>(FindObjectsSortMode.None);
        if (inventory == null) inventory = GetComponent<CharacterInventory>();
        BuildTerrainMap();
    }

    void Update()
    {
        if (player == null) player = FindFirstObjectByType<playerMovement>();
        if (Input.GetKeyDown(KeyCode.M)) showMap = !showMap;
        if (inventoryOpen)
        {
            if (Input.GetKeyDown(KeyCode.I) || Input.GetKeyDown(KeyCode.Escape)) CloseInventory();
        }
        else if (Time.timeScale > 0f && Input.GetKeyDown(KeyCode.I)) OpenInventory();
    }

    void OpenInventory()
    {
        if (inventory == null || Time.timeScale <= 0f) return;
        previousTimeScale = Time.timeScale;
        inventoryOpen = true;
        Time.timeScale = 0f;
    }

    void CloseInventory()
    {
        if (!inventoryOpen) return;
        inventoryOpen = false;
        Time.timeScale = previousTimeScale;
    }

    void OnDisable() { CloseInventory(); }
    void OnDestroy() { if (terrain != null) Destroy(terrain); }

    void BuildTerrainMap()
    {
        var maps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        bool found = false;
        foreach (var map in maps)
        {
            if (!map.CompareTag("Ground") || map.cellBounds.size.sqrMagnitude == 0) continue;
            var bounds = new Bounds();
            bounds.SetMinMax(map.CellToWorld(map.cellBounds.min), map.CellToWorld(map.cellBounds.max));
            if (!found) { worldBounds = bounds; found = true; }
            else worldBounds.Encapsulate(bounds);
        }
        if (!found) return;
        worldBounds.Expand(new Vector3(4f, 4f, 0f));
        const int width = 1024;
        int height = Mathf.Clamp(Mathf.CeilToInt(width * worldBounds.size.y / Mathf.Max(1f, worldBounds.size.x)), 64, 512);
        terrain = new Texture2D(width, height, TextureFormat.RGBA32, false);
        terrain.filterMode = FilterMode.Point;
        terrain.wrapMode = TextureWrapMode.Clamp;
        var pixels = new Color32[width * height];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(24, 33, 39, 255);
        foreach (var map in maps)
        {
            if (!map.CompareTag("Ground")) continue;
            foreach (var cell in map.cellBounds.allPositionsWithin)
            {
                if (!map.HasTile(cell)) continue;
                Vector3 a = map.CellToWorld(cell), b = map.CellToWorld(cell + Vector3Int.one);
                int x0 = Mathf.Clamp(Mathf.FloorToInt((a.x - worldBounds.min.x) / worldBounds.size.x * width), 0, width - 1);
                int y0 = Mathf.Clamp(Mathf.FloorToInt((a.y - worldBounds.min.y) / worldBounds.size.y * height), 0, height - 1);
                int x1 = Mathf.Clamp(Mathf.CeilToInt((b.x - worldBounds.min.x) / worldBounds.size.x * width), x0 + 1, width);
                int y1 = Mathf.Clamp(Mathf.CeilToInt((b.y - worldBounds.min.y) / worldBounds.size.y * height), y0 + 1, height);
                for (int y = y0; y < y1; y++)
                    for (int x = x0; x < x1; x++) pixels[y * width + x] = new Color32(149, 165, 120, 255);
            }
        }
        terrain.SetPixels32(pixels);
        terrain.Apply(false, true);
    }

    void InitStyles()
    {
        if (headingStyle != null) return;
        headingStyle = new GUIStyle(GUI.skin.label) { fontSize = 25, fontStyle = FontStyle.Bold };
        textStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true, richText = false };
        smallStyle = new GUIStyle(textStyle) { fontSize = 16 };
        slotStyle = new GUIStyle(GUI.skin.button) { fontSize = 18, wordWrap = true };
        headingStyle.normal.textColor = new Color(1f, 0.87f, 0.58f);
    }

    static void Fill(Rect rect, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    void OnGUI()
    {
        InitStyles();
        Matrix4x4 oldMatrix = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280f * scale) / 2f,
            (Screen.height - 720f * scale) / 2f, 0f), Quaternion.identity, Vector3.one * scale);
        if (!inventoryOpen)
        {
            if (showMap) DrawMinimap();
            if (GUI.Button(new Rect(1040, 242, 220, 36), "Inventory [I]")) OpenInventory();
        }
        else DrawInventory();
        GUI.matrix = oldMatrix;
    }

    void DrawMinimap()
    {
        Fill(new Rect(1010, 16, 250, 216), new Color(0.08f, 0.07f, 0.06f, 0.95f));
        GUI.Label(new Rect(1020, 20, 230, 28), SceneManager.GetActiveScene().name + "  [M]", smallStyle);
        Rect mapRect = new Rect(1020, 52, 230, 145);
        GUI.BeginGroup(mapRect);
        Fill(new Rect(0, 0, mapRect.width, mapRect.height), new Color(0.09f, 0.13f, 0.15f));
        if (player != null)
        {
            Vector2 centre = player.transform.position;
            Vector2 window = new Vector2(Mathf.Max(1f, minimapWorldSize.x), Mathf.Max(1f, minimapWorldSize.y));
            if (terrain != null)
            {
                float x = (worldBounds.min.x - centre.x) / window.x * mapRect.width + mapRect.width / 2f;
                float y = (centre.y - worldBounds.max.y) / window.y * mapRect.height + mapRect.height / 2f;
                GUI.DrawTexture(new Rect(x, y, worldBounds.size.x / window.x * mapRect.width,
                    worldBounds.size.y / window.y * mapRect.height), terrain);
            }
            if (signs != null)
                foreach (var sign in signs)
                {
                    if (sign == null) continue;
                    Vector2 delta = (Vector2)sign.transform.position - centre;
                    Fill(new Rect(mapRect.width / 2f + delta.x / window.x * mapRect.width - 2f,
                        mapRect.height / 2f - delta.y / window.y * mapRect.height - 2f, 5, 5), new Color(1f, 0.75f, 0.3f));
                }
            Fill(new Rect(mapRect.width / 2f - 3f, mapRect.height / 2f - 3f, 7, 7), Color.cyan);
        }
        GUI.EndGroup();
        GUI.Label(new Rect(1020, 201, 230, 24), "Blue: you   Gold: tutorial", smallStyle);
    }

    void DrawInventory()
    {
        Fill(new Rect(130, 85, 1020, 550), new Color(0.07f, 0.065f, 0.055f, 0.98f));
        GUI.Label(new Rect(155, 105, 650, 40), "Traveller's Inventory", headingStyle);
        if (GUI.Button(new Rect(975, 105, 150, 35), "Close [I / Esc]")) { CloseInventory(); return; }
        GUI.Label(new Rect(155, 153, 800, 30), "Coins: " + inventory.Coins + "     Library Tokens: " + inventory.LibraryTokens, textStyle);
        GUI.Label(new Rect(155, 194, 590, 30), "CARDS  -  Select a stack to inspect", smallStyle);
        int count = inventory.Cards.Count;
        scroll = GUI.BeginScrollView(new Rect(155, 235, 590, 365), scroll,
            new Rect(0, 0, 565, Mathf.Max(350, Mathf.CeilToInt(count / 3f) * 120f)));
        for (int i = 0; i < count; i++)
        {
            var stack = inventory.Cards[i];
            var card = inventory.GetCard(stack.id);
            string name = card != null ? card.name : "Unavailable card";
            Rect slot = new Rect((i % 3) * 188, (i / 3) * 120, 178, 108);
            if (GUI.Button(slot, name + "\n\nx" + stack.quantity, slotStyle)) selected = i;
        }
        if (count == 0) GUI.Label(new Rect(10, 15, 530, 80), "Your inventory is empty. Earn cards through your adventures.", textStyle);
        GUI.EndScrollView();
        if (selected >= 0 && selected < count)
        {
            var stack = inventory.Cards[selected];
            var card = inventory.GetCard(stack.id);
            if (card != null)
            {
                GUI.Label(new Rect(785, 235, 325, 65), card.name, headingStyle);
                GUI.Label(new Rect(785, 305, 325, 40), "Mana: " + card.Mana + "   Owned: " + stack.quantity, textStyle);
                GUI.Label(new Rect(785, 365, 325, 205), card.Description, textStyle);
            }
        }
        else GUI.Label(new Rect(785, 250, 310, 100), "Choose a card to view its details.", textStyle);
    }
}
