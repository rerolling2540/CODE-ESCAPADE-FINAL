using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>A scene-authored lever controlling its explicitly linked electrical traps.</summary>
public sealed class ElectricalTrapSwitch2D : MonoBehaviour
{
    [SerializeField] private Map4TrapDamageTrigger[] controlledTraps;
    [SerializeField] private bool startsPowered = true;
    [SerializeField, Min(0.5f)] private float interactionDistance = 2f;
    [SerializeField] private Transform leverPivot;
    [SerializeField] private SpriteRenderer indicator;
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private TMP_Text interactionPrompt;

    private playerMovement player;
    private float readyAt;
    public bool IsPowered { get; private set; }
    [SerializeField] private MultipleChoiceQuestionData[] puzzleQuestions;
    private bool solved, asking;
    private string saveId;
    [SerializeField, HideInInspector] private string circuitId;
    [SerializeField, HideInInspector] private string legacySaveId;

    private string LegacySaveId()
    {
        string id = gameObject.scene.path;
        for (Transform part = transform; part != null; part = part.parent) id += "/" + part.name;
        return id;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying || !gameObject.scene.IsValid() ||
            string.IsNullOrEmpty(gameObject.scene.path) ||
            UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(gameObject.scene)) return;
        var objectId = UnityEditor.GlobalObjectId.GetGlobalObjectIdSlow(this);
        if (objectId.targetObjectId == 0) return;
        string id = objectId.ToString();
        if (circuitId == id) return;
        // Existing switches migrate their own progress. A copied switch gets a new
        // circuit identity without inheriting the source switch's solved state.
        legacySaveId = string.IsNullOrEmpty(circuitId) ? LegacySaveId() : string.Empty;
        circuitId = id;
        UnityEditor.EditorUtility.SetDirty(this);
        if (UnityEditor.PrefabUtility.IsPartOfPrefabInstance(this))
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
    }
#endif

    void Start()
    {
        saveId = string.IsNullOrEmpty(circuitId) ? LegacySaveId() : gameObject.scene.path + "/circuit/" + circuitId;
        var saved=SaveSlots.Read(SaveSlots.Active);
        solved=saved?.solvedTrapSwitches!=null&&saved.solvedTrapSwitches.Exists(id=>
            id == saveId || (!string.IsNullOrEmpty(legacySaveId) && id != null &&
            System.Text.RegularExpressions.Regex.Replace(id,@"#\d+(?=/|$)","") == legacySaveId));
        if(solved&&!saved.solvedTrapSwitches.Contains(saveId))
        {
            saved.solvedTrapSwitches.Add(saveId);
            SaveSlots.Write(saved);
        }
        SetPowered(!solved&&startsPowered);
    }

    private bool CanInteract()
    {
        if (solved || asking || GameControlsGuide.MapHidden || Time.timeScale <= 0f || Time.unscaledTime < readyAt) return false;
        if (player == null) player = FindFirstObjectByType<playerMovement>();
        if (player == null) return false;
        var body = player.GetComponent<Collider2D>();
        Vector2 center = body != null ? body.bounds.center : player.transform.position;
        return Vector2.Distance(center, (Vector2)transform.position + Vector2.up * 0.65f) <= interactionDistance;
    }

    void Update()
    {
        bool nearby = CanInteract();
        if (interactionPrompt != null)
        {
            interactionPrompt.gameObject.SetActive(nearby);
            interactionPrompt.text = "[E] Solve trap puzzle";
        }
        bool pressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        pressed |= Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;
        if (nearby && pressed) TryInteract();
    }

    public bool TryInteract()
    {
        if (!CanInteract()) return false;
        var questions=QuestionSystem.Instance!=null?QuestionSystem.Instance.GetQuestionsForCurrentPlayer():null;
        if(questions==null||questions.Length==0)questions=puzzleQuestions;
        if(questions==null||questions.Length==0)return false;
        asking=true;
        DoorJavaQuestionPanel.Show(questions,()=>{
            if(this==null)return;
            asking=false;solved=true;SetPowered(false);
            var saved=SaveSlots.Read(SaveSlots.Active)??new SaveSlots.Record{character=PlayerPrefs.GetInt("SelectedCharacter",0)};
            if(saved.solvedTrapSwitches==null)saved.solvedTrapSwitches=new System.Collections.Generic.List<string>();
            if(!saved.solvedTrapSwitches.Contains(saveId))saved.solvedTrapSwitches.Add(saveId);
            SaveSlots.Write(saved);
        },()=>{if(this!=null)asking=false;},true);
        readyAt = Time.unscaledTime + 0.3f;
        return true;
    }

    public void SetPowered(bool powered)
    {
        if(solved)powered=false;
        IsPowered = powered;
        if (controlledTraps != null)
            foreach (var trap in controlledTraps)
            {
                if (trap == null) continue;
                trap.enabled = powered;
                foreach (var hitbox in trap.GetComponents<Collider2D>()) hitbox.enabled = powered;
                var animation = trap.GetComponent<TrapPackAnimation>();
                if (animation != null)
                {
                    animation.enabled = powered;
                    var renderer = trap.GetComponent<SpriteRenderer>();
                    if (!powered && renderer != null && animation.IdleSprite != null)
                        renderer.sprite = animation.IdleSprite;
                }
            }
        if (leverPivot != null) leverPivot.localRotation = Quaternion.Euler(0f, 0f, powered ? -35f : 35f);
        if (indicator != null) indicator.color = powered ? new Color(1f, 0.2f, 0.1f) : new Color(0.3f, 1f, 0.4f);
        if (statusLabel != null) statusLabel.text = powered ? "ON" : "OFF";
    }
}
