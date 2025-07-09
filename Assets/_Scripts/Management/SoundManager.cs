using UnityEngine;
using UnityEngine.Pool;

public class SoundManager : ManagerBase
{
    [SerializeField] private GameObject sfxPrefab;
    public IObjectPool<AudioSource> sfxPool;
    public int sfxCapacity = 20;
    public int sfxMaxSize = 100;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Initialize()
    {
        InitializeSFXPool();
        base.Initialize();
    }

    private void InitializeSFXPool()
    {
        sfxPool = new ObjectPool<AudioSource>(CreateSFX, OnGetSFX, OnReleaseSFX, OnDestroySFX, defaultCapacity:sfxCapacity ,maxSize:sfxMaxSize);
    }

    private AudioSource CreateSFX()
    {
        AudioSource source = Instantiate(sfxPrefab).GetComponent<AudioSource>();

        return source;
    }

    private void OnGetSFX(AudioSource source)
    {
        source.gameObject.SetActive(true);
    }

    private void OnReleaseSFX(AudioSource source)
    {
        source.gameObject.SetActive(false);
    }

    private void OnDestroySFX(AudioSource source)
    {
        Destroy(source.gameObject);
    }
}
