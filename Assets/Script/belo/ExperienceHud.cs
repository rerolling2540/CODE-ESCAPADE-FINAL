using TMPro;
using UnityEngine;
public sealed class ExperienceHud : MonoBehaviour
{
    public UnityEngine.UI.Image fill;
    public TMP_Text label;
    public TMP_Text levelLabel;
    void Update()
    {
        int level=PlayerProgression.Level;
        int xp=PlayerProgression.Experience;
        int required=PlayerProgression.RequiredExperience(level);
        bool capped=level>=100;
        if(fill!=null)fill.fillAmount=capped?1f:Mathf.Clamp01((float)xp/required);
        if(levelLabel!=null)levelLabel.text="LEVEL "+level;
        if(label!=null)label.text=levelLabel!=null
            ? capped?"EXP: MAX":"EXP: "+xp+" / "+required
            : capped?"Level "+level+" · MAX":"Level "+level+" · "+xp+" / "+required+" XP";
    }
}
