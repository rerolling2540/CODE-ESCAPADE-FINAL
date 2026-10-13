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
    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision != null &&
            (collision.GetComponentInParent<playerMovement>() != null || collision.CompareTag("Player")))
        {
           GoToScene(loadScene);
        }
    }
}
