using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Audio;

public class SoundManager : ManagerBase
{
    #region SFX Pool Settings
    [SerializeField] private SFXPoolable sfxPrefab;
    public IObjectPool<SFXPoolable> sfxPool;
    public int sfxCapacity = 20;
    public int sfxMaxSize = 100;
    [SerializeField] private Transform sfxPoolParent;
    #endregion

    #region Music Player Settings
    [SerializeField] private AudioSource musicAudioSource;
    [SerializeField] private bool loopMusic = true;
    #endregion

    #region Audio Mixer Settings
    [SerializeField] private AudioMixer masterMixer;
    [SerializeField] private AudioMixerGroup masterMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private AudioMixerGroup musicMixerGroup;

    private const string MASTER_VOLUME_PARAM = "MasterVolume";
    private const string SFX_VOLUME_PARAM = "SFXVolume";
    private const string MUSIC_VOLUME_PARAM = "MusicVolume";
    #endregion

    #region Unity Lifecycle Methods
    protected override void Awake()
    {
        base.Awake();
        if (sfxPoolParent == null)
        {
            sfxPoolParent = new GameObject("SFX Pool Parent").transform;
            sfxPoolParent.SetParent(this.transform);
            sfxPoolParent.localPosition = Vector3.zero;
            sfxPoolParent.localRotation = Quaternion.identity;
            sfxPoolParent.localScale = Vector3.one;
        }

        if (musicAudioSource == null)
        {
            GameObject musicPlayerGO = new GameObject("Music Player");
            musicPlayerGO.transform.SetParent(this.transform);
            musicAudioSource = musicPlayerGO.AddComponent<AudioSource>();
            musicAudioSource.playOnAwake = false;
        }
        musicAudioSource.outputAudioMixerGroup = musicMixerGroup;
        musicAudioSource.loop = loopMusic;
    }

    protected override void Initialize()
    {
        InitializeSFXPool();
        base.Initialize();
    }
    #endregion

    #region SFX Pool Methods
    private void InitializeSFXPool()
    {
        sfxPool = new ObjectPool<SFXPoolable>(CreateSFX, OnGetSFX, OnReleaseSFX, OnDestroySFX, defaultCapacity: sfxCapacity, maxSize: sfxMaxSize);
    }

    private SFXPoolable CreateSFX()
    {
        SFXPoolable sfx = Instantiate(sfxPrefab, sfxPoolParent);
        return sfx;
    }

    private void OnGetSFX(SFXPoolable sfx)
    {
        sfx.gameObject.SetActive(true);
    }

    private void OnReleaseSFX(SFXPoolable sfx)
    {
        sfx.gameObject.SetActive(false);
    }

    private void OnDestroySFX(SFXPoolable sfx)
    {
        Destroy(sfx.gameObject);
    }
    #endregion

    #region Public Play Methods
    public SFXPoolable PlaySFX(AudioClip clip, float volume = 1f, bool usePlayOneShot = true)
    {
        SFXPoolable sfx = sfxPool.Get();
        sfx.SetManagedPool(sfxPool);
        sfx.Play(clip, volume, sfxMixerGroup, usePlayOneShot);
        return sfx;
    }

    public void PlayMusic(AudioClip clip, float volume = 1f, bool loop = true)
    {
        if (musicAudioSource == null)
        {
            Debug.LogError("Music AudioSource is not assigned or created!");
            return;
        }

        if (musicAudioSource.isPlaying && musicAudioSource.clip == clip)
        {
            return;
        }

        musicAudioSource.clip = clip;
        musicAudioSource.volume = volume;
        musicAudioSource.loop = loop;
        musicAudioSource.Play();
    }

    public void StopMusic()
    {
        if (musicAudioSource != null)
        {
            musicAudioSource.Stop();
        }
    }

    public void PauseMusic()
    {
        if (musicAudioSource != null)
        {
            musicAudioSource.Pause();
        }
    }

    public void UnPauseMusic()
    {
        if (musicAudioSource != null)
        {
            musicAudioSource.UnPause();
        }
    }
    #endregion

    #region Volume Control Methods
    public void SetMasterVolume(float volume)
    {
        SetVolume(masterMixer, MASTER_VOLUME_PARAM, volume);
    }

    public void SetSFXVolume(float volume)
    {
        SetVolume(masterMixer, SFX_VOLUME_PARAM, volume);
    }

    public void SetMusicVolume(float volume)
    {
        SetVolume(masterMixer, MUSIC_VOLUME_PARAM, volume);
    }

    private void SetVolume(AudioMixer mixer, string parameterName, float volume)
    {
        if (mixer == null)
        {
            Debug.LogWarning("AudioMixer is not assigned in SoundManager.");
            return;
        }

        float linearVolume = Mathf.Clamp(volume, 0.0001f, 1f);
        float dbVolume = Mathf.Log10(linearVolume) * 20;

        mixer.SetFloat(parameterName, dbVolume);
    }
    #endregion

    #region Mixer Group Getters
    public AudioMixerGroup GetMasterMixerGroup()
    {
        return masterMixerGroup;
    }

    public AudioMixerGroup GetSFXMixerGroup()
    {
        return sfxMixerGroup;
    }

    public AudioMixerGroup GetMusicMixerGroup()
    {
        return musicMixerGroup;
    }
    #endregion
}