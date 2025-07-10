using UnityEngine;

public class UISilideIn : MonoBehaviour
{
    public RectTransform targetUI;
    public float slideDuration = 1f;
    public Vector2 startOffset = new Vector2(0, -200f); // 아래에서 올라오게
    private Vector2 originalPos;

    void OnEnable()
    {
        originalPos = targetUI.anchoredPosition;
        targetUI.anchoredPosition = originalPos + startOffset;
        StartCoroutine(SlideIn());
    }

    System.Collections.IEnumerator SlideIn()
    {
        float elapsed = 0f;

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / slideDuration);
            targetUI.anchoredPosition = Vector2.Lerp(originalPos + startOffset, originalPos, t);
            yield return null;
        }

        targetUI.anchoredPosition = originalPos;
    }
}
