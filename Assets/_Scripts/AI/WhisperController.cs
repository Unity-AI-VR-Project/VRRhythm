using UnityEngine;
using Whisper.Utils;
using Whisper;

[RequireComponent(typeof(WhisperManager))]
public class WhisperController : MonoBehaviour
{
    public WhisperManager whisper;
    public MicrophoneRecord record;
    public WhisperStream stream;

    private void Awake()
    {
        if (whisper == null) GetComponent<WhisperManager>();
        InitializeWhisper();
    }

    private async void InitializeWhisper()
    {
        stream = await whisper.CreateStream(record);
        stream.OnResultUpdated += OnResult;
        stream.OnSegmentUpdated += OnSegmentUpdated;
        stream.OnSegmentFinished += OnSegmentFinished;
        stream.OnStreamFinished += OnFinished;

        record.OnRecordStop += OnRecordStop;
    }

    private void OnRecordStop(AudioChunk recordedAudio)
    {

    }

    private void OnResult(string result)
    {

    }

    private void OnSegmentUpdated(WhisperResult segment)
    {

    }

    private void OnSegmentFinished(WhisperResult segment)
    {

    }

    private void OnFinished(string finalResult)
    {

    }

    private void OnDestroy()
    {
        if (stream != null)
        {
            stream.OnResultUpdated -= OnResult;
            stream.OnSegmentUpdated -= OnSegmentUpdated;
            stream.OnSegmentFinished -= OnSegmentFinished;
            stream.OnStreamFinished -= OnFinished;
        }
    }
}
