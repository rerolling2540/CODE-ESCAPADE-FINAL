
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class PortalNextScene : MonoBehaviour
{
    public string nextSceneName = "NextScene";
    public AudioClip portalSound;
    [Min(0)] public int requiredDefeatedEncounters;
    public bool hideUntilUnlocked = true;

    [Range(0f, 1f)]
    public float soundVolume = 1f;

    private bool isEntering = false;
    private Renderer[] portalVisuals;
    private bool[] originalVisualStates;
    private bool requiredVictoriesMet;
    private float nextRequirementRefresh;

    private void Start()
    {
        // A child portal keeps its own visibility and unlock requirement.
        portalVisuals = GetComponentsInChildren<Renderer>(true)
            .Where(r => r.GetComponentInParent<PortalNextScene>() == this &&
                (r.GetComponentInParent<TwoWayPortal>() == null ||
                 r.GetComponentInParent<TwoWayPortal>().gameObject == gameObject)).ToArray();
        originalVisualStates = new bool[portalVisuals.Length];
        for (int i = 0; i < portalVisuals.Length; i++)
            originalVisualStates[i] = portalVisuals[i].enabled;
        RefreshRequirement();
        RefreshPortalVisibility();

        PortalInteraction.For(gameObject).AddRoute(
            () => isActiveAndEnabled && !isEntering && HasRequiredVictories() &&
                !string.IsNullOrEmpty(nextSceneName) && Application.CanStreamedLevelBeLoaded(nextSceneName),
            player => { isEntering = true; SceneTransitionFader.Load(nextSceneName); });
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextRequirementRefresh)
            RefreshRequirement();
        RefreshPortalVisibility();
    }

    private void RefreshRequirement()
    {
        nextRequirementRefresh = Time.unscaledTime + 0.5f;
        var saved = requiredDefeatedEncounters > 0 ? SaveSlots.Read(SaveSlots.Active) : null;
        requiredVictoriesMet = requiredDefeatedEncounters <= 0 ||
            ElfQuestProgress.Cleared(saved) >= requiredDefeatedEncounters;
    }

    private bool HasRequiredVictories() => requiredVictoriesMet;

    private void RefreshPortalVisibility()
    {
        if (portalVisuals == null) return;
        bool unlocked = !hideUntilUnlocked || HasRequiredVictories();
        for (int i = 0; i < portalVisuals.Length; i++)
            if (portalVisuals[i] != null)
                portalVisuals[i].enabled = originalVisualStates[i] && unlocked;
    }
}
