using UnityEngine;
using System.Collections.Generic;
using Define; // Define 네임스페이스가 필요합니다 (예: NoteInfo, SpawnInfoBundle).

public class NoteSpawnerTime : MonoBehaviour
{
    [Header("Music & Spawner Setup")]
    [SerializeField] private SpawnerSelector spawnerSelector;
    [SerializeField] private MusicSynchronizer timeChecker;

    /// <summary>
    /// 스크립트 인스턴스가 로드될 때 호출되며, 필요한 컴포넌트 참조를 설정하고 유효성을 검사합니다.
    /// </summary>
    void Awake()
    {
        // SpawnerSelector 참조 확인
        if (spawnerSelector == null)
        {
            Debug.LogError("NoteSpawnerTime: SpawnerSelector 컴포넌트가 할당되지 않았습니다. Inspector에서 할당해주세요.", this);
            enabled = false;
            return;
        }

        // MusicSynchronizer 참조 확인
        if (timeChecker == null)
        {
            Debug.LogError("NoteSpawnerTime: MusicSynchronizer 컴포넌트가 할당되지 않았습니다. Inspector에서 할당해주세요.", this);
            enabled = false;
            return;
        }

        // NoteManager 싱글톤 인스턴스 확인
        // NoteManager가 이 스크립트보다 먼저 Awake에서 초기화되어야 합니다. (스크립트 실행 순서 조정 필요)
        if (NoteManager.Instance == null)
        {
            Debug.LogError("NoteSpawnerTime: NoteManager가 아직 초기화되지 않았습니다. NoteSpawnerTime의 Awake 실행 순서를 확인해주세요 (Edit -> Project Settings -> Script Execution Order).", this);
            enabled = false;
            return;
        }

        // 오브젝트 풀에 사용할 노트 프리팹에서 NoteMovement의 PreSpawnBeats 값을 가져와 SpawnerSelector에 설정
        GameObject pooledNotePrefab = NoteManager.Instance.notePrefabToPool;
        if (pooledNotePrefab != null)
        {
            NoteMovement prefabNoteMovement = pooledNotePrefab.GetComponent<NoteMovement>();
            if (prefabNoteMovement != null)
            {
                // NoteMovement의 GetPreSpawnBeats() 메서드를 통해 값을 가져와 SpawnerSelector에 전달합니다.
                spawnerSelector.NotePreSpawnBeats = prefabNoteMovement.GetPreSpawnBeats();
                Debug.Log($"NoteSpawnerTime: SpawnerSelector에 NotePreSpawnBeats를 {prefabNoteMovement.GetPreSpawnBeats()}로 설정했습니다.");
            }
            else
            {
                Debug.LogError("NoteSpawnerTime: NoteManager의 Pool용 노트 프리팹에 NoteMovement 컴포넌트가 없습니다! 노트 이동이 불가능하며, SpawnerSelector에 PreSpawnBeats를 전달할 수 없습니다. 프리팹을 확인해주세요.", this);
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
        // MusicSynchronizer의 currentTimeDSP (음악 재생 예약 시점 기준의 경과 시간)를 사용합니다.
        // 이 값이 음수일 때는 아직 음악 재생 예약 시점에 도달하지 않은 것이므로, 노트 스폰 로직을 실행하지 않고 대기합니다.
        if (timeChecker.currentTimeDSP < 0f)
        {
            // Debug.Log($"노트 스폰 대기 중... 현재 음악 경과 시간 (DSP 기준): {timeChecker.currentTimeDSP:F2}s");
            return;
        }

        // SpawnerSelector에서 현재 시간에 스폰할 노트 정보를 가져옵니다.
        // spawnerSelector 내부에서 다음 스폰할 노트 인덱스를 관리합니다.
        SpawnInfoBundle? spawnInfo = spawnerSelector.GetNoteAndSpawnerForCurrentTime(timeChecker.currentTimeDSP);

        if (spawnInfo.HasValue)
        {
            // 스폰할 노트 정보가 있다면 SpawnNote 메서드를 호출하여 실제 노트를 생성합니다.
            SpawnNote(
                spawnInfo.Value.NoteData,
                spawnInfo.Value.SpawnerTransform,
                spawnInfo.Value.Tempo,
                spawnInfo.Value.CalculatedTargetPos
            );
            // 디버그 로그를 통해 노트 스폰이 실제로 일어나는지 확인합니다.
            Debug.Log($"노트 스폰됨: JSON시간={spawnInfo.Value.NoteData.time:F2}s, DSP기준현재시간={timeChecker.currentTimeDSP:F2}s, 스포너={spawnInfo.Value.SpawnerTransform.name}, 타겟Pos={spawnInfo.Value.CalculatedTargetPos}");
        }
    }

    /// <summary>
    /// 주어진 정보를 바탕으로 노트를 오브젝트 풀에서 가져와 초기화하고 스폰 위치에 배치합니다.
    /// </summary>
    /// <param name="noteInfo">스폰할 노트의 정보입니다 (JSON 데이터).</param>
    /// <param name="selectedSpawner">노트가 스폰될 스포너의 Transform (초기 위치 및 방향 제공).</param>
    /// <param name="tempo">현재 음악의 템포(BPM)입니다.</param>
    /// <param name="calculatedTargetPos">노트가 이동할 최종 목표 위치 (판정선 위치).</param>
    void SpawnNote(NoteInfo noteInfo, Transform selectedSpawner, float tempo, Vector3 calculatedTargetPos)
    {
        // NoteManager에서 풀링된 노트 오브젝트를 가져옵니다.
        GameObject spawnedNote = NoteManager.Instance.GetPooledNote();
        if (spawnedNote == null)
        {
            Debug.LogWarning("NoteSpawnerTime: 노트 풀에서 노트를 가져올 수 없습니다. 풀이 비어있거나 문제가 있습니다. 풀 사이즈를 확인하세요.");
            return;
        }

        // 노트 오브젝트의 초기 위치 및 회전 설정 (스포너의 위치와 회전을 따릅니다).
        spawnedNote.transform.position = selectedSpawner.position;
        spawnedNote.transform.rotation = Quaternion.identity; // 기본 회전 리셋

        // 노트의 방향에 따라 Z축 회전 설정 (예: 화살표 노트)
        float targetZRotation = 0f;
        switch (noteInfo.requiredDirection)
        {
            case NoteDirection.Up: targetZRotation = 0f; break;
            case NoteDirection.Right: targetZRotation = 90f; break;
            case NoteDirection.Down: targetZRotation = 180f; break;
            case NoteDirection.Left: targetZRotation = -90f; break;
            case NoteDirection.Any: targetZRotation = 0f; break; // "Any" 방향에 대한 기본값 설정
            default:
                Debug.LogWarning($"NoteSpawnerTime: 알 수 없는 NoteDirection 값 ({noteInfo.requiredDirection})입니다. 기본 회전값 (0도)을 사용합니다.", spawnedNote);
                break;
        }
        spawnedNote.transform.rotation = Quaternion.Euler(0, 0, targetZRotation);

        // Note 컴포넌트를 가져와 필요한 정보 설정 및 리셋
        Note noteComponent = spawnedNote.GetComponent<Note>();
        if (noteComponent != null)
        {
            noteComponent.ResetNote(); // Note.cs에 ResetNote() 메서드가 있다고 가정
            noteComponent.requiredDirection = noteInfo.requiredDirection;
            noteComponent.requiredNoteType = noteInfo.NoteType;
        }
        else
        {
            Debug.LogWarning($"NoteSpawnerTime: 생성된 노트 프리팹({NoteManager.Instance.notePrefabToPool.name})에 Note 컴포넌트가 없습니다. 판정 시 문제가 발생할 수 있습니다. 노트를 풀에 반환합니다.", spawnedNote);
            NoteManager.Instance.ReturnPooledNote(spawnedNote);
            return;
        }

        // NoteMovement 컴포넌트를 가져와 노트 이동 초기화
        NoteMovement noteMovement = spawnedNote.GetComponent<NoteMovement>();
        if (noteMovement != null)
        {
            noteMovement.InitializeNote(
                selectedSpawner,       // 노트 스폰 위치의 기준이 되는 Transform
                tempo,                 // 음악 BPM
                noteInfo.time,         // JSON에 기록된 노트의 목표 음악 시간
                timeChecker,           // MusicSynchronizer 인스턴스 전달
                calculatedTargetPos    // 노트가 도달할 최종 목표 월드 위치 (판정선)
            );
        }
        else
        {
            Debug.LogError($"NoteSpawnerTime: 생성된 노트 프리팹({NoteManager.Instance.notePrefabToPool.name})에 NoteMovement 컴포넌트가 없습니다! 노트 이동이 불가능합니다. 이 노트를 풀에 반환합니다.", spawnedNote);
            NoteManager.Instance.ReturnPooledNote(spawnedNote);
            return;
        }
    }
}