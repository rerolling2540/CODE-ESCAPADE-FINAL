using UnityEngine;
using UnityEngine.SceneManagement;

public class TeyoSceneManager : MonoBehaviour
{
    [SerializeField]private string loadScene;
    
    public void GoToScene(string loadScene){
    SceneManager.LoadScene(loadScene);
    

    }
    public void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision)
        {
           GoToScene(loadScene);
        }
    }
}
