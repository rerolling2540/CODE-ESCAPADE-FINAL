using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight card attack effects rendered with ordinary SpriteRenderers.
/// This deliberately avoids imported particle prefabs/materials so it works
/// with the project's built-in 2D renderer and does not render magenta.
/// </summary>
public static class CardAttackVfx
{
    private const string LibraryResourceName = "CardAttackVfxLibrary";
    private const int FireballColumns = 4;
    private const int FireballRows = 4;
    private const int ImpactColumns = 8;
    private const int ImpactRows = 8;
    private static CardAttackVfxLibrary library;
    private static bool libraryLoaded;
    private static Texture2D whiteTexture;
    private static Sprite whiteSprite;
    private static Texture2D cachedFireballTexture;
    private static Sprite[] fireballFrames;
    private static Texture2D cachedImpactTexture;
    private static Sprite[] impactFrames;

    public static void Play(
        HeroView owner,
        CardData card,
        CombatantView primaryTarget,
        IList<CombatantView> affectedTargets,
        Vector3 origin,
        SpriteRenderer sortingReference)
    {
        if (owner == null || card == null || !BattleAudio.IsAttackCard(card)) return;

        List<CombatantView> targets = CollectTargets(primaryTarget, affectedTargets);
        if (targets.Count == 0) return;

        int sortingLayer = sortingReference != null ? sortingReference.sortingLayerID : 0;
        int sortingOrder = sortingReference != null ? sortingReference.sortingOrder + 15 : 15;
        if(card.attackImpactFrames!=null&&card.attackImpactFrames.Length>0)
        {
            foreach(var target in targets)
                owner.StartCoroutine(PlayAuthoredImpact(card,target,sortingLayer,sortingOrder));
            return;
        }
        CardAttackVfxType type = ResolveType(card);

        switch (type)
        {
            case CardAttackVfxType.ArrowRain:
                owner.StartCoroutine(RainArrows(owner, targets, origin, sortingLayer, sortingOrder));
                break;
            case CardAttackVfxType.Fireball:
                owner.StartCoroutine(LaunchAtTargets(owner, targets, origin, sortingLayer, sortingOrder, true));
                break;
            case CardAttackVfxType.Arrow:
                owner.StartCoroutine(LaunchAtTargets(owner, targets, origin, sortingLayer, sortingOrder, false, true));
                break;
            case CardAttackVfxType.ArcaneBolt:
                owner.StartCoroutine(LaunchAtTargets(owner, targets, origin, sortingLayer, sortingOrder, false));
                break;
            case CardAttackVfxType.MeleeSlash:
                owner.StartCoroutine(SlashTargets(owner, targets, sortingLayer, sortingOrder));
                break;
            case CardAttackVfxType.Impact:
                foreach (CombatantView target in targets)
                    owner.StartCoroutine(PlayImpact(owner, TargetPoint(target), sortingLayer, sortingOrder, 0.72f, 0.42f));
                break;
        }
    }

    private static IEnumerator PlayAuthoredImpact(CardData card,CombatantView target,int sortingLayer,int sortingOrder)
    {
        if(target==null)yield break;
        var frames=card.attackImpactFrames;
        float largest=0f;
        foreach(var frame in frames)if(frame!=null)largest=Mathf.Max(largest,frame.bounds.size.x,frame.bounds.size.y);
        if(largest<=0f)yield break;
        var effect=new GameObject("Card VFX "+card.name,typeof(SpriteRenderer));
        var renderer=effect.GetComponent<SpriteRenderer>();
        renderer.sortingLayerID=sortingLayer;renderer.sortingOrder=sortingOrder;
        effect.transform.position=TargetPoint(target);
        effect.transform.localScale=Vector3.one*(Mathf.Clamp(target.VisualBounds.size.y*1.15f,2.4f,5f)*Mathf.Clamp(card.attackImpactScale,.5f,2f)/largest);
        float fps=Mathf.Clamp(card.attackImpactFramesPerSecond,8f,40f);
        float duration=frames.Length/fps;
        // Scheduled cleanup also handles an owner coroutine stopped by death or disabling.
        Object.Destroy(effect,duration+.2f);
        try
        {
            float elapsed=0f;
            while(elapsed<duration&&effect!=null)
            {
                if(target!=null)effect.transform.position=TargetPoint(target);
                renderer.sprite=frames[Mathf.Min(frames.Length-1,Mathf.FloorToInt(elapsed*fps))];
                elapsed+=Time.deltaTime;
                yield return null;
            }
        }
        finally{if(effect!=null)Object.Destroy(effect);}
    }

