using UnityEngine;

[CreateAssetMenu(menuName = "Battle/Card Attack VFX Library")]
public sealed class CardAttackVfxLibrary : ScriptableObject
{
    [Tooltip("4x4 fireball flipbook texture from the Flipbook VFX URP pack.")]
    public Texture2D fireballProjectile;

    [Tooltip("8x8 fire impact animation sheet from the Free RPG VFX Sprite Sheet Starter Pack.")]
    public Texture2D impactSheet;
}
