using System;
using UnityEngine;
using Unity.Sentis;

public class SAController : MonoBehaviour
{
    [SerializeField]
    private ModelAsset saModelAsset;
    [SerializeField]
    private TextAsset vocabAsset;

    private SentimentInference sentimentInference;

    private bool isInitialized = false;

    private void Awake()
    {
        if (!isInitialized)
        {
            Initialize();
        }
    }

    private void Initialize()
    {
        try
        {
            if (saModelAsset == null)
            {
                Debug.LogError("SA Model Asset이 할당되지 않았습니다. Resources/SA/ 경로에 있는지 확인해주세요.");
                return;
            }
            if (vocabAsset == null)
            {
                Debug.LogError("Vocab Asset이 할당되지 않았습니다. Resources/SA/ 경로에 있는지 확인해주세요.");
                return;
            }

            sentimentInference = new SentimentInference(ModelLoader.Load(saModelAsset), vocabAsset);
            Debug.Log("Sentiment Inference 모델이 성공적으로 초기화되었습니다.");
            isInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"SentimentInference 초기화 중 오류 발생: {e.Message}");
            Debug.LogError(e.StackTrace);
        }
    }

    public int Run(string sentence)
    {
        if (!isInitialized)
        {
            Debug.LogError("SentimentInference가 초기화되지 않았습니다.");
            return -1;
        }

        try
        {
            return sentimentInference.Run(sentence);
        }
        catch (Exception e)
        {
            Debug.LogError($"감정 분석 실행 중 오류 발생: {e.Message}");
            Debug.LogError($"{e.StackTrace}");
            return -1;
        }
    }

    private void OnDestroy()
    {
        sentimentInference?.Dispose();
    }
}