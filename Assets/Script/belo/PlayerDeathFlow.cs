using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>One death sequence and respawn choice shared by exploration and battle.</summary>
public sealed class PlayerDeathFlow : MonoBehaviour
{
    private static PlayerDeathFlow instance;
    public static bool IsActive => instance != null && instance.dying;
    private bool dying, choosing, returning;
    private int observedScene = -1;
    private PlayerDeathAnimations animations;
    private GameObject menu, deathVisual;
    private playerMovement player;
    public bool IsChoosing => choosing;

    public static void EnsureInstance()
    {
        if (instance == null) new GameObject("Player Death Flow").AddComponent<PlayerDeathFlow>();
    }

    void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        animations = Resources.Load<PlayerDeathAnimations>("PlayerDeath/Animations");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void Update()
    {
        if (dying || SceneTransitionFader.IsTransitioning) return;
        var scene = SceneManager.GetActiveScene();
        if (CharacterAlmanac.IsExplorationScene(scene.name))
        {
            if (observedScene != scene.handle)
            {
                observedScene = scene.handle;
                player = null;
                StartCoroutine(RegisterSceneCheckpoint(scene));
            }
            if (player == null) player = FindFirstObjectByType<playerMovement>();
            if (player != null && PlayerHealth.IsInitialized && PlayerHealth.CurrentHealth <= 0)
                Begin(player.GetComponentsInChildren<SpriteRenderer>().FirstEnabled(), player.gameObject);
        }
        else if (scene.name == "Battle Scene" && HeroSystem.Instance != null && HeroSystem.Instance.HeroView != null &&
            HeroSystem.Instance.HeroView.CurrentHealth <= 0)
            BeginBattleDeath(HeroSystem.Instance.HeroView);
    }

    IEnumerator RegisterSceneCheckpoint(Scene scene)
    {
        yield return null;
        yield return null;
        if (dying || scene != SceneManager.GetActiveScene()) yield break;
        var saved = SaveSlots.Read(SaveSlots.Active);
        if (saved != null && saved.hasCheckpoint && saved.checkpointScene == scene.name) yield break;
        if (player == null) player = FindFirstObjectByType<playerMovement>();
        if (player == null || PlayerHealth.CurrentHealth <= 0) yield break;
        if (scene.name == "Scene 2" && animations != null)
            RecordCheckpoint(animations.villagePosition);
        else RecordCheckpoint(saved != null && saved.hasPosition && saved.scene == scene.name ? saved.position : player.transform.position);
    }

    public static void RecordCheckpoint(Vector3 position)
    {
        if (IsActive || PlayerHealth.CurrentHealth <= 0) return;
        var saved = SaveSlots.Read(SaveSlots.Active);
        if (saved == null) return;
        saved.hasCheckpoint = true;
        saved.checkpointScene = SceneManager.GetActiveScene().name;
        saved.checkpointPosition = position;
        SaveSlots.Write(saved);
    }

    public static void BeginBattleDeath(HeroView hero)
    {
        EnsureInstance();
        if (hero != null) instance.Begin(hero.MainSpriteRenderer, hero.gameObject);
    }

