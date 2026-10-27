
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AllEnemiesTM : TargetMode
{
   public override List<CombatantView> GetTargets()
    {
        if (EnemySystem.Instance?.Enemies == null) return new();
        return EnemySystem.Instance.Enemies.FindAll(enemy => enemy != null && enemy.CurrentHealth > 0)
            .ConvertAll(enemy => (CombatantView)enemy);
    }
}
