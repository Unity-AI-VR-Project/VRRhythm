using UnityEngine;
using System.Collections;
using UnityEngine.UI; // CanvasGroup을 위해 추가

public class Check : MonoBehaviour
{
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
    public float slideDuration = 0.4f;      // 슬라이드 애니메이션 지속 시간
    public float slideDistance = 1920f;     // 슬라이드할 거리 (화면 너비 기준)
    public float sidePanelOffset = 1920f;   // 양옆 패널의 초기 오프셋 (선택된 패널과의 거리)
    public float sidePanelAlpha = 0.5f;     // 양옆 패널의 불투명도 (0.0f - 1.0f)

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
            // 모든 패널의 CanvasGroup 컴포넌트 확인 및 추가
            SetupPanels();
            // 초기 상태 설정
            UpdatePanelVisibility(currentIndex);
            PlayClip(currentIndex); // 해당 패널에 대응하는 음악 재생
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
            // 시작 시 모든 패널 비활성화 (UpdatePanelVisibility에서 필요한 것만 고 위치 잡음)
            panel.SetActive(false);
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
            // 슬라이드 전환 시작 전에 이전 상태의 패널들 비활성화
            // StartCoroutine 안에서 활성화 및 위치 조정이 이루어짐
            // 현재 패널과 전환될 패널들만 관리하므로 명시적으로 비활성화 필요 없음

