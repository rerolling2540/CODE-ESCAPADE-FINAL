using UnityEngine;

public class Interactions : Singleton<Interactions>
{
   public bool PlayerIsDragging {get; set;} = false;
   public bool PlayerCanInteract()
    {
        if (QuestionUI.IsQuestionActive) return false;
        if (EncounterBattleReturn.BattleEnded) return false;
        if(!ActionSystem.Instance.IsPerforming) return true;
        else return false;
    }
    
    public bool PlayerCanHover()
    {
        if (QuestionUI.IsQuestionActive) return false;
        if (EncounterBattleReturn.BattleEnded) return false;
        if(PlayerIsDragging) return false;
        return true;
    }
}
