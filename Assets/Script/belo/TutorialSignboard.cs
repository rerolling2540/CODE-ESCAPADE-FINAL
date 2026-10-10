using System.Collections.Generic;
using UnityEngine;

public class TutorialSignboard : MonoBehaviour
{
    public string heading = "Traveller's Guide";
    [TextArea(3, 8)] public string instructions;
    public float readDistance = 2.5f;

    private static readonly List<TutorialSignboard> signs = new List<TutorialSignboard>();
    private static TutorialSignboard openedSign;
    private static int lastInteractionFrame = -1;
    private playerMovement player;
    private float previousTimeScale;
    private GUIStyle titleStyle, bodyStyle, promptStyle;

    void OnEnable() { signs.Add(this); }

    void Start() { player = FindFirstObjectByType<playerMovement>(); }

    bool IsNearest()
    {
        if (player == null) return false;
        float distance = Vector2.Distance(player.transform.position, transform.position);
        if (distance > readDistance) return false;
        foreach (var sign in signs)
        {
            if (sign != this && sign != null &&
                Vector2.Distance(player.transform.position, sign.transform.position) < distance)
                return false;
        }
        return true;
    }

    void Update()
    {
        if (lastInteractionFrame == Time.frameCount) return;
        if (openedSign == this)
        {
            if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape)) Close();
            return;
        }
        if (openedSign == null && Time.timeScale > 0f && IsNearest() &&
            Input.GetKeyDown(KeyCode.E))
        {
            openedSign = this;
            lastInteractionFrame = Time.frameCount;
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
    }

    void Close()
    {
        if (openedSign != this) return;
        openedSign = null;
        lastInteractionFrame = Time.frameCount;
        Time.timeScale = previousTimeScale;
    }

    void OnDisable()
    {
        Close();
        signs.Remove(this);
    }

    void OnGUI()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter, wordWrap = true };
            bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, wordWrap = true };
            promptStyle = new GUIStyle(GUI.skin.box) { fontSize = 20,
                alignment = TextAnchor.MiddleCenter };
            titleStyle.normal.textColor = bodyStyle.normal.textColor = new Color(1f, 0.9f, 0.68f);
        }
        Matrix4x4 previousMatrix = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280f * scale) / 2f,
            (Screen.height - 720f * scale) / 2f, 0), Quaternion.identity, Vector3.one * scale);
        if (openedSign == this)
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(0.08f, 0.06f, 0.04f, 0.96f);
            GUI.DrawTexture(new Rect(270, 160, 740, 400), Texture2D.whiteTexture);
            GUI.color = previousColor;
            GUI.Label(new Rect(300, 185, 680, 70), heading, titleStyle);
            GUI.Label(new Rect(315, 275, 650, 200), instructions, bodyStyle);
            if (GUI.Button(new Rect(490, 495, 300, 42), "Close guide  [E / Esc]")) Close();
        }
        else if (openedSign == null && Time.timeScale > 0f && IsNearest())
            GUI.Box(new Rect(420, 625, 440, 48), "[E] Read " + heading, promptStyle);
        GUI.matrix = previousMatrix;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, readDistance);
    }
}
