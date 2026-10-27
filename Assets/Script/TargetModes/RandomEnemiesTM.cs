using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RandomEnemiesTM : TargetMode
{
    public override List<CombatantView> GetTargets()
    {
        List<CombatantView> livingEnemies = EnemySystem.Instance?.Enemies
            ?.FindAll(enemy => enemy != null && enemy.CurrentHealth > 0)
            .ConvertAll(enemy => (CombatantView)enemy);
        if (livingEnemies == null || livingEnemies.Count == 0) return new();

        CombatantView target = livingEnemies[Random.Range(0, livingEnemies.Count)];
        return new() { target };
    }
}
