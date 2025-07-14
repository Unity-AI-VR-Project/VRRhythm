using UnityEngine;
using Define; // Define 네임스페이스가 필요합니다 (예: InGameState enum).

[RequireComponent(typeof(AudioSource))]
public class MusicSynchronizer : MonoBehaviour
{
    [Header("음악 설정")]
    // 이 bpm은 SpawnerSelector에서 가져올 것이므로 Inspector에서 수동 설정하는 대신 Tooltip으로 안내합니다.
    [Tooltip("SpawnerSelector에서 로드된 음악의 BPM.")]
    public float bpm;

    [Tooltip("음악 재생 시작 전 노트를 미리 스폰하기 위한 지연 비트 (예: 4비트).")]
    public int delayBeat = 4; // 프리셋 지연 비트

    [Tooltip("음악 재생 시작 시점의 추가 오프셋 (초 단위).")]
    public float offset = 0f; // 추가 오프셋 (초)

    private AudioSource audioSource;

    [Header("참조")]
    [Tooltip("음악 BPM 정보를 가져오기 위한 SpawnerSelector 컴포넌트.")]
    public SpawnerSelector spawnerSelector;

    // 현재 음악 DSP 시간 (노트 스폰 및 이동에 사용될 주 시간)
    // 음악이 예약된 시점 (_musicScheduledDSPTime)을 0으로 하는 경과 시간입니다.
    public float currentTimeDSP { get; private set; }

    // 실제 AudioSource의 재생 시간 (음악의 현재 재생 위치를 확인하는 용도)
    public float currentMusicTime { get; private set; }

    // 음악이 재생될 것으로 예약된 DSP 절대 시간
    private double _musicScheduledDSPTime;

    // 음악이 PlayScheduled로 예약되었는지 여부
    private bool _isMusicScheduled = false;

    // 1박의 길이 (초)
    private double _beatInterval;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

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

        // 오디오 클립이 할당되지 않았다면 경고
        if (audioSource.clip == null)
        {
            Debug.LogWarning("MusicSynchronizer: AudioSource에 재생할 음악 클립이 할당되지 않았습니다! Inspector에서 할당해주세요.", this);
        }
    }

    void Start()
    {
        // SpawnerSelector에서 BPM 값을 가져와 동기화합니다.
        // SpawnerSelector의 GetTempo() 메서드가 맵 데이터 로드 후에 올바른 BPM을 반환해야 합니다.
        bpm = spawnerSelector.GetTempo();

        if (bpm <= 0)
        {
            Debug.LogError($"MusicSynchronizer: 로드된 BPM 값이 유효하지 않습니다 ({bpm}). SpawnerSelector의 맵 데이터 로드를 확인해주세요.", this);
            enabled = false;
            return;
        }

        // 1박 길이 계산
        _beatInterval = 60.0 / bpm;

        // InGameManager의 게임 시작 이벤트 구독
        // InGameManager 인스턴스가 활성화된 후에 이 이벤트가 발생해야 합니다.
        if (InGameManager.Instance != null)
        {
            InGameManager.Instance.OnStarted += OnGameStarted;
        }
        else
        {
            Debug.LogError("MusicSynchronizer: InGameManager 인스턴스를 찾을 수 없습니다. 이벤트 구독에 실패했습니다.");
            enabled = false;
        }
    }

    private void OnDestroy()
    {
        // 씬 전환 시 메모리 누수 및 잠재적 오류 방지를 위해 이벤트 구독 해지
        if (InGameManager.Instance != null)
        {
            InGameManager.Instance.OnStarted -= OnGameStarted;
        }
    }

    // Update는 매 프레임 실행되므로, 현재 음악 진행 시간을 계속 업데이트합니다.
    private void Update()
    {
        // 음악이 스케줄링되었고 AudioSource가 유효할 때만 시간 업데이트
        if (_isMusicScheduled && audioSource != null)
        {
            // _musicScheduledDSPTime은 음악이 실제 재생을 시작할 DSP 시간입니다.
            // currentTimeDSP는 그 시점을 0으로 하는 경과 시간으로 사용됩니다.
            currentTimeDSP = (float)(AudioSettings.dspTime - _musicScheduledDSPTime);

            // 실제 AudioSource의 재생 시간
            currentMusicTime = audioSource.time;

            // Debug.Log($"Music Time: {currentMusicTime:F2}s, DSP Time for notes: {currentTimeDSP:F2}s");
        }
        // 음악 재생이 끝났을 때 _isMusicScheduled 플래그를 리셋 (필요시)
        else if (_isMusicScheduled && audioSource != null && !audioSource.isPlaying && currentMusicTime > 0)
        {
            // 음악 재생이 끝났을 때 (AudioSource가 멈췄을 때)
            _isMusicScheduled = false;
            Debug.Log("음악 재생이 종료되었습니다.");
            // 게임 종료 또는 다음 단계로 전환 로직 추가 가능
        }
    }

    // InGameManager.StartGame()이 호출될 때 실행될 메서드
    private void OnGameStarted(InGameState state)
    {
        if (state == InGameState.Playing)
        {
            // 음악 재생 시작 전까지의 총 지연 시간 계산 (비트 기반 지연 + 추가 오프셋)
            double totalDelay = _beatInterval * delayBeat + offset;

            // 음악이 실제로 재생될 DSP 절대 시간 예약
            // AudioSettings.dspTime은 이 함수가 호출되는 시점의 현재 DSP 시간입니다.
            _musicScheduledDSPTime = AudioSettings.dspTime + totalDelay;
            audioSource.PlayScheduled(_musicScheduledDSPTime);

            _isMusicScheduled = true; // 음악이 성공적으로 스케줄링되었음을 표시

            Debug.Log($"음악이 {totalDelay:F2}초 후에 재생될 예정입니다. (DSP Time 기준: {_musicScheduledDSPTime:F2})");
        }
    }
}