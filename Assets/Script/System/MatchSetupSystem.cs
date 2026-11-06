using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MatchSetupSystem : MonoBehaviour
{
    [SerializeField] private HeroData heroData;
    [SerializeField] private PerkData perkData;
    [SerializeField] private List<EnemyData> enemyDatas;
    [Header("Battle HUD Portraits")]
    [SerializeField] private Sprite warriorHudPortrait;
    [SerializeField] private Sprite mageHudPortrait;
    [SerializeField] private Sprite archerHudPortrait;

    private void OnEnable()
    {
        ActionSystem.AttachPerformer<StarfallSkillGA>(StarfallSkillPerformer);
    }

    private void OnDisable()
    {
        ActionSystem.DetachPerformer<StarfallSkillGA>();
    }

    private void Update()
    {
        BattleHudMedieval.UpdateEpSkillAvailability();
    }

    private void Start()
    {
        BattleHudMedieval.ResetEpMeter();
        BattleSettingsMenu.EnsureCreated();
        PlayerDeathFlow.EnsureInstance();
        BattleAudio.EnsureInstance();
        BattleBgmController.EnterBattle();
        heroData = SelectedHero.Resolve(heroData);
        BattleUITheme.Apply();
        ApplyPlayerHudLayout();
        // Resolve class HP before HeroView setup, regardless of Start order.
        var playerStats = HeroSystem.Instance.HeroView.GetComponent<PlayerStats>();
        if (playerStats != null) playerStats.InitializePlayer();
        HeroSystem.Instance.SetUp(heroData);
        ApplyPlayerHudLayout(ResolveHudPortrait(), HeroSystem.Instance.HeroView);
        bool caveBattle = CaveEnemyEncounter.PendingEnemies != null && CaveEnemyEncounter.PendingEnemies.Count > 0;
        string encounterId = CaveEnemyEncounter.PendingEncounterId;
        string encounterScene = CaveEnemyEncounter.PendingEncounterScene;
        if (caveBattle)
        {
            EnemySystem.Instance.Setup(CaveEnemyEncounter.PendingEnemies,encounterScene);
            CaveEnemyEncounter.PendingEnemies = null;
            CaveEnemyEncounter.PendingEncounterId = null;
            CaveEnemyEncounter.PendingEncounterScene = null;
        }
        else EnemySystem.Instance.Setup(enemyDatas);
        var platform = FindFirstObjectByType<BattlePlatform>();
        if (platform != null) platform.GroundCombatants(HeroSystem.Instance.HeroView, EnemySystem.Instance.Enemies);
        foreach (var enemy in EnemySystem.Instance.Enemies)
            enemy.FaceTowards(HeroSystem.Instance.HeroView);
        if (EnemySystem.Instance.Enemies.Count > 0)
            HeroSystem.Instance.HeroView.FaceTowards(EnemySystem.Instance.Enemies[0]);
        BattleEncounterNames.Show(EnemySystem.Instance.Enemies);
        gameObject.AddComponent<EncounterBattleReturn>().Configure(caveBattle, encounterId, encounterScene);
        List<CardData> ownedBattleDeck = CharacterInventory.WithChestRewards(heroData.Deck);
        Debug.Log($"Battle deck created from this save slot's owned inventory: {ownedBattleDeck.Count} card(s). Unowned catalog cards are excluded.");
        CardSystem.Instance.Setup(ownedBattleDeck);
        // Keep the starting perk's effect active without drawing its crossed-swords badge.
        PerkSystem.Instance.AddPerk(new Perk(perkData), showIcon: false);
        DrawCardsGA drawCardsGA = new(5);
        ActionSystem.Instance.Perform(drawCardsGA);
        if (BattleTutorialGuide.IsTutorialEncounter(encounterId)) gameObject.AddComponent<BattleTutorialGuide>();
    }

    private static void ApplyPlayerHudLayout()
    {
        var hudObject = GameObject.Find("PlaayerHUD");
        if (hudObject == null) return;

        Transform portraitFrameTransform = FindDescendant(hudObject.transform, "PortraitFrame");
        Transform portraitIconTransform = FindDescendant(hudObject.transform, "PortraitIcon");
        if (portraitFrameTransform == null || portraitIconTransform == null) return;

        var portraitFrame = portraitFrameTransform.gameObject;
        var portraitIcon = portraitIconTransform.gameObject;
        portraitFrame.SetActive(true);
        portraitIcon.SetActive(true);

        if (portraitFrame.GetComponent<RectMask2D>() == null)
            portraitFrame.AddComponent<RectMask2D>();

        var portrait = portraitIcon.GetComponent<Image>();
        if (portrait != null) portrait.preserveAspect = true;
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root == null) return null;
        if (root.name == objectName) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindDescendant(root.GetChild(i), objectName);
            if (match != null) return match;
        }

        return null;
    }

    private IEnumerator StarfallSkillPerformer(StarfallSkillGA action)
    {
        HeroView hero = HeroSystem.Instance != null ? HeroSystem.Instance.HeroView : null;
        List<CombatantView> targets = EnemySystem.Instance != null && EnemySystem.Instance.Enemies != null
            ? EnemySystem.Instance.Enemies.FindAll(enemy => enemy != null && enemy.CurrentHealth > 0)
                .ConvertAll(enemy => (CombatantView)enemy)
            : new List<CombatantView>();
        if (hero == null || hero.CurrentHealth <= 0 || targets.Count == 0 || !BattleHudMedieval.TrySpendFullEp())
            yield break;

        yield return PlayStarfallCutscene(hero, targets);
        if (hero == null || hero.CurrentHealth <= 0) yield break;

        int damage = Mathf.Max(1, hero.AttackPower * 2);
        ActionSystem.Instance.AddReaction(new DealDamageGA(damage, targets, hero));
    }

    public static bool StarfallCutsceneActive { get; private set; }

    private IEnumerator PlayStarfallCutscene(HeroView hero, List<CombatantView> targets)
    {
        // Clear the hover before capturing renderer visibility for the cinematic.
        CardViewHoverSystem.Instance?.Hide();
        float duration = 6f;
        const float originalTimelineDuration = 3f;
        Camera battleCamera = Camera.main;
        Vector3 cameraStartPosition = battleCamera != null ? battleCamera.transform.position : Vector3.zero;
        float cameraStartSize = battleCamera != null && battleCamera.orthographic ? battleCamera.orthographicSize : 0f;
        Color cameraStartColor = battleCamera != null ? battleCamera.backgroundColor : Color.black;
        CameraClearFlags cameraStartClearFlags = battleCamera != null ? battleCamera.clearFlags : CameraClearFlags.Skybox;
        Vector3 heroFocus = hero.VisualCenter;
        Vector3 targetFocus = heroFocus;
        float targetCameraSize = cameraStartSize;
        if (battleCamera != null && battleCamera.orthographic)
        {
            float minX = float.PositiveInfinity, maxX = float.NegativeInfinity;
            float minY = float.PositiveInfinity, maxY = float.NegativeInfinity;
            int visibleTargetCount = 0;
            foreach (CombatantView enemy in targets)
            {
                if (enemy == null || enemy.CurrentHealth <= 0) continue;
                Vector3 center = enemy.VisualCenter;
                minX = Mathf.Min(minX, center.x); maxX = Mathf.Max(maxX, center.x);
                minY = Mathf.Min(minY, center.y); maxY = Mathf.Max(maxY, center.y);
                targetFocus += center;
                visibleTargetCount++;
            }
            if (visibleTargetCount > 0)
            {
                targetFocus = (targetFocus - heroFocus) / visibleTargetCount;
                // Fit the enemy group and leave enough room for the slash bursts around
                // their bodies, instead of keeping the camera tightly on the Warrior.
                float halfWidth = (maxX - minX) * 0.5f + 1.0f;
                float halfHeight = (maxY - minY) * 0.5f + 1.15f;
                targetCameraSize = Mathf.Clamp(Mathf.Max(halfHeight, halfWidth / battleCamera.aspect),
                    cameraStartSize * 0.55f, cameraStartSize);
            }
        }
        BattleCaveBackground cave = FindFirstObjectByType<BattleCaveBackground>();
        bool caveWasEnabled = cave != null && cave.enabled;
        bool archer = SelectedHero.IsArcher;
        bool mage = SelectedHero.IsMage;
        bool warrior = !archer && !mage;
        var hiddenRenderers = new List<(Renderer renderer, bool enabled)>();
        var hiddenCanvases = new List<(Canvas canvas, bool enabled)>();
        var warriorRenderers = new List<(Renderer renderer, bool enabled)>();
        var warriorSpriteColors = new List<(SpriteRenderer renderer, Color color)>();
        var visibleEnemyRenderers = new List<(Renderer renderer, bool enabled)>();
        var visibleEnemyCanvases = new List<(Canvas canvas, bool enabled)>();
        if (cave != null)
        {
            cave.enabled = false;
        }
        if (warrior || mage || archer)
        {
            // Keep enemies visible throughout the Warrior and Mage cinematics. Hide
            // the hand, piles, HUD, and other gameplay art, but preserve enemy visuals.
            var capturedEnemyRenderers = new HashSet<Renderer>();
            var capturedEnemyCanvases = new HashSet<Canvas>();
            bool IsEnemyHierarchy(Transform candidate)
            {
                if (candidate == null || targets == null) return false;
                foreach (CombatantView enemy in targets)
                    if (enemy != null && (candidate == enemy.transform || candidate.IsChildOf(enemy.transform)))
                        return true;
                return false;
            }
            if (targets != null)
            {
                foreach (CombatantView enemy in targets)
                {
                    if (enemy == null) continue;

                    // Some enemies can enter the cutscene with their sprite or
                    // world-space health UI disabled. Snapshot then enable them so
                    // they remain visible for the vanish, attack, and slash beats.
                    foreach (Renderer renderer in enemy.GetComponentsInChildren<Renderer>(true))
                    {
                        if (renderer == null || !capturedEnemyRenderers.Add(renderer)) continue;
                        visibleEnemyRenderers.Add((renderer, renderer.enabled));
                        renderer.enabled = true;
                    }
                    foreach (Canvas canvas in enemy.GetComponentsInChildren<Canvas>(true))
                    {
                        if (canvas == null || !capturedEnemyCanvases.Add(canvas)) continue;
                        visibleEnemyCanvases.Add((canvas, canvas.enabled));
                        canvas.enabled = !archer;
                    }
                }
            }
            foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (renderer == null || renderer.transform == hero.transform || renderer.transform.IsChildOf(hero.transform)
                    || IsEnemyHierarchy(renderer.transform)) continue;
                hiddenRenderers.Add((renderer, renderer.enabled));
                renderer.enabled = false;
            }
            foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (canvas == null || canvas.transform == hero.transform || canvas.transform.IsChildOf(hero.transform)
                    || IsEnemyHierarchy(canvas.transform)) continue;
                hiddenCanvases.Add((canvas, canvas.enabled));
                canvas.enabled = false;
            }
            if (archer)
            {
                foreach (Canvas canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (hiddenCanvases.Exists(item => item.canvas == canvas)) continue;
                    hiddenCanvases.Add((canvas, canvas.enabled));
                    canvas.enabled = false;
                }
            }
            foreach (Renderer renderer in hero.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                warriorRenderers.Add((renderer, renderer.enabled));
                if (renderer is SpriteRenderer spriteRenderer)
                    warriorSpriteColors.Add((spriteRenderer, spriteRenderer.color));
            }
        }
        else
        {
            foreach (CombatantView target in targets)
            {
                if (target == null) continue;
                foreach (Renderer renderer in target.GetComponentsInChildren<Renderer>(true))
                {
                    hiddenRenderers.Add((renderer, renderer.enabled));
                    renderer.enabled = false;
                }
                foreach (Canvas canvas in target.GetComponentsInChildren<Canvas>(true))
                {
                    hiddenCanvases.Add((canvas, canvas.enabled));
                    canvas.enabled = false;
                }
            }
        }

        Color accent = archer ? new Color(0.25f, 1f, 0.4f) : mage
            ? new Color(0.22f, 0.68f, 1f) : new Color(1f, 0.16f, 0.12f);
        string title = BattleHudMedieval.SpecialSkillTitle;

        if (!archer) BattleMessageUI.Instance?.ShowAt(title, "Special skill unleashed!", hero.VisualCenter + Vector3.up * 1.2f);
        CombatantView primaryTarget = targets.Find(target => target != null && target.CurrentHealth > 0);
        StarfallCutsceneVisual cutsceneVfx = CreateStarfallCutsceneVfx(hero, primaryTarget, targets, accent, archer, mage);
        float elapsed = 0f;
        bool attackPlayed = false;
        bool attackFinished = false;
        bool warriorBlurStarted = false;
        float attackFinishedAt = -1f;
        float enemyVfxStartAt = -1f;
        float enemyVfxEndAt = -1f;
        float cameraPanBackAt = -1f;

        bool previousCutsceneActive = StarfallCutsceneActive;
        StarfallCutsceneActive = true;
        try
        {
            if (battleCamera != null)
            {
                battleCamera.clearFlags = CameraClearFlags.SolidColor;
                battleCamera.backgroundColor = new Color(0.025f, 0.018f, 0.09f);
            }
            while ((elapsed < duration || (warrior && attackPlayed && !attackFinished)) && hero != null && hero.CurrentHealth > 0)
            {
                // Keep the authored three-second sequence intact, but play it across
                // the requested six-second cutscene so every action has room to read.
                float timelineTime = elapsed * (originalTimelineDuration / duration);
                // Warrior gets a vanish, reappearance, attack, then enemy-focused
                // slashes. Mage gets a readable charge, scythe travel, and ghost impact.
                float zoomIn = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(warrior || mage ? elapsed / 0.8f : timelineTime / 0.55f));
                if (warrior && attackFinished && attackFinishedAt < 0f)
                    attackFinishedAt = elapsed;
                float panStartAt = warrior && attackFinishedAt >= 0f ? attackFinishedAt + .08f : float.PositiveInfinity;
                float magePanStartAt = mage ? 3.25f : archer && cutsceneVfx != null && cutsceneVfx.ArcherShotAt >= 0f ? cutsceneVfx.ArcherShotAt + .15f : float.PositiveInfinity;
                float magePanBackAt = mage || archer ? 5.5f : float.PositiveInfinity;
                float zoomOutStartAt = warrior && cameraPanBackAt >= 0f ? cameraPanBackAt : float.PositiveInfinity;
                float zoomOut = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(warrior
                    ? (elapsed - zoomOutStartAt) / 0.65f
                    : mage || archer
                        ? (elapsed - magePanBackAt) / 0.55f
                        : (timelineTime - 2.15f) / 0.85f));
                // Keep the Warrior visible for the opening beat, fade through a short
                // motion-blur trail, then restore her before the attack animation starts.
                if (warrior && elapsed >= 1f && !warriorBlurStarted)
                {
                    warriorBlurStarted = true;
                    if (cutsceneVfx != null) cutsceneVfx.BeginWarriorVanishBlur(elapsed);
                }
                if (warrior && warriorBlurStarted)
                {
                    float vanishProgress = Mathf.Clamp01((elapsed - 1f) / .22f);
                    foreach (var item in warriorSpriteColors)
                    {
                        if (item.renderer == null) continue;
                        Color color = item.color;
                        color.a *= 1f - vanishProgress;
                        item.renderer.color = color;
                    }
                    foreach (var item in warriorRenderers)
                        if (item.renderer != null)
                            item.renderer.enabled = elapsed >= 1.22f ? elapsed >= 1.72f && item.enabled : item.enabled;
                    if (elapsed >= 1.72f)
                        foreach (var item in warriorSpriteColors)
                            if (item.renderer != null) item.renderer.color = item.color;
                }
                if (battleCamera != null)
                {
                    float panToTargets = warrior
                        ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - panStartAt) / 0.7f))
                            * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - (cameraPanBackAt >= 0f ? cameraPanBackAt : float.PositiveInfinity)) / 0.55f)))
                        : mage || archer
                            ? Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - magePanStartAt) / 0.65f))
                                * (1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((elapsed - magePanBackAt) / 0.55f)))
                            : 0f;
                    Vector3 closeFocus = warrior || mage || archer ? Vector3.Lerp(heroFocus, targetFocus, panToTargets) : heroFocus;
                    Vector3 cinematicPosition = Vector3.Lerp(cameraStartPosition,
                        new Vector3(closeFocus.x, closeFocus.y, cameraStartPosition.z), zoomIn);
                    battleCamera.transform.position = Vector3.Lerp(cinematicPosition, cameraStartPosition, zoomOut);
                    if (battleCamera.orthographic)
                    {
                        float closeSize = warrior
                            ? Mathf.Lerp(cameraStartSize * 0.72f, targetCameraSize, panToTargets)
                            : mage ? Mathf.Lerp(cameraStartSize * 0.78f, targetCameraSize, panToTargets)
                            : Mathf.Lerp(cameraStartSize * 0.78f, targetCameraSize, panToTargets);
                        battleCamera.orthographicSize = Mathf.Lerp(Mathf.Lerp(cameraStartSize, closeSize, zoomIn),
                            cameraStartSize, zoomOut);
                    }
                }

                if (!attackPlayed && (warrior ? elapsed >= 1.9f : mage ? elapsed >= 2.35f : elapsed >= .8f))
                {
                    attackPlayed = true;
                    if (primaryTarget != null)
                    {
                        hero.FaceTowards(primaryTarget);
                        if (archer)
                            StartCoroutine(PlayArcherStarfallAttack(hero,
                                () => { if (cutsceneVfx != null) cutsceneVfx.BeginArcherShot(elapsed); },
                                () => attackFinished = true));
                        else
                            StartCoroutine(PlayStarfallAttack(hero, () => attackFinished = true));
                    }
                    else attackFinished = true;
                }

                // Start the Warrior's slash frames only after her clean attack
                // animation has finished and the camera has reached the enemies.
                if (warrior && enemyVfxStartAt < 0f && attackFinishedAt >= 0f
                    && elapsed >= panStartAt + .7f)
                {
                    enemyVfxStartAt = elapsed;
                    if (cutsceneVfx != null)
                        cutsceneVfx.BeginWarriorBursts(elapsed);
                    enemyVfxEndAt = elapsed + (cutsceneVfx != null ? cutsceneVfx.WarriorBurstDuration : .1f);
                    cameraPanBackAt = enemyVfxEndAt + .35f;
                    duration = Mathf.Max(duration, cameraPanBackAt + .65f);
                }

                elapsed += Time.unscaledDeltaTime;
                if (cutsceneVfx != null) cutsceneVfx.Update(elapsed);
                yield return null;
            }
        }
        finally
        {
            StarfallCutsceneActive = previousCutsceneActive;
            if (cutsceneVfx != null) cutsceneVfx.Dispose();
            foreach (var item in hiddenRenderers)
                if (item.renderer != null) item.renderer.enabled = item.enabled;
            foreach (var item in hiddenCanvases)
                if (item.canvas != null) item.canvas.enabled = item.enabled;
            foreach (var item in warriorRenderers)
                if (item.renderer != null) item.renderer.enabled = item.enabled;
            foreach (var item in warriorSpriteColors)
                if (item.renderer != null) item.renderer.color = item.color;
            foreach (var item in visibleEnemyRenderers)
                if (item.renderer != null) item.renderer.enabled = item.enabled;
            foreach (var item in visibleEnemyCanvases)
                if (item.canvas != null) item.canvas.enabled = item.enabled;
            if (cave != null) cave.enabled = caveWasEnabled;
            if (battleCamera != null)
            {
                battleCamera.transform.position = cameraStartPosition;
                battleCamera.clearFlags = cameraStartClearFlags;
                battleCamera.backgroundColor = cameraStartColor;
                if (battleCamera.orthographic) battleCamera.orthographicSize = cameraStartSize;
            }
        }
    }

    private static IEnumerator PlayArcherStarfallAttack(HeroView hero, System.Action onRelease, System.Action onComplete)
    {
        if (hero != null && hero.CurrentHealth > 0)
            yield return hero.PlayChargedBowAttack(onRelease);
        onComplete?.Invoke();
    }

    private static IEnumerator PlayStarfallAttack(HeroView hero, System.Action onComplete)
    {
        if (hero != null && hero.CurrentHealth > 0)
            yield return hero.PlayAttackAnimation();
        onComplete?.Invoke();
    }

    private static StarfallCutsceneVisual CreateStarfallCutsceneVfx(HeroView hero, CombatantView target, List<CombatantView> targets, Color accent, bool archer, bool mage)
    {
        if (hero == null) return null;
        return new StarfallCutsceneVisual(hero, target, targets, accent, archer, mage);
    }

    private sealed class StarfallCutsceneVisual
    {
        private readonly HeroView hero;
        private readonly CombatantView target;
        private readonly List<CombatantView> targets;
        private readonly bool archer, mage;
        private readonly Color accent;
        private readonly GameObject root;
        private readonly LineRenderer aura;
        private readonly SpriteRenderer lightningArrow;
        private readonly Sprite[] lightningArrowFrames;
        private readonly List<LineRenderer> lightningStrikes = new List<LineRenderer>();
        private readonly Sprite[] warriorSlashFrames, warriorImpactFrames;
        private readonly List<WarriorVfxBurst> warriorBursts = new List<WarriorVfxBurst>();
        private readonly List<WarriorAfterimage> warriorAfterimages = new List<WarriorAfterimage>();
        private readonly List<SpriteRenderer> nightStars = new List<SpriteRenderer>();
        private readonly SpriteRenderer scythe, ghost;
        private readonly Sprite[] scytheFrames, ghostFrames;
        private readonly Material material;
        private readonly Sprite pixel;
        private readonly Texture2D pixelTexture;
        public float ArcherShotAt { get; private set; } = -1f;

        public void BeginArcherShot(float elapsed)
        {
            ArcherShotAt = elapsed;
        }

        private float warriorBurstsStartAt = -1f;
        private float warriorBlurStartAt = -1f;

        public float WarriorBurstDuration
        {
            get
            {
                float end = 0f;
                foreach (WarriorVfxBurst burst in warriorBursts)
                    end = Mathf.Max(end, burst.startTime + burst.duration);
                return end;
            }
        }

        public void BeginWarriorBursts(float elapsed)
        {
            warriorBurstsStartAt = elapsed;
        }

        public void BeginWarriorVanishBlur(float elapsed)
        {
            if (warriorBlurStartAt >= 0f || hero == null) return;
            warriorBlurStartAt = elapsed;
            SpriteRenderer[] sources = hero.GetComponentsInChildren<SpriteRenderer>(true);
            Vector3 direction = target != null ? (target.VisualCenter - hero.VisualCenter).normalized : Vector3.right;
            if (direction.sqrMagnitude < .01f) direction = Vector3.right;

            foreach (SpriteRenderer source in sources)
            {
                if (source == null || source.sprite == null || !source.enabled) continue;
                // Two translucent snapshots leave a quick directional blur as the hero fades.
                for (int trail = 0; trail < 2; trail++)
                    warriorAfterimages.Add(new WarriorAfterimage(root.transform, source, direction,
                        trail * .055f, trail == 0 ? .48f : .28f, trail));
            }
        }

        public StarfallCutsceneVisual(HeroView owner, CombatantView enemy, List<CombatantView> allTargets, Color color, bool isArcher, bool isMage)
        {
            hero = owner; target = enemy; targets = allTargets; accent = color; archer = isArcher; mage = isMage;
            root = new GameObject("Starfall Special Skill Cutscene VFX");
            Shader shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader != null) material = new Material(shader);
            pixelTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            pixelTexture.SetPixel(0, 0, Color.white); pixelTexture.Apply(); pixel = Sprite.Create(pixelTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1f);
            Camera camera = Camera.main;
            float viewHeight = camera != null && camera.orthographic ? camera.orthographicSize * 1.2f : 7f;
            float viewWidth = viewHeight * (camera != null ? camera.aspect : 1.78f);
            for (int i = 0; i < 46; i++)
            {
                var starObject = new GameObject("Night sky star " + i);
                starObject.transform.SetParent(root.transform, false);
                starObject.transform.position = owner.VisualCenter + new Vector3(
                    Mathf.Lerp(-viewWidth, viewWidth, Mathf.Repeat(i * .6180339f, 1f)),
                    Mathf.Lerp(-viewHeight, viewHeight, Mathf.Repeat(i * .7548777f, 1f)),
                    .5f);
                var star = starObject.AddComponent<SpriteRenderer>();
                star.sprite = pixel;
                star.sortingOrder = -20;
                if (owner.MainSpriteRenderer != null) star.sortingLayerID = owner.MainSpriteRenderer.sortingLayerID;
                float size = .018f + Mathf.Repeat(i * .371f, 1f) * .034f;
                star.transform.localScale = new Vector3(size, size, 1f);
                float brightness = .28f + Mathf.Repeat(i * .217f, 1f) * .58f;
                star.color = new Color(.72f, .79f, 1f, brightness);
                nightStars.Add(star);
            }
            Vector3 floor = new Vector3(owner.VisualCenter.x, owner.BattleGroundY + .04f, owner.transform.position.z - .4f);
            aura = MakeLine("Class-colored foot aura", 40, .09f, color, 50);
            aura.loop = true;
            for (int i = 0; i < aura.positionCount; i++)
            {
                float angle = Mathf.PI * 2f * i / aura.positionCount;
                aura.SetPosition(i, floor + new Vector3(Mathf.Cos(angle) * .48f, Mathf.Sin(angle) * .12f, 0));
            }

            if (isArcher)
            {
                Texture2D sheet = Resources.Load<Texture2D>("StarfallArcherVfx/LightningArrow");
                if (sheet != null)
                {
                    lightningArrowFrames = new Sprite[32];
                    int w = sheet.width / 4, h = sheet.height / 8;
                    for (int i = 0; i < 32; i++)
                        lightningArrowFrames[i] = Sprite.Create(sheet,
                            new Rect((i % 4) * w, sheet.height - (i / 4 + 1) * h, w, h),
                            new Vector2(.5f, .5f), 100f);
                }
                lightningArrow = MakeSprite("Lightning Arrow projectile", 75, Color.clear);
                foreach (CombatantView enemyTarget in targets)
                {
                    if (enemyTarget == null) continue;
                    // Each jagged path has a broad blue halo and a thin white core.
                    for (int path = 0; path < 4; path++)
                    {
                        for (int layer = 0; layer < 2; layer++)
                        {
                            bool halo = layer == 0;
                            float width = (path == 0 ? .05f : .025f) * (halo ? 4f : 1f);
                            LineRenderer bolt = MakeLine("Lightning " + enemyTarget.name + " path " + path
                                + (halo ? " glow" : " core"), path == 0 ? 17 : 9, width, Color.clear, halo ? 76 : 77);
                            bolt.numCapVertices = 0;
                            bolt.numCornerVertices = 0;
                            bolt.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, path == 0 ? .45f : .02f);
                            if (owner.MainSpriteRenderer != null) bolt.sortingLayerID = owner.MainSpriteRenderer.sortingLayerID;
                            lightningStrikes.Add(bolt);
                        }
                    }
                }
            }
            else if (isMage)
            {
                scytheFrames = LoadFrameSprites("StarfallMageVfx/DarkVfx1");
                ghostFrames = LoadFrameSprites("StarfallMageVfx/DarkVfx2");
                scythe = MakeSprite("Dark VFX scythe", 70, new Color(1f, .45f, 1f, 1f));
                ghost = MakeSprite("Ghost leaving enemy", 71, new Color(.85f, .75f, 1f, 0f));
            }
            else
            {
                warriorSlashFrames = LoadFrameSprites("WarriorStarfallVfx/Slashes");
                warriorImpactFrames = LoadFrameSprites("WarriorStarfallVfx/Impacts");
                if (targets != null)
                {
                    for (int targetIndex = 0; targetIndex < targets.Count; targetIndex++)
                    {
                        CombatantView slashTarget = targets[targetIndex];
                        if (slashTarget == null || slashTarget.CurrentHealth <= 0) continue;

                        // Start these only after the camera has reached the enemy group.
                        // Each slash is a frame animation sourced from VFX WARRIOR.
                        float enemyStart = targetIndex * .18f;
                        Vector2[] offsets = { new Vector2(-.16f, .22f), new Vector2(.18f, .06f), new Vector2(-.04f, -.14f) };
                        float[] rotations = { -24f, 28f, 8f };
                        for (int slashIndex = 0; slashIndex < offsets.Length; slashIndex++)
                        {
                            warriorBursts.Add(new WarriorVfxBurst(root.transform, slashTarget, warriorSlashFrames,
                                enemyStart + slashIndex * .23f, .34f, offsets[slashIndex], rotations[slashIndex],
                                1.8f, 68 + slashIndex));
                        }
                        warriorBursts.Add(new WarriorVfxBurst(root.transform, slashTarget, warriorImpactFrames,
                            enemyStart + .72f, .38f, Vector2.zero, 0f, 1.45f, 72));
                    }
                }
            }
        }

        private LineRenderer MakeLine(string name, int points, float width, Color color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root.transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true; line.positionCount = points; line.startWidth = line.endWidth = width;
            line.numCapVertices = 4; line.sortingOrder = order;
            if (material != null) line.material = material;
            line.startColor = line.endColor = color;
            return line;
        }

        private SpriteRenderer MakeSprite(string name, int order, Color color)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform, false);
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sortingOrder = order; renderer.color = color;
            if (hero.MainSpriteRenderer != null) renderer.sortingLayerID = hero.MainSpriteRenderer.sortingLayerID;
            return renderer;
        }

        public void Update(float elapsed)
        {
            if (hero == null) return;
            if (warriorBurstsStartAt >= 0f)
                UpdateWarriorBursts(elapsed - warriorBurstsStartAt);
            if (warriorBlurStartAt >= 0f)
                foreach (WarriorAfterimage afterimage in warriorAfterimages)
                    afterimage.Update(elapsed - warriorBlurStartAt);
            Vector3 floor = new Vector3(hero.VisualCenter.x, hero.BattleGroundY + .04f, hero.transform.position.z - .4f);
            float auraPulse = .8f + .2f * Mathf.Sin(elapsed * 9f);
            float auraRadius = .48f * auraPulse;
            for (int i = 0; i < aura.positionCount; i++)
            {
                float angle = Mathf.PI * 2f * i / aura.positionCount;
                aura.SetPosition(i, floor + new Vector3(Mathf.Cos(angle) * auraRadius, Mathf.Sin(angle) * .12f * auraPulse, 0));
            }

            if (target == null) return;
            Vector3 hit = target.VisualCenter;
            if (archer)
            {
                // The release animation frame supplies the timestamp.
                if (ArcherShotAt < 0f) return;
                float launchAt = ArcherShotAt;
                float impactAt = launchAt + 1f;
                float impactEnd = Mathf.Min(impactAt + 1.5f, 5.5f);
                float flight = Mathf.Clamp01((elapsed - launchAt) / (impactAt - launchAt));
                Vector3 start = hero.VisualCenter + Vector3.up * .35f;
                Vector3 end = hit + Vector3.up * .15f;
                if (lightningArrow != null && lightningArrowFrames != null)
                {
                    bool flying = elapsed >= launchAt && elapsed < impactAt;
                    bool impact = elapsed >= impactAt && elapsed < impactEnd;
                    lightningArrow.enabled = flying || impact;
                    int index = flying ? 4 + Mathf.FloorToInt((elapsed - launchAt) * 14f) % 16
                        : 20 + Mathf.Clamp(Mathf.FloorToInt((elapsed - impactAt) / ((impactEnd - impactAt) / 12f)), 0, 11);
                    lightningArrow.sprite = lightningArrowFrames[index];
                    lightningArrow.transform.position = flying ? Vector3.Lerp(start, end, flight) : end;
                    lightningArrow.transform.localScale = Vector3.one * (flying ? 3f : 2.5f);
                    lightningArrow.transform.rotation = flying
                        ? Quaternion.Euler(0f, 0f, Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg)
                        : Quaternion.identity;
                    lightningArrow.color = Color.white;
                }
                int boltIndex = 0;
                int enemyIndex = 0;
                foreach (CombatantView enemyTarget in targets)
                {
                    if (enemyTarget == null) continue;
                    float age = elapsed - impactAt - enemyIndex * .045f;
                    // Brief return strokes, with dark gaps between them. The shape
                    // stays fixed during each flash rather than wriggling every frame.
                    int stroke = age < .26f ? 0 : age < .64f ? 1 : age < .98f ? 2 : 3;
                    float strokeStart = stroke == 0 ? 0f : stroke == 1 ? .26f : stroke == 2 ? .64f : .98f;
                    float flashAge = age - strokeStart;
                    float flashDuration = stroke == 0 ? .22f : .16f;
                    float intensity = Mathf.Clamp01(1f - flashAge / flashDuration);
                    intensity *= intensity;
                    bool visible = age >= 0f && flashAge >= 0f && flashAge < flashDuration
                        && elapsed < impactEnd && enemyTarget.CurrentHealth > 0;
                    Vector3 contact = enemyTarget.VisualCenter;
                    float seed = enemyIndex * 31f + stroke * 17f;
                    for (int path = 0; path < 4; path++)
                    {
                        for (int layer = 0; layer < 2; layer++)
                        {
                            LineRenderer bolt = lightningStrikes[boltIndex++];
                            bolt.enabled = visible;
                            if (!visible) continue;
                            for (int point = 0; point < bolt.positionCount; point++)
                            {
                                Vector3 position;
                                if (path == 0)
                                    position = ArcherLightningPoint(contact, seed, point / 16f);
                                else
                                {
                                    float progress = point / 8f;
                                    float attach = .25f + (path - 1) * .1875f;
                                    Vector3 origin = ArcherLightningPoint(contact, seed, attach);
                                    float side = path == 2 ? -1f : 1f;
                                    float jag = point == 0 || point == 8 ? 0f
                                        : LightningNoise(seed + path * 23f + point * 7f) * .12f;
                                    position = origin + new Vector3(side * (.55f + path * .12f) * progress + jag,
                                        -(.65f + path * .12f) * progress, 0f);
                                }
                                bolt.SetPosition(point, position);
                            }
                            Color color = layer == 0 ? new Color(.12f, .46f, 1f, intensity * .35f)
                                : new Color(.83f, .98f, 1f, intensity);
                            bolt.startColor = bolt.endColor = color;
                        }
                    }
                    enemyIndex++;
                }
            }
            else if (mage)
            {
                // Mage sequence: charge in the first beat, send the DarkVfx1
                // scythe across the arena, then lift a DarkVfx2 soul from impact.
                const float scytheStart = 2.35f;
                const float scytheTravelDuration = 1.2f;
                const float soulStart = 3.7f;
                const float soulDuration = 1.35f;
                Vector3 start = hero.VisualCenter + Vector3.up * .55f;
                Vector3 end = hit + Vector3.up * .12f;
                float travel = Mathf.Clamp01((elapsed - scytheStart) / scytheTravelDuration);

                if (scythe != null && elapsed >= scytheStart && elapsed < soulStart)
                {
                    SetFrame(scythe, scytheFrames, elapsed - scytheStart, .055f);
                    scythe.transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, travel));
                    scythe.transform.localScale = Vector3.one * 1.08f;
                    scythe.flipX = target.VisualCenter.x < hero.VisualCenter.x;
                    float fadeIn = Mathf.Clamp01((elapsed - scytheStart) / .12f);
                    float fadeOut = Mathf.Clamp01((soulStart - elapsed) / .12f);
                    scythe.color = new Color(.72f, .82f, 1f, fadeIn * fadeOut);
                }
                else if (scythe != null)
                    scythe.color = new Color(.72f, .82f, 1f, 0f);

                if (ghost != null && elapsed >= soulStart && elapsed < soulStart + soulDuration)
                {
                    float soulElapsed = elapsed - soulStart;
                    float soulProgress = Mathf.Clamp01(soulElapsed / soulDuration);
                    SetFrame(ghost, ghostFrames, soulElapsed, .075f);
                    ghost.transform.position = hit + Vector3.Lerp(Vector3.zero, new Vector3(.18f, 1.3f, 0f),
                        Mathf.SmoothStep(0f, 1f, soulProgress));
                    float soulScale = 1.25f + Mathf.Sin(soulProgress * Mathf.PI) * .12f;
                    ghost.transform.localScale = Vector3.one * soulScale;
                    float soulAlpha = Mathf.Clamp01(Mathf.Min(soulElapsed / .12f, (1f - soulProgress) * 2f));
                    ghost.color = new Color(.78f, .84f, 1f, soulAlpha);
                    ghost.flipX = target.MainSpriteRenderer != null && target.MainSpriteRenderer.flipX;
                }
                else if (ghost != null)
                    ghost.color = new Color(.78f, .84f, 1f, 0f);
            }
            else
            {
                // Warrior VFX are positioned on enemies by UpdateWarriorBursts; keep
                // the Warrior's own attack animation clean, with no aura or slash overlay.
            }
        }

        private void UpdateWarriorBursts(float elapsed)
        {
            foreach (WarriorVfxBurst burst in warriorBursts)
            {
                if (burst.renderer == null || burst.target == null || burst.frames == null || burst.frames.Length == 0)
                    continue;

                float age = elapsed - burst.startTime;
                bool visible = age >= 0f && age < burst.duration && burst.target.CurrentHealth > 0;
                burst.renderer.enabled = visible;
                if (!visible) continue;

                int frameIndex = Mathf.Clamp(Mathf.FloorToInt(age / (burst.duration / burst.frames.Length)),
                    0, burst.frames.Length - 1);
                burst.renderer.sprite = burst.frames[frameIndex];
                burst.renderer.transform.position = burst.target.VisualCenter
                    + new Vector3(burst.offset.x, burst.offset.y, -0.65f);
                burst.renderer.transform.rotation = Quaternion.Euler(0f, 0f, burst.rotation);

                float targetWidth = 1f, targetHeight = 1.4f;
                if (burst.target.MainSpriteRenderer != null)
                {
                    Bounds bounds = burst.target.MainSpriteRenderer.bounds;
                    targetWidth = Mathf.Max(.45f, bounds.size.x);
                    targetHeight = Mathf.Max(.6f, bounds.size.y);
                    burst.renderer.sortingLayerID = burst.target.MainSpriteRenderer.sortingLayerID;
                    burst.renderer.sortingOrder = burst.target.MainSpriteRenderer.sortingOrder + burst.sortingOffset;
                }
                Vector2 spriteSize = burst.renderer.sprite.bounds.size;
                burst.renderer.transform.localScale = new Vector3(
                    targetWidth * burst.sizeMultiplier / Mathf.Max(.01f, spriteSize.x),
                    targetHeight * burst.sizeMultiplier / Mathf.Max(.01f, spriteSize.y), 1f);
            }
        }

        private sealed class WarriorVfxBurst
        {
            public readonly CombatantView target;
            public readonly Sprite[] frames;
            public readonly float startTime, duration, rotation, sizeMultiplier;
            public readonly Vector2 offset;
            public readonly int sortingOffset;
            public readonly SpriteRenderer renderer;

            public WarriorVfxBurst(Transform cutsceneRoot, CombatantView target, Sprite[] frames, float startTime, float duration,
                Vector2 offset, float rotation, float sizeMultiplier, int sortingOffset)
            {
                this.target = target;
                this.frames = frames;
                this.startTime = startTime;
                this.duration = duration;
                this.offset = offset;
                this.rotation = rotation;
                this.sizeMultiplier = sizeMultiplier;
                this.sortingOffset = sortingOffset;
                var effect = new GameObject("Warrior slash VFX on enemy");
                effect.transform.SetParent(cutsceneRoot, false);
                renderer = effect.AddComponent<SpriteRenderer>();
                renderer.enabled = false;
            }
        }

        private sealed class WarriorAfterimage
        {
            private const float Lifetime = .24f;
            private readonly GameObject gameObject;
            private readonly SpriteRenderer renderer;
            private readonly Vector3 origin;
            private readonly Vector3 trailDirection;
            private readonly Color startColor;
            private readonly float delay;

            public WarriorAfterimage(Transform cutsceneRoot, SpriteRenderer source, Vector3 direction,
                float delay, float opacity, int index)
            {
                this.delay = delay;
                origin = source.transform.position;
                trailDirection = -direction;
                startColor = source.color;
                startColor.a *= opacity;
                gameObject = new GameObject("Warrior vanish blur " + index);
                gameObject.transform.SetParent(cutsceneRoot, true);
                gameObject.transform.position = origin + trailDirection * (.06f + index * .055f);
                gameObject.transform.rotation = source.transform.rotation;
                gameObject.transform.localScale = source.transform.lossyScale;
                renderer = gameObject.AddComponent<SpriteRenderer>();
                renderer.sprite = source.sprite;
                renderer.flipX = source.flipX;
                renderer.flipY = source.flipY;
                renderer.drawMode = source.drawMode;
                renderer.size = source.size;
                renderer.sharedMaterial = source.sharedMaterial;
                renderer.maskInteraction = source.maskInteraction;
                renderer.sortingLayerID = source.sortingLayerID;
                renderer.sortingOrder = source.sortingOrder + 3 + index;
                renderer.color = startColor;
                renderer.enabled = false;
            }

            public void Update(float elapsed)
            {
                float localTime = elapsed - delay;
                if (renderer == null || localTime < 0f) return;
                float progress = Mathf.Clamp01(localTime / Lifetime);
                renderer.enabled = progress < 1f;
                if (!renderer.enabled) return;
                gameObject.transform.position = origin + trailDirection * (.06f + progress * .2f);
                Color color = startColor;
                color.a *= 1f - progress;
                renderer.color = color;
            }

            public void Dispose()
            {
                if (gameObject != null) UnityEngine.Object.Destroy(gameObject);
            }
        }

        private static float LightningNoise(float seed)
        {
            return Mathf.Repeat(Mathf.Sin(seed * 12.9898f + 78.233f) * 43758.5453f, 1f) * 2f - 1f;
        }

        private static Vector3 ArcherLightningPoint(Vector3 contact, float seed, float progress)
        {
            // End exactly on the enemy; coarse bends give the bolt an irregular silhouette.
            float index = Mathf.Round(progress * 16f);
            float envelope = Mathf.Sin(progress * Mathf.PI);
            float bend = LightningNoise(seed + Mathf.Floor(index / 3f) * 11f) * .28f;
            float detail = LightningNoise(seed + index * 5f) * .16f;
            return contact + new Vector3((bend + detail) * envelope,
                Mathf.Lerp(3.2f, 0f, progress), 0f);
        }

        private static Sprite[] LoadFrameSprites(string path)
        {
            Texture2D[] textures = Resources.LoadAll<Texture2D>(path);
            System.Array.Sort(textures, (left, right) => System.StringComparer.Ordinal.Compare(left.name, right.name));
            var sprites = new Sprite[textures.Length];
            for (int i = 0; i < textures.Length; i++)
            {
                Texture2D texture = textures[i];
                sprites[i] = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 48f);
            }
            return sprites;
        }

        private static void SetFrame(SpriteRenderer renderer, Sprite[] frames, float elapsed, float frameSeconds)
        {
            if (frames == null || frames.Length == 0) return;
            renderer.sprite = frames[Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0f, elapsed) / frameSeconds), 0, frames.Length - 1)];
        }

        public void Dispose()
        {
            foreach (WarriorAfterimage afterimage in warriorAfterimages)
                afterimage.Dispose();
            warriorAfterimages.Clear();
            if (root != null) UnityEngine.Object.Destroy(root);
            if (material != null) UnityEngine.Object.Destroy(material);
            if (pixel != null) UnityEngine.Object.Destroy(pixel);
            if (pixelTexture != null) UnityEngine.Object.Destroy(pixelTexture);
            if (lightningArrowFrames != null)
                foreach (Sprite frame in lightningArrowFrames)
                    if (frame != null) UnityEngine.Object.Destroy(frame);
            if (scytheFrames != null)
                foreach (Sprite frame in scytheFrames)
                    if (frame != null) UnityEngine.Object.Destroy(frame);
            if (ghostFrames != null)
                foreach (Sprite frame in ghostFrames)
                    if (frame != null) UnityEngine.Object.Destroy(frame);
            if (warriorSlashFrames != null)
                foreach (Sprite frame in warriorSlashFrames)
                    if (frame != null) UnityEngine.Object.Destroy(frame);
            if (warriorImpactFrames != null)
                foreach (Sprite frame in warriorImpactFrames)
                    if (frame != null) UnityEngine.Object.Destroy(frame);
        }
    }

    private Sprite ResolveHudPortrait()
    {
        if (SelectedHero.IsMage && mageHudPortrait != null) return mageHudPortrait;
        if (SelectedHero.IsArcher && archerHudPortrait != null) return archerHudPortrait;
        return warriorHudPortrait != null ? warriorHudPortrait : heroData != null ? heroData.Image : null;
    }

    private static void ApplyPlayerHudLayout(Sprite portrait, HeroView hero)
    {
        GameObject hudObject = GameObject.Find("PlaayerHUD");
        Transform portraitFrame = hudObject != null ? FindDescendant(hudObject.transform, "PortraitFrame") : null;
        if (portraitFrame != null)
        {
            portraitFrame.gameObject.SetActive(true);
            if (portraitFrame.GetComponent<UnityEngine.UI.RectMask2D>() == null)
                portraitFrame.gameObject.AddComponent<UnityEngine.UI.RectMask2D>();
        }

        BattleHudMedieval.Apply(portrait, hero);
    }
}

