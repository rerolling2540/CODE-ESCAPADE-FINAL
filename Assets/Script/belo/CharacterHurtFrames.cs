using UnityEngine;
using System.Collections.Generic;
public sealed class CharacterHurtFrames : ScriptableObject
{
 public Sprite[] warrior;
 public Texture2D archer,mage;
 private readonly Dictionary<string,Sprite[]> cache=new Dictionary<string,Sprite[]>();
 public Sprite[] Frames(int character,float ppu)
 {
  string key=character+":"+ppu;if(cache.TryGetValue(key,out var frames))return frames;
  if(character==0){frames=new Sprite[warrior.Length];for(int i=0;i<frames.Length;i++){var s=warrior[i];frames[i]=Sprite.Create(s.texture,s.rect,new Vector2(s.pivot.x/s.rect.width,s.pivot.y/s.rect.height),ppu);}}
  else{var t=character==1?archer:mage;int width=character==1?160:128;frames=new Sprite[t.width/width];for(int i=0;i<frames.Length;i++)frames[i]=Sprite.Create(t,new Rect(i*width,0,width,t.height),new Vector2(.5f,47f/t.height),ppu);}
  cache[key]=frames;return frames;
 }
}
