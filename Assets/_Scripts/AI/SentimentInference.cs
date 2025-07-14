using System;
using System.Collections.Generic;
using Unity.Sentis;
using UnityEngine;

public class SentimentInference : IDisposable
{
    private WordPieceTokenizer tokenizer;
    private Model saModel;
    private Worker saWorker;

    public SentimentInference(Model saModel, TextAsset vocabAsset)
    {
        InitializeTokenizer(vocabAsset);
        InitializeSAModel(saModel);
    }

    public int Run(string sentence)
    {
        Dictionary<string, List<int>> token = tokenizer.Encode(sentence, 128, true, true);

        using Tensor<int> inputIdsTensor = new Tensor<int>(new TensorShape(1, 128), token["input_ids"].ToArray());
        using Tensor<int> attentionMaskTensor = new Tensor<int>(new TensorShape(1, 128), token["attention_mask"].ToArray());

        saWorker.SetInput(0, inputIdsTensor);
        saWorker.SetInput(1, attentionMaskTensor);

        saWorker.Schedule();

        int result = (saWorker.PeekOutput()).ReleaseTensorData().Download<int>(saWorker.PeekOutput().shape[0]).ToArray()[0];

        return result;
    }

    private void InitializeTokenizer(TextAsset vocabAsset)
    {
        tokenizer = new WordPieceTokenizer(vocabAsset.text, doLowerCase: false, stripAccents: false, cleanText: true);
    }

    private void InitializeSAModel(Model saModel)
    {
        this.saModel = saModel;
        saWorker = CreateSAModel();
    }

    private Worker CreateSAModel()
    {
        FunctionalGraph graph = new FunctionalGraph();
        FunctionalTensor[] inputs = graph.AddInputs(saModel);
        FunctionalTensor[] outputs = Functional.Forward(saModel, inputs);

        FunctionalTensor output = Functional.ArgMax(outputs[0], -1);

        Model resultModel = graph.Compile(output);

        return new Worker(resultModel, BackendType.CPU);
    }

    public void Dispose()
    {
        saWorker?.Dispose();
    }
}