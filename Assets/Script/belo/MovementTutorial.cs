
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
    private static readonly System.Collections.Generic.List<MovementTutorial> openTutorials = new System.Collections.Generic.List<MovementTutorial>();
    public static bool AnyOpen => openTutorials.Exists(t=>t!=null&&t.isActiveAndEnabled&&t.isOpen);
    void OnEnable(){if(!openTutorials.Contains(this))openTutorials.Add(this);}
    void OnDisable(){openTutorials.Remove(this);}





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
        if (GameControlsGuide.GameplayOverlayHidden || Time.timeScale <= 0f) return;
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
        if (GameControlsGuide.GameplayOverlayHidden || Time.timeScale <= 0f) return;
        if (!isOpen)
            return;

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

        if (GameControlsGuide.DrawTravellerGuide(heading,instructions,alpha)) CloseTutorial();

        GUI.matrix = oldMatrix;
    }
}
