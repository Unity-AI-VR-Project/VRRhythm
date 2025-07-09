using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Audio;

public class SFXPoolable : MonoBehaviour
{
    private AudioSource audioSource;
    private IObjectPool<SFXPoolable> parentPool;

    public AudioSource AudioSource
    {
        get
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    audioSource = gameObject.AddComponent<AudioSource>();
                }
            }
            return audioSource;
        }
    }

    public void SetManagedPool(IObjectPool<SFXPoolable> pool)
    {
        parentPool = pool;
    }

    public void Play(AudioClip clip, float volume = 1f, AudioMixerGroup outputMixerGroup = null, bool usePlayOneShot = true)
    {
        AudioSource.outputAudioMixerGroup = outputMixerGroup;

        if (usePlayOneShot)
        {
            AudioSource.PlayOneShot(clip, volume);
            Invoke("ReleaseToPool", clip.length);
        }
        else
        {
            AudioSource.clip = clip;
            AudioSource.volume = volume;
            AudioSource.Play();
            Invoke("ReleaseToPool", AudioSource.clip.length);
        }
    }

    public void ReleaseToPool()
    {
        AudioSource.Stop();
        CancelInvoke("ReleaseToPool");

        if (parentPool != null)
        {
            parentPool.Release(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}