/// <summary>
/// Applies the ToffeeCraft medieval sprite sheet to the battle HUD while
/// retaining the existing five mana bubbles and health slider behavior.
/// </summary>
public static class BattleHudMedieval
{
    private const string AtlasResourcePath = "ToffeeCraft/MediavelFree";
    private static Texture2D atlas;
    private static Sprite portraitFrameSprite;
    private static Sprite healthBarSprite;
    private static Sprite healthFillSprite;
    private static Sprite epStarEmptySprite;
    private static Sprite epStarFillSprite;
    private static readonly Image[] epStarFills = new Image[5];
    private static int currentEp;
    private static int cardsPlayedTowardEp;
    private static int enemyHitsTowardEp;
    private static Button epSkillButton;
    private static Image epSkillBackground;
    private static Image epSkillIcon;
    private static Outline epSkillOutline;
    private static CanvasGroup epSkillCanvasGroup;
    private static TMP_Text epCounterText;
    public static string SpecialSkillTitle => SelectedHero.IsArcher ? "Thunderfall Arrow"
        : SelectedHero.IsMage ? "Soul Eclipse" : "Mugengiri Higan";

    public static string SpecialSkillDescription => SelectedHero.IsArcher
        ? "Loose a giant lightning arrow and call down thunder. Deal 200% ATK damage to all enemies."
        : SelectedHero.IsMage
            ? "Unleash a spectral scythe to reap enemy souls. Deal 200% ATK damage to all enemies."
            : "Vanish in a blur and unleash countless slashes. Deal 200% ATK damage to all enemies.";

