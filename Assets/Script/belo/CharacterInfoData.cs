using UnityEngine;
public sealed class CharacterInfoData : ScriptableObject
{
 public HeroClassData battleClass;
 public int battleLevel=1;
 public HeroData[] heroes;
 public Texture2D[] portraits;
 [System.NonSerialized] private Sprite[] portraitSprites;

 public Texture2D GetPortraitTexture(int index)
 {
  return portraits != null && index >= 0 && index < portraits.Length ? portraits[index] : null;
 }

 public Sprite GetPortraitSprite(int index)
 {
  var texture = GetPortraitTexture(index);
  if (texture == null) return null;
  if (portraitSprites == null || portraitSprites.Length != portraits.Length)
   portraitSprites = new Sprite[portraits.Length];
  if (portraitSprites[index] == null || portraitSprites[index].texture != texture)
  {
   portraitSprites[index] = Sprite.Create(texture, new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100f);
   portraitSprites[index].name = texture.name + " Character Portrait";
  }
  return portraitSprites[index];
 }
}
