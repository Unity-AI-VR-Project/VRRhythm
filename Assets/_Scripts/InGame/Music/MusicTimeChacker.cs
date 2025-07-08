using UnityEngine;

public class MusicTimeChacker : MonoBehaviour
{
    // 이 오디오 소스는 MusicSynchronizer와 동일한 AudioSource여야 합니다.
    // MusicSynchronizer가 Attach된 GameObject에 MusicTimeChacker도 함께 Attach하는 것이 좋습니다.
    public AudioSource audioSource; 

    // 현재 음악의 경과 시간 (외부에서 읽기 전용)
    public double elapsedTime { get; private set; } 

    // 음악이 재생될 것으로 예약된 DSP 시간
    private double _musicScheduledStartTime; 
    private bool _isMusicPlaying = false;

    void Awake()
    {
        // 동일 GameObject에 붙어있는 AudioSource 컴포넌트를 자동으로 가져옵니다.
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                Debug.LogError("MusicTimeChacker: AudioSource 컴포넌트가 이 GameObject에 없거나 할당되지 않았습니다. 할당해주세요.", this);
                enabled = false; // 이 컴포넌트 비활성화
            }
        }
    }

    // MusicSynchronizer에서 이 메서드를 호출하여 음악 시작 시간을 초기화합니다.
    public void InitializeMusicStartTime(double scheduledDspTime)
    {
        _musicScheduledStartTime = scheduledDspTime;
        _isMusicPlaying = true;
        Debug.Log($"MusicTimeChacker: 음악이 DSP 시간 {_musicScheduledStartTime:F4}에 시작하도록 예약되었습니다.");
    }

    void Update()
    {
        // 음악이 재생 중이고 AudioSource가 유효할 때만 경과 시간을 업데이트합니다.
        if (_isMusicPlaying && audioSource != null && audioSource.isPlaying) 
        {
            elapsedTime = AudioSettings.dspTime - _musicScheduledStartTime;
        }
        else if (_isMusicPlaying && audioSource != null && !audioSource.isPlaying && elapsedTime > 0)
        {
            // 음악이 재생을 멈췄을 때 (예: 노래 끝)
            _isMusicPlaying = false;
            // 선택적으로, 자연스럽게 끝났다면 elapsedTime을 클립 길이로 설정할 수 있습니다.
            // elapsedTime = audioSource.clip.length;
        }
    }
}