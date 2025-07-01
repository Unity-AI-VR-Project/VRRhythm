using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicSynchronizer : MonoBehaviour
{
    [Header("음악 설정")]
    public float bpm = 75f;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // 오디오 DSP 기준 정확한 재생 시간 예약
        double beatInterval = 60.0 / bpm;
        double scheduledStartTime = AudioSettings.dspTime + beatInterval;

        audioSource.PlayScheduled(scheduledStartTime);
    }
}