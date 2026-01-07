using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadGame(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}