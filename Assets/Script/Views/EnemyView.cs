using TMPro;
using UnityEngine;

public class EnemyView : CombatantView
{
   [SerializeField] private TMP_Text attackText;
   public int AttackPower {get; set; }
   public void SetUp(EnemyData enemyData)
    {
        AttackPower = enemyData.AttackPower;
        UpdateAttackText();
        SetUpBase(enemyData.Health, enemyData.Image);
    }
    private void UpdateAttackText()
    {
        attackText.text = "ATK: " + AttackPower;
    }
}
