using UnityEngine;

/// <summary>Keeps contact damage on the crusher plate, excluding the hanging chains.</summary>
[RequireComponent(typeof(TrapPackAnimation), typeof(BoxCollider2D))]
public sealed class CeilingCrusherHitbox2D : MonoBehaviour
{
    [SerializeField] private Vector2[] frameOffsets;
    [SerializeField] private Vector2[] frameSizes;
    private TrapPackAnimation animation;
    private BoxCollider2D hitbox;

    private void Awake()
    {
        animation = GetComponent<TrapPackAnimation>();
        hitbox = GetComponent<BoxCollider2D>();
        UpdateHitbox();
    }

    private void LateUpdate() => UpdateHitbox();

    private void UpdateHitbox()
    {
        if (animation == null || hitbox == null || frameOffsets == null || frameSizes == null) return;
        int frame = animation.CurrentFrame;
        if (frame < 0 || frame >= frameOffsets.Length || frame >= frameSizes.Length) return;
        hitbox.offset = frameOffsets[frame];
        hitbox.size = frameSizes[frame];
    }
}
