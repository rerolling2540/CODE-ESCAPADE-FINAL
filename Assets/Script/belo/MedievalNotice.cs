using UnityEngine;

/// <summary>Shared parchment styling for small exploration messages.</summary>
public static class MedievalNotice
{
    private static Font font;
    public static void Apply(GUIStyle style)
    {
        if(font==null)font=Font.CreateDynamicFontFromOSFont("Georgia",24);
        style.font=font;
        style.border=new RectOffset(10,10,10,10);
        style.padding=new RectOffset(20,20,14,14);
        var background=GameControlsGuide.Frame(true).texture;
        style.normal.background=style.hover.background=style.active.background=style.focused.background=background;
        style.normal.textColor=new Color(.14f,.065f,.025f);UiTextColors.Stable(style);
    }
}
