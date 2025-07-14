using UnityEngine;
using Whisper.Utils;
using Whisper;
using System;
using Define;

public delegate void OnRecordDelegate(bool isRecord);

[RequireComponent(typeof(WhisperManager), typeof(MicrophoneRecord))]
public class WhisperController : MonoBehaviour
{
    public WhisperManager whisper;
    public MicrophoneRecord record;
    public string whisperModelLevel;
    private string outputText;
    public string OutputText
    {
        get { return outputText; }
        set
        {
            outputText = value;
            // Sentiment Analysis
            int result = GameManager.Instance.aiManager.saController.Run(outputText);
            if (result != -1)
            {
                Debug.Log($"SA Result :{result}");
                ChatObjectData COD = new ChatObjectData(DateTime.Now, OutputText, result);
            }
        }
    }

    private void Awake()
    {
        InitializeWhisper();
    }

    private void InitializeWhisper()
    {
        if (whisper == null) GetComponent<WhisperManager>();
        if (record == null) GetComponent<MicrophoneRecord>();
        whisper.ModelPath = $"Whisper/ggml-{whisperModelLevel}.bin";
        record.OnRecordStop += OnRecordStop;
    }

    private async void OnRecordStop(AudioChunk recordedAudio)
    {
        var res = await whisper.GetTextAsync(recordedAudio.Data, recordedAudio.Frequency, recordedAudio.Channels);
        if (res == null)
            return;

        OutputText = res.Result;

        Debug.Log($"STT Result : {outputText}");
    }

    private void OnDestroy()
    {
        if (record != null)
        {
            record.OnRecordStop -= OnRecordStop;
        }
    }
}
