using UnityEngine;
using UnityEngine.SceneManagement;

public class TeyoSceneManager : MonoBehaviour
{
    [SerializeField]private string loadScene;
    private bool isLoading;
    
    public void GoToScene(string loadScene){
    if (isLoading || string.IsNullOrWhiteSpace(loadScene) ||
        !Application.CanStreamedLevelBeLoaded(loadScene)) return;
    isLoading = true;
    Time.timeScale = 1f;
    SceneManager.LoadScene(loadScene);
    

    }
    private void Start()
    {
        PortalInteraction.For(gameObject).AddRoute(
            () => isActiveAndEnabled && !isLoading && !string.IsNullOrWhiteSpace(loadScene) && Application.CanStreamedLevelBeLoaded(loadScene),
            player => GoToScene(loadScene));
    }
}
