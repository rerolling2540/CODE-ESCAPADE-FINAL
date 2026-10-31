using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterSelector))]
public sealed class MainMenuCharacterIdle : MonoBehaviour
{
    [System.Serializable]
    public sealed class HeroIdle
    {
        public Sprite[] frames;
        [Min(1f)] public float framesPerSecond = 8f;
    }

    public HeroIdle[] heroes;
    private CharacterSelector selector;
    private Image preview;
    private int selected = -1;
    private float elapsed;

    void OnEnable()
    {
        selector = GetComponent<CharacterSelector>();
        preview = selector.characterImage;
        ShowCharacter(selector.SelectedCharacter);
    }

    public void ShowCharacter(int index)
    {
        selected = index;
        elapsed = 0f;
        if (selector == null) selector = GetComponent<CharacterSelector>();
        preview = selector.characterImage;
        ApplyFrame();
    }

    void Update()
    {
        if (preview == null || !preview.isActiveAndEnabled) return;
        if (selected != selector.SelectedCharacter) ShowCharacter(selector.SelectedCharacter);
        AdvanceAnimation(Time.unscaledDeltaTime);
    }

    void AdvanceAnimation(float deltaTime)
    {
        if (!HasFrames()) return;
        var hero = heroes[selected];
        float duration = hero.frames.Length / Mathf.Max(1f, hero.framesPerSecond);
        elapsed = Mathf.Repeat(elapsed + Mathf.Max(0f, deltaTime), duration);
        ApplyFrame();
    }

    bool HasFrames() => heroes != null && selected >= 0 && selected < heroes.Length &&
        heroes[selected] != null && heroes[selected].frames != null && heroes[selected].frames.Length > 0;

    void ApplyFrame()
    {
        if (preview == null || !HasFrames()) return;
        var hero = heroes[selected];
        int frame = Mathf.FloorToInt(elapsed * Mathf.Max(1f, hero.framesPerSecond)) % hero.frames.Length;
        preview.sprite = hero.frames[frame];
        preview.preserveAspect = true;
    }
}