    private const int MaxEp = 5;
    private const int EventsPerEpStar = 2;
    public static void ResetEpMeter()
    {
        currentEp = 0;
        cardsPlayedTowardEp = 0;
        enemyHitsTowardEp = 0;
        for (int i = 0; i < epStarFills.Length; i++) epStarFills[i] = null;
        epSkillButton = null;
        epSkillBackground = null;
        epSkillIcon = null;
        epSkillOutline = null;
        epSkillCanvasGroup = null;
        epCounterText = null;
        RefreshEpMeter();
    }

    public static void GainEpFromCard()
    {
        cardsPlayedTowardEp++;
        if (cardsPlayedTowardEp >= EventsPerEpStar)
        {
            cardsPlayedTowardEp -= EventsPerEpStar;
            AddEp(1);
        }
    }

    public static void GainEpFromEnemyHit()
    {
        enemyHitsTowardEp++;
        if (enemyHitsTowardEp >= EventsPerEpStar)
        {
            enemyHitsTowardEp -= EventsPerEpStar;
            AddEp(1);
        }
    }

    public static void GainEpFromTurnEnd()
    {
        AddEp(1);
    }

    private static void AddEp(int amount)
    {
        currentEp = Mathf.Clamp(currentEp + amount, 0, MaxEp);
        RefreshEpMeter();
    }

