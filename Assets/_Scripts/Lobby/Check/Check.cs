using UnityEngine;
using System.Collections;
using UnityEngine.UI; // CanvasGroup을 위해 추가
using System; // Action을 위해 추가

public class SongSelect : MonoBehaviour
{
    // LobbyUIManager에 알릴 이벤트 정의
    // 현재 선택된 노래의 인덱스를 전달합니다.
    public static event Action<int> OnSongChanged;

    // 버튼 타입을 정의: 왼쪽 또는 오른쪽
    public enum ButtonType { Left, Right }
    public ButtonType buttonType;

    // UI 패널 배열과 오디오 클립 배열
    public GameObject[] panels;
    public AudioClip[] clips;

    // 현재 선택된 패널 인덱스 (static 유지)
    public static int currentIndex = 0;

    // 버튼 재입력을 막기 위한 쿨다운 설정
    [SerializeField] private float cooldown = 1f;
    private static float lastTriggerTime = -999f;

    private AudioSource audioSource;

    [Header("Transition Settings")]
    public float slideDuration = 0.4f;    // 슬라이드 애니메이션 지속 시간
    public float slideDistance = 1920f;   // 슬라이드할 거리 (화면 너비 기준)
    public float sidePanelOffset = 1920f; // 양옆 패널의 초기 오프셋 (선택된 패널과의 거리)
    public float sidePanelAlpha = 0.5f;   // 양옆 패널의 불투명도 (0.0f - 1.0f)

    // 선택된 패널의 스케일 배율
    [Header("Panel Scale Settings")]
    public float selectedPanelScale = 2.0f; // 선택된 패널의 스케일 배율

    // 양옆 패널의 회전 각도 (Y축)
    [Header("Panel Rotation Settings")]
    public float sidePanelRotationAngle = 15f; // 양옆 패널의 회전 각도 (Y축)

