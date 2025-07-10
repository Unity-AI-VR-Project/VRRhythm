using UnityEngine;
using UnityEngine.Pool;
using Define;

public class NoteManager : MonoBehaviour
{
    public static NoteManager Instance { get; private set; }

    private NoteJudger activeNoteJudger;

    [Header("Judgement Timing Windows")]
    [SerializeField] private float autoMissTimingWindow = 0.3f;
    [SerializeField] private float perfectTimingWindow = 0.05f;
    [SerializeField] private float excellentTimingWindow = 0.1f;
    [SerializeField] private float goodTimingWindow = 0.15f;

    public MusicSynchronizer musicTimeChecker;

    [Header("Note Pooling Setup")]
    [SerializeField] public GameObject notePrefabToPool;
    [SerializeField] private int defaultPoolSize = 10;
    [SerializeField] private int maxPoolSize = 20;

    private IObjectPool<GameObject> notePool;

    /// <summary>
    /// NoteManager의 인스턴스를 초기화하고 NoteJudger를 설정하며 노트 풀을 초기화합니다.
    /// </summary>
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
            activeNoteJudger = new NoteJudger(autoMissTimingWindow, perfectTimingWindow, excellentTimingWindow, goodTimingWindow);
            InitializeNotePool();
        }

        if (musicTimeChecker == null)
        {
            Debug.LogError("NoteManager: MusicTimeChecker가 할당되지 않았습니다. Inspector에서 할당해주세요.", this);
            enabled = false;
        }

        if (notePrefabToPool == null)
        {
            Debug.LogError("NoteManager: Pool링할 노트 프리팹이 할당되지 않았습니다. Inspector에서 할당해주세요.", this);
            enabled = false;
        }
    }

    /// <summary>
    /// 유니티 오브젝트 풀을 사용하여 노트를 미리 생성하고 초기화합니다.
    /// </summary>
    private void InitializeNotePool()
    {
        notePool = new ObjectPool<GameObject>(CreatePooledNote, OnGetNoteFromPool, OnReleaseNoteToPool, OnDestroyNoteInPool,
                                            collectionCheck: false, defaultCapacity: defaultPoolSize, maxSize: maxPoolSize);

        for (int i = 0; i < defaultPoolSize; i++)
        {
            notePool.Release(CreatePooledNote());
        }
    }

    /// <summary>
    /// 풀에서 새 노트 오브젝트를 생성할 때 호출됩니다.
    /// </summary>
    private GameObject CreatePooledNote()
    {
        GameObject note = Instantiate(notePrefabToPool);
        Note noteComponent = note.GetComponent<Note>();
        if (noteComponent == null)
        {
            noteComponent = note.AddComponent<Note>();
        }
        return note;
    }

    /// <summary>
    /// 풀에서 노트 오브젝트를 가져올 때 호출됩니다. 노트를 활성화하고 초기 상태로 재설정합니다.
    /// </summary>
    private void OnGetNoteFromPool(GameObject note)
    {
        note.SetActive(true);
        Note noteComponent = note.GetComponent<Note>();
        if (noteComponent != null)
        {
            noteComponent.ResetNote();
            MeshFilter meshFilter = note.GetComponent<MeshFilter>();
            if (meshFilter != null && noteComponent.OriginalMesh != null)
            {
                meshFilter.mesh = noteComponent.OriginalMesh;
                MeshCollider meshCollider = note.GetComponent<MeshCollider>();
                if (meshCollider != null)
                {
                    meshCollider.sharedMesh = noteComponent.OriginalMesh;
                    meshCollider.convex = true;
                    meshCollider.isTrigger = true;
                }
            }
            Collider currentCollider = note.GetComponent<Collider>();
            if (currentCollider != null)
            {
                currentCollider.enabled = true;
            }
        }
    }

    /// <summary>
    /// 노트 오브젝트를 풀로 반환할 때 호출됩니다. 노트를 비활성화합니다.
    /// </summary>
    private void OnReleaseNoteToPool(GameObject note)
    {
        note.SetActive(false);
    }

    /// <summary>
    /// 풀에서 노트 오브젝트를 완전히 파괴할 때 호출됩니다.
    /// </summary>
    private void OnDestroyNoteInPool(GameObject note)
    {
        Destroy(note);
    }

    /// <summary>
    /// NoteJudger로부터 호출되어 노트의 절단 시각 효과를 처리하고 노트를 풀에 반환합니다.
    /// </summary>
    /// <param name="noteObject">절단될 노트 게임 오브젝트입니다.</param>
    /// <param name="hitPoint">노트가 충돌한 정확한 지점입니다.</param>
    /// <param name="saberSwingDirection">세이버의 스윙 방향입니다.</param>
    public void HandleNoteCutVisuals(GameObject hitNoteObject, Vector3 hitPoint, Vector3 saberSwingDirection)
    {
        NoteMovement noteMovement = hitNoteObject.GetComponent<NoteMovement>();
        Collider noteCollider = hitNoteObject.GetComponent<Collider>();

        if (noteMovement != null) noteMovement.enabled = false;
        if (noteCollider != null) noteCollider.enabled = false;

        Cutter.Cut(hitNoteObject, hitPoint, saberSwingDirection);

        ReturnPooledNote(hitNoteObject);
    }

    /// <summary>
    /// 노트의 절단 시각 효과를 처리하고 노트를 풀에 반환합니다. 미스 처리 시 절단 연출을 건너뛸 수 있습니다.
    /// </summary>
    /// <param name="hitNoteObject">처리될 노트 게임 오브젝트입니다.</param>
    /// <param name="hitPoint">노트가 충돌한 정확한 지점입니다.</param>
    /// <param name="saberSwingDirection">세이버의 스윙 방향입니다.</param>
    /// <param name="isMissed">노트가 미스되었는지 여부입니다.</param>
    public void HandleNoteCutVisuals(GameObject hitNoteObject, Vector3 hitPoint, Vector3 saberSwingDirection, bool isMissed)
    {
        NoteMovement noteMovement = hitNoteObject.GetComponent<NoteMovement>();
        Collider noteCollider = hitNoteObject.GetComponent<Collider>();

        if (noteMovement != null) noteMovement.enabled = false;
        if (noteCollider != null) noteCollider.enabled = false;

        if (!isMissed)
        {
            Cutter.Cut(hitNoteObject, hitPoint, saberSwingDirection);
        }

        ReturnPooledNote(hitNoteObject);
    }

    /// <summary>
    /// 오브젝트 풀에서 노트 오브젝트를 가져옵니다.
    /// </summary>
    /// <returns>활성화된 노트 게임 오브젝트입니다.</returns>
    public GameObject GetPooledNote()
    {
        return notePool.Get();
    }

    /// <summary>
    /// 사용이 끝난 노트 오브젝트를 오브젝트 풀로 반환합니다.
    /// </summary>
    /// <param name="note">풀로 반환할 노트 게임 오브젝트입니다.</param>
    public void ReturnPooledNote(GameObject note)
    {
        notePool.Release(note);
    }

    /// <summary>
    /// 현재 NoteManager가 관리하는 NoteJudger 인스턴스를 반환합니다.
    /// </summary>
    /// <returns>활성화된 NoteJudger 인스턴스입니다.</returns>
    public NoteJudger GetNoteJudger()
    {
        return activeNoteJudger;
    }
}