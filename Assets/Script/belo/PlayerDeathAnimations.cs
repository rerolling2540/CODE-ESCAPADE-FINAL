using UnityEngine;

[CreateAssetMenu(menuName = "CodeEscapade/Player Death Animations")]
public sealed class PlayerDeathAnimations : ScriptableObject
{
    public Sprite[] warrior;
    public Sprite[] archer;
    public Sprite[] mage;
    [Min(1f)] public float framesPerSecond = 10f;
    public string villageScene = "Scene 2";
    public Vector3 villagePosition;

    public Sprite[] ForCharacter(int character) => character == SelectedHero.ArcherIndex ? archer :
        character == SelectedHero.MageIndex ? mage : warrior;
}
