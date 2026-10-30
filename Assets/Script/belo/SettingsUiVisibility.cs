using System.Collections.Generic;
using UnityEngine;

/// <summary>Settings owns the screen. New Canvas UI is included automatically.</summary>
public sealed class SettingsUiVisibility : MonoBehaviour
{
    private static SettingsUiVisibility instance;
    private readonly Dictionary<CanvasGroup, State> hidden = new Dictionary<CanvasGroup, State>();
    private readonly Dictionary<CanvasGroup, bool> overrides = new Dictionary<CanvasGroup, bool>();
    private struct State { public float alpha; public bool interactable, raycasts; }

    public static void EnsureInstance()
    {
        if (instance != null) return;
        instance = new GameObject("Settings UI Visibility").AddComponent<SettingsUiVisibility>();
        DontDestroyOnLoad(instance.gameObject);
    }
    public static void RefreshNow()
    {
        // A newly opened modal may live below a branch hidden on the previous
        // frame. Restore first, then evaluate the newly protected hierarchy.
        if (instance != null) { instance.Restore(); instance.Refresh(); }
    }
    void OnEnable() => Canvas.willRenderCanvases += Refresh;
    void OnDisable() { Canvas.willRenderCanvases -= Refresh; Restore(); }
    void LateUpdate() => Refresh();

    void Refresh()
    {
        // MainMenuPresentation already owns home/modal visibility and focus.
        if(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="MainMenu"){Restore();return;}
        if (!GameControlsGuide.SettingsOpen) { Restore(); return; }
        // Include newly authored UI and canvases created while settings is open.
        var roots = new HashSet<Canvas>();
        foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) roots.Add(canvas.rootCanvas);
        foreach (var canvas in roots) HideOutsideSettings(canvas.transform);
    }
    void HideOutsideSettings(Transform branch)
    {
        if (GameControlsGuide.IsSettingsContent(branch)) return;
        if (GameControlsGuide.IsSettingsBranch(branch))
        {
            foreach (Transform child in branch) HideOutsideSettings(child);
            return;
        }
        var group = branch.GetComponent<CanvasGroup>();
        if (group == null) group = branch.gameObject.AddComponent<CanvasGroup>();
        if (!hidden.ContainsKey(group)) hidden.Add(group,new State { alpha=group.alpha, interactable=group.interactable, raycasts=group.blocksRaycasts });
        group.alpha=0; group.interactable=false; group.blocksRaycasts=false;
        foreach (var nested in branch.GetComponentsInChildren<CanvasGroup>(true))
        {
            if (!overrides.ContainsKey(nested)) overrides.Add(nested,nested.ignoreParentGroups);
            nested.ignoreParentGroups=false;
        }
    }
    void Restore()
    {
        foreach (var entry in hidden)
            if (entry.Key != null) { entry.Key.alpha=entry.Value.alpha; entry.Key.interactable=entry.Value.interactable; entry.Key.blocksRaycasts=entry.Value.raycasts; }
        foreach (var entry in overrides) if(entry.Key!=null)entry.Key.ignoreParentGroups=entry.Value;
        hidden.Clear(); overrides.Clear();
    }
}
