using UnityEngine;
using Define;

public class TitleController : MonoBehaviour
{
    public AudioClip titleBGMClip;

    private void Start()
    {
        GameManager.Instance.soundManager.PlayMusic(titleBGMClip);
    }

    public void LoadNextScene()
    {
        GameManager.Instance.sceneController.LoadScene(eScenes.Lobby);
    }
}
