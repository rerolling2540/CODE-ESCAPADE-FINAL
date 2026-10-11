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
    private Font medievalFont;
    private Vector2 detailScroll;
    private static readonly Color Ink = new Color(0.19f, 0.12f, 0.07f);
    private static readonly Color Brass = new Color(0.72f, 0.52f, 0.23f);
    private static readonly Color Parchment = new Color(0.87f, 0.78f, 0.59f);

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
    void OnDestroy() { if (terrain != null) Destroy(terrain); if (medievalFont != null) Destroy(medievalFont); }

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
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(218, 198, 153, 255);
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
                    for (int x = x0; x < x1; x++) pixels[y * width + x] = new Color32(102, 111, 72, 255);
            }
        }
        terrain.SetPixels32(pixels);
        terrain.Apply(false, true);
    }

    void InitStyles()
    {
        if (headingStyle != null) return;
        var fonts = Font.GetOSInstalledFontNames();
        if (System.Array.IndexOf(fonts, "Georgia") >= 0)
            medievalFont = Font.CreateDynamicFontFromOSFont("Georgia", 24);
        headingStyle = new GUIStyle(GUI.skin.label) { fontSize = 29, fontStyle = FontStyle.Bold, wordWrap = true };
        textStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, wordWrap = true, richText = false };
        smallStyle = new GUIStyle(textStyle) { fontSize = 16 };
        slotStyle = new GUIStyle(textStyle) { alignment = TextAnchor.MiddleCenter, fontSize = 19 };
        foreach (var style in new[] { headingStyle, textStyle, smallStyle, slotStyle })
        {
            if (medievalFont != null) style.font = medievalFont;
            style.normal.textColor = Ink;
        }
    }

    static Rect Inset(Rect rect, float amount)
    {
        return new Rect(rect.x + amount, rect.y + amount, rect.width - amount * 2f, rect.height - amount * 2f);
    }

    static void Frame(Rect rect, bool parchment = true)
    {
        Fill(new Rect(rect.x + 5, rect.y + 7, rect.width, rect.height), new Color(0, 0, 0, 0.45f));
        Fill(rect, new Color(0.17f, 0.095f, 0.045f));
        Fill(Inset(rect, 4), Brass);
        Fill(Inset(rect, 7), new Color(0.29f, 0.18f, 0.085f));
        Fill(Inset(rect, 12), parchment ? Parchment : new Color(0.21f, 0.13f, 0.065f));
        // Subtle horizontal paper fibres and brass corner studs.
        if (parchment)
            for (float y = rect.y + 15; y < rect.yMax - 12; y += 7)
                Fill(new Rect(rect.x + 12, y, rect.width - 24, 1), new Color(0.37f, 0.25f, 0.1f, 0.035f));
        foreach (float x in new[] { rect.x + 5, rect.xMax - 10 })
            foreach (float y in new[] { rect.y + 5, rect.yMax - 10 })
                Fill(new Rect(x, y, 5, 5), new Color(0.95f, 0.78f, 0.4f));
    }

    bool MedievalButton(Rect rect, string label)
    {
        bool hover = rect.Contains(Event.current.mousePosition);
        Fill(rect, Ink);
        Fill(Inset(rect, 2), Brass);
        Fill(Inset(rect, 4), hover ? new Color(0.97f, 0.88f, 0.66f) : Parchment);
        GUI.Label(Inset(rect, 5), label, slotStyle);
        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    static void DrawCardArt(Rect rect, Sprite sprite)
    {
        if (sprite == null) return;
        // Tight atlas packing may not expose a rectangular texture region.
        if (sprite.packed && sprite.packingMode == SpritePackingMode.Tight) return;
        Rect uv = sprite.textureRect;
        Texture2D texture = sprite.texture;
        float ratio = uv.width / Mathf.Max(1f, uv.height);
        float width = Mathf.Min(rect.width, rect.height * ratio);
        float height = width / ratio;
        GUI.DrawTextureWithTexCoords(new Rect(rect.center.x - width / 2f, rect.center.y - height / 2f, width, height),
            texture, new Rect(uv.x / texture.width, uv.y / texture.height, uv.width / texture.width, uv.height / texture.height));
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
            DrawInventoryShortcut();
        }
        else DrawInventory();
        GUI.matrix = oldMatrix;
    }

    void DrawInventoryShortcut()
    {
        // Keep the shortcut compact and clear of the top-right settings controls,
        // even when the minimap is hidden.
        Rect button = new Rect(1195, 635, 60, 60);
        if (MedievalButton(button, "")) OpenInventory();
        Fill(new Rect(1216, 645, 18, 12), Ink);
        Fill(new Rect(1219, 648, 12, 9), Parchment);
        Fill(new Rect(1209, 654, 32, 25), Ink);
        Fill(new Rect(1212, 657, 26, 19), new Color(0.48f, 0.29f, 0.12f));
        Fill(new Rect(1223, 658, 5, 7), Brass);
        GUI.Label(new Rect(1200, 678, 50, 17), "[I]", slotStyle);
    }

    void DrawMinimap()
    {
        Frame(new Rect(16, 16, 310, 260));
        GUI.Label(new Rect(39, 33, 265, 28), "Traveller's Map  [M]", smallStyle);
        Rect mapRect = new Rect(38, 72, 266, 157);
        GUI.BeginGroup(mapRect);
        Fill(new Rect(0, 0, mapRect.width, mapRect.height), Parchment);
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
        GUI.Label(new Rect(38, 239, 270, 24), "Blue: you   Gold: signboards", smallStyle);
    }

    void DrawInventory()
    {
        Fill(new Rect(0, 0, 1280, 720), new Color(0.035f, 0.025f, 0.015f, 0.68f));
        Frame(new Rect(40, 25, 1200, 670));
        GUI.Label(new Rect(70, 52, 760, 48), "The Traveller's Satchel", headingStyle);
        if (MedievalButton(new Rect(1020, 52, 185, 42), "Close [I / Esc]")) { CloseInventory(); return; }
        GUI.Label(new Rect(73, 105, 1050, 32), "Gold Coins: " + inventory.Coins + "     |     Library Tokens: " + inventory.LibraryTokens, textStyle);
        Fill(new Rect(70, 148, 1135, 2), Brass);
        GUI.Label(new Rect(75, 165, 755, 30), "CARD COLLECTION    /    " + inventory.Cards.Count + " stacks", smallStyle);
        Frame(new Rect(850, 163, 355, 478));
        int count = inventory.Cards.Count;
        const int columns = 4;
        int rows = Mathf.Max(3, Mathf.CeilToInt(count / (float)columns));
        scroll = GUI.BeginScrollView(new Rect(70, 205, 765, 440), scroll,
            new Rect(0, 0, 742, rows * 145));
        for (int i = 0; i < Mathf.Max(12, count); i++)
        {
            Rect slot = new Rect((i % columns) * 185, (i / columns) * 145, 173, 133);
            Fill(slot, Ink);
            Fill(Inset(slot, 2), i == selected ? new Color(0.96f, 0.73f, 0.23f) : Brass);
            Fill(Inset(slot, 5), new Color(0.79f, 0.69f, 0.49f));
            if (i >= count)
            {
                GUI.Label(new Rect(slot.x + 10, slot.y + 48, slot.width - 20, 35), "Empty", slotStyle);
                continue;
            }
            var stack = inventory.Cards[i];
            var card = inventory.GetCard(stack.id);
            if (card != null) DrawCardArt(new Rect(slot.x + 45, slot.y + 9, 83, 63), card.Image);
            GUI.Label(new Rect(slot.x + 8, slot.y + 75, slot.width - 16, 32), card != null ? card.name : "Unavailable", slotStyle);
            GUI.Label(new Rect(slot.x + 8, slot.y + 107, slot.width - 16, 22), "Owned: " + stack.quantity, smallStyle);
            if (GUI.Button(slot, GUIContent.none, GUIStyle.none)) { selected = i; detailScroll = Vector2.zero; }
        }
        GUI.EndScrollView();
        if (selected >= 0 && selected < count)
        {
            var stack = inventory.Cards[selected];
            var card = inventory.GetCard(stack.id);
            if (card != null)
            {
                GUI.Label(new Rect(875, 185, 305, 74), card.name, headingStyle);
                DrawCardArt(new Rect(930, 267, 190, 130), card.Image);
                GUI.Label(new Rect(875, 410, 305, 32), "Mana: " + card.Mana + "   |   Owned: " + stack.quantity, textStyle);
                Fill(new Rect(875, 450, 305, 1), Brass);
                string description = card.Description ?? "";
                float height = Mathf.Max(145, textStyle.CalcHeight(new GUIContent(description), 280));
                detailScroll = GUI.BeginScrollView(new Rect(875, 468, 305, 146), detailScroll, new Rect(0, 0, 280, height));
                GUI.Label(new Rect(0, 0, 280, height), description, textStyle);
                GUI.EndScrollView();
            }
        }
        else GUI.Label(new Rect(885, 300, 285, 150), "Select a card from your collection to study its abilities.", textStyle);
        GUI.Label(new Rect(75, 655, 1080, 25), "Select a card to inspect  |  Scroll to browse  |  I / Esc to return to your journey", smallStyle);
    }
}
