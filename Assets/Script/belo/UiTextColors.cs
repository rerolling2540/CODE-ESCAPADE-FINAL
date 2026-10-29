using UnityEngine;

/// <summary>Keep authored UI text readable in every pointer and focus state.</summary>
public static class UiTextColors
{
    public static void Stable(GUIStyle style)
    {
        Color color=style.normal.textColor;
        style.hover.textColor=style.active.textColor=style.focused.textColor=color;
        style.onNormal.textColor=style.onHover.textColor=style.onActive.textColor=style.onFocused.textColor=color;
    }
}
