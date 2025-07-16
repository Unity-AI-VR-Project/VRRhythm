using Define;
using System.Collections;
using UnityEngine;

public class MusicDisplay : MonoBehaviour
{
    public GameObject MusicSelectPrefab;
    public RectTransform displayGroup;
    [SerializeField] private int currentMusicNumber = 1;
    private int maxMusicNumber = 2;
    [SerializeField] private float displayMoveDistance = 32;
    [SerializeField] private float displayMoveDuration = .4f;
    private bool isMoving = false;
    private float lastMoveCompleteTime = -Mathf.Infinity;
    private AudioClip[] musicClips;
    [SerializeField] private AudioClip buttonClickClip;
    void Start()
    {
        Initialize();
        
    }

    private void Initialize()
    {
        foreach (Transform child in displayGroup.transform)
        {
            Destroy(child.gameObject);
        }
        MusicData data = GameManager.Instance.dataManager.musicData;
        musicClips = new AudioClip[data.Music.Length];
        for (int i = 0; i < data.Music.Length; i++)
        {
            MusicDisplayObject obj = Instantiate(MusicSelectPrefab, displayGroup).GetComponent<MusicDisplayObject>();
            MusicItem item = data.Music[i];
            obj.SetObject(i, item.Name, item.Artist, item.BPM, item.Length);
            AudioClip clip = Resources.Load<AudioClip>($"Music/Sound/{i}");
            musicClips[i] = clip;
        }
        maxMusicNumber = data.Music.Length-1;
        GameManager.Instance.soundManager.PlayMusic(musicClips[currentMusicNumber]);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K))
            NextMusic();
        if (Input.GetKeyDown(KeyCode.J))
            PreviousMusic();
    }

    public void NextMusic()
    {
        Debug.Log($"current :{currentMusicNumber}\t{maxMusicNumber}");
        if (currentMusicNumber >= maxMusicNumber)
            return;
        if (TryMoveMusic(Vector2.left))
            currentMusicNumber++;
    }

    public void SelectMusic()
    {
        GameManager.Instance.soundManager.PlaySFX(buttonClickClip);
        GameManager.Instance.dataManager.selectedMusicNumber = currentMusicNumber;
        GameManager.Instance.sceneController.LoadScene(eScenes.InGame); 
    }

    public void PreviousMusic()
    {
        if (currentMusicNumber <= 0)
            return;
        if (TryMoveMusic(Vector2.right))
            currentMusicNumber--;
    }

    private bool TryMoveMusic(Vector2 direction)
    {
        if (isMoving || (Time.time < lastMoveCompleteTime + displayMoveDuration + 0.01f))
        {
            return false;
        }

        isMoving = true;
        StartCoroutine(DisplayMove(direction));
        return true;
    }

    private IEnumerator DisplayMove(Vector2 direction)
    {
        GameManager.Instance.soundManager.PauseMusic();
        GameManager.Instance.soundManager.PlaySFX(buttonClickClip);
        Vector2 currentPosition = displayGroup.anchoredPosition;
        Vector2 targetPosition = currentPosition + (direction * displayMoveDistance);
        float elapsedTime = 0f;

        while (elapsedTime < displayMoveDuration)
        {
            displayGroup.anchoredPosition = Vector2.Lerp(currentPosition, targetPosition, elapsedTime / displayMoveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        displayGroup.anchoredPosition = targetPosition;
        isMoving = false;
        lastMoveCompleteTime = Time.time;
        // Music
        GameManager.Instance.soundManager.PlayMusic(musicClips[currentMusicNumber]);
    }
}
