using UnityEngine;

/// <summary>Aligned full-cell frames keep clothing attached to the body during idle.</summary>
public sealed class MentorSpriteLayers : MonoBehaviour
{
    [System.Serializable] public sealed class Layer { public SpriteRenderer renderer; public Sprite[] frames; }
    public Layer[] layers;
    void Update()
    {
        if(layers==null)return;
        int frame=(int)(Time.time*6)%5;
        foreach(var layer in layers)if(layer.renderer!=null&&layer.frames!=null&&layer.frames.Length>0)
            layer.renderer.sprite=layer.frames[frame%layer.frames.Length];
    }
}
