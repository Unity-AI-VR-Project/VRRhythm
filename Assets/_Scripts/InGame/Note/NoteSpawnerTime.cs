using UnityEngine;
using System.Collections.Generic;

public class NoteSpawnerTime : MonoBehaviour
{
    [Header("Music & Spawner Setup")]
    [Tooltip("생성할 노트 프리팹.")]
    public GameObject notePrefab;
    [Tooltip("스포너 선택 및 노트 데이터 관리를 담당하는 SpawnerSelector 컴포넌트.")]
    public SpawnerSelector spawnerSelector; // SpawnerSelector 참조
    [Tooltip("현재 음악 시간을 추적하는 MusicTimeChacker 컴포넌트.")]
    public MusicTimeChacker timeChacker; 
    
    void Awake()
    {
        if (spawnerSelector == null)
        {
            spawnerSelector = FindAnyObjectByType<SpawnerSelector>(); 
            if (spawnerSelector == null)
            {
                Debug.LogError("NoteSpawnerTime: SpawnerSelector 컴포넌트가 할당되지 않았습니다. 씬에 SpawnerSelector를 추가하고 할당하거나, 수동으로 할당해주세요.", this);
                enabled = false; 
                return;
            }
        }

        if (timeChacker == null)
        {
            timeChacker = FindAnyObjectByType<MusicTimeChacker>();
            if (timeChacker == null)
            {
                Debug.LogError("NoteSpawnerTime: MusicTimeChacker 컴포넌트가 할당되지 않았습니다. 씬에 MusicTimeChacker를 추가하고 할당하거나, 수동으로 할당해주세요.", this);
                enabled = false;
                return;
            }
        }

        if (notePrefab != null)
        {
            NoteMover prefabNoteMover = notePrefab.GetComponent<NoteMover>();
            if (prefabNoteMover != null)
            {
                // 이 줄을 다시 활성화했습니다. SpawnerSelector가 노트의 이동 비트 수를 알 수 있도록 이 값을 전달해야 합니다.
                // SpawnerSelector.cs에 'public float NotePreSpawnBeats;' 또는
                // 'public float NotePreSpawnBeats { get; set; }' 형태로 선언되어야 합니다.
                spawnerSelector.NotePreSpawnBeats = prefabNoteMover.PreSpawnBeats; 
            }
            else
            {
                Debug.LogError("NoteSpawnerTime: 노트 프리팹에 NoteMover 컴포넌트가 없습니다! SpawnerSelector에 PreSpawnBeats를 전달할 수 없습니다.", this);
                enabled = false; 
                return;
            }
        }
        else
        {
            Debug.LogError("NoteSpawnerTime: 노트 프리팹이 할당되지 않았습니다! SpawnerSelector에 PreSpawnBeats를 전달할 수 없습니다.", this);
            enabled = false;
            return;
        }
    }

    void Update()
    {
        if (timeChacker == null) 
        {
            Debug.LogError("NoteSpawnerTime: MusicTimeChacker가 없습니다. 노트를 스폰할 수 없습니다. 스크립트 할당을 확인하세요.", this);
            return;
        }

        float currentElapsedTime = (float)timeChacker.elapsedTime; 
        // Debug.Log($"[NoteSpawnerTime] 현재 음악 경과 시간: {currentElapsedTime:F3}초"); // 디버깅용: 활성화하여 시간 흐름 확인

        SpawnInfoBundle? spawnInfo = spawnerSelector.GetNoteAndSpawnerForCurrentTime(currentElapsedTime);

        if (spawnInfo.HasValue)
        {
            // Debug.Log($"[NoteSpawnerTime] 노트 스폰 정보 발견! 음악 시간: {spawnInfo.Value.NoteData.time:F3}초"); // 디버깅용: 노트 스폰 확인
            SpawnNote(spawnInfo.Value.NoteData, spawnInfo.Value.SpawnerTransform, spawnInfo.Value.Tempo, spawnInfo.Value.CalculatedTargetPos);
        }
        else
        {
            // Debug.Log($"[NoteSpawnerTime] 현재 음악 시간({currentElapsedTime:F3}초)에 스폰할 노트 없음."); // 디버깅용: 노트 스폰 안 되는 경우 확인
        }
    }

    /// <summary>
    /// 단일 노트를 생성하고 초기 속성을 설정합니다.
    /// </summary>
    /// <param name="noteInfo">생성할 노트의 데이터.</param>
    /// <param name="selectedSpawner">노트를 스폰할 Spawner Transform.</param>
    /// <param name="tempo">현재 음악의 템포.</param>
    /// <param name="calculatedTargetPos">SpawnerSelector에서 계산된 노트의 최종 목표 위치.</param>
    void SpawnNote(NoteInfo noteInfo, Transform selectedSpawner, float tempo, Vector3 calculatedTargetPos)
    {
        GameObject spawnedNote = Instantiate(notePrefab, selectedSpawner.position, Quaternion.identity); 
        NoteMover noteMover = spawnedNote.GetComponent<NoteMover>();

        float targetZRotation = 0f;

        switch (noteInfo.requiredDirection)
        {
            case NoteJudger.NoteDirection.Up:
                targetZRotation = 0f;
                break;
            case NoteJudger.NoteDirection.Right:
                targetZRotation = 90f;
                break;
            case NoteJudger.NoteDirection.Down:
                targetZRotation = 180f;
                break;
            case NoteJudger.NoteDirection.Left:
                targetZRotation = -90f;
                break;
            default:
                Debug.LogWarning($"NoteSpawnerTime: 알 수 없는 NoteDirection 값 ({noteInfo.requiredDirection})입니다. 기본 회전값 (0도)을 사용합니다.", spawnedNote);
                targetZRotation = 0f;
                break;
        }

        spawnedNote.transform.rotation = Quaternion.Euler(0, 0, targetZRotation);

        if (noteMover != null)
        {
            noteMover.InitializeNote(
                selectedSpawner,       
                tempo,                 
                noteInfo.time,          
                noteInfo,               
                timeChacker,
                calculatedTargetPos,
                noteInfo.NoteType 
            );
        }
        else
        {
            Debug.LogWarning("생성된 노트 프리팹에 NoteMover 컴포넌트가 없습니다! 노트 이동이 불가능합니다.", spawnedNote);
            Destroy(spawnedNote); 
        }

        NoteJudger noteJudger = spawnedNote.GetComponent<NoteJudger>();
        if (noteJudger != null)
        {
            noteJudger.Initialize(noteMover, noteInfo.requiredDirection, noteInfo.NoteType); 
        }
    }
}