    private static List<CombatantView> CollectTargets(CombatantView primaryTarget, IList<CombatantView> affectedTargets)
    {
        List<CombatantView> result = new();
        if (affectedTargets != null)
        {
            foreach (CombatantView candidate in affectedTargets)
                if (candidate != null && candidate.CurrentHealth > 0 && !result.Contains(candidate))
                    result.Add(candidate);
        }
        if (result.Count == 0 && primaryTarget != null && primaryTarget.CurrentHealth > 0)
            result.Add(primaryTarget);
        return result;
    }

    private static CardAttackVfxType ResolveType(CardData card)
    {
        if (card.attackVfxType != CardAttackVfxType.Auto) return card.attackVfxType;

        string name = card.name.ToLowerInvariant();
        if (name.Contains("rain") || name.Contains("volley")) return CardAttackVfxType.ArrowRain;
        if (name.Contains("fire") || name.Contains("flame") || name.Contains("burn") || name.Contains("ignite"))
            return CardAttackVfxType.Fireball;
        if (name.Contains("arrow") || name.Contains("shot") || name.Contains("pierc")) return CardAttackVfxType.Arrow;
        if (SelectedHero.IsArcher) return CardAttackVfxType.Arrow;
        if (SelectedHero.IsMage) return CardAttackVfxType.ArcaneBolt;
        return CardAttackVfxType.MeleeSlash;
    }