    void Begin(SpriteRenderer source, GameObject subject)
    {
        if (dying) return;
        // Capture the visible pose before disabling polish resets its transform.
        Vector3 visualScale = source != null ? source.transform.lossyScale : Vector3.one;
        Vector3 visualPosition = source != null ? source.transform.position : subject.transform.position;
        Quaternion visualRotation = source != null ? source.transform.rotation : subject.transform.rotation;
        float sourcePixelsPerUnit = source != null && source.sprite != null ? source.sprite.pixelsPerUnit : 16f;
        dying = true;
        choosing = returning = false;
        Time.timeScale = 0f;
        if (DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive) DialogueManager.Instance.EndDialogue();
        var movement = subject.GetComponent<playerMovement>();
        if (movement != null) movement.enabled = false;
        var body = subject.GetComponent<Rigidbody2D>();
        if (body != null) body.linearVelocity = Vector2.zero;
        foreach (var animator in subject.GetComponentsInChildren<Animator>()) animator.enabled = false;
        foreach (var polish in subject.GetComponentsInChildren<ArcherAnimationPolish>()) polish.enabled = false;
        var combatant = subject.GetComponent<CombatantView>();
        if (combatant != null) combatant.StopAllCoroutines();
        if (CardViewHoverSystem.Instance != null) CardViewHoverSystem.Instance.Hide();
        BattleAudio.EnsureInstance();
        BattleAudio.Play(BattleSound.Defeat);
        SpriteRenderer visual = null;
        if (source != null)
        {
            deathVisual = new GameObject("Player Death Animation", typeof(SpriteRenderer));
            deathVisual.transform.SetParent(transform, false);
            deathVisual.transform.SetPositionAndRotation(visualPosition, visualRotation);
            deathVisual.transform.localScale = visualScale;
            visual = deathVisual.GetComponent<SpriteRenderer>();
            visual.sprite = source.sprite;
            visual.sharedMaterial = source.sharedMaterial;
            visual.sortingLayerID = source.sortingLayerID;
            visual.sortingOrder = source.sortingOrder;
            visual.flipX = source.flipX;
            source.enabled = false;
        }
        int character = movement != null && movement.mageAppearance ? SelectedHero.MageIndex :
            movement != null && movement.archerAppearance ? SelectedHero.ArcherIndex : PlayerPrefs.GetInt("SelectedCharacter", 0);
        float ground = source != null ? source.bounds.min.y : visualPosition.y;
        if (movement != null)
        {
            var collider = subject.GetComponent<Collider2D>();
            if (collider != null)
            {
                ground = collider.bounds.min.y;
                // The game is paused during death, so a midair player cannot fall
                // through physics. Find the actual floor for the visual instead.
                foreach (var hit in Physics2D.RaycastAll(new Vector2(collider.bounds.center.x, collider.bounds.max.y + 0.1f), Vector2.down, 200f))
                    if (hit.collider != null && !hit.collider.isTrigger && hit.collider.CompareTag("Ground") &&
                        hit.normal.y > 0.5f && hit.point.y <= collider.bounds.min.y + 0.2f)
                    { ground = hit.point.y; break; }
            }
        }
        StartCoroutine(PlayDeath(visual, character, visualScale, sourcePixelsPerUnit, ground));
    }

    IEnumerator PlayDeath(SpriteRenderer visual, int character, Vector3 originalScale, float sourcePixelsPerUnit, float ground)
    {
        var frames = animations != null ? animations.ForCharacter(character) : null;
        if (visual != null && frames != null && frames.Length > 0)
            foreach (var frame in frames)
            {
                if (frame != null)
                {
                    // Different sheets use different import units. Preserve pixel density;
                    // never resize to each pose's bounds as the body falls or dissolves.
                    visual.transform.localScale = originalScale * (frame.pixelsPerUnit / sourcePixelsPerUnit);
                    visual.sprite = frame;
                    if (character == 0)
                    {
                        // This sheet's floor is one pixel above the cell bottom.
                        // Keep that authored baseline fixed while the body falls.
                        float baseline = (1f - frame.pivot.y) / frame.pixelsPerUnit;
                        Vector3 position = visual.transform.position;
                        position.y = ground - visual.transform.TransformVector(new Vector3(0, baseline, 0)).y;
                        visual.transform.position = position;
                    }
                }
                yield return new WaitForSecondsRealtime(1f / Mathf.Max(1f, animations.framesPerSecond));
            }
        else yield return new WaitForSecondsRealtime(0.75f);
        yield return new WaitForSecondsRealtime(0.25f);
        choosing = true;
        ShowChoices();
    }

    public void ReturnToCheckpoint() => Respawn(false);
    public void ReturnToVillage() => Respawn(true);