            StartCoroutine(SlideTransition(currentIndex, nextIndex, type));
            currentIndex = nextIndex;
            PlayClip(currentIndex);
        }
    }

    IEnumerator SlideTransition(int fromIndex, int toIndex, ButtonType type)
    {
        // 이전, 현재, 다음 패널 인덱스 계산
        int prevFromIndex = (fromIndex - 1 + panels.Length) % panels.Length;
        int nextFromIndex = (fromIndex + 1) % panels.Length;

        int prevToIndex = (toIndex - 1 + panels.Length) % panels.Length;
        int nextToIndex = (toIndex + 1) % panels.Length;

        // 관련 패널들의 RectTransform과 CanvasGroup 가져오기
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

        // 슬라이드 방향 설정
        float dir = type == ButtonType.Left ? 1 : -1; // 왼쪽으로 이동하면 패널은 오른쪽으로 이동 (+), 오른쪽으로 이동하면 패널은 왼쪽으로 이동 (-)

        // 각 패널의 시작 및 끝 위치 설정 (3개 패널이 나란히 배치되는 기준)
        // 현재 화면 중앙을 (0,0)으로 가정합니다.

        // from 패널 (이전 선택 패널) 관련
        // prevFrom (현재 화면 왼쪽) -> (더 왼쪽으로 사라짐)
        Vector2 prevFromStartPos = new Vector2(-sidePanelOffset, 0);
        Vector2 prevFromEndPos = new Vector2(-sidePanelOffset + dir * slideDistance, 0);

        // fromIndex (현재 화면 중앙) -> (왼쪽 또는 오른쪽으로 사라짐)
        Vector2 fromStartPos = Vector2.zero;
        Vector2 fromEndPos = new Vector2(dir * slideDistance, 0);

        // nextFrom (현재 화면 오른쪽) -> (더 오른쪽으로 사라짐)
        Vector2 nextFromStartPos = new Vector2(sidePanelOffset, 0);
        Vector2 nextFromEndPos = new Vector2(sidePanelOffset + dir * slideDistance, 0);


        // to 패널 (새로운 선택 패널) 관련
        // prevTo (이전 위치에서 나타남) -> (새로운 왼쪽 패널 위치)
        Vector2 prevToStartPos = new Vector2(-sidePanelOffset - dir * slideDistance, 0); // 슬라이드 될 위치에서 시작
        Vector2 prevToEndPos = new Vector2(-sidePanelOffset, 0);

        // toIndex (화면 밖에서 나타남) -> (새로운 중앙 패널 위치)
        Vector2 toStartPos = new Vector2(-dir * slideDistance, 0); // 슬라이드 될 위치에서 시작
        Vector2 toEndPos = Vector2.zero;

        // nextTo (다음 위치에서 나타남) -> (새로운 오른쪽 패널 위치)
        Vector2 nextToStartPos = new Vector2(sidePanelOffset - dir * slideDistance, 0); // 슬라이드 될 위치에서 시작
        Vector2 nextToEndPos = new Vector2(sidePanelOffset, 0);

        // --- 초기 상태 설정 ---
        // 모든 패널 비활성화 후 필요한 패널만 활성화
        foreach (GameObject p in panels) p.SetActive(false);

        panels[prevFromIndex].SetActive(true);
        panels[fromIndex].SetActive(true);
        panels[nextFromIndex].SetActive(true);

        panels[prevToIndex].SetActive(true); // toIndex로 전환될 때 나타날 prev 패널
        panels[toIndex].SetActive(true);     // toIndex (새로운 중앙 패널)
        panels[nextToIndex].SetActive(true); // toIndex로 전환될 때 나타날 next 패널

        // 애니메이션 시작 전 패널들의 초기 위치 설정
        prevFromRT.anchoredPosition = prevFromStartPos;
        fromRT.anchoredPosition = fromStartPos;
        nextFromRT.anchoredPosition = nextFromStartPos;

        prevToRT.anchoredPosition = prevToStartPos;
        toRT.anchoredPosition = toStartPos;
        nextToRT.anchoredPosition = nextToStartPos;

        // 애니메이션 시작 전 알파값 설정
        // 기존 3개 패널
        prevFromCG.alpha = sidePanelAlpha;
        fromCG.alpha = 1f;
        nextFromCG.alpha = sidePanelAlpha;

        // 새롭게 나타날 3개 패널 (transition 시작 시점)
        prevToCG.alpha = sidePanelAlpha;
        toCG.alpha = sidePanelAlpha; // 일단 불투명하게 시작해서 Lerp로 1f까지 갈 예정
        nextToCG.alpha = sidePanelAlpha;


        // 슬라이드 애니메이션 처리
        float t = 0;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            float lerpT = t / slideDuration;

            // 기존 패널들 슬라이드
            prevFromRT.anchoredPosition = Vector2.Lerp(prevFromStartPos, prevFromEndPos, lerpT);
            fromRT.anchoredPosition = Vector2.Lerp(fromStartPos, fromEndPos, lerpT);
            nextFromRT.anchoredPosition = Vector2.Lerp(nextFromStartPos, nextFromEndPos, lerpT);

            // 새롭게 나타날 패널들 슬라이드
            prevToRT.anchoredPosition = Vector2.Lerp(prevToStartPos, prevToEndPos, lerpT);
            toRT.anchoredPosition = Vector2.Lerp(toStartPos, toEndPos, lerpT);
            nextToRT.anchoredPosition = Vector2.Lerp(nextToStartPos, nextToEndPos, lerpT);

            // 알파값 보간 (선택된 패널만 1f로)
            fromCG.alpha = Mathf.Lerp(1f, sidePanelAlpha, lerpT); // 중앙 패널은 투명해지고
            toCG.alpha = Mathf.Lerp(sidePanelAlpha, 1f, lerpT);   // 새 중앙 패널은 불투명해짐

            yield return null;
        }

        // 애니메이션 완료 후 최종 위치 및 알파값 설정
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

        // 최종 상태 업데이트: 모든 패널 비활성화 후 현재 인덱스 기준으로 3개만 활성화
        UpdatePanelVisibility(toIndex);
    }

    // 패널 가시성 및 투명도 업데이트 메서드
    void UpdatePanelVisibility(int centerIndex)
    {
        // 모든 패널 비활성화 및 초기 위치, 투명도 설정
        foreach (GameObject p in panels)
        {
            p.SetActive(false);
            RectTransform rt = p.GetComponent<RectTransform>();
            CanvasGroup cg = p.GetComponent<CanvasGroup>();
            if (rt != null) rt.anchoredPosition = Vector2.zero; // 중앙으로 초기화
            if (cg != null) cg.alpha = 1f; // 불투명하게 초기화
        }

        // 현재 선택된 패널 (가운데)
        panels[centerIndex].SetActive(true);
        panels[centerIndex].GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        panels[centerIndex].GetComponent<CanvasGroup>().alpha = 1f;

        // 왼쪽 패널
        int prevIndex = (centerIndex - 1 + panels.Length) % panels.Length;
        panels[prevIndex].SetActive(true);
        panels[prevIndex].GetComponent<RectTransform>().anchoredPosition = new Vector2(-sidePanelOffset, 0);
        panels[prevIndex].GetComponent<CanvasGroup>().alpha = sidePanelAlpha;

        // 오른쪽 패널
        int nextIndex = (centerIndex + 1) % panels.Length;
        panels[nextIndex].SetActive(true);
        panels[nextIndex].GetComponent<RectTransform>().anchoredPosition = new Vector2(sidePanelOffset, 0);
        panels[nextIndex].GetComponent<CanvasGroup>().alpha = sidePanelAlpha;

        Debug.Log($"[Panels] Current: {centerIndex}, Prev: {prevIndex}, Next: {nextIndex}");
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