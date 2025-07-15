using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Sentis;
using UnityEngine;
using System;
using System.Diagnostics;
using Define;
#pragma warning disable CS1998

public class RunWhisper : MonoBehaviour
{
    public ModelAsset audioDecoder1, audioDecoder2;
    public ModelAsset audioEncoder;
    public ModelAsset logMelSpectro;
    public AudioClip testAudioClip;
    public TextAsset vocabAsset;

    private AudioProcessor audioProcessor;
    private WhisperInference whisperInference;

    private bool isProcessingAudio = false;

    public bool IsProcessingAudio => isProcessingAudio;

    void Awake()
    {
        audioProcessor = GetComponent<AudioProcessor>();
        if (audioProcessor == null)
        {
            audioProcessor = gameObject.AddComponent<AudioProcessor>();
        }

        audioProcessor.maxRecordingSeconds = 4;
        audioProcessor.micSampleRate = 16000;
        audioProcessor.maxSamples = 30 * 16000;

        audioDecoder1 = Resources.Load<ModelAsset>("Whisper/decoder_model");
        audioDecoder2 = Resources.Load<ModelAsset>("Whisper/decoder_with_past_model");
        audioEncoder = Resources.Load<ModelAsset>("Whisper/encoder_model");
        logMelSpectro = Resources.Load<ModelAsset>("Whisper/logmel_spectrogram");
        vocabAsset = Resources.Load<TextAsset>("Whisper/vocab");

        if (audioDecoder1 == null) UnityEngine.Debug.LogError("Failed to load audioDecoder1 from Resources/Whisper.");
        if (audioDecoder2 == null) UnityEngine.Debug.LogError("Failed to load audioDecoder2 from Resources/Whisper.");
        if (audioEncoder == null) UnityEngine.Debug.LogError("Failed to load audioEncoder from Resources/Whisper.");
        if (logMelSpectro == null) UnityEngine.Debug.LogError("Failed to load logMelSpectro from Resources/Whisper.");
        if (vocabAsset == null) UnityEngine.Debug.LogError("Failed to load vocab from Resources/Whisper.");

        whisperInference = new WhisperInference(
            ModelLoader.Load(audioDecoder1),
            ModelLoader.Load(audioDecoder2),
            ModelLoader.Load(audioEncoder),
            ModelLoader.Load(logMelSpectro),
            vocabAsset
        );
    }

    public async void Start()
#pragma warning restore CS1998
    {
        if (Microphone.devices.Length == 0)
        {
            UnityEngine.Debug.LogError("마이크 장치를 찾을 수 없습니다. 마이크가 연결되어 있는지 확인해주세요.");
        }

        UnityEngine.Debug.Log("스페이스바를 눌러 녹음을 시작하세요.");
        UnityEngine.Debug.Log("P 키를 눌러 모델 성능 테스트를 시작하세요.");
        TestModelPerformancePublic();
    }

    void Update()
    {

    }

    public void StartRecordingPublic()
    {
        if (!audioProcessor.IsRecording && !isProcessingAudio)
        {
            StartRecording();
        }
    }

    public void StopRecordingPublic()
    {
        if (audioProcessor.IsRecording)
        {
            StopRecording();
        }
    }

    public void TestModelPerformancePublic()
    {
        if (!audioProcessor.IsRecording && !isProcessingAudio)
        {
            if (testAudioClip == null)
            {
                UnityEngine.Debug.LogError("성능 테스트를 위한 'Test Audio Clip'이 설정되지 않았습니다.");
                return;
            }
            TestModelPerformance();
        }
    }

    void StartRecording()
    {
        UnityEngine.Debug.Log("녹음 시작...");
        audioProcessor.StartRecording();
    }

    async void StopRecording()
    {
        if (!audioProcessor.IsRecording) return;

        isProcessingAudio = true;
        UnityEngine.Debug.Log("녹음 종료. 처리 중...");

        AudioClip recordedClip = audioProcessor.StopRecording();
        string finalOutputString = await ProcessAudioClip(recordedClip);

        isProcessingAudio = false;
        int sentiment = GameManager.Instance.aiManager.saController.Run(finalOutputString);
        UnityEngine.Debug.Log("최종 변환 결과: " + finalOutputString +" "+ sentiment);
        GameManager.Instance.chatManager.chatUI.AddChat(finalOutputString, sentiment);
    }

    async Awaitable<string> ProcessAudioClip(AudioClip clipToProcess)
    {
        if (clipToProcess == null)
        {
            UnityEngine.Debug.LogError("처리할 AudioClip이 없습니다.");
            return "";
        }

        int totalSamples = clipToProcess.samples * clipToProcess.channels;
        int currentSampleOffset = 0;
        string fullTranscription = "";

        while (currentSampleOffset < totalSamples)
        {
            float[] rawChunkData = new float[audioProcessor.maxSamples];
            clipToProcess.GetData(rawChunkData, currentSampleOffset);

            string chunkTranscription = await whisperInference.TranscribeAudioChunk(rawChunkData);
            fullTranscription += chunkTranscription;

            UnityEngine.Debug.Log($"청크 처리 완료. 현재까지의 결과: {fullTranscription}");

            currentSampleOffset += audioProcessor.maxSamples;
        }
        return fullTranscription;
    }

    async void TestModelPerformance()
    {
        if (testAudioClip == null)
        {
            UnityEngine.Debug.LogError("Test audio clip not assigned for performance testing!");
            return;
        }

        isProcessingAudio = true;
        UnityEngine.Debug.Log("모델 성능 테스트 시작...");

        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();

        float[] rawAudioData = new float[testAudioClip.samples * testAudioClip.channels];
        testAudioClip.GetData(rawAudioData, 0);

        int totalSamples = rawAudioData.Length;
        int currentSampleOffset = 0;
        string testOutputString = "";

        while (currentSampleOffset < totalSamples)
        {
            float[] chunkData = new float[audioProcessor.maxSamples];
            Array.Copy(rawAudioData, currentSampleOffset, chunkData, 0, Mathf.Min(audioProcessor.maxSamples, totalSamples - currentSampleOffset));

            string chunkTranscription = await whisperInference.TranscribeAudioChunk(chunkData);
            testOutputString += chunkTranscription;

            currentSampleOffset += audioProcessor.maxSamples;
        }

        stopwatch.Stop();
        UnityEngine.Debug.Log($"모델 성능 테스트 완료!");
        UnityEngine.Debug.Log($"총 처리 시간: {stopwatch.ElapsedMilliseconds} ms");
        UnityEngine.Debug.Log($"변환된 텍스트 (테스트): {testOutputString}");
        
        isProcessingAudio = false;
        UnityEngine.Debug.Log("스페이스바를 눌러 다시 녹음을 시작하세요.");
        UnityEngine.Debug.Log("P 키를 눌러 모델 성능 테스트를 시작하세요.");
    }

    private void OnDestroy()
    {
        whisperInference?.Dispose();
    }

    public AudioProcessor GetAudioProcessor()
    {
        return audioProcessor;
    }
}