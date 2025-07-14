using UnityEngine;

public class AudioProcessor : MonoBehaviour
{
    private AudioSource audioSource;
    private AudioClip micClip;

    public int maxSamples;
    public int micSampleRate;
    public int maxRecordingSeconds;

    public bool IsRecording { get; private set; } = false;
    public float RecordingStartTime { get; private set; } = 0f;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    public void StartRecording()
    {
        IsRecording = true;
        RecordingStartTime = Time.time;
        micClip = Microphone.Start(null, false, maxRecordingSeconds, micSampleRate);
        audioSource.clip = micClip;
    }

    public AudioClip StopRecording()
    {
        if (!IsRecording) return null;

        IsRecording = false;
        Microphone.End(null);
        return micClip;
    }

    private void OnDestroy()
    {
        if (micClip != null)
        {
            Destroy(micClip);
        }
    }
}