using UnityEngine;

public class EndTurnButtonUI : MonoBehaviour
{
    public void OnClick()
    {
        if (QuestionUI.IsQuestionActive || EncounterBattleReturn.BattleEnded || ActionSystem.Instance.IsPerforming) return;
        EnemyTurnGA enemyTurnGA = new();
        ActionSystem.Instance.Perform(enemyTurnGA);
    }
}
