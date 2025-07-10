using System;
using System.Collections;
using UnityEngine;

[Serializable]
public struct EnergyPoint
{
    public float energyMin;
    public float energyMax;
}

[RequireComponent(typeof(AudioSource))]
public class AudioFrequencyBandDetector : MonoBehaviour
{
    public AudioSource audioSource;

    public int spectrumSize = 1024; // 스펙트럼 데이터 배열 크기 (2의 거듭제곱)
    public FFTWindow fftWindow = FFTWindow.Blackman;
    

    // 감지된 주파수 대역 에너지 (0.0 ~ 1.0 사이 값)
    [Range(0f, 1f)]
    public float lowBandEnergy;
    [Range(0f, 1f)]
    public float midBandEnergy;
    [Range(0f, 1f)]
    public float highBandEnergy;

    // 각 주파수 대역의 기준 주파수 (Hz)
    [Range(0f, 22050f)] public float lowFrequencyThresholdEnd = 250f;  // 저음의 상한선 (0Hz ~ 250Hz)
    [Range(0f, 22050f)] public float midFrequencyThresholdStart = 500f;
    [Range(0f, 22050f)] public float midFrequencyThresholdEnd = 2000f; // 중음의 상한선 (250Hz ~ 2000Hz)
    [Range(0f, 22050f)] public float highFrequencyThresholdStart = 2000f; // 고음은 2000Hz ~ Nyquist (~22050Hz)
    [Range(0f, 22050f)] public float highFrequencyThresholdEnd = 22050f;     // 고음은 2000Hz ~ Nyquist (~22050Hz)

    [Header("BeatPer High/Low")] 
    public EnergyPoint lowPoint;
    public EnergyPoint midPoint;
    public EnergyPoint highPoint;
    public static float bpm = 102f;
    public static float beatPer = 1;

    private float[] spectrumData;
    private float binBandwidth; // 각 스펙트럼 bin이 담당하는 주파수 폭

    void Start()
    {
        if (audioSource == null)
        {
            audioSource = transform.GetComponent<AudioSource>();
        }

        spectrumData = new float[spectrumSize];

        // 각 bin의 주파수 폭 = (샘플레이트 / 2) / bin 개수
        binBandwidth = (AudioSettings.outputSampleRate / 2f) / spectrumSize;
        
        lowPoint.energyMin = 1f;
        lowPoint.energyMax = 0f;
        midPoint.energyMin = 1f;
        midPoint.energyMax = 0f;
        highPoint.energyMin = 1f;
        highPoint.energyMax = 0f;

        Debug.Log($"Bin Bandwidth: {binBandwidth:F2} Hz per bin");
        Debug.Log($"Nyquist Frequency (Max Analyzed Freq): {AudioSettings.outputSampleRate / 2f} Hz");
        Debug.Log($"Clip Frequency : {(audioSource.clip != null ? audioSource.clip.frequency : 0)} Hz");
    }

    private void FixedUpdate()
    {
        // 실시간 스펙트럼 데이터 가져오기
        audioSource.GetSpectrumData(spectrumData, 0, fftWindow);
        
        

        // 주파수 대역별 에너지 계산
        CalculateFrequencyBandEnergy();
        
        // energy Max, Min 갱신
        lowPoint = CheckBandEnergy(lowPoint, lowBandEnergy);
        midPoint = CheckBandEnergy(midPoint, midBandEnergy);
        highPoint = CheckBandEnergy(highPoint, highBandEnergy);
        
    }
    

    private EnergyPoint CheckBandEnergy(EnergyPoint energyPoint, float bandEnergy)
    {
        if (energyPoint.energyMin >= bandEnergy)
        {
            energyPoint.energyMin = bandEnergy;
        }

        if (energyPoint.energyMax <= bandEnergy)
        {
            energyPoint.energyMax = bandEnergy;
        }
        return energyPoint;
    }

    void CalculateFrequencyBandEnergy()
    {
        int lowBandEndIndex = Mathf.FloorToInt(lowFrequencyThresholdEnd / binBandwidth);
        int midBandStartIndex = Mathf.FloorToInt(midFrequencyThresholdStart / binBandwidth); 
        int midBandEndIndex = Mathf.FloorToInt(midFrequencyThresholdEnd / binBandwidth);
        int highBandStartIndex = Mathf.FloorToInt(highFrequencyThresholdStart / binBandwidth);
        int highBandEndIndex = Mathf.FloorToInt(highFrequencyThresholdEnd / binBandwidth);

        lowBandEndIndex = Mathf.Min(lowBandEndIndex, spectrumSize);
        midBandStartIndex = Mathf.Min(midBandStartIndex, spectrumSize);
        midBandEndIndex = Mathf.Min(midBandEndIndex, spectrumSize);
        highBandStartIndex = Mathf.Min(highBandStartIndex, spectrumSize);
        highBandEndIndex = Mathf.Min(highBandEndIndex, spectrumSize);

        // === 저음 대역 RMS 계산 ===
        float lowSqSum = 0f;
        int lowCount = 0;
        for (int i = 0; i < lowBandEndIndex; i++)
        {
            lowSqSum += spectrumData[i] * spectrumData[i]; // 💡 제곱합
            lowCount++;
        }
        lowBandEnergy = (lowCount > 0) ? Mathf.Sqrt(lowSqSum / lowCount) : 0f; // 💡 RMS 적용
        lowBandEnergy = NormalizeLog(lowBandEnergy);

        // === 중음 대역 RMS 계산 ===
        float midSqSum = 0f;
        int midCount = 0;
        for (int i = midBandStartIndex; i < midBandEndIndex; i++)
        {
            midSqSum += spectrumData[i] * spectrumData[i]; // 💡 제곱합
            midCount++;
        }
        midBandEnergy = (midCount > 0) ? Mathf.Sqrt(midSqSum / midCount) : 0f; // 💡 RMS 적용
        midBandEnergy = NormalizeLog(midBandEnergy);

        // === 고음 대역 RMS 계산 ===
        float highSqSum = 0f;
        int highCount = 0;
        for (int i = highBandStartIndex; i < highBandEndIndex; i++)
        {
            highSqSum += spectrumData[i] * spectrumData[i]; // 💡 제곱합
            highCount++;
        }
        highBandEnergy = (highCount > 0) ? Mathf.Sqrt(highSqSum / highCount) : 0f; // 💡 RMS 적용
        highBandEnergy = NormalizeLog(highBandEnergy);
    }

    float NormalizeLog(float value)
    {
        // value는 0 ~ 매우 작은 값 (예: 0.00001)
        float db = Mathf.Log10(value + 1e-6f); // -6 ~ 0 범위
        return Mathf.Clamp01((db + 6f) / 6f);  // 0 ~ 1로 정규화
        
        
    }
}