    public static bool TrySpendFullEp()
    {
        if (currentEp < MaxEp) return false;
        currentEp = 0;
        RefreshEpMeter();
        return true;
    }

    public static void UpdateEpSkillAvailability()
    {
        if (epSkillButton == null) return;
        bool resolving = ActionSystem.Instance != null && ActionSystem.Instance.IsPerforming;
        bool ready = currentEp >= MaxEp && !GameControlsGuide.GameplayOverlayHidden && !resolving && !QuestionUI.IsQuestionActive &&
            !BattleTutorialGuide.BlocksInput && !EncounterBattleReturn.BattleEnded &&
            EnemySystem.Instance != null && EnemySystem.Instance.Enemies.Exists(enemy => enemy != null && enemy.CurrentHealth > 0);
        epSkillButton.interactable = ready;
        if (epSkillBackground != null)
            epSkillBackground.color = ready ? new Color(1f, 0.92f, 0.68f, 1f) : new Color(0.82f, 0.72f, 0.57f, 1f);
        if (epSkillOutline != null)
        {
            epSkillOutline.effectColor = ready ? new Color(1f, 0.83f, 0.3f, 1f) : new Color(0.62f, 0.46f, 0.25f, 0.9f);
            epSkillOutline.effectDistance = ready ? new Vector2(4f, -4f) : new Vector2(2f, -2f);
        }
        if (epSkillCanvasGroup != null)
        {
            epSkillCanvasGroup.alpha = ready ? 1f : 0f;
            epSkillCanvasGroup.interactable = ready;
            epSkillCanvasGroup.blocksRaycasts = ready;
        }
        if (epSkillIcon != null) epSkillIcon.color = Color.white;
    }

