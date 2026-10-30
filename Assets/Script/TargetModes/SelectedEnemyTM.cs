using System.Collections.Generic;

[System.Serializable]
public sealed class SelectedEnemyTM : TargetMode
{
    public override List<CombatantView> GetTargets()
    {
        CombatantView target = CardSystem.Instance != null ? CardSystem.Instance.CurrentManualTarget : null;
        return target != null && target.CurrentHealth > 0
            ? new List<CombatantView> { target }
            : new List<CombatantView>();
    }
}
