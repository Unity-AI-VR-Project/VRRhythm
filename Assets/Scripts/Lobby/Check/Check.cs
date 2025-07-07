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

            // 시작 시 모든 패널 비활성화
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

        foreach (GameObject p in panels) p.SetActive(false);

        panels[prevFromIndex].SetActive(true);
        panels[fromIndex].SetActive(true);
        panels[nextFromIndex].SetActive(true);

        panels[prevToIndex].SetActive(true);
        panels[toIndex].SetActive(true);
        panels[nextToIndex].SetActive(true);

        prevFromRT.anchoredPosition = prevFromStartPos;
        fromRT.anchoredPosition = fromStartPos;
        nextFromRT.anchoredPosition = nextFromStartPos;

        prevToRT.anchoredPosition = prevToStartPos;
        toRT.anchoredPosition = toStartPos;
        nextToRT.anchoredPosition = nextToStartPos;

        prevFromCG.alpha = sidePanelAlpha;
        fromCG.alpha = 1f;
        nextFromCG.alpha = sidePanelAlpha;

        prevToCG.alpha = sidePanelAlpha;
        toCG.alpha = sidePanelAlpha;
        nextToCG.alpha = sidePanelAlpha;

        float t = 0;
        while (t < slideDuration)
        {
            t += Time.deltaTime;
            float lerpT = t / slideDuration;

            prevFromRT.anchoredPosition = Vector2.Lerp(prevFromStartPos, prevFromEndPos, lerpT);
            fromRT.anchoredPosition = Vector2.Lerp(fromStartPos, fromEndPos, lerpT);
            nextFromRT.anchoredPosition = Vector2.Lerp(nextFromStartPos, nextFromEndPos, lerpT);

            prevToRT.anchoredPosition = Vector2.Lerp(prevToStartPos, prevToEndPos, lerpT);
            toRT.anchoredPosition = Vector2.Lerp(toStartPos, toEndPos, lerpT);
            nextToRT.anchoredPosition = Vector2.Lerp(nextToStartPos, nextToEndPos, lerpT);

            fromCG.alpha = Mathf.Lerp(1f, sidePanelAlpha, lerpT);
            toCG.alpha = Mathf.Lerp(sidePanelAlpha, 1f, lerpT);

            yield return null;
        }

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

        UpdatePanelVisibility(toIndex);
    }

    void UpdatePanelVisibility(int centerIndex)
    {
        foreach (GameObject p in panels)
        {
            p.SetActive(false);
            RectTransform rt = p.GetComponent<RectTransform>();
            CanvasGroup cg = p.GetComponent<CanvasGroup>();
            if (rt != null) rt.anchoredPosition = Vector2.zero;
            if (cg != null) cg.alpha = 1f;
        }

        panels[centerIndex].SetActive(true);
        panels[centerIndex].GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        panels[centerIndex].GetComponent<CanvasGroup>().alpha = 1f;

        int prevIndex = (centerIndex - 1 + panels.Length) % panels.Length;
        panels[prevIndex].SetActive(true);
        panels[prevIndex].GetComponent<RectTransform>().anchoredPosition = new Vector2(-sidePanelOffset, 0);
        panels[prevIndex].GetComponent<CanvasGroup>().alpha = sidePanelAlpha;

        int nextIndex = (centerIndex + 1) % panels.Length;
        panels[nextIndex].SetActive(true);
        panels[nextIndex].GetComponent<RectTransform>().anchoredPosition = new Vector2(sidePanelOffset, 0);
        panels[nextIndex].GetComponent<CanvasGroup>().alpha = sidePanelAlpha;

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
