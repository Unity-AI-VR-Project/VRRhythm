using UnityEngine;
using UnityEngine.Pool;

public class NoteManager : MonoBehaviour
{
    public static NoteManager Instance { get; private set; }

    [SerializeField] private NoteJudger activeNoteJudger;

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
    /// 풀에서 노트 오브젝트를 가져올 때 호출됩니다. 노트를 활성화하고 일반적인 초기 상태로 재설정합니다.
    /// </summary>
    private void OnGetNoteFromPool(GameObject noteObject) // 매개변수 이름 변경 (note -> noteObject)
    {
        noteObject.SetActive(true);
        Note noteComponent = noteObject.GetComponent<Note>();
        if (noteComponent != null) // 주석 해제!
        {
            noteComponent.ResetNote(); // Note 스크립트의 ResetNote() 호출
            // MeshFilter나 Collider 복원 로직은 Note 스크립트의 ResetNote()에서 처리하거나,
            // 절단 효과를 제거했다면 더 이상 필요 없을 수 있습니다.
        }
    }

    /// <summary>
    /// 노트 오브젝트를 풀로 반환할 때 호출됩니다. 노트를 비활성화합니다.
    /// </summary>
    private void OnReleaseNoteToPool(GameObject note)
    {
        note.SetActive(false);
        // 노트가 풀로 돌아갈 때 NoteMovement도 비활성화되어야 합니다.
        // 이는 NoteMovement의 Update에서 CheckForPoolReturn을 통해 이루어지거나,
        // HandleNoteCutVisuals에서 명시적으로 false로 설정됩니다.
        // 이 OnReleaseNoteToPool에서는 추가적인 Reset이나 비활성화가 필요하지 않을 수 있습니다.
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
    /// (절단 효과 제거 후, 노트 비활성화 및 풀 반환만 수행)
    /// </summary>
    public void HandleNoteCutVisuals(GameObject hitNoteObject, Vector3 hitPoint, Vector3 saberSwingDirection)
    {
        NoteMovement noteMovement = hitNoteObject.GetComponent<NoteMovement>();
        Collider noteCollider = hitNoteObject.GetComponent<Collider>();

        if (noteMovement != null) noteMovement.enabled = false; // 이동 중지
        if (noteCollider != null) noteCollider.enabled = false; // 콜라이더 비활성화

        // Cutter.Cut(hitNoteObject, hitPoint, saberSwingDirection); // 이 줄은 완전히 제거 (주석 X)

        ReturnPooledNote(hitNoteObject);
    }

    /// <summary>
    /// 노트의 절단 시각 효과를 처리하고 노트를 풀에 반환합니다. 미스 처리 시 절단 연출을 건너뛸 수 있습니다.
    /// (절단 효과 제거 후, 노트 비활성화 및 풀 반환만 수행)
    /// </summary>
    public void HandleNoteCutVisuals(GameObject hitNoteObject, Vector3 hitPoint, Vector3 saberSwingDirection, bool isMissed)
    {
        NoteMovement noteMovement = hitNoteObject.GetComponent<NoteMovement>();
        Collider noteCollider = hitNoteObject.GetComponent<Collider>();

        if (noteMovement != null) noteMovement.enabled = false; // 이동 중지
        if (noteCollider != null) noteCollider.enabled = false; // 콜라이더 비활성화

        // if (!isMissed) { Cutter.Cut(...); } 블록은 완전히 제거 (주석 X)

        ReturnPooledNote(hitNoteObject);
    }

    /// <summary>
    /// 오브젝트 풀에서 노트 오브젝트를 가져옵니다.
    /// NoteMovement의 InitializeNote는 이 메서드 호출 후 스포너에서 담당해야 합니다.
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

    // --- 새로운 노트 스폰 메서드 추가 ---
    /// <summary>
    /// 풀에서 노트를 가져와 필요한 파라미터로 초기화한 후 스폰합니다.
    /// 이 메서드는 노트 생성 로직(예: NoteSpawner 스크립트)에서 호출되어야 합니다.
    /// </summary>
    /// <param name="spawnerParent">노트가 스폰될 부모 Transform (주로 Spawner의 Transform)</param>
    /// <param name="bpm">현재 음악의 BPM</param>
    /// <param name="targetMusicTime">노트가 타겟 위치에 도달해야 하는 음악 시간</param>
    /// <param name="targetPos">노트가 도달해야 하는 타겟 월드 위치</param>
    /// <returns>스폰되어 초기화된 노트 게임 오브젝트</returns>
    public GameObject SpawnNote(Transform spawnerParent, float bpm, float targetMusicTime, Vector3 targetPos)
    {
        GameObject newNoteObject = GetPooledNote(); // 1. 풀에서 노트 가져오기

        // 2. NoteMovement 컴포넌트 초기화
        NoteMovement noteMovement = newNoteObject.GetComponent<NoteMovement>();
        if (noteMovement != null)
        {
            // NoteMovement의 InitializeNote 메서드를 호출하여 이동 파라미터를 설정합니다.
            noteMovement.InitializeNote(spawnerParent, bpm, targetMusicTime, musicTimeChecker, targetPos);
        }
        else
        {
            Debug.LogError($"NoteManager: 스폰된 노트 {newNoteObject.name}에 NoteMovement 컴포넌트가 없습니다.", newNoteObject);
            ReturnPooledNote(newNoteObject); // 초기화 실패 시 풀로 다시 반환
            return null;
        }

        // 필요하다면 여기서 노트의 부모를 설정할 수 있습니다 (예: spawnerParent가 노트가 속할 Hierarchy의 부모인 경우)
        // newNoteObject.transform.SetParent(spawnerParent); // SetParent 호출 시 false를 사용하여 월드 위치 유지

        return newNoteObject;
    }
}