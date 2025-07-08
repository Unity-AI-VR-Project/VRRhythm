using System.Collections;
using UnityEngine;

public enum HzChack
{
    Low,
    Medium,
    High
}

public class NoteSpawner : MonoBehaviour
{
    public AudioFrequencyBandDetector detector;
    public GameObject notePrefab;

    [Header("리듬 설정")]
    public float bpm = 75f;
    public float energyThreshold = 0.5f;
    public float beatsPerNote = 1;

    [Header("정밀 싱크 보정")]
    public float musicOffset = 0.3f; // 음악의 실제 비트 시작 지점 (초)
    
    [Header("감지할 Hz")]
    public HzChack hzChack;
    
    private float beatTime;              // 한 비트 시간
    private float nextCheckTime = 0f;    // 다음 비트 체크 시각
    private float previousOffset;  // 이전 offset 값을 저장
    private bool starting = false; // 음악 시작 검사
    private float energyMin;
    private float energyMax;
    

    void Start()
    {
        nextCheckTime = Time.time + musicOffset;
        previousOffset = musicOffset;  // 초기값 저장
        
    }

    void FixedUpdate()
    {
        beatTime = 60f / bpm * beatsPerNote;
        if (!detector)
        {
            Debug.LogWarning("AudioFrequencyBandDetector가 없습니다.");
            return;
        }

        float currentTime = Time.time;

        // offset이 수정되었을 때 nextCheckTime 보정
        if (Mathf.Abs(previousOffset - musicOffset) > 0.0001f)
        {
            float offsetDelta = musicOffset - previousOffset;
            nextCheckTime += offsetDelta;  // 시간차 반영
            previousOffset = musicOffset;
        }
        float hzEnergy = 0f;

        switch (hzChack)
        {
            case HzChack.Low:
                hzEnergy = detector.lowBandEnergy;
                break;
            case HzChack.Medium:
                hzEnergy = detector.midBandEnergy;
                break;
            case HzChack.High:
                hzEnergy = detector.highBandEnergy;
                break;
        }
        

        
        // 비트 간격마다 체크
        if (currentTime >= nextCheckTime)
        {
            if ((energyMin + energyMax) / 2 < hzEnergy)
            {
                SpawnNote();
            }
            switch (hzChack)
            {
                
                case HzChack.Low:
                    detector.lowPoint.energyMin = 1f;
                    detector.lowPoint.energyMax = 0f;
                    Debug.Log(detector.lowPoint.energyMin);
                    Debug.Log(detector.lowPoint.energyMax);
                    break;
                case HzChack.Medium:
                    detector.midPoint.energyMin= 1f;
                    detector.midPoint.energyMax= 0f;
                    break;
                case HzChack.High:
                    detector.highPoint.energyMin= 1f;
                    detector.highPoint.energyMax= 0f;
                    break;
            }
            nextCheckTime += beatTime;
        }
        
        
        switch (hzChack)
        {
            case HzChack.Low:
                energyMin = detector.lowPoint.energyMin;
                energyMax = detector.lowPoint.energyMax;
                break;
            case HzChack.Medium:
                energyMin = detector.midPoint.energyMin;
                energyMax = detector.midPoint.energyMax;
                break;
            case HzChack.High:
                energyMin = detector.highPoint.energyMin;
                energyMax = detector.highPoint.energyMax;
                break;
        }
        
    }

    void SpawnNote()
    {
        Instantiate(notePrefab, transform.position, transform.rotation);
    }
}