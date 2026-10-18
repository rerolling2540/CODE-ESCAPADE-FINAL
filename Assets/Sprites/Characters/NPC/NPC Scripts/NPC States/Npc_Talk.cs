using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class Npc_Talk : MonoBehaviour
{
    private Animator anim;
    public Animator interactAnim;
    public DialogueSO dialogueSO;
    public bool offersElfQuest;
    private readonly List<DialogueSO> generated = new List<DialogueSO>();
    private bool ownsDialogue;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        if (offersElfQuest && FindFirstObjectByType<ElfQuestTracker>() == null)
            new GameObject("Elf Quest Tracker").AddComponent<ElfQuestTracker>();
    }

    private void OnEnable()
    {
        if (anim != null && anim.HasState(0, Animator.StringToHash("Idle"))) anim.Play("Idle");
        if (interactAnim != null) interactAnim.Play("Open");
    }

    private void OnDisable()
    {
        if (interactAnim != null) interactAnim.Play("Close");
        if (ownsDialogue && DialogueManager.Instance != null) DialogueManager.Instance.EndDialogue();
        ownsDialogue = false;
        ClearGenerated();
    }

    private void Update()
    {
        var manager = DialogueManager.Instance;
        if (manager == null || Time.timeScale <= 0f || manager.isDialogueActive) return;
        bool interact = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        interact |= Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;
        if (!interact) return;
        ClearGenerated();
        ownsDialogue = true;
        manager.StartDialogue(offersElfQuest ? BuildQuestDialogue() : dialogueSO);
    }

    void ClearGenerated()
    {
        foreach (var node in generated) if (node != null) Destroy(node);
        generated.Clear();
    }

    DialogueSO Node(params string[] lines)
    {
        var node = ScriptableObject.CreateInstance<DialogueSO>();
        generated.Add(node);
        ActorSO speaker = dialogueSO != null && dialogueSO.lines != null && dialogueSO.lines.Length > 0
            ? dialogueSO.lines[0].speaker : null;
        node.lines = new DialogueLine[lines.Length];
        for (int i = 0; i < lines.Length; i++) node.lines[i] = new DialogueLine { speaker = speaker, text = lines[i] };
        node.options = new DialogueOption[0];
        return node;
    }

    DialogueSO BuildQuestDialogue()
    {
        var saved = SaveSlots.Read(SaveSlots.Active) ?? new SaveSlots.Record();
        int stage = saved.elfQuestStage;
        if (stage == 0)
        {
            var accepted = Node("Then let our pact begin. Clear three encounters in the cave, and return to me. Each victory already won will count.",
                "Your reward will be 100 coins and one library token. Read each battle question carefully: knowledge is the sharpest blade.");
            var offer = Node("Well met, traveler. I am the keeper of this hollow. Something has stirred beneath our ancient halls.",
                "Skeletons guard the passages, goblins lay ambushes, and stranger creatures haunt the dark. Help me restore the ward, one trial at a time.");
            offer.options = new[] {
                new DialogueOption { optionText = "Accept: Warden of the Hollow", nextDialogue = accepted, onSelected = () => SetStage(0, 1) },
                new DialogueOption { optionText = "Not yet" }
            };
            return offer;
        }
        if (stage >= 4)
            return Node("The hollow is quiet once more. You have earned the title Warden of the Hollow.",
                "Keep your library tokens for the trials ahead. May your blade stay true, and your knowledge never falter.");

        if (!ElfQuestProgress.Ready(saved))
        {
            string task = ElfQuestProgress.Objective(saved);
            return Node("Our pact still stands. " + task,
                stage == 2 ? "Skeletons and goblins fight at close range. Mushrooms and flying eyes attack from afar. Learn their patterns, then return to me." :
                "Explore the cave and overcome its encounters. Retreats do not count as victories. I will be here when your task is done.");
        }

        int coins = stage == 1 ? 100 : stage == 2 ? 200 : 350;
        int tokens = stage;
        string nextTask = stage == 1 ? "Next, prove your mastery: defeat at least one Skeleton, Goblin, Mushroom, and Flying Eye encounter. Earlier victories count." :
            stage == 2 ? "One final duty remains: clear all twelve cave encounters. Return when the last passage is safe." :
            "The ward is restored. From this day forward, you are Warden of the Hollow.";
        var thanks = Node("Your reward: " + coins + " coins and " + tokens + " library token" + (tokens == 1 ? "." : "s."), nextTask);
        var complete = Node(stage == 1 ? "Three threats silenced. You have given the hollow room to breathe." :
            stage == 2 ? "You have faced every creature that stalks these halls. Now you understand our enemy." :
            "Every passage is safe. You have done what I could not do alone.");
        complete.options = new[] {
            new DialogueOption { optionText = stage == 3 ? "Claim reward and title" : "Claim reward and next quest", nextDialogue = thanks,
                onSelected = () => ClaimReward(stage, coins, tokens) },
            new DialogueOption { optionText = "I will return later" }
        };
        return complete;
    }

    static void SetStage(int expected, int next)
    {
        GameSave.EnsureInstance().SaveCurrentPlayer();
        var saved = SaveSlots.Read(SaveSlots.Active) ?? new SaveSlots.Record();
        if (saved.elfQuestStage != expected) return;
        saved.elfQuestStage = next;
        SaveSlots.Write(saved);
    }

    static void ClaimReward(int expected, int coins, int tokens)
    {
        var saved = SaveSlots.Read(SaveSlots.Active);
        if (saved == null || saved.elfQuestStage != expected || !ElfQuestProgress.Ready(saved)) return;
        var inventory = FindFirstObjectByType<CharacterInventory>();
        if (inventory == null)
        {
            var player = FindFirstObjectByType<playerMovement>();
            if (player == null) return;
            inventory = player.gameObject.AddComponent<CharacterInventory>();
        }
        // Advance before closing the choice so repeated clicks cannot pay twice.
        saved.elfQuestStage++;
        SaveSlots.Write(saved);
        inventory.AddCoins(coins);
        inventory.AddLibraryTokens(tokens);
    }
}

