using UnityEngine;

// Selection indices are persisted in SaveSlots; keep existing slots stable.
public static class SelectedHero
{
    public const int WarriorIndex = 0;
    public const int ArcherIndex = 1;
    public const int MageIndex = 2;
    public static bool IsArcher => PlayerPrefs.GetInt("SelectedCharacter", 0) == ArcherIndex;
    public static bool IsMage => PlayerPrefs.GetInt("SelectedCharacter", 0) == MageIndex;

    public static HeroData Resolve(HeroData fallback)
    {
        if (IsMage) return Resources.Load<HeroData>("Mage/Mage") ?? fallback;
        return IsArcher ? Resources.Load<HeroData>("Archer/Archer") ?? fallback : fallback;
    }

    public static SpriteRenderer ApplyArcher(playerMovement movement, SpriteRenderer original)
    {
        return ApplyVisual(movement, original, "Archer", false);
    }

    public static SpriteRenderer ApplyMage(playerMovement movement, SpriteRenderer original)
    {
        return ApplyVisual(movement, original, "Mage", true);
    }

    public static SpriteRenderer ApplyWarrior(playerMovement movement, SpriteRenderer original)
    {
        // Severed Fang's body is 46 pixels tall; its wider frame includes hair and the weapon.
        return ApplyVisual(movement, original, "Warrior", false, 46f / 16f);
    }

    private static SpriteRenderer ApplyVisual(playerMovement movement, SpriteRenderer original, string character, bool floating, float bodyHeight = 0f)
    {
        var hero = Resources.Load<HeroData>($"{character}/{character}");
        var controller = Resources.Load<RuntimeAnimatorController>($"{character}/{character}Movement");
        if (hero == null || controller == null || original == null) return original;

        // Keep the existing player root, collider, camera target and attached interactions.
        // Scale only the artwork to match the warrior's collision capsule.
        var visual = new GameObject(character + " Visual");
        visual.transform.SetParent(movement.transform, false);
        var capsule = movement.GetComponent<CapsuleCollider2D>();
        // The mage's halo extends above the body; match the body to the capsule.
        float spriteHeight = Mathf.Max(bodyHeight > 0f ? bodyHeight : hero.Image.bounds.size.y * (floating ? 0.72f : 1f), 0.01f);
        float scale = capsule != null ? capsule.size.y / spriteHeight : 1f;
        visual.transform.localScale = Vector3.one * scale;
        Vector2 center = capsule != null ? capsule.offset : Vector2.zero;
        // Gameplay frames share a horizontal anchor and a pivot at the feet.
        float feet = capsule != null ? center.y - capsule.size.y * 0.5f : 0f;
        visual.transform.localPosition = new Vector3(center.x, feet, 0);
        var renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = hero.Image;
        renderer.sharedMaterial = original.sharedMaterial;
        renderer.sortingLayerID = original.sortingLayerID;
        renderer.sortingOrder = original.sortingOrder;
        renderer.color = original.color;
        renderer.flipX = original.flipX;
        original.sprite = hero.Image;
        original.enabled = false;
        if (movement.animator != null) movement.animator.enabled = false;
        movement.animator = visual.AddComponent<Animator>();
        movement.animator.runtimeAnimatorController = controller;
        movement.dashAnimation = Resources.Load<AnimationClip>($"{character}/{character}Dash");
        if (character == "Warrior")
            movement.hurtAnimation = Resources.Load<AnimationClip>("Warrior/WarriorHurt");
        visual.AddComponent<ArcherAnimationPolish>().Configure(movement.animator, movement.rb, renderer, floating);
        return renderer;
    }
}
