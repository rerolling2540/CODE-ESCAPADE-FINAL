using TMPro;
using UnityEngine;
public sealed class ExperienceHud : MonoBehaviour
{
    public UnityEngine.UI.Image fill;
    public TMP_Text label;
    void Update()
    {
        int level=PlayerProgression.Level;
        int xp=PlayerProgression.Experience;
        int required=PlayerProgression.RequiredExperience(level);
        bool capped=level>=100;
        if(fill!=null)fill.fillAmount=capped?1f:Mathf.Clamp01((float)xp/required);
        if(label!=null)label.text=capped?"Level "+level+" · MAX":"Level "+level+" · "+xp+" / "+required+" XP";
    }
}