public static class ElfQuestProgress
{
    public static int Cleared(SaveSlots.Record saved)
    {
        var names = new HashSet<string>();
        if (saved?.defeatedEncounters != null)
            foreach (var id in saved.defeatedEncounters)
                if (IsCaveEncounter(id)) names.Add(id);
        return names.Count;
    }

    static bool IsCaveEncounter(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        // Count only the twelve authored encounters, not arbitrary battle IDs.
        string[] types = { "Skeleton", "Goblin", "Mushroom", "Flying Eye" };
        for (int i = 1; i <= 12; i++)
            if (id == "Cave " + types[(i - 1) % 4] + " " + i.ToString("00")) return true;
        return false;
    }

    public static int Kinds(SaveSlots.Record saved)
    {
        var kinds = new HashSet<string>();
        if (saved?.defeatedEncounters != null)
            foreach (var id in saved.defeatedEncounters)
                if (IsCaveEncounter(id))
                    foreach (var kind in new[] { "Skeleton", "Goblin", "Mushroom", "Flying Eye" })
                        if (id.Contains(kind)) kinds.Add(kind);
        return kinds.Count;
    }

    public static bool Ready(SaveSlots.Record saved) => saved != null &&
        (saved.elfQuestStage == 1 && Cleared(saved) >= 3 ||
         saved.elfQuestStage == 2 && Kinds(saved) >= 4 ||
         saved.elfQuestStage == 3 && Cleared(saved) >= 12);

    public static string Objective(SaveSlots.Record saved)
    {
        int stage = saved == null ? 0 : saved.elfQuestStage;
        if (stage == 0) return "Speak to the elf at the cave entrance [E].";
        if (stage >= 4) return "Warden of the Hollow - questline complete.";
        if (Ready(saved)) return "Return to the elf at the cave entrance to claim your reward.";
        if (stage == 1) return "First Light: clear cave encounters (" + Cleared(saved) + "/3).";
        if (stage == 2)
        {
            var missing = new List<string>();
            foreach (var kind in new[] { "Skeleton", "Goblin", "Mushroom", "Flying Eye" })
            {
                bool found = saved.defeatedEncounters != null && saved.defeatedEncounters.Exists(id => IsCaveEncounter(id) && id.Contains(kind));
                if (!found) missing.Add(kind);
            }
            return "Know Your Enemy (" + Kinds(saved) + "/4). Still needed: " + string.Join(", ", missing) + ".";
        }
        return "Restore the Ward: clear the cave (" + Cleared(saved) + "/12).";
    }
}

public class ElfQuestTracker : MonoBehaviour
{
    private float refreshAt;
    private string objective;
    private GUIStyle textStyle;
    void Update()
    {
        if (Time.unscaledTime < refreshAt) return;
        refreshAt = Time.unscaledTime + 0.5f;
        objective = ElfQuestProgress.Objective(SaveSlots.Read(SaveSlots.Active));
    }
    void OnGUI()
    {
        if (DialogueManager.Instance != null && DialogueManager.Instance.isDialogueActive) return;
        if (textStyle == null) textStyle = new GUIStyle(GUI.skin.label) {
            fontSize = 16, wordWrap = true, normal = { textColor = new Color(1f, 0.89f, 0.65f) }
        };
        float width = Mathf.Min(360f, Screen.width - 32f);
        var oldColor = GUI.color;
        GUI.color = new Color(0.25f, 0.16f, 0.09f, 0.94f);
        GUI.Box(new Rect(16, Screen.height - 120, width, 102), GUIContent.none);
        GUI.color = oldColor;
        GUI.Label(new Rect(28, Screen.height - 112, width - 24, 28), "WARDEN OF THE HOLLOW", textStyle);
        GUI.Label(new Rect(28, Screen.height - 84, width - 24, 64), objective ?? "Speak to the elf [E].", textStyle);
    }
}
