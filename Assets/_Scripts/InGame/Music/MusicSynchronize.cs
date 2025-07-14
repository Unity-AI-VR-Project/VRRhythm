using UnityEngine;
using Define;

[RequireComponent(typeof(AudioSource))]
public class MusicSynchronizer : MonoBehaviour
{
    [Header("음악 설정")]
    // 이 bpm은 SpawnerSelector에서 가져올 것이므로 Inspector에서 수동 설정하는 대신 Tooltip으로 안내합니다.
    [Tooltip("SpawnerSelector에서 로드된 음악의 BPM.")]
    public float bpm; 

    public int delayBeat = 4;
    [Tooltip("음악 재생 시작 시점의 추가 오프셋 (초 단위).")]
    public float offset = 0f;
    private AudioSource audioSource;

    [Header("참조")]
    [Tooltip("음악 BPM 정보를 가져오기 위한 SpawnerSelector 컴포넌트.")]
    public SpawnerSelector spawnerSelector; // SpawnerSelector 참조를 추가합니다.

    // 현재 음악 DSP 시간
    public float currentTimeDSP {  get; private set; }
    public float currentTime { get; private set; }

    // 노트 생성을 위한 지연된 DSP 시간
    public float elapsedTime { get; private set; }

    // 음악이 재생될 것으로 예약된 DSP 시간
    private double _musicScheduledStartTime;
    private bool _isMusicPlaying = false;

    // 음악 지연을 위한 변수
    private double beatInterval;
    private double delay;
    private double scheduledStartTime;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        // SpawnerSelector 컴포넌트가 할당되었는지 확인합니다.
        if (spawnerSelector == null)
        {
            spawnerSelector = FindAnyObjectByType<SpawnerSelector>(); 
            if (spawnerSelector == null)
            {
                Debug.LogError("MusicSynchronizer: SpawnerSelector 컴포넌트가 할당되지 않았습니다. 씬에 SpawnerSelector를 추가하고 할당하거나, 수동으로 할당해주세요.", this);
                enabled = false; 
                return;
            }
        }
    }

    void Start()
    {
        // SpawnerSelector에서 BPM 값을 가져와 동기화합니다.
        bpm = spawnerSelector.GetTempo();

        // 1박 길이 계산
        beatInterval = 60.0 / bpm;

        // 노트가 먼저 움직이기 위한 지연 시간 (delayBeat는 NotePreSpawnBeats와 유사한 역할)
        delay = beatInterval * delayBeat;
        


        // 이벤트 구독
        InGameManager.Instance.OnStarted += OnStarted;

       
    }

    private void Update()
    {
        UpdateMusicTime();
        //Debug.Log($"{currentTime}, {elapsedTime}, {currentTime - elapsedTime}");
    }

    private void OnStarted(InGameState State)
    {
        audioSource.PlayScheduled(scheduledStartTime);

        _isMusicPlaying = true;

        Debug.Log($"음악은 {delay + offset:F2}초 후에 재생됩니다. (DSP Time 기준: {scheduledStartTime:F2})");
    }


    public void UpdateMusicTime()
    {
        if (!_isMusicPlaying)
        {
            // 현재 음악 진행 DSP
            currentTimeDSP = (float)AudioSettings.dspTime;

            // 현재 dsp 시간 + 계산된 지연 시간 + offset에 맞춰 음악 재생 예약
            // offset을 여기에 추가하여 음악 재생 시작 시점을 미세하게 조정합니다.
            scheduledStartTime = (float)AudioSettings.dspTime + delay + offset;
        }
        

        // 음악이 재생 중이고 AudioSource가 유효할 때만 경과 시간을 업데이트합니다.
        if (_isMusicPlaying && audioSource != null && audioSource.isPlaying)
        {

            currentTime = (float)(AudioSettings.dspTime - currentTimeDSP);

            // 현재 음악 진행 DSP - 노트 속도 = 판정 시간
            elapsedTime = (float)(AudioSettings.dspTime - scheduledStartTime);

            //Debug.Log("현재 음악 진행 시간: " + currentTime + "판정 시간: " + elapsedTime + "초");

        }
        else if (_isMusicPlaying && audioSource != null && !audioSource.isPlaying && elapsedTime > 0)
        {
            // 음악이 재생을 멈췄을 때 (예: 노래 끝)
            _isMusicPlaying = false;
        }
        
    }

}