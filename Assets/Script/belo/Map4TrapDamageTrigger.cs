using UnityEngine;

/// <summary>Applies one health hit when the player enters a trap's hitbox.</summary>
public sealed class Map4TrapDamageTrigger : MonoBehaviour
{
    private Map4TrapBattleSetup owner;
    private int damage = 18;

    public void Initialize(Map4TrapBattleSetup setup, int damageAmount)
    {
        owner = setup;
        damage = Mathf.Max(1, damageAmount);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner == null || other == null) return;
        if (other.GetComponentInParent<playerMovement>() == null) return;
        owner.DamagePlayer(damage);
    }
}
