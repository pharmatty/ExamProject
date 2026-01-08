using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public class LoadSceneOnCutsceneEnd : MonoBehaviour
{
    [Header("Scene to load after cutscene")]
    public string sceneName;

    private PlayableDirector director;

    void Awake()
    {
        director = GetComponent<PlayableDirector>();

        if (director != null)
        {
            director.stopped += OnTimelineStopped;
        }
        else
        {
            Debug.LogError("PlayableDirector not found on this GameObject.");
        }
    }

    private void OnTimelineStopped(PlayableDirector pd)
    {
        SceneManager.LoadScene(sceneName);
    }

    private void OnDestroy()
    {
        if (director != null)
        {
            director.stopped -= OnTimelineStopped;
        }
    }
}