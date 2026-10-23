using UnityEngine;
using UnityEngine.SceneManagement;

// A self-contained story scene. Copy is serialized so it can be revised in the Inspector.
public class StoryScene : MonoBehaviour
{
    [SerializeField] string destinationScene = "Scene 2";
    [SerializeField] string[] titles = { "THE TOWER CALLS", "KNOWLEDGE IS YOUR WEAPON", "BEYOND THE GATE", "YOUR FIRST CHAPTER" };
    [SerializeField, TextArea(3, 8)] string[] passages = {
        "At The RiftSpire, an elf awaits a new adventurer. Beyond its walls lie unfamiliar paths, restless creatures, and questions that cannot be answered by a blade alone.",
        "In this land, every challenge is a lesson. Read carefully, think through each problem, and turn your knowledge of code into the strength to survive. Your cards and your choices will shape each battle.",
        "The trail leads from the tower toward the caves. Goblins, skeletons, and stranger creatures guard the way. Prepare before crossing a portal: courage will carry you forward, but a clear mind will carry you home.",
        "Now it is your turn. Meet the elf, explore your surroundings, and learn what waits beyond the tower. The next page of Code Escapade belongs to you." };

    int page;
    bool leaving;
    GUIStyle heading, body, small, button;
    readonly Color gold = new Color(0.87f, 0.71f, 0.39f);

    void Awake()
    {
        Time.timeScale = 1f;
        var camera = new GameObject("Story Camera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.055f, 0.09f);
        camera.cullingMask = 0;
    }

    void Update()
    {
        if (leaving) return;
        if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space)) Next();
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) page = Mathf.Max(0, page - 1);
        else if (Input.GetKeyDown(KeyCode.Escape)) BeginAdventure();
    }

    void Next()
    {
        if (page < Mathf.Min(titles.Length, passages.Length) - 1) page++;
        else BeginAdventure();
    }

    void BeginAdventure()
    {
        if (leaving) return;
        if (!Application.CanStreamedLevelBeLoaded(destinationScene))
        {
            Debug.LogError("Story destination is missing from Build Settings: " + destinationScene);
            return;
        }
        leaving = true;
        SceneManager.LoadScene(destinationScene);
    }

    void Fill(Rect rect, Color color)
    {
        var previous = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previous;
    }

    void OnGUI()
    {
        if (titles.Length == 0 || passages.Length == 0) return;
        page = Mathf.Clamp(page, 0, Mathf.Min(titles.Length, passages.Length) - 1);
        if (heading == null)
        {
            heading = new GUIStyle(GUI.skin.label) { fontSize = 38, fontStyle = FontStyle.Bold, wordWrap = true };
            heading.normal.textColor = gold;
            body = new GUIStyle(GUI.skin.label) { fontSize = 25, wordWrap = true };
            body.normal.textColor = new Color(0.9f, 0.89f, 0.83f);
            small = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleLeft };
            small.normal.textColor = gold;
            button = new GUIStyle(GUI.skin.button) { fontSize = 19, fontStyle = FontStyle.Bold };
        }
        var previousMatrix = GUI.matrix;
        float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) / 2, (Screen.height - 720 * scale) / 2), Quaternion.identity, Vector3.one * scale);
        Fill(new Rect(-1280, -720, 3840, 2160), new Color(0.035f, 0.055f, 0.09f));
        // Layered silhouettes frame the tower like an illustrated storybook.
        Fill(new Rect(70, 90, 360, 530), new Color(0.065f, 0.11f, 0.16f));
        for (int i = 0; i < 20; i++)
        {
            float x = 88 + (i * 73 % 320);
            float y = 110 + (i * 47 % 240);
            Fill(new Rect(x, y, 2, 2), gold * 0.7f);
        }
        Fill(new Rect(208, 250, 95, 310), new Color(0.12f, 0.18f, 0.23f));
        Fill(new Rect(193, 235, 125, 35), new Color(0.18f, 0.24f, 0.28f));
        for (int i = 0; i < 4; i++) Fill(new Rect(193 + i * 34, 215, 23, 30), new Color(0.18f, 0.24f, 0.28f));
        for (int i = 0; i < 3; i++) Fill(new Rect(246, 295 + i * 65, 18, 29), gold);
        Fill(new Rect(234, 500, 42, 60), new Color(0.035f, 0.055f, 0.09f));
        Fill(new Rect(70, 560, 360, 60), new Color(0.08f, 0.14f, 0.17f));
        GUI.Label(new Rect(99, 578, 320, 28), "The RiftSpire / PROLOGUE", small);
        GUI.Label(new Rect(490, 100, 680, 30), "CODE ESCAPADE   /   THE STORY", small);
        Fill(new Rect(490, 150, 80, 3), gold);
        GUI.Label(new Rect(490, 185, 680, 105), titles[page], heading);
        GUI.Label(new Rect(490, 305, 660, 205), passages[page], body);
        int count = Mathf.Min(titles.Length, passages.Length);
        GUI.Label(new Rect(490, 530, 200, 30), "CHAPTER " + (page + 1) + " / " + count, small);
        for (int i = 0; i < count; i++) Fill(new Rect(490 + i * 38, 575, 26, 4), i == page ? gold : new Color(0.2f, 0.26f, 0.31f));
        GUI.enabled = page > 0 && !leaving;
        if (GUI.Button(new Rect(760, 550, 120, 50), "Back", button)) page--;
        GUI.enabled = !leaving;
        if (GUI.Button(new Rect(895, 550, 265, 50), page == count - 1 ? "Begin adventure" : "Next chapter", button)) Next();
        if (GUI.Button(new Rect(1010, 100, 150, 32), "Skip story", button)) BeginAdventure();
        GUI.Label(new Rect(490, 635, 680, 30), "ARROWS to turn pages   /   ENTER to continue   /   ESC to skip", small);
        GUI.enabled = true;
        GUI.matrix = previousMatrix;
    }
}
