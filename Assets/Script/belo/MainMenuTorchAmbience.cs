using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public sealed class MainMenuTorchAmbience : MonoBehaviour
{
    public MenuTorchEmbersGraphic embers;
    private Image background;
    private Material template, animated;
    private float clock;
    private static readonly int MenuTime = Shader.PropertyToID("_MenuTime");

    void OnEnable()
    {
        background = GetComponent<Image>();
        template = background.material;
        animated = new Material(template) { name = "Main Menu Torch Animation (Instance)" };
        background.material = animated;
        clock = 0f;
        SetAnimationTime(0f);
    }

    void Update()
    {
        clock += Time.unscaledDeltaTime;
        SetAnimationTime(clock);
    }

    public void SetAnimationTime(float time)
    {
        if (animated != null) animated.SetFloat(MenuTime,time);
        if (embers != null) embers.SetAnimationTime(time);
    }

    void OnDisable()
    {
        if (background != null && background.material == animated) background.material = template;
        if (animated == null) return;
        if (Application.isPlaying) Destroy(animated);
        else DestroyImmediate(animated);
        animated = null;
    }
}
