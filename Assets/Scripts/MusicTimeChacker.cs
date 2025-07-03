using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicTimeChacker : MonoBehaviour
{
    private AudioSource audioSource;
    private double startDspTime;
    private float musicLength;

    public double elapsedTime { get; private set; }
    public float progress { get; private set; }

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (audioSource.clip == null)
        {
            Debug.LogError("OtherMusicTracker: 오디오 클립이 없습니다!");
            return;
        }

        musicLength = audioSource.clip.length;

        // 음악 즉시 재생
        audioSource.Play();
        startDspTime = AudioSettings.dspTime;
    }

    void Update()
    {
        if (!audioSource.isPlaying)
            return;

        elapsedTime = AudioSettings.dspTime - startDspTime;
        progress = Mathf.Clamp01((float)(elapsedTime / musicLength));

        //Debug.Log($"[OtherMusic] 진행 시간: {elapsedTime:F3}초, 진행도: {progress * 100f:F1}%");
    }
}