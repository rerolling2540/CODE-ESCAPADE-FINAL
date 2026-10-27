using UnityEngine;

/// <summary>All NPCs resolve this single authored dialogue canvas when a scene has none.</summary>
[CreateAssetMenu(menuName = "Dialogue/Shared NPC Settings")]
public sealed class NPCDialogueSettings : ScriptableObject
{
    public GameObject dialogueCanvasPrefab;
}

public interface INPCDialogueProvider
{
    DialogueSO BuildDialogue(Npc_Talk host);
}
