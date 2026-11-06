using UnityEngine;

[CreateAssetMenu(menuName="Data/World Question Bank Settings")]
public sealed class WorldQuestionBankSettings : ScriptableObject
{
    public string projectUrl;
    [Tooltip("Public client key only. Never use a Supabase service-role key.")]
    public string publishableKey;
}
