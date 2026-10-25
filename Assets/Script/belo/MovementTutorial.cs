
using System.Collections;
using UnityEngine;

public class MovementTutorial : MonoBehaviour
{
    [Header("Tutorial Content")]
    public string heading = "Movement Tutorial";

    [TextArea(3, 8)]
    public string instructions =
        "Use A and D or the Left and Right Arrow Keys to move.\n\n" +
        "Press Space to jump.\n\n" +
        "Press E or Escape to close this guide.";

    [Header("Fade Settings")]
    public float fadeInDuration = 2f;
    public float fadeOutDuration = 0.25f;

    private float alpha = 0f;
    private bool isClosing = false;
    private bool isOpen = true;

    private GUIStyle titleStyle;
    private GUIStyle bodyStyle;
    private GUIStyle buttonStyle;

    void Start()
    {
        // Pause the game while the tutorial is open.
        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        alpha = 0f;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, fadeInDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        alpha = 1f;
    }

    void Update()
    {
        if (GameControlsGuide.SettingsOpen) return;
        if (!isOpen || isClosing)
            return;

        if (Input.GetKeyDown(KeyCode.E) ||
            Input.GetKeyDown(KeyCode.Escape))
        {
            StartCoroutine(FadeOut());
        }
    }

    public void CloseTutorial()
    {
        if (!isOpen || isClosing)
            return;

        StartCoroutine(FadeOut());
    }

    IEnumerator FadeOut()
    {
        isClosing = true;

        float startAlpha = alpha;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, fadeOutDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            alpha = Mathf.Lerp(
                startAlpha,
                0f,
                Mathf.Clamp01(elapsed / duration)
            );

            yield return null;
        }

        alpha = 0f;
        isOpen = false;
        isClosing = false;

        // Tutorial ends; movement scripts remain untouched.
        enabled = false;
    }

    void OnGUI()
    {
        if (GameControlsGuide.SettingsOpen) return;
        if (!isOpen)
            return;

        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 28,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };

            bodyStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22,
                wordWrap = true,
                alignment = TextAnchor.UpperLeft
            };

            buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter
            };

            titleStyle.normal.textColor =
                new Color(1f, 0.9f, 0.68f);

            bodyStyle.normal.textColor =
                new Color(1f, 0.9f, 0.68f);
        }

        Matrix4x4 oldMatrix = GUI.matrix;

        float scale = Mathf.Min(
            Screen.width / 1280f,
            Screen.height / 720f
        );

        GUI.matrix = Matrix4x4.TRS(
            new Vector3(
                (Screen.width - 1280f * scale) / 2f,
                (Screen.height - 720f * scale) / 2f,
                0f
            ),
            Quaternion.identity,
            Vector3.one * scale
        );

        Color oldColor = GUI.color;

        // Original dark brown panel.
        GUI.color = new Color(
            0.08f, 0.06f, 0.04f, 0.96f * alpha
        );

        GUI.DrawTexture(
            new Rect(270, 160, 740, 400),
            Texture2D.whiteTexture
        );

        GUI.color = new Color(1f, 1f, 1f, alpha);

        GUI.Label(
            new Rect(300, 185, 680, 70),
            heading,
            titleStyle
        );

        GUI.Label(
            new Rect(315, 275, 650, 200),
            instructions,
            bodyStyle
        );

        GUI.color = new Color(1f, 1f, 1f, alpha);

        if (GUI.Button(
            new Rect(490, 495, 300, 42),
            "Close Tutorial [E / Esc]",
            buttonStyle
        ))
        {
            CloseTutorial();
        }

        GUI.color = oldColor;
        GUI.matrix = oldMatrix;
    }
}
