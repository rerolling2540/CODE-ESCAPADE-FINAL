
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyConfirmation : MonoBehaviour
{
    [Header("Confirmation Panel")]
    public GameObject confirmPanel;

    [Header("Scene Name")]
    public string lobbySceneName = "MainMenu";
    private GameObject overlay;
    private float previousTimeScale;

    public void ShowConfirmation()
    {
        if (confirmPanel == null) return;
        if (overlay == null) CreateOverlay();
        if (!overlay.activeSelf) previousTimeScale = Time.timeScale;
        GameControlsGuide.RegisterMapBlocker(confirmPanel);
        GameControlsGuide.RegisterSettings(overlay);
        overlay.SetActive(true);
        confirmPanel.SetActive(true);
        Time.timeScale = 0f;
        SettingsUiVisibility.RefreshNow();
    }

    public void ConfirmYes()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(lobbySceneName);
    }

    public void ConfirmNo()
    {
        confirmPanel.SetActive(false);
        if (overlay != null) overlay.SetActive(false);
        Time.timeScale = previousTimeScale;
        SettingsUiVisibility.RefreshNow();
    }

    void CreateOverlay()
    {
        var originalCanvas = confirmPanel.GetComponentInParent<Canvas>();
        var originalScaler = originalCanvas != null ? originalCanvas.rootCanvas.GetComponent<UnityEngine.UI.CanvasScaler>() : null;
        overlay = new GameObject("Lobby Confirmation Overlay", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        var canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32700;
        var scaler = overlay.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = originalScaler != null ? originalScaler.uiScaleMode : UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = originalScaler != null ? originalScaler.referenceResolution : new Vector2(1920,1080);
        scaler.matchWidthOrHeight = originalScaler != null ? originalScaler.matchWidthOrHeight : 0.5f;
        var shade = new GameObject("Confirmation Input Blocker", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        shade.transform.SetParent(overlay.transform,false);
        var rect = (RectTransform)shade.transform;
        rect.anchorMin=Vector2.zero; rect.anchorMax=Vector2.one; rect.offsetMin=rect.offsetMax=Vector2.zero;
        shade.GetComponent<UnityEngine.UI.Image>().color=new Color(0,0,0,0.6f);
        // Preserve the authored artwork, dimensions, callbacks and typography.
        var panelRect = confirmPanel.GetComponent<RectTransform>();
        Vector2 size = panelRect.rect.size;
        confirmPanel.transform.SetParent(overlay.transform,false);
        panelRect.anchorMin=panelRect.anchorMax=panelRect.pivot=new Vector2(0.5f,0.5f);
        panelRect.sizeDelta=size; panelRect.anchoredPosition=Vector2.zero;
        overlay.SetActive(false);
    }
}
