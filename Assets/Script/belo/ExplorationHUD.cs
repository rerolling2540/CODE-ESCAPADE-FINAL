
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

public class ExplorationHUD : MonoBehaviour
{
    public CharacterInventory inventory;
    public Vector2 minimapWorldSize = new Vector2(64f, 38f);

    private playerMovement player;
    private TutorialSignboard[] signs;
    private CaveEnemyEncounter[] enemies;
    private Npc_Talk[] questNpcs;
    private GUIStyle questMarkerStyle;
    private Texture2D terrain;
    private Bounds worldBounds;

    private bool showMap = true;
    private Vector2 minimapPosition = new Vector2(918f,16f);
    private Vector2 mapDragOffset;
    private bool draggingMap;
    private bool resizingMap;
    private float minimapScale=1f;
    private Vector2 resizeStart;
    private float resizeStartScale;
    private Texture2D resizeIcon;
    private bool inventoryOpen;
    private bool characterInfoOpen;
    private bool showingDraftCards;
    private CharacterInfoData characterInfo;
    private CardDraftCatalog cardDraftCatalog;
    public bool IsMapVisible => showMap && !inventoryOpen && !GameControlsGuide.MapHidden &&
        !SceneTransitionFader.IsTransitioning && CharacterAlmanac.IsExplorationScene(SceneManager.GetActiveScene().name);
    private float previousTimeScale = 1f;

    private int selected = -1;
    private Vector2 scroll;
    private Vector2 detailScroll;

    private GUIStyle headingStyle, textStyle, smallStyle, slotStyle;
    private GUIStyle mapTitleStyle, mapLegendStyle;
    private Font medievalFont;

    private static readonly Color Ink = new Color(0.08f, 0.035f, 0.015f, 1f);
    private static readonly Color Brass = new Color(0.72f, 0.52f, 0.23f);
    private static readonly Color Parchment = new Color(0.87f, 0.78f, 0.59f);

    void Start()
    {
        characterInfo=Resources.Load<CharacterInfoData>("UI/CharacterInfo");
        cardDraftCatalog=Resources.Load<CardDraftCatalog>("CardDraftCatalog");
        resizeIcon=CreateResizeIcon();
        minimapScale=Mathf.Clamp(PlayerPrefs.GetFloat("CodeEscapade.Minimap.Scale",1f),0.7f,2.2f);
        minimapPosition = new Vector2(PlayerPrefs.GetFloat("CodeEscapade.Minimap.X",918f),PlayerPrefs.GetFloat("CodeEscapade.Minimap.Y",16f));
        ClampMinimap();
        if (GetComponent<CharacterAlmanac>() == null) gameObject.AddComponent<CharacterAlmanac>();
        if (FindFirstObjectByType<ElfQuestTracker>() == null)
            new GameObject("Elf Quest Tracker").AddComponent<ElfQuestTracker>();
        GameSave.EnsureInstance();

        player = FindFirstObjectByType<playerMovement>();
        signs = FindObjectsByType<TutorialSignboard>(FindObjectsSortMode.None);
        enemies = FindObjectsByType<CaveEnemyEncounter>(FindObjectsSortMode.None);
        questNpcs = FindObjectsByType<Npc_Talk>(FindObjectsSortMode.None);

        if (inventory == null)
            inventory = GetComponent<CharacterInventory>();

        BuildTerrainMap();
    }

    void Update()
    {
        if (player == null)
            player = FindFirstObjectByType<playerMovement>();
        UpdateMinimapDrag();
        if (GameControlsGuide.GameplayOverlayHidden) return;

        if (!inventoryOpen && !GameControlsGuide.MapHidden && Input.GetKeyDown(KeyCode.M))
            showMap = !showMap;

        if (inventoryOpen)
        {
            if (Input.GetKeyDown(KeyCode.I) ||
                Input.GetKeyDown(KeyCode.Escape))
            {
                CloseInventory();
            }
        }
        else if (Time.timeScale > 0f && Input.GetKeyDown(KeyCode.I))
        {
            OpenInventory();
        }
    }

    public void ToggleInventoryFromButton()
    {
        if (inventoryOpen)
            CloseInventory();
        else
            OpenInventory();
    }

    public void OpenInventoryFromButton()
    {
        OpenInventory();
    }

    public void CloseInventoryFromButton()
    {
        CloseInventory();
    }

