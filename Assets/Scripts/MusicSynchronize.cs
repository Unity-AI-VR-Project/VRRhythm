using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicSynchronizer : MonoBehaviour
{
    [Header("음악 설정")]
    public float bpm = 102f;

    public int delayBeat = 2;
    public float offset = 0.3f;
    private AudioSource audioSource;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        // 1박 길이 계산
        double beatInterval = 60.0 / bpm;

        // 노트가 먼저 움직이기 위한 지연 시간 (2박)
        double delay = beatInterval * delayBeat;

        // 현재 dsp 시간 + 지연 시간에 맞춰 음악 재생 예약
        double scheduledStartTime = AudioSettings.dspTime + delay;

        audioSource.PlayScheduled(scheduledStartTime);

        Debug.Log($"음악은 {delay:F2}초 후에 재생됩니다.");
    }
}