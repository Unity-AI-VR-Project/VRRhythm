using System;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;

public class SAController : MonoBehaviour
{
    private WordPieceTokenizer tokenizer = null;
    bool isInitialized = false;
    [SerializeField]
    private ModelAsset saModelAsset;
    private Model saModel;
    private Worker saWorker;

    public string sentence;
    public int result;

    private void Awake()
    {
        if (!isInitialized)
        {
            Initialize();
        }
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Test(sentence);    
        }
    }

    private void Initialize()
    {
        try
        {
            InitializeTokenizer();
            InitializeSAModel();
            isInitialized = true;
        }
        catch (Exception e)
        {
            Debug.LogError(e.Message);
            Debug.LogError(e.StackTrace);
        }
    }

    private void InitializeTokenizer()
    {
        if (tokenizer == null)
        {
            tokenizer = new WordPieceTokenizer("vocab.txt", doLowerCase: false, stripAccents: false, cleanText: true);
        }
        Debug.Log($"{tokenizer.GetVocabSize()}개의 단어 어휘집을 불러왔습니다.");
    }

    private void InitializeSAModel()
    {
        saModel = ModelLoader.Load(saModelAsset);
    }

    public void Test(string testSentence)
    {
        try
        {
            Dictionary<string,List<int>> token = tokenizer.Encode(testSentence,128,true,true);

            Tensor<int> inputIdsTensor = new Tensor<int>(new TensorShape(1, 128), token["input_ids"].ToArray());
            Tensor<int> attentionMaskTensor = new Tensor<int>(new TensorShape(1, 128), token["attention_mask"].ToArray());

            saWorker = CreateSAModel();
            saWorker.SetInput(0, inputIdsTensor);
            saWorker.SetInput(1, attentionMaskTensor);

            saWorker.Schedule();

            result = (saWorker.PeekOutput()).ReleaseTensorData().Download<int>(saWorker.PeekOutput().shape[0]).ToArray()[0];

            inputIdsTensor?.Dispose();
            attentionMaskTensor?.Dispose();
            saWorker?.Dispose();
        }
        catch (Exception e)
        {
            Debug.LogError($"Tensor 생성 중 오류 발생: {e.Message}");
            Debug.LogError($"{e.StackTrace}");
            return;
        }
    }

    private Worker CreateSAModel()
    {
        FunctionalGraph graph = new FunctionalGraph();
        FunctionalTensor[] inputs = graph.AddInputs(saModel);
        FunctionalTensor[] outputs = Functional.Forward(saModel, inputs);

        FunctionalTensor output = Functional.ArgMax(outputs[0], -1);

        Model resultModel = graph.Compile(output);

        Debug.Log($"Sentiment model compiled successfully.");

        return new Worker(resultModel, BackendType.CPU);
    }

    private void OnApplicationQuit()
    {
        saWorker?.Dispose();
    }
}