    private static void RefreshEpMeter()
    {
        for (int i = 0; i < epStarFills.Length; i++)
            if (epStarFills[i] != null) epStarFills[i].fillAmount = currentEp > i ? 1f : 0f;

        if (epCounterText != null)
            epCounterText.text = $"EP {currentEp}/{MaxEp}";
        UpdateEpSkillAvailability();
    }

    public static void Apply(Sprite portrait, HeroView hero)
    {
        GameObject hudObject = GameObject.Find("PlaayerHUD");
        if (hudObject == null) return;
        hudObject.SetActive(true);

        // Build the EP meter independently so the combat resource still works
        // if the optional medieval HUD frame atlas cannot be loaded.
        ApplyEpStar(hudObject.GetComponentInParent<Canvas>());

        // Configure the authored health slider even when the optional portrait
        // art atlas is unavailable. Health must remain a horizontal slider in
        // every battle setup path.
        if (healthFillSprite == null)
            healthFillSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        Slider authoredHealth = FindDescendant(hudObject.transform, "Player Health Bar")?.GetComponent<Slider>();
        TMP_Text authoredHealthLabel = FindDescendant(hudObject.transform, "Player Health Text")?.GetComponent<TMP_Text>();
        if (authoredHealth != null)
        {
            ApplyHealthBar(authoredHealth);
            if (hero != null) hero.ConfigureHudHealthBar(authoredHealth, authoredHealthLabel);
        }

        if (atlas == null) atlas = Resources.Load<Texture2D>(AtlasResourcePath);
        if (atlas == null)
        {
            Debug.LogWarning("Medieval battle HUD sprite sheet is missing from Resources/ToffeeCraft.");
            return;
        }

        atlas.filterMode = FilterMode.Point;
        if (portraitFrameSprite == null) portraitFrameSprite = Slice("HUD portrait frame", new Rect(0, 45, 76, 77), new Vector4(12, 12, 12, 12));
        if (healthBarSprite == null) healthBarSprite = Slice("HUD health bar", new Rect(2, 18, 61, 12), new Vector4(8, 4, 8, 4));
        if (healthFillSprite == null) healthFillSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));

        var hudRect = hudObject.GetComponent<RectTransform>();
        var hudImage = hudObject.GetComponent<UnityEngine.UI.Image>();
        if (hudRect == null || hudImage == null) return;

        if (hudRect.sizeDelta == Vector2.zero)
        {
            hudRect.anchorMin = hudRect.anchorMax = new Vector2(0, 1);
            hudRect.pivot = new Vector2(0, 1);
            hudRect.anchoredPosition = new Vector2(16, -14);
            hudRect.sizeDelta = new Vector2(456, 118);
        }
        hudImage.enabled = false;
        hudImage.raycastTarget = false;

        ApplyPortrait(hudRect, portrait);
        if (authoredHealth == null)
        {
            RectTransform playerHealthBar = hero != null ? hero.HealthSliderRect : null;
            ApplyHealthBar(hudRect, playerHealthBar);
            if (hero != null) hero.ConfigureHudHealthBar(playerHealthBar);
        }
        ApplyManaBubbles();
        ApplyPlayerStatusIcons(hudRect, hero);
        RemoveCoinBalance(hudRect);

        // This prefab was authored for exploration. Its other labels, XP bar,
        // and panel should not remain visible behind the battle-specific HUD.
        GameObject legacyHud = GameObject.Find("Battle Player Health HUD");
        if (legacyHud != null) legacyHud.SetActive(false);
    }

    private static Sprite Slice(string spriteName, Rect rect, Vector4 border)
    {
        Sprite sprite = Sprite.Create(atlas, rect, new Vector2(0.5f, 0.5f), 100, 0,
            SpriteMeshType.FullRect, border, false);
        sprite.name = spriteName;
        return sprite;
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root == null) return null;
        if (root.name == objectName) return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform match = FindDescendant(root.GetChild(i), objectName);
            if (match != null) return match;
        }

        return null;
    }

    private static void ApplyPortrait(RectTransform hudRect, Sprite portrait)
    {
        var info=Resources.Load<CharacterInfoData>("UI/CharacterInfo");
        var sharedPortrait=info != null ? info.GetPortraitSprite(Mathf.Clamp(PlayerPrefs.GetInt("SelectedCharacter",0),0,2)) : null;
        if(sharedPortrait != null)portrait=sharedPortrait;
        Transform frameTransform = FindDescendant(hudRect, "PortraitFrame");
        Transform iconTransform = FindDescendant(hudRect, "PortraitIcon");
        if (frameTransform == null || iconTransform == null) return;

        GameObject frameObject = frameTransform.gameObject;
        GameObject iconObject = iconTransform.gameObject;
        frameObject.SetActive(true);
        iconObject.SetActive(true);

        var frameRect = frameObject.GetComponent<RectTransform>();
        var frameImage = frameObject.GetComponent<UnityEngine.UI.Image>();
        if (frameRect == null || frameImage == null) return;

        // Keep the portrait frame's parent and RectTransform authored in the
        // scene. Runtime setup should update its appearance, not its layout.
        if (frameImage.sprite == null) frameImage.sprite = portraitFrameSprite;
        if (frameImage.sprite == portraitFrameSprite) frameImage.type = UnityEngine.UI.Image.Type.Sliced;
        // Keep the portrait clickable when the saved battle codex button is attached.
        frameImage.raycastTarget = frameObject.GetComponent<Button>() != null;

        var iconRect = iconObject.GetComponent<RectTransform>();
        var icon = iconObject.GetComponent<UnityEngine.UI.Image>();
        if (iconRect == null || icon == null) return;
        // PortraitIcon remains where it was placed under PortraitFrame in the
        // Hierarchy; only the selected character sprite changes at runtime.
        if (portrait != null) icon.sprite = portrait;
        // Use the same artwork and aspect ratio as Character Information.
        icon.preserveAspect = true;
        icon.raycastTarget = false;
    }

    private static void ApplyHealthBar(RectTransform hudRect, RectTransform sliderRect)
    {
        GameObject sliderObject = sliderRect != null ? sliderRect.gameObject : null;
        if (sliderObject != null) sliderObject.SetActive(true);
        var slider = sliderObject != null ? sliderObject.GetComponent<UnityEngine.UI.Slider>() : null;
        if (slider == null) return;

        var barRect = slider.GetComponent<RectTransform>();
        barRect.SetParent(hudRect, false);
        barRect.anchorMin = barRect.anchorMax = new Vector2(0, 1);
        barRect.pivot = new Vector2(0, 1);
        // Keep the bar at a fixed panel width. The hero's health ratio changes
        // the fill amount only, never the overall bar size.
        barRect.anchoredPosition = new Vector2(10, -8);
        barRect.sizeDelta = new Vector2(320, 42);
        barRect.localScale = Vector3.one;
        slider.interactable = false;

        var backgroundTransform = slider.transform.Find("Background") as RectTransform;
        if (backgroundTransform != null)
        {
            backgroundTransform.anchorMin = new Vector2(0, 0.03f);
            backgroundTransform.anchorMax = new Vector2(1, 0.97f);
            backgroundTransform.anchoredPosition = Vector2.zero;
            backgroundTransform.sizeDelta = Vector2.zero;
        }

        var fillArea = slider.transform.Find("Fill Area") as RectTransform;
        if (fillArea != null)
        {
            fillArea.anchorMin = new Vector2(0, 0.03f);
            fillArea.anchorMax = new Vector2(1, 0.97f);
            fillArea.anchoredPosition = Vector2.zero;
            fillArea.sizeDelta = new Vector2(-10, 0);
        }

        var background = backgroundTransform != null ? backgroundTransform.GetComponent<UnityEngine.UI.Image>() : null;
        if (background != null)
        {
            background.sprite = healthBarSprite;
            background.type = UnityEngine.UI.Image.Type.Sliced;
            // Keep a dark green empty track under the green health fill.
            background.color = new Color(0.16f, 0.25f, 0.14f, 1f);
            background.raycastTarget = false;
        }

        var fill = slider.fillRect != null ? slider.fillRect.GetComponent<UnityEngine.UI.Image>() : null;
        if (fill != null)
        {
            // Slider already sizes its Fill Rect from the health ratio. A flat sprite
            // prevents the atlas bar's rounded ends from stretching with that width.
            fill.sprite = healthFillSprite;
            fill.type = UnityEngine.UI.Image.Type.Simple;
            fill.color = new Color(0.25f, 0.78f, 0.28f, 1f);
            fill.raycastTarget = false;
        }
    }

    private static void ApplyHealthBar(Slider slider)
    {
        if (slider == null) return;
        slider.gameObject.SetActive(true);
        // Keep slider dimensions, anchors, and position under scene authoring.
        slider.direction = Slider.Direction.LeftToRight;
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        Transform backgroundTransform = slider.transform.Find("Background");
        Transform fillArea = slider.transform.Find("Fill Area");
        if (backgroundTransform != null)
        {
            Image background = backgroundTransform.GetComponent<Image>();
            if (background != null)
            {
                // Keep this authored slider's empty track neutral. The old
                // atlas health sprite is red and turns the short bar into a
                // red block when stretched horizontally.
                background.sprite = healthFillSprite;
                background.type = Image.Type.Simple;
                background.color = new Color(0.12f, 0.2f, 0.12f, 1f);
                background.raycastTarget = false;
            }
        }
        if (fillArea != null)
        {
            Image fill = slider.fillRect != null ? slider.fillRect.GetComponent<Image>() : null;
            if (fill != null)
            {
                if (healthFillSprite != null) fill.sprite = healthFillSprite;
                fill.type = Image.Type.Simple;
                fill.color = new Color(0.25f, 0.78f, 0.28f, 1f);
                fill.raycastTarget = false;
            }
        }
    }

    private static void ApplyManaBubbles()
    {
        var manaUI = Object.FindFirstObjectByType<ManaUI>(FindObjectsInactive.Include);
        if (manaUI == null) return;
        // ManaUI reads the scene-authored bubble layout; it owns only the
        // visible/fill state, not placement or dimensions.
    }

    private static void ApplyEpStar(Canvas hudCanvas)
    {
        if (hudCanvas == null) return;

        EnsureEpStarSprites();
        Transform existing = hudCanvas.transform.Find("EP Star Meter");
        GameObject meterObject = existing != null
            ? existing.gameObject
            : new GameObject("EP Star Meter", typeof(RectTransform));
        if (meterObject.transform.parent != hudCanvas.transform)
            meterObject.transform.SetParent(hudCanvas.transform, false);

        var meterRect = meterObject.GetComponent<RectTransform>();
        if (meterRect.sizeDelta == Vector2.zero)
        {
            meterRect.anchorMin = meterRect.anchorMax = Vector2.zero;
            meterRect.pivot = Vector2.zero;
            meterRect.anchoredPosition = new Vector2(8f, 14f);
            meterRect.sizeDelta = new Vector2(84f, 320f);
        }

        // Reuse the original single-star objects as the first star when this
        // HUD is rebuilt during Play Mode/domain reload.
        Transform oldEmptyStar = meterRect.Find("Empty Star");
        if (oldEmptyStar != null && meterRect.Find("EP Star 1 Empty") == null)
            oldEmptyStar.name = "EP Star 1 Empty";
        Transform oldEnergyFill = meterRect.Find("Energy Fill");
        if (oldEnergyFill != null && meterRect.Find("EP Star 1 Fill") == null)
            oldEnergyFill.name = "EP Star 1 Fill";

        // Five independent stars fill one by one as cards are used or combatants
        // lose health. The special skill consumes all five.
        const int starCount = 5;
        const float starSize = 48f;
        const float starSpacing = 52f;
        for (int i = 0; i < starCount; i++)
        {
            float y = 16f + i * starSpacing;
            Image baseStar = GetOrCreateImage(meterRect, $"EP Star {i + 1} Empty", epStarEmptySprite);
            if (baseStar.rectTransform.sizeDelta == Vector2.zero)
                PositionEpStarImage(baseStar.rectTransform, new Vector2(-4f, y), new Vector2(starSize, starSize));

            Image energyFill = GetOrCreateImage(meterRect, $"EP Star {i + 1} Fill", epStarFillSprite);
            if (energyFill.rectTransform.sizeDelta == Vector2.zero)
                PositionEpStarImage(energyFill.rectTransform, new Vector2(-4f, y), new Vector2(41f, 41f));
            energyFill.type = Image.Type.Filled;
            energyFill.fillMethod = Image.FillMethod.Vertical;
            energyFill.fillOrigin = (int)Image.OriginVertical.Bottom;
            energyFill.fillAmount = currentEp > i ? 1f : 0f;
            energyFill.color = Color.white;
            epStarFills[i] = energyFill;
        }

        Image counterBacking = GetOrCreateImage(meterRect, "EP Counter Background", null);
        if (counterBacking.sprite == null) BattleUITheme.DecoratePanel(counterBacking);
        if (counterBacking.rectTransform.sizeDelta == Vector2.zero)
            PositionEpStarImage(counterBacking.rectTransform, new Vector2(-4f, 280f), new Vector2(72f, 34f));
        counterBacking.raycastTarget = false;
        Outline counterOutline = counterBacking.GetComponent<Outline>();
        if (counterOutline == null) counterOutline = counterBacking.gameObject.AddComponent<Outline>();
        counterOutline.effectColor = new Color(0.35f, 0.78f, 1f, 0.95f);
        counterOutline.effectDistance = new Vector2(2f, -2f);

        Transform labelTransform = meterRect.Find("EP Label");
        TextMeshProUGUI label;
        if (labelTransform == null)
        {
            var labelObject = new GameObject("EP Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(meterRect, false);
            label = labelObject.GetComponent<TextMeshProUGUI>();
        }
        else label = labelTransform.GetComponent<TextMeshProUGUI>();

        if (label != null)
        {
            label.text = $"EP {currentEp}/{MaxEp}";
            if (label.font == null) label.font = TMP_Settings.defaultFontAsset;
            if (label.fontSize <= 0f) label.fontSize = 16f;
            if (label.color == default) label.color = new Color(0.72f, 0.93f, 1f, 1f);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.raycastTarget = false;
            var labelRect = label.rectTransform;
            if (labelRect.sizeDelta == Vector2.zero)
            {
                labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0f);
                labelRect.pivot = new Vector2(0.5f, 0f);
                labelRect.anchoredPosition = new Vector2(-4f, 280f);
                labelRect.sizeDelta = new Vector2(72f, 34f);
            }
            epCounterText = label;
        }

        CreateGoldSkillCard(hudCanvas.transform);
        RefreshEpMeter();
    }

    private static void CreateGoldSkillCard(Transform canvasTransform)
    {
        Transform existing = canvasTransform.Find("EP Gold Skill Card");
        GameObject cardObject = existing != null
            ? existing.gameObject
            : new GameObject("EP Gold Skill Card", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        if (cardObject.transform.parent != canvasTransform) cardObject.transform.SetParent(canvasTransform, false);

        RectTransform cardRect = cardObject.GetComponent<RectTransform>();
        if (cardRect.sizeDelta == Vector2.zero)
        {
            cardRect.anchorMin = cardRect.anchorMax = Vector2.zero;
            cardRect.pivot = Vector2.zero;
            cardRect.anchoredPosition = new Vector2(98f, 100f);
            cardRect.sizeDelta = new Vector2(116f, 154f);
        }

        Image background = cardObject.GetComponent<Image>();
        if (background.sprite == null) BattleUITheme.DecoratePanel(background);
        if (background.color == Color.white) background.color = new Color(0.92f, 0.76f, 0.5f, 1f);
        background.raycastTarget = true;
        epSkillBackground = background;
        epSkillCanvasGroup = cardObject.GetComponent<CanvasGroup>();
        if (epSkillCanvasGroup == null) epSkillCanvasGroup = cardObject.AddComponent<CanvasGroup>();
        // The Button controls readiness. Keep the CanvasGroup enabled so the
        // card remains part of normal UI input and inherits Settings visibility.
        epSkillCanvasGroup.interactable = true;
        epSkillCanvasGroup.blocksRaycasts = true;
        epSkillCanvasGroup.ignoreParentGroups = false;
        epSkillOutline = cardObject.GetComponent<Outline>();
        if (epSkillOutline == null) epSkillOutline = cardObject.AddComponent<Outline>();
        epSkillOutline.effectColor = new Color(0.72f, 0.52f, 0.22f, 1f);
        epSkillOutline.effectDistance = new Vector2(2f, -2f);

        Button button = cardObject.GetComponent<Button>();
        button.targetGraphic = background;
        // Readiness is shown by fading the entire card until all five EP stars fill.
        button.transition = Selectable.Transition.None;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Color.white;
        colors.disabledColor = Color.white;
        button.colors = colors;
        button.onClick.RemoveListener(OnEpSkillClick);
        button.onClick.AddListener(OnEpSkillClick);

        CreateEpCardText(cardRect, "EP Skill Title", SpecialSkillTitle, new Vector2(0f, -8f), new Vector2(106f, 24f),
            TextAlignmentOptions.Center, 15f, new Color(1f, 0.88f, 0.58f, 1f), topAnchored: true);
        Transform oldSkillIcon = cardRect.Find("EP Skill Star Icon");
        if (oldSkillIcon != null) oldSkillIcon.name = "EP Skill Icon";
        string skillArt = SelectedHero.IsArcher ? "Storm Volley" : SelectedHero.IsMage ? "Arcane Tempest" : "Runic Cleave";
        CardData skillCard = Resources.Load<CardData>("MerchantCards/" + skillArt);
        Sprite skillSprite = skillCard != null && skillCard.Image != null ? skillCard.Image : CreateEpSkillFallbackSprite();
        Image skillIcon = GetOrCreateImage(cardRect, "EP Skill Icon", skillSprite);
        if (skillIcon.rectTransform.sizeDelta == Vector2.zero)
            PositionEpStarImage(skillIcon.rectTransform, new Vector2(58f, 80f), new Vector2(42f, 42f));
        skillIcon.sprite = skillSprite;
        skillIcon.color = Color.white;
        skillIcon.preserveAspect = true;
        epSkillIcon = skillIcon;
        CreateEpCardText(cardRect, "EP Skill Description", SpecialSkillDescription, new Vector2(0f, 31f), new Vector2(106f, 42f),
            TextAlignmentOptions.Center, 12f, new Color(1f, 0.94f, 0.82f, 1f));
        Transform oldCharge = cardRect.Find("EP Skill Charge");
        if (oldCharge != null) oldCharge.gameObject.SetActive(false);

        var skillUi = cardObject.GetComponent<SpecialSkillCardUI>();
        if (skillUi == null) skillUi = cardObject.AddComponent<SpecialSkillCardUI>();
        skillUi.Configure();
        epSkillButton = button;
    }

    private static void OnEpSkillClick()
    {
        if (QuestionUI.IsQuestionActive || BattleTutorialGuide.BlocksInput || EncounterBattleReturn.BattleEnded ||
            ActionSystem.Instance == null || ActionSystem.Instance.IsPerforming || currentEp < MaxEp)
            return;
        ActionSystem.Instance.Perform(new StarfallSkillGA());
    }

    private static Sprite CreateEpSkillFallbackSprite()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Starfall Skill Icon Texture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        var pixels = new Color32[size * size];
        Vector2[] bolt =
        {
            new Vector2(38, 58), new Vector2(18, 34), new Vector2(29, 34),
            new Vector2(24, 7), new Vector2(47, 36), new Vector2(35, 36)
        };
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float dx = x - 31.5f;
            float dy = y - 31.5f;
            float radius = Mathf.Sqrt(dx * dx + dy * dy);
            Color32 pixel = new Color32(0, 0, 0, 0);
            if (radius <= 29f)
                pixel = radius >= 23f ? new Color32(255, 208, 91, 255) : new Color32(28, 56, 111, 240);
            if (IsInsidePolygon(new Vector2(x + 0.5f, y + 0.5f), bolt))
                pixel = new Color32(135, 226, 255, 255);
            pixels[y * size + x] = pixel;
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static TMP_Text CreateEpCardText(RectTransform parent, string name, string value, Vector2 position,
        Vector2 size, TextAlignmentOptions alignment, float fontSize, Color color, bool topAnchored = false)
    {
        Transform existing = parent.Find(name);
        TextMeshProUGUI text;
        if (existing != null)
        {
            text = existing.GetComponent<TextMeshProUGUI>();
            if (text == null) text = existing.gameObject.AddComponent<TextMeshProUGUI>();
        }
        else
        {
            var textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            text = textObject.GetComponent<TextMeshProUGUI>();
        }

        text.text = value;
        if (text.font == null) text.font = TMP_Settings.defaultFontAsset;
        if (text.fontSize <= 0f) text.fontSize = fontSize;
        if (text.color == default) text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(8f, fontSize - 2f);
        text.fontSizeMax = fontSize;
        text.raycastTarget = false;

        RectTransform rect = text.rectTransform;
        if (rect.sizeDelta == Vector2.zero)
        {
            rect.anchorMin = rect.anchorMax = topAnchored ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
            rect.pivot = topAnchored ? new Vector2(0.5f, 1f) : new Vector2(0.5f, 0f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }
        return text;
    }

    private static Image GetOrCreateImage(RectTransform parent, string objectName, Sprite sprite)
    {
        Transform existing = parent.Find(objectName);
        Image image;
        if (existing != null)
        {
            image = existing.GetComponent<Image>();
            if (image == null) image = existing.gameObject.AddComponent<Image>();
        }
        else
        {
            var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(parent, false);
            image = imageObject.GetComponent<Image>();
        }

        if (image.sprite == null) image.sprite = sprite;
        if (image.rectTransform.sizeDelta == Vector2.zero) image.preserveAspect = true;
        image.raycastTarget = image.gameObject.name == "EP Gold Skill Card";
        return image;
    }

    private static void PositionEpStarImage(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void EnsureEpStarSprites()
    {
        if (epStarEmptySprite == null)
            epStarEmptySprite = CreateEpStarSprite("EP Empty Star", outlineOnly: true);
        if (epStarFillSprite == null)
            epStarFillSprite = CreateEpStarSprite("EP Blue Energy Fill", outlineOnly: false);
    }

    private static Sprite CreateEpStarSprite(string spriteName, bool outlineOnly)
    {
        const int size = 128;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = spriteName + " Texture",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        Vector2[] vertices = MakeEpStarVertices(size, outlineOnly ? 58f : 51f, outlineOnly ? 28f : 24f);
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                float edgeDistance = DistanceToPolygon(point, vertices);
                bool inside = IsInsidePolygon(point, vertices);
                Color32 color = new Color32(0, 0, 0, 0);

                if (outlineOnly)
                {
                    if (inside && edgeDistance <= 4.5f)
                        color = new Color32(120, 225, 255, 255);
                    else if (inside)
                        color = new Color32(8, 27, 52, 220);
                    else if (edgeDistance < 11f)
                    {
                        byte alpha = (byte)Mathf.RoundToInt(65f * (1f - edgeDistance / 11f));
                        color = new Color32(32, 157, 255, alpha);
                    }
                }
                else if (inside)
                {
                    float vertical = Mathf.Clamp01(point.y / size);
                    color = new Color32(
                        (byte)Mathf.RoundToInt(Mathf.Lerp(13f, 26f, vertical)),
                        (byte)Mathf.RoundToInt(Mathf.Lerp(94f, 195f, vertical)),
                        (byte)Mathf.RoundToInt(Mathf.Lerp(220f, 255f, vertical)),
                        255);
                }

                pixels[y * size + x] = color;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Vector2[] MakeEpStarVertices(int size, float outerRadius, float innerRadius)
    {
        var vertices = new Vector2[10];
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        for (int i = 0; i < vertices.Length; i++)
        {
            float angle = Mathf.Deg2Rad * (-90f + i * 36f);
            float radius = i % 2 == 0 ? outerRadius : innerRadius;
            vertices[i] = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
        }
        return vertices;
    }

    private static bool IsInsidePolygon(Vector2 point, Vector2[] vertices)
    {
        bool inside = false;
        for (int i = 0, j = vertices.Length - 1; i < vertices.Length; j = i++)
        {
            Vector2 a = vertices[i];
            Vector2 b = vertices[j];
            bool crosses = (a.y > point.y) != (b.y > point.y) &&
                point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x;
            if (crosses) inside = !inside;
        }
        return inside;
    }

    private static float DistanceToPolygon(Vector2 point, Vector2[] vertices)
    {
        float minDistance = float.MaxValue;
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector2 a = vertices[i];
            Vector2 b = vertices[(i + 1) % vertices.Length];
            Vector2 edge = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(point - a, edge) / edge.sqrMagnitude);
            minDistance = Mathf.Min(minDistance, Vector2.Distance(point, a + edge * t));
        }
        return minDistance;
    }

    private static void RemoveCoinBalance(RectTransform hudRect)
    {
        foreach (string childName in new[] { "BattleCoinIcon", "BattleCoinCount" })
        {
            var child = hudRect.Find(childName);
            if (child != null) Object.Destroy(child.gameObject);
        }
    }

    private static void ApplyPlayerStatusIcons(RectTransform hudRect, HeroView hero)
    {
        if (hero == null || hero.StatusEffectsUI == null) return;
        // This replaces the old crossed-swords badge beneath the mana bubbles.
        Transform host = FindDescendant(hudRect, "Player Status Effects");
        hero.StatusEffectsUI.MoveToHud(host != null ? host as RectTransform : hudRect,
            new Vector2(96, -78), new Vector2(220, 32));
    }
}

public static class BattleUITheme
{
    private static readonly Color Brass = new Color(0.75f, 0.55f, 0.25f);
    private static readonly Color Parchment = new Color(0.95f, 0.87f, 0.7f);
    private static readonly Color Wood = new Color(0.2f, 0.12f, 0.065f);
    private static Sprite panelSprite;

    public static void DecoratePanel(UnityEngine.UI.Image image)
    {
        if (image == null) return;
        if (panelSprite == null)
        {
            var texture = new Texture2D(32, 32, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            var pixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    int edge = Mathf.Min(Mathf.Min(x, 31 - x), Mathf.Min(y, 31 - y));
                    pixels[y * 32 + x] = edge < 1 ? new Color(0.07f, 0.04f, 0.02f) :
                        edge < 3 ? Brass : edge < 5 ? new Color(0.35f, 0.22f, 0.1f) : Wood;
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            panelSprite = Sprite.Create(texture, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f),
                100, 0, SpriteMeshType.FullRect, new Vector4(6, 6, 6, 6));
        }
        image.sprite = panelSprite;
        image.type = UnityEngine.UI.Image.Type.Sliced;
        image.color = Color.white;
    }

    public static void DrawResultPanel(Rect rect, string text)
    {
        Color previous = GUI.color;
        GUI.color = Brass;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Wood;
        GUI.DrawTexture(new Rect(rect.x + 4, rect.y + 4, rect.width - 8, rect.height - 8), Texture2D.whiteTexture);
        GUI.color = previous;
        var style = new GUIStyle(GUI.skin.label) { fontSize = 28, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        style.normal.textColor = Parchment; UiTextColors.Stable(style);
        GUI.Label(new Rect(rect.x + 15, rect.y + 12, rect.width - 30, rect.height - 24), text, style);
    }

    public static bool DrawRetreatButton(Rect rect)
    {
        Color previousBackground = GUI.backgroundColor;
        Color previousContent = GUI.contentColor;
        GUI.backgroundColor = Wood;
        GUI.contentColor = Parchment;
        bool clicked = GUI.Button(rect, "Retreat");
        GUI.backgroundColor = previousBackground;
        GUI.contentColor = previousContent;
        return clicked;
    }

    public static void Apply()
    {
        foreach (var button in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var image = button.targetGraphic as UnityEngine.UI.Image;
            if (image != null)
            {
                DecoratePanel(image);
                var outline = image.GetComponent<UnityEngine.UI.Outline>();
                if (outline == null) outline = image.gameObject.AddComponent<UnityEngine.UI.Outline>();
                outline.effectColor = Brass;
                outline.effectDistance = new Vector2(2, -2);
            }
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.12f, 1.02f);
            colors.pressedColor = new Color(0.7f, 0.65f, 0.55f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.2f, 0.18f, 0.15f, 0.6f);
            button.colors = colors;
            foreach (var text in button.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                if (text.text.Trim().Equals("End Turn", System.StringComparison.OrdinalIgnoreCase) &&
                    button.GetComponent<BattleEndTurnState>() == null)
                    button.gameObject.AddComponent<BattleEndTurnState>();
                text.color = Parchment;
                text.enableAutoSizing = true;
                text.fontSizeMin = 16;
                text.fontSizeMax = 27;
                text.fontStyle = TMPro.FontStyles.Bold;
                text.alignment = TMPro.TextAlignmentOptions.Center;
                text.margin = new Vector4(12, 5, 12, 5);
            }
        }

        foreach (var question in Object.FindObjectsByType<QuestionUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            foreach (var image in question.GetComponentsInChildren<UnityEngine.UI.Image>(true))
            {
                if (image.GetComponentInParent<UnityEngine.UI.Button>() != null) continue;
                if (image.name == "Background" || image.gameObject == question.gameObject)
                {
                    DecoratePanel(image);
                    var outline = image.GetComponent<UnityEngine.UI.Outline>();
                    if (outline == null) outline = image.gameObject.AddComponent<UnityEngine.UI.Outline>();
                    outline.effectColor = Brass;
                    outline.effectDistance = new Vector2(3, -3);
                }
            }
            foreach (var text in question.GetComponentsInChildren<TMPro.TMP_Text>(true))
            {
                text.color = Parchment;
                text.enableAutoSizing = true;
                text.fontSizeMin = 17;
                text.fontSizeMax = text.name.Contains("Snippet") ? 26 : 32;
                text.margin = new Vector4(12, 8, 12, 8);
            }
        }
        foreach (var slider in Object.FindObjectsByType<UnityEngine.UI.Slider>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (slider.fillRect == null) continue;
            if (slider.GetComponentInParent<ExplorationHealthHud>() != null) continue;
            var image = slider.fillRect.GetComponent<UnityEngine.UI.Image>();
            if (image != null) image.color = slider.name.ToLowerInvariant().Contains("timer") ? Brass : new Color(0.7f, 0.1f, 0.075f);
        }
        foreach (var text in Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (text.name.Contains("Health") || text.name.Contains("Mana") || text.name.Contains("Attack"))
            { text.color = Parchment; text.fontStyle = TMPro.FontStyles.Bold; }
        foreach (var image in Object.FindObjectsByType<UnityEngine.UI.Image>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (image.name.Contains("Frame")) image.color = Brass;
    }
}

public class BattleEndTurnState : MonoBehaviour
{
    private UnityEngine.UI.Button button;
    private TMPro.TMP_Text label;
    private QuestionUI question;

    void Awake()
    {
        button = GetComponent<UnityEngine.UI.Button>();
        label = GetComponentInChildren<TMPro.TMP_Text>(true);
        question = Object.FindFirstObjectByType<QuestionUI>(FindObjectsInactive.Include);
    }

    void Update()
    {
        bool resolving = ActionSystem.Instance != null && ActionSystem.Instance.IsPerforming;
        bool answering = question != null && question.isActiveAndEnabled;
        button.interactable = !PlayerDeathFlow.IsActive && !resolving && !answering && !BattleTutorialGuide.BlocksInput && !EncounterBattleReturn.BattleEnded;
        if (label != null) label.text = EncounterBattleReturn.BattleEnded ? "Battle Complete" :
            answering ? "Answer Question" : resolving ? "Resolving..." : "End Turn";
    }
}

public class EncounterBattleReturn : MonoBehaviour
{
    public static bool BattleEnded { get; private set; }
    private bool returning;
    private bool caveBattle;
    private bool hadEnemies;
    private bool victory;
    private string encounterId;
    private string encounterScene;
    private RectTransform playerHealthPanel;
    private int encounterExperience, awardedExperience,encounterGold,awardedGold;

    public void Configure(bool fromCave, string id, string sourceScene = null)
    {
        BattleQuestionReport.BeginBattle(id);
        if(id==WardenQuest.FinalEncounter&&GetComponent<WardenFinalTrial>()==null)gameObject.AddComponent<WardenFinalTrial>();
        caveBattle = fromCave;
        encounterExperience=encounterGold=0;
        foreach(var enemy in EnemySystem.Instance.Enemies)if(enemy!=null)
        {encounterExperience+=enemy.ExperienceReward;encounterGold+=enemy.GoldReward;}
        encounterId = id;
        encounterScene = sourceScene;
        var healthHud = FindFirstObjectByType<ExplorationHealthHud>(FindObjectsInactive.Include);
        if (healthHud != null) playerHealthPanel = healthHud.transform.Find("Player Health Panel") as RectTransform;
        hadEnemies = EnemySystem.Instance.Enemies.Count > 0;
        BattleEnded = false;
    }

    void Update()
    {
        if (PlayerDeathFlow.IsActive || returning || BattleEnded || !hadEnemies || EnemySystem.Instance == null ||
            ActionSystem.Instance == null || ActionSystem.Instance.IsPerforming) return;
        bool heroDefeated = HeroSystem.Instance != null && HeroSystem.Instance.HeroView.CurrentHealth <= 0;
        if (heroDefeated)
        {
            BattleEnded = true;
            PlayerDeathFlow.BeginBattleDeath(HeroSystem.Instance.HeroView);
            return;
        }
        if (EnemySystem.Instance.Enemies.Count == 0 || heroDefeated)
        {
            victory = !heroDefeated && EnemySystem.Instance.Enemies.Count == 0;
            BattleEnded = true;
            BattleAudio.Play(victory ? BattleSound.Victory : BattleSound.Defeat);
            if (victory) PlayerAdventureStats.RecordVictory();
            if (victory) BattleBgmController.PlayVictory();
            if (CardViewHoverSystem.Instance != null) CardViewHoverSystem.Instance.Hide();
            if (victory && caveBattle && !string.IsNullOrEmpty(encounterId))
            {
                // Persist gold before marking the encounter defeated. A retry cannot duplicate gold.
                awardedGold=CharacterInventory.AwardEncounterGold(encounterScene,encounterId,encounterGold);
                awardedExperience=PlayerProgression.AwardEncounter(encounterId,encounterExperience,encounterScene);
            }
            StartCoroutine(EndBattle());
        }
    }

    IEnumerator EndBattle()
    {
        yield return BattleTutorialGuide.ExplainResult(victory);
        yield return BattleReportScroll.Present("VICTORY",awardedExperience,awardedGold);
        if (victory && caveBattle && !returning &&
            MapSceneNames.Resolve(encounterScene) == "Forest of Dawn" && encounterId == WardenQuest.FinalBossEncounter)
        {
            returning = true;
            FinalVictoryEnding.Show();
            yield break;
        }
        Return();
    }

    void Return(bool retreated = false)
    {
        if (returning) return;
        returning = true;
        Time.timeScale = 1f;
        var saved = SaveSlots.Read(SaveSlots.Active);
        if (caveBattle && saved != null && Application.CanStreamedLevelBeLoaded(saved.scene))
            GameSave.EnsureInstance().ReturnFromBattle(retreated);
        else SceneManager.LoadScene("MainMenu");
    }

    public void ReturnToLobby() => Return(retreated: !BattleEnded);

    void OnDestroy() { BattleEnded = false; }
}

/// <summary>Activates the five-star EP special skill from the battle HUD.</summary>
public sealed class StarfallSkillGA : GameAction { }