    private void Start()
    {
        audioSource = FindAnyObjectByType<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("[SelectSong] AudioSource not found in scene.");
        }
        else
        {
            SetupPanels();
            UpdatePanelVisibility(currentIndex);
            PlayClip(currentIndex);
        }
    }

    private void SetupPanels()
    {
        foreach (GameObject panel in panels)
        {
            if (panel.GetComponent<CanvasGroup>() == null)
            {
                panel.AddComponent<CanvasGroup>();
            }
            panel.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Saber")) return;
        if (Time.time - lastTriggerTime < cooldown) return;

        lastTriggerTime = Time.time;
        OnClick(buttonType);
    }

    void OnClick(ButtonType type)
    {
        int nextIndex = currentIndex;

        switch (type)
        {
            case ButtonType.Left:
                nextIndex = (currentIndex - 1 + panels.Length) % panels.Length;
                break;
            case ButtonType.Right:
                nextIndex = (currentIndex + 1) % panels.Length;
                break;
        }

        if (nextIndex != currentIndex)
        {
            StartCoroutine(SlideTransition(currentIndex, nextIndex, type));
            currentIndex = nextIndex;
            PlayClip(currentIndex);
            OnSongChanged?.Invoke(currentIndex);
        }
    }

    IEnumerator SlideTransition(int fromIndex, int toIndex, ButtonType type)
    {
        int prevFromIndex = (fromIndex - 1 + panels.Length) % panels.Length;
        int nextFromIndex = (fromIndex + 1) % panels.Length;
        int prevToIndex = (toIndex - 1 + panels.Length) % panels.Length;
        int nextToIndex = (toIndex + 1) % panels.Length;

        RectTransform fromRT = panels[fromIndex].GetComponent<RectTransform>();
        CanvasGroup fromCG = panels[fromIndex].GetComponent<CanvasGroup>();

        RectTransform toRT = panels[toIndex].GetComponent<RectTransform>();
        CanvasGroup toCG = panels[toIndex].GetComponent<CanvasGroup>();

        RectTransform prevFromRT = panels[prevFromIndex].GetComponent<RectTransform>();
        CanvasGroup prevFromCG = panels[prevFromIndex].GetComponent<CanvasGroup>();

        RectTransform nextFromRT = panels[nextFromIndex].GetComponent<RectTransform>();
        CanvasGroup nextFromCG = panels[nextFromIndex].GetComponent<CanvasGroup>();

        RectTransform prevToRT = panels[prevToIndex].GetComponent<RectTransform>();
        CanvasGroup prevToCG = panels[prevToIndex].GetComponent<CanvasGroup>();

        RectTransform nextToRT = panels[nextToIndex].GetComponent<RectTransform>();
        CanvasGroup nextToCG = panels[nextToIndex].GetComponent<CanvasGroup>();

        float dir = type == ButtonType.Left ? 1 : -1;

        Vector2 prevFromStartPos = new Vector2(-sidePanelOffset, 0);
        Vector2 prevFromEndPos = new Vector2(-sidePanelOffset + dir * slideDistance, 0);

        Vector2 fromStartPos = Vector2.zero;
        Vector2 fromEndPos = new Vector2(dir * slideDistance, 0);

        Vector2 nextFromStartPos = new Vector2(sidePanelOffset, 0);
        Vector2 nextFromEndPos = new Vector2(sidePanelOffset + dir * slideDistance, 0);

        Vector2 prevToStartPos = new Vector2(-sidePanelOffset - dir * slideDistance, 0);
        Vector2 prevToEndPos = new Vector2(-sidePanelOffset, 0);

        Vector2 toStartPos = new Vector2(-dir * slideDistance, 0);
        Vector2 toEndPos = Vector2.zero;

        Vector2 nextToStartPos = new Vector2(sidePanelOffset - dir * slideDistance, 0);
        Vector2 nextToEndPos = new Vector2(sidePanelOffset, 0);

        // 스케일 설정
        Vector3 fromStartScale = Vector3.one * selectedPanelScale;
        Vector3 fromEndScale = Vector3.one;
        Vector3 toStartScale = Vector3.one;
        Vector3 toEndScale = Vector3.one * selectedPanelScale;
        Vector3 sidePanelFixedScale = Vector3.one;

        // 회전 설정 (Y축으로 변경)
        // 왼쪽 패널은 Y축 양의 방향으로 회전 (오른쪽으로 돌아가는 느낌)
        // 오른쪽 패널은 Y축 음의 방향으로 회전 (왼쪽으로 돌아가는 느낌)
        Quaternion fromStartRot = Quaternion.Euler(0, 0, 0); // 현재 중앙 패널은 회전 없음
        Quaternion fromEndRot = (type == ButtonType.Left) ? Quaternion.Euler(0, sidePanelRotationAngle, 0) : Quaternion.Euler(0, -sidePanelRotationAngle, 0); // 이동 후 양옆 패널 회전
        Quaternion toStartRot = (type == ButtonType.Left) ? Quaternion.Euler(0, -sidePanelRotationAngle, 0) : Quaternion.Euler(0, sidePanelRotationAngle, 0); // 이동할 패널은 양옆 패널 회전
        Quaternion toEndRot = Quaternion.Euler(0, 0, 0); // 이동 후 중앙 패널 회전 없음

        Quaternion sidePanelFixedLeftRot = Quaternion.Euler(0, -sidePanelRotationAngle, 0); // 왼쪽에 배치될 패널은 Y축 음수 회전
        Quaternion sidePanelFixedRightRot = Quaternion.Euler(0, sidePanelRotationAngle, 0); // 오른쪽에 배치될 패널은 Y축 양수 회전


        foreach (GameObject p in panels) p.SetActive(false);

        panels[prevFromIndex].SetActive(true);
        panels[fromIndex].SetActive(true);
        panels[nextFromIndex].SetActive(true);

        panels[prevToIndex].SetActive(true);
        panels[toIndex].SetActive(true);
        panels[nextToIndex].SetActive(true);

        // 초기 위치 설정
        prevFromRT.anchoredPosition = prevFromStartPos;
        fromRT.anchoredPosition = fromStartPos;
        nextFromRT.anchoredPosition = nextFromStartPos;

        prevToRT.anchoredPosition = prevToStartPos;
        toRT.anchoredPosition = toStartPos;
        nextToRT.anchoredPosition = nextToStartPos;

        // 초기 알파 설정
        prevFromCG.alpha = sidePanelAlpha;
        fromCG.alpha = 1f;
        nextFromCG.alpha = sidePanelAlpha;

        prevToCG.alpha = sidePanelAlpha;
        toCG.alpha = sidePanelAlpha;
        nextToCG.alpha = sidePanelAlpha;

        // 초기 스케일 설정
        fromRT.localScale = fromStartScale;
        toRT.localScale = toStartScale;
        prevFromRT.localScale = sidePanelFixedScale;
        nextFromRT.localScale = sidePanelFixedScale;
        prevToRT.localScale = sidePanelFixedScale;
        nextToRT.localScale = sidePanelFixedScale;

        // 초기 회전 설정
        fromRT.rotation = fromStartRot;
        toRT.rotation = toStartRot;
        prevFromRT.rotation = (prevFromRT.anchoredPosition.x < 0) ? sidePanelFixedLeftRot : sidePanelFixedRightRot;
        nextFromRT.rotation = (nextFromRT.anchoredPosition.x < 0) ? sidePanelFixedLeftRot : sidePanelFixedRightRot;
        prevToRT.rotation = (prevToRT.anchoredPosition.x < 0) ? sidePanelFixedLeftRot : sidePanelFixedRightRot;
        nextToRT.rotation = (nextToRT.anchoredPosition.x < 0) ? sidePanelFixedLeftRot : sidePanelFixedRightRot;


        float t = 0;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            float lerpT = t / slideDuration;

            // 위치 애니메이션
            prevFromRT.anchoredPosition = Vector2.Lerp(prevFromStartPos, prevFromEndPos, lerpT);
            fromRT.anchoredPosition = Vector2.Lerp(fromStartPos, fromEndPos, lerpT);
            nextFromRT.anchoredPosition = Vector2.Lerp(nextFromStartPos, nextFromEndPos, lerpT);

            prevToRT.anchoredPosition = Vector2.Lerp(prevToStartPos, prevToEndPos, lerpT);
            toRT.anchoredPosition = Vector2.Lerp(toStartPos, toEndPos, lerpT);
            nextToRT.anchoredPosition = Vector2.Lerp(nextToStartPos, nextToEndPos, lerpT);

            // 알파 애니메이션
            fromCG.alpha = Mathf.Lerp(1f, sidePanelAlpha, lerpT);
            toCG.alpha = Mathf.Lerp(sidePanelAlpha, 1f, lerpT);

            // 스케일 애니메이션
            fromRT.localScale = Vector3.Lerp(fromStartScale, fromEndScale, lerpT);
            toRT.localScale = Vector3.Lerp(toStartScale, toEndScale, lerpT);

            // 회전 애니메이션 (Y축으로 변경)
            fromRT.rotation = Quaternion.Slerp(fromStartRot, fromEndRot, lerpT);
            toRT.rotation = Quaternion.Slerp(toStartRot, toEndRot, lerpT);

            yield return null;
        }

        // 애니메이션 종료 후 최종 상태 설정
        prevFromRT.anchoredPosition = prevFromEndPos;
        fromRT.anchoredPosition = fromEndPos;
        nextFromRT.anchoredPosition = nextFromEndPos;

        prevToRT.anchoredPosition = prevToEndPos;
        toRT.anchoredPosition = toEndPos;
        nextToRT.anchoredPosition = nextToEndPos;

        prevFromCG.alpha = sidePanelAlpha;
        fromCG.alpha = sidePanelAlpha;
        nextFromCG.alpha = sidePanelAlpha;

        prevToCG.alpha = sidePanelAlpha;
        toCG.alpha = 1f;
        nextToCG.alpha = sidePanelAlpha;

        // 최종 스케일 설정
        fromRT.localScale = fromEndScale;
        toRT.localScale = toEndScale;
        prevFromRT.localScale = sidePanelFixedScale;
        nextFromRT.localScale = sidePanelFixedScale;
        prevToRT.localScale = sidePanelFixedScale;
        nextToRT.localScale = sidePanelFixedScale;

        // 최종 회전 설정 (Y축으로 변경)
        fromRT.rotation = fromEndRot;
        toRT.rotation = toEndRot;
        prevFromRT.rotation = (prevFromRT.anchoredPosition.x < 0) ? sidePanelFixedLeftRot : sidePanelFixedRightRot;
        nextFromRT.rotation = (nextFromRT.anchoredPosition.x < 0) ? sidePanelFixedLeftRot : sidePanelFixedRightRot;
        prevToRT.rotation = (prevToRT.anchoredPosition.x < 0) ? sidePanelFixedLeftRot : sidePanelFixedRightRot;
        nextToRT.rotation = (nextToRT.anchoredPosition.x < 0) ? sidePanelFixedLeftRot : sidePanelFixedRightRot;

        UpdatePanelVisibility(toIndex);
    }

    void UpdatePanelVisibility(int centerIndex)
    {
        // 모든 패널의 상태를 초기화
        foreach (GameObject p in panels)
        {
            p.SetActive(false);
            RectTransform rt = p.GetComponent<RectTransform>();
            CanvasGroup cg = p.GetComponent<CanvasGroup>();
            if (rt != null)
            {
                rt.anchoredPosition = Vector2.zero;
                rt.localScale = Vector3.one;
                rt.rotation = Quaternion.identity; // 회전 초기화 (0도)
            }
            if (cg != null) cg.alpha = 1f;
        }

        // 중앙 패널 설정
        panels[centerIndex].SetActive(true);
        panels[centerIndex].GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        panels[centerIndex].GetComponent<CanvasGroup>().alpha = 1f;
        panels[centerIndex].GetComponent<RectTransform>().localScale = Vector3.one * selectedPanelScale;
        panels[centerIndex].GetComponent<RectTransform>().rotation = Quaternion.identity; // 회전 없음

        // 이전 패널 (왼쪽) 설정
        int prevIndex = (centerIndex - 1 + panels.Length) % panels.Length;
        panels[prevIndex].SetActive(true);
        panels[prevIndex].GetComponent<RectTransform>().anchoredPosition = new Vector2(-sidePanelOffset, 100);
        panels[prevIndex].GetComponent<CanvasGroup>().alpha = sidePanelAlpha;
        panels[prevIndex].GetComponent<RectTransform>().localScale = Vector3.one;
        // 이전 패널을 왼쪽으로 회전 (Y축 음수 회전)
        panels[prevIndex].GetComponent<RectTransform>().rotation = Quaternion.Euler(0, -sidePanelRotationAngle, 0);

        // 다음 패널 (오른쪽) 설정
        int nextIndex = (centerIndex + 1) % panels.Length;
        panels[nextIndex].SetActive(true);
        panels[nextIndex].GetComponent<RectTransform>().anchoredPosition = new Vector2(sidePanelOffset, 100);
        panels[nextIndex].GetComponent<CanvasGroup>().alpha = sidePanelAlpha;
        panels[nextIndex].GetComponent<RectTransform>().localScale = Vector3.one;
        // 다음 패널을 오른쪽으로 회전 (Y축 양수 회전)
        panels[nextIndex].GetComponent<RectTransform>().rotation = Quaternion.Euler(0, sidePanelRotationAngle, 0);

        Debug.Log($"[Panels] Current: {centerIndex}, Prev: {prevIndex}, Next: {nextIndex}");
    }

    void PlayClip(int index)
    {
        if (audioSource == null || clips == null || index >= clips.Length) return;

        audioSource.clip = clips[index];
        audioSource.Play();
        Debug.Log($"[Audio] Playing clip: {clips[index].name}");
    }
}