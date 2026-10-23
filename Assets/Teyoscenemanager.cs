using UnityEngine;

public class TeyoSceneManager : MonoBehaviour
{
    [SerializeField]private string loadScene;
    [Min(0)] public int requiredDefeatedEncounters;
    private bool isLoading;
    private bool requiredVictoriesMet;
    private float nextRequirementRefresh;

    private void RefreshRequirement()
    {
        nextRequirementRefresh = Time.unscaledTime + 0.5f;
        var saved = requiredDefeatedEncounters > 0 ? SaveSlots.Read(SaveSlots.Active) : null;
        requiredVictoriesMet = requiredDefeatedEncounters <= 0 ||
            ElfQuestProgress.Cleared(saved) >= requiredDefeatedEncounters;
    }

    private bool HasRequiredVictories() => requiredVictoriesMet;
    
    public void GoToScene(string loadScene){
    if (isLoading || string.IsNullOrWhiteSpace(loadScene) ||
        !Application.CanStreamedLevelBeLoaded(loadScene) || !HasRequiredVictories()) return;
    isLoading = true;
    Time.timeScale = 1f;
    SceneTransitionFader.Load(loadScene);
    

    }
    private void Start()
    {
        RefreshRequirement();
        PortalInteraction.For(gameObject).AddRoute(
            () => isActiveAndEnabled && !isLoading && HasRequiredVictories() &&
                !string.IsNullOrWhiteSpace(loadScene) && Application.CanStreamedLevelBeLoaded(loadScene),
            player => GoToScene(loadScene));
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextRequirementRefresh)
            RefreshRequirement();
    }
}