    private static IEnumerator LaunchAtTargets(
        HeroView owner,
        List<CombatantView> targets,
        Vector3 origin,
        int sortingLayer,
        int sortingOrder,
        bool useFireball,
        bool useArrow = false)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            CombatantView target = targets[i];
            if (target != null && target.CurrentHealth > 0)
            {
                Vector3 destination = TargetPoint(target);
                if (useArrow)
                    owner.StartCoroutine(TravelArrow(owner, origin, destination, sortingLayer, sortingOrder, i % 2 == 0));
                else
                    owner.StartCoroutine(TravelOrb(owner, origin, destination, sortingLayer, sortingOrder, useFireball));
            }
            if (i + 1 < targets.Count) yield return new WaitForSeconds(0.055f);
        }
    }

    private static IEnumerator TravelOrb(
        HeroView owner, Vector3 start, Vector3 end, int sortingLayer, int sortingOrder, bool fireball)
    {
        EnsureLibrary();
        Sprite[] animationFrames = fireball ? GetFireballFrames() : null;
        Sprite sprite = animationFrames != null && animationFrames.Length > 0 ? animationFrames[0] : GetWhiteSprite();
        GameObject projectile = CreateSpriteObject(fireball ? "Card VFX Fireball" : "Card VFX Arcane Bolt",
            sprite, fireball ? Color.white : new Color(0.63f, 0.78f, 1f, 1f), start, sortingLayer, sortingOrder);
        if (projectile == null) yield break;

        projectile.transform.localScale = fireball ? Vector3.one * 0.76f : Vector3.one * 0.29f;
        Vector3 direction = end - start;
        projectile.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

        const float duration = 0.29f;
        float elapsed = 0f;
        while (elapsed < duration && owner != null && projectile != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            projectile.transform.position = Vector3.Lerp(start, end, eased) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 0.2f);
            if (fireball && animationFrames != null && animationFrames.Length > 0)
            {
                int frame = Mathf.Min(animationFrames.Length - 1, Mathf.FloorToInt(t * animationFrames.Length));
                SpriteRenderer renderer = projectile.GetComponent<SpriteRenderer>();
                if (renderer != null) renderer.sprite = animationFrames[frame];
            }
            yield return null;
        }

        if (projectile != null) Object.Destroy(projectile);
        if (owner != null) owner.StartCoroutine(PlayImpact(owner, end, sortingLayer, sortingOrder, fireball ? 0.86f : 0.55f, 0.4f));
    }

    private static IEnumerator TravelArrow(
        HeroView owner, Vector3 start, Vector3 end, int sortingLayer, int sortingOrder, bool bright)
    {
        GameObject arrow = CreateArrow(start, sortingLayer, sortingOrder, bright ? new Color(0.82f, 0.94f, 1f) : new Color(1f, 0.83f, 0.46f));
        if (arrow == null) yield break;
        Vector3 direction = end - start;
        arrow.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f);
        const float duration = 0.24f;
        float elapsed = 0f;
        while (elapsed < duration && owner != null && arrow != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            arrow.transform.position = Vector3.Lerp(start, end, t * t);
            yield return null;
        }
        if (arrow != null) Object.Destroy(arrow);
        if (owner != null) owner.StartCoroutine(PlayImpact(owner, end, sortingLayer, sortingOrder, 0.4f, 0.24f));
    }

    private static IEnumerator RainArrows(
        HeroView owner, List<CombatantView> targets, Vector3 origin, int sortingLayer, int sortingOrder)
    {
        for (int i = 0; i < targets.Count; i++)
            if (targets[i] != null && targets[i].CurrentHealth > 0)
                owner.StartCoroutine(RainOnTarget(owner, targets[i], i * 0.045f, sortingLayer, sortingOrder));
        yield break;
    }

    private static IEnumerator RainOnTarget(
        HeroView owner, CombatantView target, float initialDelay, int sortingLayer, int sortingOrder)
    {
        if (initialDelay > 0f) yield return new WaitForSeconds(initialDelay);
        const int arrowCount = 8;
        for (int i = 0; i < arrowCount && owner != null && target != null; i++)
        {
            Bounds bounds = target.BattleBodyBounds;
            float spread = Random.Range(-bounds.extents.x * 0.72f, bounds.extents.x * 0.72f);
            Vector3 end = TargetPoint(target) + Vector3.right * spread;
            Vector3 start = new(end.x + Random.Range(-0.22f, 0.22f), end.y + bounds.extents.y + Random.Range(1.25f, 2.05f), end.z);
            GameObject arrow = CreateArrow(start, sortingLayer, sortingOrder, i % 2 == 0
                ? new Color(0.8f, 0.93f, 1f, 1f)
                : new Color(1f, 0.87f, 0.62f, 1f));
            if (arrow != null)
            {
                arrow.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-7f, 7f));
                owner.StartCoroutine(DropArrow(owner, arrow, start, end, sortingLayer, sortingOrder, i % 2 == 0));
            }
            yield return new WaitForSeconds(0.035f);
        }
    }

    private static IEnumerator DropArrow(
        HeroView owner, GameObject arrow, Vector3 start, Vector3 end, int sortingLayer, int sortingOrder, bool impact)
    {
        const float duration = 0.17f;
        float elapsed = 0f;
        while (elapsed < duration && owner != null && arrow != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            arrow.transform.position = Vector3.Lerp(start, end, t * t * (3f - 2f * t));
            yield return null;
        }
        if (arrow != null) Object.Destroy(arrow);
        if (impact && owner != null)
            owner.StartCoroutine(PlayImpact(owner, end, sortingLayer, sortingOrder, 0.3f, 0.19f));
    }

    private static IEnumerator SlashTargets(HeroView owner, List<CombatantView> targets, int sortingLayer, int sortingOrder)
    {
        for (int i = 0; i < targets.Count; i++)
        {
            CombatantView target = targets[i];
            if (target != null && target.CurrentHealth > 0)
                owner.StartCoroutine(PlaySlash(owner, target, sortingLayer, sortingOrder));
            if (i + 1 < targets.Count) yield return new WaitForSeconds(0.06f);
        }
    }

    private static IEnumerator PlaySlash(HeroView owner, CombatantView target, int sortingLayer, int sortingOrder)
    {
        Vector3 point = TargetPoint(target);
        GameObject slash = CreateSpriteObject("Card VFX Melee Slash", GetWhiteSprite(), new Color(1f, 0.88f, 0.55f), point, sortingLayer, sortingOrder);
        if (slash == null) yield break;
        slash.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-48f, -32f));
        float elapsed = 0f;
        const float duration = 0.18f;
        SpriteRenderer renderer = slash.GetComponent<SpriteRenderer>();
        while (elapsed < duration && owner != null && slash != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            slash.transform.localScale = new Vector3(Mathf.Lerp(0.12f, 1.15f, t), Mathf.Lerp(0.04f, 0.13f, t), 1f);
            slash.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-45f, 22f, t));
            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = 1f - t;
                renderer.color = color;
            }
            yield return null;
        }
        if (slash != null) Object.Destroy(slash);
    }

    private static IEnumerator PlayImpact(
        HeroView owner, Vector3 position, int sortingLayer, int sortingOrder, float scale, float duration)
    {
        Sprite[] frames = GetImpactFrames();
        if (frames == null || frames.Length == 0)
        {
            yield return PlayImpactFlash(owner, position, sortingLayer, sortingOrder, scale, duration);
            yield break;
        }

        GameObject effect = CreateSpriteObject("Card VFX Impact", frames[0], Color.white, position, sortingLayer, sortingOrder + 1);
        if (effect == null) yield break;
        effect.transform.localScale = Vector3.one * scale;
        SpriteRenderer renderer = effect.GetComponent<SpriteRenderer>();
        float elapsed = 0f;
        while (elapsed < duration && owner != null && effect != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            int frame = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(t * frames.Length));
            if (renderer != null) renderer.sprite = frames[frame];
            yield return null;
        }
        if (effect != null) Object.Destroy(effect);
    }

    private static IEnumerator PlayImpactFlash(HeroView owner, Vector3 position, int sortingLayer, int sortingOrder, float scale, float duration)
    {
        GameObject flash = CreateSpriteObject("Card VFX Impact Flash", GetWhiteSprite(), new Color(1f, 0.88f, 0.55f), position, sortingLayer, sortingOrder + 1);
        if (flash == null) yield break;
        SpriteRenderer renderer = flash.GetComponent<SpriteRenderer>();
        float elapsed = 0f;
        while (elapsed < duration && owner != null && flash != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            flash.transform.localScale = Vector3.one * Mathf.Lerp(0.12f, scale, t);
            if (renderer != null)
            {
                Color color = renderer.color;
                color.a = 1f - t;
                renderer.color = color;
            }
            yield return null;
        }
        if (flash != null) Object.Destroy(flash);
    }

    private static GameObject CreateArrow(Vector3 position, int sortingLayer, int sortingOrder, Color color)
    {
        GameObject root = new("Card VFX Arrow Projectile");
        root.transform.position = position;
        AddArrowPart(root.transform, "Arrow Shaft", new Vector3(0f, 0.045f, 0f), Vector3.zero,
            new Vector3(0.055f, 0.47f, 1f), color, sortingLayer, sortingOrder);
        AddArrowPart(root.transform, "Arrow Head Left", new Vector3(-0.035f, -0.15f, 0f), new Vector3(0f, 0f, -38f),
            new Vector3(0.05f, 0.19f, 1f), color, sortingLayer, sortingOrder + 1);
        AddArrowPart(root.transform, "Arrow Head Right", new Vector3(0.035f, -0.15f, 0f), new Vector3(0f, 0f, 38f),
            new Vector3(0.05f, 0.19f, 1f), color, sortingLayer, sortingOrder + 1);
        return root;
    }

    private static void AddArrowPart(
        Transform parent, string name, Vector3 localPosition, Vector3 rotation, Vector3 scale,
        Color color, int sortingLayer, int sortingOrder)
    {
        GameObject part = new(name, typeof(SpriteRenderer));
        part.transform.SetParent(parent, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.Euler(rotation);
        part.transform.localScale = scale;
        SpriteRenderer renderer = part.GetComponent<SpriteRenderer>();
        renderer.sprite = GetWhiteSprite();
        renderer.color = color;
        renderer.sortingLayerID = sortingLayer;
        renderer.sortingOrder = sortingOrder;
    }

    private static GameObject CreateSpriteObject(
        string name, Sprite sprite, Color color, Vector3 position, int sortingLayer, int sortingOrder)
    {
        if (sprite == null) return null;
        GameObject result = new(name, typeof(SpriteRenderer));
        result.transform.position = position;
        SpriteRenderer renderer = result.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingLayerID = sortingLayer;
        renderer.sortingOrder = sortingOrder;
        return result;
    }

    private static Vector3 TargetPoint(CombatantView target)
    {
        Bounds body = target.BattleBodyBounds;
        return new Vector3(body.center.x, body.center.y + body.extents.y * 0.12f, body.center.z - 0.15f);
    }

    private static void EnsureLibrary()
    {
        if (libraryLoaded) return;
        libraryLoaded = true;
        library = Resources.Load<CardAttackVfxLibrary>(LibraryResourceName);
        if (library == null) Debug.LogWarning("CardAttackVfxLibrary is missing from Resources; using built-in sprite effects.");
    }

    private static Sprite[] GetFireballFrames()
    {
        EnsureLibrary();
        Texture2D texture = library != null ? library.fireballProjectile : null;
        if (texture == null || texture.width < FireballColumns || texture.height < FireballRows) return null;
        if (fireballFrames != null && cachedFireballTexture == texture) return fireballFrames;

        cachedFireballTexture = texture;
        int cellWidth = texture.width / FireballColumns;
        int cellHeight = texture.height / FireballRows;
        fireballFrames = new Sprite[FireballColumns * FireballRows];
        for (int i = 0; i < fireballFrames.Length; i++)
        {
            int column = i % FireballColumns;
            int rowFromTop = i / FireballColumns;
            int y = texture.height - (rowFromTop + 1) * cellHeight;
            fireballFrames[i] = Sprite.Create(texture,
                new Rect(column * cellWidth, y, cellWidth, cellHeight), new Vector2(0.5f, 0.5f), cellWidth,
                0, SpriteMeshType.FullRect);
            fireballFrames[i].name = "Runtime Fireball Frame " + i;
        }
        return fireballFrames;
    }

    private static Sprite[] GetImpactFrames()
    {
        EnsureLibrary();
        Texture2D texture = library != null ? library.impactSheet : null;
        if (texture == null || texture.width < ImpactColumns || texture.height < ImpactRows) return null;
        if (impactFrames != null && cachedImpactTexture == texture) return impactFrames;

        cachedImpactTexture = texture;
        int cellWidth = texture.width / ImpactColumns;
        int cellHeight = texture.height / ImpactRows;
        impactFrames = new Sprite[ImpactColumns * ImpactRows];
        for (int i = 0; i < impactFrames.Length; i++)
        {
            int column = i % ImpactColumns;
            int rowFromTop = i / ImpactColumns;
            int y = texture.height - (rowFromTop + 1) * cellHeight;
            impactFrames[i] = Sprite.Create(texture,
                new Rect(column * cellWidth, y, cellWidth, cellHeight), new Vector2(0.5f, 0.5f), cellWidth,
                0, SpriteMeshType.FullRect);
            impactFrames[i].name = "Runtime Impact Frame " + i;
        }
        return impactFrames;
    }

    private static Sprite GetWhiteSprite()
    {
        if (whiteSprite != null) return whiteSprite;
        whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
        {
            name = "Runtime Card VFX White Pixel",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply(false, true);
        whiteSprite = Sprite.Create(whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        whiteSprite.name = "Runtime Card VFX White Sprite";
        return whiteSprite;
    }
}
