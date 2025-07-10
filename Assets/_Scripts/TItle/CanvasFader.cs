using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CanvasFader : MonoBehaviour
{
    [Header("페이드 아웃 설정")]
    [SerializeField] private float moveSpeed = 50f; // 캔버스가 위로 올라가는 속도
    [SerializeField] private float fadeDuration = 2f; // 캔버스가 사라지는 데 걸리는 시간

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private bool isFadingOut = false;

    public delegate void OnFadeComplete();
    public static event OnFadeComplete onFadeComplete;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            // CanvasGroup이 없으면 추가합니다. 투명도 조절에 필요합니다.
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        rectTransform = GetComponent<RectTransform>();
    }

    /// <summary>
    /// 캔버스 페이드 아웃을 시작합니다.
    /// </summary>
    public void StartFadeOut()
    {
        if (!isFadingOut)
        {
            isFadingOut = true;
            StartCoroutine(FadeOutCoroutine());
        }
    }

    private IEnumerator FadeOutCoroutine()
    {
        float timer = 0f;
        Vector3 initialPosition = rectTransform.anchoredPosition; // 캔버스의 현재 위치

        while (timer < fadeDuration)
        {
            // 시간 경과에 따라 투명도 조절
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);

            // 캔버스를 위로 이동
            rectTransform.anchoredPosition = new Vector3(
                initialPosition.x,
                initialPosition.y + (moveSpeed * timer),
                initialPosition.z
            );

            timer += Time.deltaTime;
            yield return null;
        }

        // 완전히 사라진 후 투명도를 0으로 고정
        canvasGroup.alpha = 0f;
        isFadingOut = false;

        // 페이드 아웃이 완료되었음을 알립니다.
        onFadeComplete?.Invoke();
    }

    //씬 전환 테스트를 위한 임시 UI 버튼 등에서 사용할 수 있습니다.
     void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) // 테스트를 위해 스페이스바 누르면 페이드 아웃 시작
        {
            StartFadeOut();
        }
    }
}