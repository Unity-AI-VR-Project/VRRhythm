using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicSynchronizer : MonoBehaviour
{
    [Header("음악 설정")]
    // 이 bpm은 SpawnerSelector에서 가져올 것이므로 Inspector에서 수동 설정하는 대신 Tooltip으로 안내합니다.
    [Tooltip("SpawnerSelector에서 로드된 음악의 BPM.")]
    public float bpm; 

    public int delayBeat = 4;
    [Tooltip("음악 재생 시작 시점의 추가 오프셋 (초 단위).")]
    public float offset = 1.16f;
    private AudioSource audioSource;

    [Header("참조")]
    [Tooltip("음악 BPM 정보를 가져오기 위한 SpawnerSelector 컴포넌트.")]
    public SpawnerSelector spawnerSelector; // SpawnerSelector 참조를 추가합니다.
    [Tooltip("음악 경과 시간을 추적하는 MusicTimeChacker 컴포넌트.")]
    public MusicTimeChecker timeChecker; // MusicTimeChacker 참조를 추가합니다.

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

        // MusicTimeChacker 컴포넌트가 할당되었는지 확인합니다.
        if (timeChecker == null)
        {
            timeChecker = FindAnyObjectByType<MusicTimeChecker>();
            if (timeChecker == null)
            {
                Debug.LogError("MusicSynchronizer: MusicTimeChacker 컴포넌트가 할당되지 않았습니다. 씬에 MusicTimeChacker를 추가하고 할당하거나, 수동으로 할당해주세요.", this);
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
        double beatInterval = 60.0 / bpm;

        // 노트가 먼저 움직이기 위한 지연 시간 (delayBeat는 NotePreSpawnBeats와 유사한 역할)
        double delay = beatInterval * delayBeat;

        // 현재 dsp 시간 + 계산된 지연 시간 + offset에 맞춰 음악 재생 예약
        // offset을 여기에 추가하여 음악 재생 시작 시점을 미세하게 조정합니다.
        double scheduledStartTime = AudioSettings.dspTime + delay + offset;

        audioSource.PlayScheduled(scheduledStartTime);

        // MusicTimeChacker에 음악 시작 예약 시간을 전달하여 시간 추적의 기준점으로 삼습니다.
        if (timeChecker != null)
        {
            timeChecker.InitializeMusicStartTime(scheduledStartTime); // MusicTimeChacker에 초기화 메서드 필요
        }
        
        Debug.Log($"음악은 {delay + offset:F2}초 후에 재생됩니다. (DSP Time 기준: {scheduledStartTime:F2})");
    }
}