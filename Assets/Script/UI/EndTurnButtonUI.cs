using UnityEngine;

public class EndTurnButtonUI : MonoBehaviour
{
    public void OnClick()
    {
        if (EncounterBattleReturn.BattleEnded || BattleTutorialGuide.BlocksInput || ActionSystem.Instance.IsPerforming) return;
        if (BattleTutorialGuide.ExplainEndTurn(() => { if (this != null) OnClick(); })) return;
        EnemyTurnGA enemyTurnGA = new();
        ActionSystem.Instance.Perform(enemyTurnGA);
    }
}
