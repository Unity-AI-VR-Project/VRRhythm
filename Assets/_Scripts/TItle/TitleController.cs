using UnityEngine;
using UnityEngine.UI;
using Define;

public class TitleController : MonoBehaviour
{
    public AudioClip titleBGMClip;
    public TitleNoteSpawner[] titleNoteSpawners;
    public Button toLobbyButton;
    private void Start()
    {
        toLobbyButton.onClick.AddListener(LoadNextScene);
        Invoke("InitializeTitle", .5f);
    }

    void InitializeTitle()
    {
        
        GameManager.Instance.soundManager.PlayMusic(titleBGMClip);
        foreach (TitleNoteSpawner spawner in titleNoteSpawners)
        {
            spawner.NoteCreate();
        }
    }

    public void LoadNextScene()
    {
        GameManager.Instance.soundManager.PauseMusic();
        GameManager.Instance.sceneController.LoadScene(eScenes.Lobby);
    }
}