    void OpenInventory()
    {
        if (GameControlsGuide.MapHidden) return;
        if (inventory == null || inventoryOpen)
            return;

        if (Time.timeScale <= 0f)
            return;

        previousTimeScale = Time.timeScale;
        inventoryOpen = true;
        Time.timeScale = 0f;
    }

    void CloseInventory()
    {
        if (!inventoryOpen)
            return;

        inventoryOpen = false;
        Time.timeScale = previousTimeScale;
    }

    void OnDisable()
    {
        EndMapDrag();
        CloseInventory();
    }

    void OnDestroy()
    {
        if (terrain != null)
            Destroy(terrain);

        if (medievalFont != null)
            Destroy(medievalFont);
    }

    void BuildTerrainMap()
    {
        var maps = FindObjectsByType<Tilemap>(FindObjectsSortMode.None);
        bool found = false;

        foreach (var map in maps)
        {
            if (!map.CompareTag("Ground") ||
                map.cellBounds.size.sqrMagnitude == 0)
                continue;

            var bounds = new Bounds();

            bounds.SetMinMax(
                map.CellToWorld(map.cellBounds.min),
                map.CellToWorld(map.cellBounds.max)
            );

            if (!found)
            {
                worldBounds = bounds;
                found = true;
            }
            else
            {
                worldBounds.Encapsulate(bounds);
            }
        }

        if (!found)
            return;

        worldBounds.Expand(new Vector3(4f, 4f, 0f));

        const int width = 1024;

        int height = Mathf.Clamp(
            Mathf.CeilToInt(
                width * worldBounds.size.y /
                Mathf.Max(1f, worldBounds.size.x)
            ),
            64,
            512
        );

        terrain = new Texture2D(width, height, TextureFormat.RGBA32, false);
        terrain.filterMode = FilterMode.Point;
        terrain.wrapMode = TextureWrapMode.Clamp;

        var pixels = new Color32[width * height];

        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(218, 198, 153, 255);

        foreach (var map in maps)
        {
            if (!map.CompareTag("Ground"))
                continue;

            foreach (var cell in map.cellBounds.allPositionsWithin)
            {
                if (!map.HasTile(cell))
                    continue;

                Vector3 a = map.CellToWorld(cell);
                Vector3 b = map.CellToWorld(cell + Vector3Int.one);

                int x0 = Mathf.Clamp(
                    Mathf.FloorToInt(
                        (a.x - worldBounds.min.x) /
                        worldBounds.size.x * width
                    ),
                    0, width - 1
                );

                int y0 = Mathf.Clamp(
                    Mathf.FloorToInt(
                        (a.y - worldBounds.min.y) /
                        worldBounds.size.y * height
                    ),
                    0, height - 1
                );

                int x1 = Mathf.Clamp(
                    Mathf.CeilToInt(
                        (b.x - worldBounds.min.x) /
                        worldBounds.size.x * width
                    ),
                    x0 + 1, width
                );

                int y1 = Mathf.Clamp(
                    Mathf.CeilToInt(
                        (b.y - worldBounds.min.y) /
                        worldBounds.size.y * height
                    ),
                    y0 + 1, height
                );

                for (int y = y0; y < y1; y++)
                {
                    for (int x = x0; x < x1; x++)
                        pixels[y * width + x] =
                            new Color32(102, 111, 72, 255);
                }
            }
        }

        terrain.SetPixels32(pixels);
        terrain.Apply(false, true);
    }

    void InitStyles()
    {
        if (headingStyle != null)
            return;

        var fonts = Font.GetOSInstalledFontNames();

        if (System.Array.IndexOf(fonts, "Georgia") >= 0)
            medievalFont = Font.CreateDynamicFontFromOSFont("Georgia", 24);

        headingStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 29,
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };

        textStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 20,
            wordWrap = true,
            richText = false
        };

        smallStyle = new GUIStyle(textStyle)
        {
            fontSize = 16
        };

        slotStyle = new GUIStyle(textStyle)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 19
        };

        foreach (var style in new[]
        {
            headingStyle, textStyle, smallStyle, slotStyle
        })
        {
            if (medievalFont != null)
                style.font = medievalFont;

            style.normal.textColor = Ink; UiTextColors.Stable(style);
        }

        // MAP-ONLY TEXT STYLES
        mapTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 19,
            fontStyle = FontStyle.Bold,
            wordWrap = false,
            alignment = TextAnchor.MiddleLeft
        };

        mapLegendStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold,
            wordWrap = false,
            alignment = TextAnchor.MiddleLeft
        };

        if (medievalFont != null)
        {
            mapTitleStyle.font = medievalFont;
            mapLegendStyle.font = medievalFont;
        }

        // Dark text with a light outline for extra readability
        mapTitleStyle.normal.textColor = new Color(0.06f, 0.025f, 0.005f, 1f); UiTextColors.Stable(mapTitleStyle);
        mapLegendStyle.normal.textColor = new Color(0.06f, 0.025f, 0.005f, 1f); UiTextColors.Stable(mapLegendStyle);

        mapTitleStyle.hover.textColor = mapTitleStyle.normal.textColor;
        mapLegendStyle.hover.textColor = mapLegendStyle.normal.textColor;
    }

    static Rect Inset(Rect rect, float amount)
    {
        return new Rect(
            rect.x + amount,
            rect.y + amount,
            rect.width - amount * 2f,
            rect.height - amount * 2f
        );
    }

    static void Frame(Rect rect, bool parchment = true)
    {
        Fill(
            new Rect(rect.x + 5, rect.y + 7, rect.width, rect.height),
            new Color(0, 0, 0, 0.45f)
        );

        Fill(rect, new Color(0.17f, 0.095f, 0.045f));
        Fill(Inset(rect, 4), Brass);
        Fill(Inset(rect, 7), new Color(0.29f, 0.18f, 0.085f));

        Fill(
            Inset(rect, 12),
            parchment ? Parchment : new Color(0.21f, 0.13f, 0.065f)
        );

        if (parchment)
        {
            for (float y = rect.y + 15; y < rect.yMax - 12; y += 7)
            {
                Fill(
                    new Rect(rect.x + 12, y, rect.width - 24, 1),
                    new Color(0.37f, 0.25f, 0.1f, 0.035f)
                );
            }
        }

        foreach (float x in new[] { rect.x + 5, rect.xMax - 10 })
        {
            foreach (float y in new[] { rect.y + 5, rect.yMax - 10 })
            {
                Fill(
                    new Rect(x, y, 5, 5),
                    new Color(0.95f, 0.78f, 0.4f)
                );
            }
        }
    }

    bool MedievalButton(Rect rect, string label)
    {
        bool hover = rect.Contains(Event.current.mousePosition);

        Fill(rect, Ink);
        Fill(Inset(rect, 2), Brass);

        Fill(
            Inset(rect, 4),
            hover
                ? new Color(0.97f, 0.88f, 0.66f)
                : Parchment
        );

        GUI.Label(Inset(rect, 5), label, slotStyle);

        return GUI.Button(rect, GUIContent.none, GUIStyle.none);
    }

    static void DrawCardArt(Rect rect, Sprite sprite)
    {
        if (sprite == null)
            return;

        if (sprite.packed &&
            sprite.packingMode == SpritePackingMode.Tight)
            return;

        Rect uv = sprite.textureRect;
        Texture2D texture = sprite.texture;

        float ratio = uv.width / Mathf.Max(1f, uv.height);
        float width = Mathf.Min(rect.width, rect.height * ratio);
        float height = width / ratio;

        GUI.DrawTextureWithTexCoords(
            new Rect(
                rect.center.x - width / 2f,
                rect.center.y - height / 2f,
                width, height
            ),
            texture,
            new Rect(
                uv.x / texture.width,
                uv.y / texture.height,
                uv.width / texture.width,
                uv.height / texture.height
            )
        );
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
        if (GameControlsGuide.GameplayOverlayHidden) { EndMapDrag(); return; }
        InitStyles();

        Matrix4x4 oldMatrix = GUI.matrix;

        float scale = Mathf.Min(
            Screen.width / 1280f,
            Screen.height / 720f
        );

        GUI.matrix = Matrix4x4.TRS(
            new Vector3(
                (Screen.width - 1280f * scale) / 2f,
                (Screen.height - 720f * scale) / 2f,
                0f
            ),
            Quaternion.identity,
            Vector3.one * scale
        );

        if (!inventoryOpen)
        {
            if (IsMapVisible)
                DrawMinimap();
            else EndMapDrag();
        }
        else
        {
            EndMapDrag();
            DrawInventory();
        }

        GUI.matrix = oldMatrix;
    }

    void DrawMinimap()
    {
        Matrix4x4 mapMatrix=GUI.matrix;
        GUI.matrix=mapMatrix*Matrix4x4.TRS(minimapPosition,Quaternion.identity,Vector3.one*minimapScale);
        float mapX = 0, mapY = 0;
        Frame(new Rect(mapX, mapY, 250, 202));

        // Larger map title only
        GUI.Label(
            new Rect(mapX + 16, mapY + 11, 218, 30),
            "Traveller's Map  [M]",
            mapTitleStyle
        );

        Rect mapRect = new Rect(mapX + 16, mapY + 46, 218, 128);

        GUI.BeginGroup(mapRect);

        Fill(
            new Rect(0, 0, mapRect.width, mapRect.height),
            Parchment
        );

        if (player != null)
        {
            Vector2 centre = player.transform.position;

            Vector2 window = new Vector2(
                Mathf.Max(1f, minimapWorldSize.x),
                Mathf.Max(1f, minimapWorldSize.y)
            );

            if (terrain != null)
            {
                float x =
                    (worldBounds.min.x - centre.x) / window.x *
                    mapRect.width + mapRect.width / 2f;

                float y =
                    (centre.y - worldBounds.max.y) / window.y *
                    mapRect.height + mapRect.height / 2f;

                GUI.DrawTexture(
                    new Rect(
                        x, y,
                        worldBounds.size.x / window.x * mapRect.width,
                        worldBounds.size.y / window.y * mapRect.height
                    ),
                    terrain
                );
            }

            if (signs != null)
            {
                foreach (var sign in signs)
                {
                    if (sign == null)
                        continue;

                    Vector2 delta =
                        (Vector2)sign.transform.position - centre;

                    Fill(
                        new Rect(
                            mapRect.width / 2f +
                            delta.x / window.x * mapRect.width - 2f,
                            mapRect.height / 2f -
                            delta.y / window.y * mapRect.height - 2f,
                            5, 5
                        ),
                        new Color(1f, 0.75f, 0.3f)
                    );
                }
            }

            if (enemies != null)
            {
                foreach (var encounter in enemies)
                {
                    if (encounter == null || !encounter.isActiveAndEnabled)
                        continue;

                    Vector2 delta = (Vector2)encounter.transform.position - centre;
                    Vector2 point = new Vector2(
                        mapRect.width / 2f + delta.x / window.x * mapRect.width,
                        mapRect.height / 2f - delta.y / window.y * mapRect.height
                    );
                    Rect marker = new Rect(point.x - 9f, point.y - 9f, 18f, 18f);
                    Fill(marker, new Color(0.65f, 0.12f, 0.1f));

                    Sprite portrait = encounter.enemy != null ? encounter.enemy.Image : null;
                    if (portrait != null)
                    {
                        // Use only this sprite's region of the enemy sprite sheet.
                        Rect region = portrait.textureRect;
                        float scale = 16f / Mathf.Max(region.width, region.height);
                        Rect icon = new Rect(
                            point.x - region.width * scale / 2f,
                            point.y - region.height * scale / 2f,
                            region.width * scale, region.height * scale
                        );
                        Rect uv = new Rect(
                            region.x / portrait.texture.width,
                            region.y / portrait.texture.height,
                            region.width / portrait.texture.width,
                            region.height / portrait.texture.height
                        );
                        GUI.DrawTextureWithTexCoords(icon, portrait.texture, uv);
                    }
                }
            }

            DrawQuestNpcs(mapRect, centre, window);

            Fill(
                new Rect(
                    mapRect.width / 2f - 3f,
                    mapRect.height / 2f - 3f,
                    7, 7
                ),
                Color.cyan
            );
        }

        GUI.EndGroup();
        float handle=26f/minimapScale;
        var grip=new Rect(250-handle,202+2f/minimapScale,handle,handle);
        if(resizeIcon!=null)GUI.DrawTexture(grip,resizeIcon,ScaleMode.ScaleToFit,true);
        GUI.matrix=mapMatrix;

        // Larger, darker legend only
        //GUI.Label(
            //new Rect(38, 235, 280, 29),
           // "Blue: you   Gold: signboards",
            //mapLegendStyle
        //);
    }

    void ClampMinimap()
    {
        minimapPosition.x=Mathf.Clamp(minimapPosition.x,0f,1280f-250f*minimapScale);
        minimapPosition.y=Mathf.Clamp(minimapPosition.y,0f,720f-202f*minimapScale-30f);
    }

    static Texture2D CreateResizeIcon()
    {
        const int size=32;
        var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){filterMode=FilterMode.Point};
        var pixels=new Color[size*size];
        System.Func<float,float,bool> arrow=(x,y)=>
            (y>=5&&y<=10&&Mathf.Abs(x)<=2)||(y>=9&&y<=14&&Mathf.Abs(x)<=14-y);
        System.Func<int,int,bool> shape=(x,y)=>{
            float a=x-15.5f,b=y-15.5f;
            return arrow(a,b)||arrow(-a,-b)||arrow(b,-a)||arrow(-b,a);
        };
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            bool inside=shape(x,y),edge=false;
            for(int dy=-1;dy<=1;dy++)for(int dx=-1;dx<=1;dx++)edge|=shape(x+dx,y+dy);
            pixels[y*size+x]=inside?(y%8<2?new Color(.45f,.88f,.8f):new Color(.18f,.67f,.6f)):
                edge?new Color(.035f,.09f,.09f,1):Color.clear;
        }
        texture.SetPixels(pixels);texture.Apply();return texture;
    }

    void UpdateMinimapDrag()
    {
        var mouse=UnityEngine.InputSystem.Mouse.current;
        if(!IsMapVisible||mouse==null) { EndMapDrag(); return; }
        float scale=Mathf.Min(Screen.width/1280f,Screen.height/720f);
        Vector2 screenPoint=mouse.position.ReadValue();
        Vector2 point=new Vector2(
            (screenPoint.x-(Screen.width-1280f*scale)/2f)/scale,
            (Screen.height-screenPoint.y-(Screen.height-720f*scale)/2f)/scale);
        var mapBounds=new Rect(minimapPosition.x,minimapPosition.y,250*minimapScale,202*minimapScale);
        var handle=new Rect(mapBounds.xMax-28,mapBounds.yMax,28,30);
        if(mouse.leftButton.wasPressedThisFrame&&(mapBounds.Contains(point)||handle.Contains(point)))
        {
            resizingMap=handle.Contains(point);draggingMap=!resizingMap;
            resizeStart=point;resizeStartScale=minimapScale;mapDragOffset=point-minimapPosition;
        }
        if(resizingMap&&mouse.leftButton.isPressed)
        {
            Vector2 delta=point-resizeStart;
            float maximum=Mathf.Min(2.2f,(1280f-minimapPosition.x)/250f,(720f-minimapPosition.y-30f)/202f);
            minimapScale=Mathf.Clamp(resizeStartScale+Vector2.Dot(delta,new Vector2(250,202))/(250f*250f+202f*202f),0.7f,maximum);
            ClampMinimap();
        }
        if(draggingMap&&mouse.leftButton.isPressed)
        {
            minimapPosition=point-mapDragOffset; ClampMinimap();
        }
        if((draggingMap||resizingMap)&&!mouse.leftButton.isPressed) EndMapDrag();
    }

    void EndMapDrag()
    {
        if(!draggingMap&&!resizingMap)return;
        draggingMap=resizingMap=false;
        PlayerPrefs.SetFloat("CodeEscapade.Minimap.Scale",minimapScale);
        PlayerPrefs.SetFloat("CodeEscapade.Minimap.X",minimapPosition.x);
        PlayerPrefs.SetFloat("CodeEscapade.Minimap.Y",minimapPosition.y);
        PlayerPrefs.Save();
    }

    void DrawQuestNpcs(Rect mapRect, Vector2 centre, Vector2 window)
    {
        if (questNpcs == null) return;
        var saved = SaveSlots.Read(SaveSlots.Active);
        int stage = saved != null ? saved.elfQuestStage : 0;
        bool highlighted = stage == 0 || ElfQuestProgress.Ready(saved);
        Color markerColor = stage >= 4 ? new Color(0.4f, 0.55f, 0.4f) :
            highlighted ? new Color(1f, 0.78f, 0.12f) : new Color(0.4f, 0.65f, 0.85f);
        if (questMarkerStyle == null)
            questMarkerStyle = new GUIStyle(GUI.skin.label) {
                fontSize = 20, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter, padding = new RectOffset()
            };

        foreach (var npc in questNpcs)
        {
            // Talking is disabled outside interaction range; the map icon stays visible.
            if (npc == null || !npc.offersElfQuest || !npc.gameObject.activeInHierarchy) continue;
            Vector2 delta = (Vector2)npc.transform.position - centre;
            Vector2 point = new Vector2(
                mapRect.width / 2f + delta.x / window.x * mapRect.width,
                mapRect.height / 2f - delta.y / window.y * mapRect.height
            );
            if (point.x < 0 || point.x > mapRect.width || point.y < 0 || point.y > mapRect.height) continue;
            // Keep the portrait and quest badge readable at the map's edges.
            point.x = Mathf.Clamp(point.x, 12f, mapRect.width - 22f);
            point.y = Mathf.Clamp(point.y, 24f, mapRect.height - 12f);
            float pulse = highlighted ? 0.7f + 0.3f * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f)) : 0.65f;
            Color glow = markerColor;
            glow.a = pulse;
            Fill(new Rect(point.x - 12f, point.y - 12f, 24f, 24f), glow);
            Fill(new Rect(point.x - 10f, point.y - 10f, 20f, 20f), Ink);

            Sprite portrait = null;
            if (npc.dialogueSO != null && npc.dialogueSO.lines != null)
                foreach (var line in npc.dialogueSO.lines)
                    if (line.speaker != null && line.speaker.portrait != null)
                    { portrait = line.speaker.portrait; break; }
            if (portrait == null)
            {
                var renderer = npc.GetComponentInChildren<SpriteRenderer>();
                if (renderer != null) portrait = renderer.sprite;
            }
            if (portrait != null)
            {
                Rect region = portrait.textureRect;
                float scale = 18f / Mathf.Max(region.width, region.height);
                GUI.DrawTextureWithTexCoords(
                    new Rect(point.x - region.width * scale / 2f, point.y - region.height * scale / 2f,
                        region.width * scale, region.height * scale), portrait.texture,
                    new Rect(region.x / portrait.texture.width, region.y / portrait.texture.height,
                        region.width / portrait.texture.width, region.height / portrait.texture.height));
            }
            if (stage < 4)
            {
                Rect badge = new Rect(point.x + 4f, point.y - 24f, 18f, 22f);
                Fill(badge, Ink);
                questMarkerStyle.normal.textColor = markerColor; UiTextColors.Stable(questMarkerStyle);
                GUI.Label(badge, new GUIContent(stage == 0 ? "!" : "?",
                    stage == 0 ? "Quest available" : highlighted ? "Reward ready" : "Quest accepted"), questMarkerStyle);
            }
        }
    }

    void DrawCharacterInfo()
    {
        Fill(new Rect(0,0,1280,720),new Color(0.035f,0.025f,0.015f,0.68f));
        Frame(new Rect(40,25,1200,670));
        GUI.Label(new Rect(70,52,680,48),"Character Information",headingStyle);
        if(MedievalButton(new Rect(795,52,210,42),"Card Collection"))characterInfoOpen=false;
        if(MedievalButton(new Rect(1020,52,185,42),"Close [I / Esc]")){CloseInventory();return;}
        if(characterInfo==null){GUI.Label(new Rect(80,160,800,50),"Character data unavailable.",textStyle);return;}
        int index=Mathf.Clamp(PlayerPrefs.GetInt("SelectedCharacter",0),0,2);
        string[] titles={"WARRIOR","ARCHER","MAGE"};
        string[] languages={"Java","C#","Python"};
        var stats=FindFirstObjectByType<PlayerStats>();
        int level=PlayerProgression.Level;
        var heroClass=stats!=null&&stats.HeroClass!=null?stats.HeroClass:characterInfo.battleClass;
        var hero=characterInfo.heroes[index];
        Frame(new Rect(75,145,360,470));
        if(characterInfo.portraits[index]!=null)GUI.DrawTexture(new Rect(100,175,310,330),characterInfo.portraits[index],ScaleMode.ScaleToFit,true);
        GUI.Label(new Rect(110,540,300,45),titles[index],headingStyle);
        GUI.Label(new Rect(480,155,680,50),titles[index]+"  /  LEVEL "+level,headingStyle);
        Fill(new Rect(480,218,700,2),Brass);
        string details="XP: "+PlayerProgression.Experience+" / "+PlayerProgression.RequiredExperience(level)+"\n\nHealth: "+PlayerHealth.CurrentHealth+" / "+PlayerHealth.MaxHealth;
        if(heroClass!=null)details+="\n\nAttack Power: "+heroClass.GetAttackPowerForLevel(level)+"\n\nAttack Bonus: "+heroClass.GetAttackBonusForLevel(level)+"\n\nDefense: "+heroClass.GetDefenseForLevel(level);
        details+="\n\nStarting Deck: "+(hero!=null&&hero.Deck!=null?hero.Deck.Count:0)+" cards";
        GUI.Label(new Rect(485,245,680,370),details,textStyle);
    }

    void DrawInventory()
    {
        if(characterInfoOpen){DrawCharacterInfo();return;}
        if (inventory == null && cardDraftCatalog == null)
        {
            GUI.Label(
                new Rect(50, 50, 500, 50),
                "CharacterInventory is not assigned."
            );
            return;
        }

        Fill(
            new Rect(0, 0, 1280, 720),
            new Color(0.035f, 0.025f, 0.015f, 0.68f)
        );

        Frame(new Rect(40, 25, 1200, 670));
        if(MedievalButton(new Rect(795,52,210,42),"Character Info"))characterInfoOpen=true;
        bool canSwitchCardView = !showingDraftCards || inventory != null;
        if(canSwitchCardView && MedievalButton(new Rect(570,52,210,42),showingDraftCards?"Owned Cards":"Class Card Library"))
        {
            showingDraftCards=!showingDraftCards;
            selected=-1;
            scroll=Vector2.zero;
            detailScroll=Vector2.zero;
        }

        GUI.Label(
            new Rect(70, 52, 760, 48),
            "The Traveller's Satchel",
            headingStyle
        );

        if (MedievalButton(
            new Rect(1020, 52, 185, 42),
            "Close [I / Esc]"
        ))
        {
            CloseInventory();
            return;
        }

        GUI.Label(
            new Rect(73, 105, 1050, 32),
            inventory == null ? "Class card library. Purchase cards from the Syntax Merchant to add them to your collection." :
            "Coins: " + inventory.Coins + "   |   Tokens: " + inventory.LibraryTokens +
            "   |   Shards: " + inventory.SyntaxShards + "   |   Scraps: " + inventory.LowLogicScraps,
            textStyle
        );

        Fill(new Rect(70, 148, 1135, 2), Brass);

        int selectedCharacter = Mathf.Clamp(PlayerPrefs.GetInt("SelectedCharacter", 0), 0, 2);
        string[] characterNames = { "WARRIOR · JAVA", "ARCHER · C#", "MAGE · PYTHON" };
        var draftCards = cardDraftCatalog != null ? cardDraftCatalog.ForCharacter(selectedCharacter) : null;
        int ownedCount = inventory != null ? inventory.Cards.Count : 0;
        int count = showingDraftCards ? (draftCards != null ? draftCards.Count : 0) : ownedCount;

        GUI.Label(
            new Rect(75, 165, 755, 30),
            showingDraftCards
                ? characterNames[selectedCharacter] + " CLASS LIBRARY    /    " + count + " cards · AVAILABLE FROM THE SYNTAX MERCHANT"
                : "CARD COLLECTION    /    " + ownedCount + " stacks",
            smallStyle
        );

        if (showingDraftCards && draftCards == null)
        {
            GUI.Label(new Rect(95, 250, 700, 80), "The class card library is unavailable.", textStyle);
        }

        Frame(new Rect(850, 163, 355, 478));

        const int columns = 4;

        int rows = Mathf.Max(
            3,
            Mathf.CeilToInt(count / (float)columns)
        );

        scroll = GUI.BeginScrollView(
            new Rect(70, 205, 765, 440),
            scroll,
            new Rect(0, 0, 742, rows * 145)
        );

        for (int i = 0; i < Mathf.Max(12, count); i++)
        {
            Rect slot = new Rect(
                (i % columns) * 185,
                (i / columns) * 145,
                173, 133
            );

            Fill(slot, Ink);

            Fill(
                Inset(slot, 2),
                i == selected
                    ? new Color(0.96f, 0.73f, 0.23f)
                    : Brass
            );

            Fill(Inset(slot, 5), new Color(0.79f, 0.69f, 0.49f));

            if (i >= count)
            {
                GUI.Label(
                    new Rect(
                        slot.x + 10, slot.y + 48,
                        slot.width - 20, 35
                    ),
                    "Empty",
                    slotStyle
                );

                continue;
            }

            CharacterInventory.Stack stack = null;
            CardData card;
            if (showingDraftCards)
            {
                if (draftCards == null || i >= draftCards.Count) continue;
                card = draftCards[i];
            }
            else
            {
                stack = inventory.Cards[i];
                card = inventory.GetCard(stack.id);
            }

            if (card != null)
            {
                DrawCardArt(
                    new Rect(slot.x + 45, slot.y + 9, 83, 63),
                    card.Image
                );
            }

            GUI.Label(
                new Rect(
                    slot.x + 8, slot.y + 75,
                    slot.width - 16, 32
                ),
                card != null ? card.name : "Unavailable",
                slotStyle
            );

            GUI.Label(
                new Rect(
                    slot.x + 8, slot.y + 107,
                    slot.width - 16, 22
                ),
                showingDraftCards ? "NOT OWNED · MERCHANT" :
                    "Owned: " + stack.quantity + "  |  Lv " + inventory.GetLevel(stack.id),
                smallStyle
            );

            if (GUI.Button(slot, GUIContent.none, GUIStyle.none))
            {
                selected = i;
                detailScroll = Vector2.zero;
            }
        }

        GUI.EndScrollView();

        if (selected >= 0 && selected < count)
        {
            CharacterInventory.Stack stack = null;
            CardData card;
            int cardLevel = 1;
            int quantity = 0;
            if (showingDraftCards)
            {
                card = draftCards != null ? draftCards[selected] : null;
            }
            else
            {
                stack = inventory.Cards[selected];
                card = inventory.GetCard(stack.id);
                cardLevel = inventory.GetLevel(stack.id);
                quantity = stack.quantity;
            }

            if (card != null)
            {
                GUI.Label(
                    new Rect(875, 185, 305, 74),
                    showingDraftCards ? card.name + "  [LIBRARY]" : card.name + "  [Lv " + cardLevel + "]",
                    headingStyle
                );

                DrawCardArt(
                    new Rect(930, 267, 190, 130),
                    card.Image
                );

                GUI.Label(
                    new Rect(875, 410, 305, 32),
                    showingDraftCards ? "Mana: " + card.Mana + "   |   Buy from the Syntax Merchant" :
                    "Mana: " + card.Mana + "   |   Owned: " + quantity,
                    textStyle
                );

                Fill(new Rect(875, 450, 305, 1), Brass);

                string description = showingDraftCards
                    ? (card.Description ?? "") + "\n\nCLASS LIBRARY\nNot owned yet. Purchase this card from the Syntax Merchant to add it to your saved inventory and battle deck."
                    : (card.Description ?? "") + "\n\nCurrent effects:\n" + CardRefinementRules.Describe(card, cardLevel);

                float height = Mathf.Max(
                    145,
                    textStyle.CalcHeight(
                        new GUIContent(description),
                        280
                    )
                );

                detailScroll = GUI.BeginScrollView(
                    new Rect(875, 468, 305, 146),
                    detailScroll,
                    new Rect(0, 0, 280, height)
                );

                GUI.Label(
                    new Rect(0, 0, 280, height),
                    description,
                    textStyle
                );

                GUI.EndScrollView();
            }
        }
        else
        {
            GUI.Label(
                new Rect(885, 300, 285, 150),
                showingDraftCards
                    ? (draftCards == null ? "Could not load the class card library." : "Select a card to inspect its effect. Purchase it from the Syntax Merchant to play it.")
                    : "Select a card from your collection to study its abilities.",
                textStyle
            );
        }

        GUI.Label(
            new Rect(75, 655, 1080, 25),
            showingDraftCards
                ? "Purchase class cards from the Syntax Merchant to add them to your saved inventory and battle deck  |  Scroll to browse  |  I / Esc to return"
                : "Select a card to inspect  |  Scroll to browse  |  I / Esc to return to your journey",
            smallStyle
        );
    }
}
