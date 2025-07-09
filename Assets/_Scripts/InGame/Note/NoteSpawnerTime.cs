using UnityEngine;
using System.Collections.Generic;
using Define;

public class NoteSpawnerTime : MonoBehaviour
{
    [Header("Music & Spawner Setup")]
    [SerializeField] private SpawnerSelector spawnerSelector;
    [SerializeField] private MusicTimeChecker timeChecker;

    /// <summary>
    /// 스크립트 인스턴스가 로드될 때 호출되며, 필요한 컴포넌트 참조를 설정하고 유효성을 검사합니다.
    /// </summary>
    void Awake()
    {
        if (spawnerSelector == null)
        {
            Debug.LogError("NoteSpawnerTime: SpawnerSelector 컴포넌트가 할당되지 않았습니다. Inspector에서 할당해주세요.", this);
            enabled = false;
            return;
        }

        if (timeChecker == null)
        {
            Debug.LogError("NoteSpawnerTime: MusicTimeChecker 컴포넌트가 할당되지 않았습니다. Inspector에서 할당해주세요.", this);
            enabled = false;
            return;
        }

        if (NoteManager.Instance == null)
        {
            Debug.LogError("NoteSpawnerTime: NoteManager가 아직 초기화되지 않았습니다. NoteSpawnerTime의 Awake 실행 순서를 확인해주세요.", this);
            enabled = false;
            return;
        }

        GameObject pooledNotePrefab = NoteManager.Instance.notePrefabToPool;
        if (pooledNotePrefab != null)
        {
            // NoteMover 대신 NoteMovement 사용
            NoteMovement prefabNoteMovement = pooledNotePrefab.GetComponent<NoteMovement>();
            if (prefabNoteMovement != null)
            {
                // prefabNoteMovement.PreSpawnBeats는 이제 private이므로 직접 접근 불가.
                // NoteMovement 클래스에 PreSpawnBeats 값을 반환하는 public 프로퍼티 또는 메서드를 추가해야 합니다.
                // 예를 들어 NoteMovement 클래스에 public float GetPreSpawnBeats() { return _preSpawnBeats; } 추가 후 아래 사용.
                // 아니면 NoteMovement의 _preSpawnBeats를 다시 public으로 변경하는 방법도 있습니다.
                // 여기서는 NoteMovement에서 GetPreSpawnBeats() 메서드가 있다고 가정합니다.
                spawnerSelector.NotePreSpawnBeats = prefabNoteMovement.GetPreSpawnBeats(); // <- NoteMovement 수정 필요
            }
            else
            {
                Debug.LogError("NoteSpawnerTime: NoteManager의 Pool용 노트 프리팹에 NoteMovement 컴포넌트가 없습니다! 노트 이동이 불가능하며, SpawnerSelector에 PreSpawnBeats를 전달할 수 없습니다.", this);
                enabled = false;
                return;
            }
        }
        else
        {
            Debug.LogError("NoteSpawnerTime: NoteManager에 Pool링할 노트 프리팹이 할당되지 않았습니다. Inspector에서 할당해주세요.", this);
            enabled = false;
            return;
        }
    }

    /// <summary>
    /// 매 프레임 호출되며, 현재 음악 시간에 맞춰 노트를 스폰할지 확인하고 처리합니다.
    /// </summary>
    void Update()
    {
        float currentElapsedTime = (float)timeChecker.elapsedTime;

        SpawnInfoBundle? spawnInfo = spawnerSelector.GetNoteAndSpawnerForCurrentTime(currentElapsedTime);

        if (spawnInfo.HasValue)
        {
            SpawnNote(
                spawnInfo.Value.NoteData,
                spawnInfo.Value.SpawnerTransform,
                spawnInfo.Value.Tempo,
                spawnInfo.Value.CalculatedTargetPos
            );
        }
    }

