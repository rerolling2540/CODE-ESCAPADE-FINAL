using UnityEngine;

public class HeroView : CombatantView
{
    public void SetUp(HeroData heroData)
    {
        // If PlayerStats already calculated MaxHealth from level scaling, keep it!
        // Otherwise, fall back to heroData.Health as a default.
        int targetHealth = (MaxHealth > 0) ? MaxHealth : heroData.Health;

        SetUpBase(targetHealth, heroData.Image);
    }
}