using UnityEngine;

/// <summary>One health pool per save slot, shared by exploration and battle.</summary>
public static class PlayerHealth
{
    private static SaveSlots.Record previewHealth;
    private static int previewSlot = -1;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetPreview()
    {
        previewHealth = null;
        previewSlot = -1;
    }

    private static SaveSlots.Record Read()
    {
        var saved = SaveSlots.Read(SaveSlots.Active);
        if (saved != null) return saved;
        // Playing a scene directly in the Editor must not create a save slot.
        if (previewHealth == null || previewSlot != SaveSlots.Active)
        {
            previewSlot = SaveSlots.Active;
            previewHealth = new SaveSlots.Record();
        }
        return previewHealth;
    }

    public static int MaxHealth => Mathf.Max(1, Read().maxHealth);
    public static int CurrentHealth => Mathf.Clamp(Read().currentHealth, 0, MaxHealth);

    public static void EnsureInitialized(int maximum)
    {
        var saved = Read();
        if (saved.maxHealth > 0 && saved.currentHealth >= 0) return;
        Set(maximum, maximum);
    }

    public static void ConfigureMaximum(int maximum)
    {
        maximum = Mathf.Max(1, maximum);
        var saved = Read();
        int current = saved.maxHealth > 0 && saved.currentHealth >= 0
            ? saved.currentHealth + maximum - saved.maxHealth : maximum;
        // Increasing maximum HP grants the new HP without erasing existing damage.
        Set(current, maximum);
    }

    public static void Set(int current, int maximum)
    {
        maximum = Mathf.Max(1, maximum);
        current = Mathf.Clamp(current, 0, maximum);
        var saved = Read();
        if (saved.currentHealth == current && saved.maxHealth == maximum) return;
        saved.currentHealth = current;
        saved.maxHealth = maximum;
        if (SaveSlots.Read(SaveSlots.Active) != null) SaveSlots.Write(saved);
    }

    public static void Damage(int amount)
    {
        Set(CurrentHealth - Mathf.Max(0, amount), MaxHealth);
    }

    public static void CopyTo(SaveSlots.Record destination)
    {
        var health = Read();
        if (health.maxHealth <= 0 || health.currentHealth < 0) return;
        destination.currentHealth = health.currentHealth;
        destination.maxHealth = health.maxHealth;
    }
}
