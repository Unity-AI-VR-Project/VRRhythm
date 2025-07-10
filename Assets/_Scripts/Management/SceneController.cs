using System.Collections;
using Define;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public eScenes currentScene;

    private void Awake()
    {
        currentScene = (eScenes)SceneManager.GetActiveScene().buildIndex;
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsyncCoroutine(sceneName));
    }

    public void LoadScene(eScenes scene)
    {
        string sceneName = scene.ToString();
        StartCoroutine(LoadSceneAsyncCoroutine(sceneName));
    }

    public IEnumerator LoadSceneAsyncCoroutine(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }
}
