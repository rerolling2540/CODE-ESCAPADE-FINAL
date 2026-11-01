using System;
using System.Collections;
using UnityEngine;

public class ManaSystem : Singleton<ManaSystem>
{
    [SerializeField] private ManaUI manaUI;
    private const int TURN_START_MANA = ManaUI.BaseTurnMana;
    private int currentMana = TURN_START_MANA;
    public int CurrentMana => currentMana;

    void OnEnable()
    {
        ActionSystem.AttachPerformer<SpendManaGA>(SpendManaPerformer);
        ActionSystem.AttachPerformer<GainManaGA>(GainManaPerformer);
        ActionSystem.AttachPerformer<RefillManaGA>(RefillManaPerformer);
        ActionSystem.SubscribedReaction<EnemyTurnGA>(EnemyTurnPostReaction,ReactionTiming.POST);
    }
    void OnDisable()
    {
        ActionSystem.DetachPerformer<SpendManaGA>();
        ActionSystem.DetachPerformer<GainManaGA>();
        ActionSystem.DetachPerformer<RefillManaGA>();
        ActionSystem.UnSubscribedReaction<EnemyTurnGA>(EnemyTurnPostReaction,ReactionTiming.POST);
    }
    public bool HasEnoughMana(int mana)
    {
        return currentMana >= mana;
    }

    private IEnumerator SpendManaPerformer(SpendManaGA spendManaGA)
    {
        currentMana = Mathf.Max(0, currentMana - Mathf.Max(0, spendManaGA.Amount));
        if (manaUI != null) manaUI.UpdateManaText(currentMana);
        yield return null;
    }
    private IEnumerator GainManaPerformer(GainManaGA gainManaGA)
    {
        if (gainManaGA != null)
        {
            long result = (long)currentMana + Mathf.Max(0, gainManaGA.Amount);
            currentMana = (int)Math.Min(int.MaxValue, result);
            if (manaUI != null) manaUI.UpdateManaText(currentMana);
        }
        yield return null;
    }
    private IEnumerator RefillManaPerformer(RefillManaGA refillManaGA)
    {
        currentMana = TURN_START_MANA;
        if (manaUI != null) manaUI.UpdateManaText(currentMana);
        yield return null;
    }

    private void EnemyTurnPostReaction(EnemyTurnGA  enemyTurnGA)
    {
        RefillManaGA refillManaGA = new();
        ActionSystem.Instance.AddReaction(refillManaGA);
    }
}
