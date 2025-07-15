using Define;
using System.Collections;
using Unity.VisualScripting;
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

    void Start()
    {
        Initialize();
        maxMusicNumber = displayGroup.childCount-1;
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
        if (currentMusicNumber >= maxMusicNumber)
            return;
        if (TryMoveMusic(Vector2.left))
            currentMusicNumber++;
    }

    public void SelectMusic()
    {

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
            Debug.Log("현재 이동 중이거나 너무 짧은 간격으로 실행 요청됨. 무시합니다.");
            return false;
        }

        isMoving = true;
        StartCoroutine(DisplayMove(direction));
        return true;
    }

    private IEnumerator DisplayMove(Vector2 direction)
    {
        GameManager.Instance.soundManager.PauseMusic();
        Vector2 currentPosition = displayGroup.anchoredPosition;
        Vector2 targetPosition = currentPosition + (direction * displayMoveDistance);
        float elapsedTime = 0f;

        while (elapsedTime < displayMoveDuration)
        {
            displayGroup.anchoredPosition = Vector2.Lerp(currentPosition, targetPosition, elapsedTime / displayMoveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 목표 위치에 정확히 도달하도록 마지막으로 설정
        displayGroup.anchoredPosition = targetPosition;
        isMoving = false;
        lastMoveCompleteTime = Time.time;
        // Music
        GameManager.Instance.soundManager.PlayMusic(musicClips[currentMusicNumber]);
    }
}
