using UnityEngine;
using System.Collections;

public class SelectSong : MonoBehaviour
{
    // 버튼 타입을 정의: 왼쪽 또는 오른쪽
    public enum ButtonType { Left, Right }
    public ButtonType buttonType;

    // UI 패널 배열과 오디오 클립 배열
    public GameObject[] panels;
    public AudioClip[] clips;

    // 현재 선택된 패널 인덱스
    public static int currentIndex = 0;

    // 버튼 재입력을 막기 위한 쿨다운 설정
    [SerializeField] private float cooldown = 1f;
    private static float lastTriggerTime = -999f;

    private AudioSource audioSource;

    [Header("Transition Settings")]
    public float slideDuration = 0.4f;       // 슬라이드 애니메이션 지속 시간
    public float slideDistance = 1920f;      // 슬라이드할 거리 (화면 너비 기준)

    private void Start()
    {
        // 씬에서 AudioSource를 찾아 초기화
        audioSource = FindAnyObjectByType<AudioSource>();

        if (audioSource == null)
        {
            Debug.LogError("[SelectSong] AudioSource not found in scene.");
        }
        else
        {
            // 현재 인덱스의 패널만 활성화
            panels[currentIndex].SetActive(true);
            PlayClip(currentIndex); // 해당 패널에 대응하는 음악 재생
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Saber 태그를 가진 오브젝트에 의해 트리거될 경우만 반응
        if (!other.CompareTag("Saber")) return;

        // 쿨다운 시간 내에 중복 입력 방지
        if (Time.time - lastTriggerTime < cooldown) return;

        lastTriggerTime = Time.time;

        // 버튼 타입에 따라 이동 처리
        OnClick(buttonType);
    }

    void OnClick(ButtonType type)
    {
        int nextIndex = currentIndex;

        // 왼쪽 버튼일 경우 이전 인덱스로, 오른쪽이면 다음 인덱스로
        switch (type)
        {
            case ButtonType.Left:
                nextIndex = (currentIndex - 1 + panels.Length) % panels.Length;
                break;

            case ButtonType.Right:
                nextIndex = (currentIndex + 1) % panels.Length;
                break;
        }

        // 인덱스가 변경되었을 경우에만 슬라이드 전환 및 음악 재생
        if (nextIndex != currentIndex)
        {
            StartCoroutine(SlideTransition(currentIndex, nextIndex, type));
            currentIndex = nextIndex;
            PlayClip(currentIndex);
        }
    }

    IEnumerator SlideTransition(int fromIndex, int toIndex, ButtonType type)
    {
        // 기존 패널과 새 패널의 RectTransform 가져오기
        GameObject fromPanel = panels[fromIndex];
        GameObject toPanel = panels[toIndex];

        RectTransform fromRT = fromPanel.GetComponent<RectTransform>();
        RectTransform toRT = toPanel.GetComponent<RectTransform>();

        // 왼쪽 방향일 경우 + 슬라이드, 오른쪽은 - 슬라이드
        float dir = type == ButtonType.Left ? 1 : -1;

        // 시작 및 끝 위치 설정
        Vector2 fromStart = Vector2.zero;
        Vector2 fromEnd = new Vector2(dir * slideDistance, 0);
        Vector2 toStart = new Vector2(-dir * slideDistance, 0);
        Vector2 toEnd = Vector2.zero;

        // 새 패널 활성화 및 시작 위치 설정
        toPanel.SetActive(true);
        toRT.anchoredPosition = toStart;

        // 슬라이드 애니메이션 처리
        float t = 0;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            float lerpT = t / slideDuration;

            fromRT.anchoredPosition = Vector2.Lerp(fromStart, fromEnd, lerpT);
            toRT.anchoredPosition = Vector2.Lerp(toStart, toEnd, lerpT);

            yield return null;
        }

        // 이전 패널 위치 보정 및 비활성화
        fromRT.anchoredPosition = fromEnd;
        fromPanel.SetActive(false);
    }

    void PlayClip(int index)
    {
        // 오디오 소스 또는 클립이 없거나 인덱스가 범위를 넘으면 실행 안 함
        if (audioSource == null || clips == null || index >= clips.Length) return;

        // 해당 인덱스의 오디오 클립을 재생
        audioSource.clip = clips[index];
        audioSource.Play();
        Debug.Log($"[Audio] Playing clip: {clips[index].name}");
    }
}