    /// <summary>
    /// 주어진 정보를 바탕으로 노트를 오브젝트 풀에서 가져와 초기화하고 스폰 위치에 배치합니다.
    /// </summary>
    /// <param name="noteInfo">스폰할 노트의 정보입니다.</param>
    /// <param name="selectedSpawner">노트가 스폰될 스포너의 Transform입니다.</param>
    /// <param name="tempo">현재 음악의 템포(BPM)입니다.</param>
    /// <param name="calculatedTargetPos">노트가 이동할 최종 목표 위치입니다.</param>
    void SpawnNote(NoteInfo noteInfo, Transform selectedSpawner, float tempo, Vector3 calculatedTargetPos)
    {
        GameObject spawnedNote = NoteManager.Instance.GetPooledNote();
        if (spawnedNote == null)
        {
            Debug.LogWarning("NoteSpawnerTime: 노트 풀에서 노트를 가져올 수 없습니다. 풀이 비어있거나 문제가 있습니다.");
            return;
        }

        // 노트의 초기 위치 및 회전 설정
        spawnedNote.transform.position = selectedSpawner.position;
        spawnedNote.transform.rotation = Quaternion.identity;

        float targetZRotation = 0f;
        switch (noteInfo.requiredDirection)
        {
            case NoteDirection.Up: targetZRotation = 0f; break;
            case NoteDirection.Right: targetZRotation = 90f; break;
            case NoteDirection.Down: targetZRotation = 180f; break;
            case NoteDirection.Left: targetZRotation = -90f; break;
            case NoteDirection.Any: targetZRotation = 0f; break;
            default:
                Debug.LogWarning($"NoteSpawnerTime: 알 수 없는 NoteDirection 값 ({noteInfo.requiredDirection})입니다. 기본 회전값 (0도)을 사용합니다.", spawnedNote);
                break;
        }
        spawnedNote.transform.rotation = Quaternion.Euler(0, 0, targetZRotation);

        // NoteMovement 컴포넌트 가져오기 (이름 변경 반영)
        NoteMovement noteMovement = spawnedNote.GetComponent<NoteMovement>();
        if (noteMovement != null)
        {
            // NoteMovement.InitializeNote 파라미터 변경 반영
            noteMovement.InitializeNote(
                selectedSpawner,
                tempo,
                noteInfo.time, // targetMusicTime
                timeChecker,
                calculatedTargetPos
            // noteInfo는 이제 NoteMovement 내부에서 Note 컴포넌트를 통해 접근
            // noteInfo.NoteType은 NoteMovement 내부에서 Note 컴포넌트를 통해 접근
            );
        }
        else
        {
            Debug.LogError("생성된 노트 프리팹에 NoteMovement 컴포넌트가 없습니다! 노트 이동이 불가능합니다. 이 노트를 풀에 반환합니다.", spawnedNote);
            NoteManager.Instance.ReturnPooledNote(spawnedNote);
            return;
        }

        // Note 컴포넌트에 requiredDirection과 requiredNoteType 할당
        // 이 부분은 NoteMovement가 내부적으로 Note 컴포넌트의 값을 사용하도록 변경되었으므로,
        // NoteMovement.InitializeNote가 호출되기 전에 Note 컴포넌트에 값을 설정하는 것이 더 논리적입니다.
        // 하지만 현재 NoteSpawnerTime이 NoteInfo를 가지고 있으므로 여기서 설정하는 것도 가능합니다.
        // NoteMovement가 Note 컴포넌트의 값을 사용한다면, 여기서 Note 컴포넌트에 값을 설정하는 것이 중요합니다.
        Note noteComponent = spawnedNote.GetComponent<Note>();
        if (noteComponent != null)
        {
            noteComponent.requiredDirection = noteInfo.requiredDirection;
            noteComponent.requiredNoteType = noteInfo.NoteType;
        }
        else
        {
            Debug.LogWarning($"NoteSpawnerTime: 생성된 노트 프리팹({NoteManager.Instance.notePrefabToPool.name})에 Note 컴포넌트가 없습니다. 판정 시 문제가 발생할 수 있습니다.", spawnedNote);
        }
    }
}