    void Respawn(bool village)
    {
        if (!choosing || returning || animations == null) return;
        returning = true;
        var saved = SaveSlots.Read(SaveSlots.Active) ?? new SaveSlots.Record { character = PlayerPrefs.GetInt("SelectedCharacter", 0) };
        bool useCheckpoint = !village && saved.hasPosition && !string.IsNullOrEmpty(saved.scene) &&
            CharacterAlmanac.IsExplorationScene(saved.scene) && Application.CanStreamedLevelBeLoaded(saved.scene);
        string destination = useCheckpoint ? saved.scene : animations.villageScene;
        Vector3 position = useCheckpoint ? saved.position : animations.villagePosition;
        if (!Application.CanStreamedLevelBeLoaded(destination)) { returning = false; return; }
        saved.scene = destination;
        saved.position = position;
        saved.hasPosition = true;
        saved.maxHealth = PlayerHealth.MaxHealth;
        saved.currentHealth = saved.maxHealth;
        saved.hasCheckpoint = true;
        saved.checkpointScene = destination;
        saved.checkpointPosition = position;
        SaveSlots.Write(saved);
        CaveEnemyEncounter.PendingEnemies = null;
        CaveEnemyEncounter.PendingEncounterId = null;
        if (menu != null) Destroy(menu);
        if (deathVisual != null) Destroy(deathVisual);
        dying = choosing = false;
        observedScene = -1;
        Time.timeScale = 1f;
        GameSave.EnsureInstance().ContinueGame();
    }

    void ShowChoices()
    {
        menu = new GameObject("Death Choices", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        menu.transform.SetParent(transform, false);
        var canvas = menu.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;
        var scaler = menu.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 0.5f;
        var shade = Box("Shade", menu.transform, Vector2.zero, new Vector2(1280,720), new Color(0,0,0,0.7f));
        shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one; shade.sizeDelta = Vector2.zero;
        var panel = Box("Death Panel", menu.transform, Vector2.zero, new Vector2(760,380), new Color(0.12f,0.065f,0.04f));
        var outline = panel.gameObject.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(0.75f,0.52f,0.22f); outline.effectDistance = new Vector2(3,-3);
        Text("Title", panel, new Vector2(0,115), new Vector2(680,70), "YOU HAVE FALLEN", 42, new Color(0.9f,0.3f,0.22f));
        Text("Description", panel, new Vector2(0,38), new Vector2(680,70), "Checkpoint returns to your last saved point.\nYour health will be fully restored.", 23, new Color(1f,0.9f,0.7f));
        var checkpoint = Button("Return to Checkpoint", panel, new Vector2(-175,-85), ReturnToCheckpoint);
        Button("Return to Village", panel, new Vector2(175,-85), ReturnToVillage);
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            var events = new GameObject("Death Menu EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            events.transform.SetParent(menu.transform,false);
        }
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(checkpoint.gameObject);
    }

    static RectTransform Box(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Image));
        var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent,false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f,0.5f); rect.anchoredPosition = position; rect.sizeDelta = size;
        go.GetComponent<UnityEngine.UI.Image>().color = color; return rect;
    }

    static void Text(string name, Transform parent, Vector2 position, Vector2 size, string text, float fontSize, Color color)
    {
        var go = new GameObject(name,typeof(RectTransform),typeof(TextMeshProUGUI));
        var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent,false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f,0.5f); rect.anchoredPosition = position; rect.sizeDelta = size;
        var label = go.GetComponent<TextMeshProUGUI>(); label.font = TMP_Settings.defaultFontAsset;
        label.text = text; label.fontSize = fontSize; label.alignment = TextAlignmentOptions.Center; label.color = color; label.raycastTarget = false;
    }

    static UnityEngine.UI.Button Button(string name, Transform parent, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        var rect = Box(name,parent,position,new Vector2(320,62),new Color(0.32f,0.2f,0.1f));
        var button = rect.gameObject.AddComponent<UnityEngine.UI.Button>(); button.onClick.AddListener(action);
        Text("Text",rect,Vector2.zero,new Vector2(302,58),name,24,new Color(1f,0.9f,0.7f));
        return button;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!dying) return;
        StopAllCoroutines();
        if (menu != null) Destroy(menu);
        if (deathVisual != null) Destroy(deathVisual);
        dying = choosing = false;
        observedScene = -1;
        Time.timeScale = 1f;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) { if(dying)Time.timeScale=1f; instance=null; }
    }
}

internal static class DeathSpriteSelection
{
    public static SpriteRenderer FirstEnabled(this SpriteRenderer[] renderers)
    {
        foreach (var renderer in renderers) if (renderer.enabled) return renderer;
        return null;
    }
}
