using UnityEngine;

/// <summary>Damages the player on contact with a painted hazard or animated trap.</summary>
[RequireComponent(typeof(Collider2D))]
public sealed class Map4TrapDamageTrigger : MonoBehaviour
{
    [SerializeField, Min(1)] private int damage = 18;
    [SerializeField, Tooltip("Powered contact removes a full health bar.")]
    private bool lethalContact;

    public void Initialize(int damageAmount)
    {
        damage = Mathf.Max(1, damageAmount);
    }

    private void OnTriggerEnter2D(Collider2D other) => DamagePlayer(other);
    private void OnTriggerStay2D(Collider2D other) => DamagePlayer(other);

    private void DamagePlayer(Collider2D other)
    {
        if (!isActiveAndEnabled || other == null) return;
        var player = other.GetComponentInParent<playerMovement>();
        if (player != null)
            player.TryTakeDamage(lethalContact ? Mathf.Max(damage, PlayerHealth.MaxHealth) : damage);
    }
}
