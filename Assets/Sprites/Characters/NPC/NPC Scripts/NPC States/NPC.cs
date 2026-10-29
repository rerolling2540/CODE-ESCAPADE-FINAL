using UnityEngine;

[RequireComponent(typeof(Npc_Talk))]
public class NPC : MonoBehaviour
{   
    public enum NPCState {Default, Idle, Talk}
    public NPCState currentState = NPCState.Default;
    private NPCState defaultState;


    public Npc_Talk talk;
    private GUIStyle questBubbleStyle;
    void Reset() { talk = GetComponent<Npc_Talk>(); }
    void Awake() { if (talk == null) talk = GetComponent<Npc_Talk>(); }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        defaultState = currentState;
        SwitchState(currentState);
        if (talk != null && talk.offersElfQuest && talk.interactAnim != null)
        {
            // Replace the baked speech text with the live quest bubble.
            talk.interactAnim.enabled = false;
            var bubble = talk.interactAnim.GetComponent<SpriteRenderer>();
            if (bubble != null) bubble.enabled = false;
        }
    }
    
    public void SwitchState(NPCState newState)
    {
        currentState = newState;
        if (talk != null) talk.enabled = newState == NPCState.Talk;

    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag ("Player"))
            SwitchState(NPCState.Talk);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
            SwitchState(defaultState);
    }



    void OnGUI()
    {
        if (GameControlsGuide.MapHidden) return;
        if (talk == null || !talk.offersElfQuest || Camera.main == null) return;
        var saved = SaveSlots.Read(SaveSlots.Active);
        int stage = saved != null ? saved.elfQuestStage : 0;
        if (stage >= 4) return;
        bool highlighted = stage == 0 || ElfQuestProgress.Ready(saved);
        Color color = highlighted ? new Color(1f, 0.78f, 0.12f) : new Color(0.4f, 0.65f, 0.85f);
        Vector3 anchor = talk.interactAnim != null ? talk.interactAnim.transform.position : transform.position + Vector3.up * 1.5f;
        Vector3 screen = Camera.main.WorldToScreenPoint(anchor);
        if (screen.z <= 0f) return;
        if (questBubbleStyle == null)
            questBubbleStyle = new GUIStyle(GUI.skin.label) {
                fontSize = 26, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter, padding = new RectOffset()
            };
        var oldMatrix = GUI.matrix;
        var oldColor = GUI.color;
        GUI.matrix = Matrix4x4.identity;
        float pulse = highlighted ? 0.85f + 0.15f * Mathf.Sin(Time.unscaledTime * 4f) : 1f;
        Rect bubbleRect = new Rect(screen.x - 16f, Screen.height - screen.y - 20f, 32f, 36f);
        GUI.color = new Color(color.r, color.g, color.b, pulse);
        GUI.DrawTexture(bubbleRect, Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(screen.x - 4f, bubbleRect.yMax, 8f, 5f), Texture2D.whiteTexture);
        GUI.color = new Color(0.08f, 0.035f, 0.015f);
        GUI.DrawTexture(new Rect(bubbleRect.x + 2f, bubbleRect.y + 2f, 28f, 32f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        questBubbleStyle.normal.textColor = color; UiTextColors.Stable(questBubbleStyle);
        GUI.Label(bubbleRect, stage == 0 ? "!" : "?", questBubbleStyle);
        GUI.color = oldColor;
        GUI.matrix = oldMatrix;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
