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
        // SpawnerSelector 컴포넌트가 할당되었는지 확인
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

        // MusicTimeChacker 컴포넌트가 할당되었는지 확인
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

        // NoteMover의 PreSpawnBeats 값을 SpawnerSelector에 전달
        if (notePrefab != null)
        {
            NoteMover prefabNoteMover = notePrefab.GetComponent<NoteMover>();
            if (prefabNoteMover != null)
            {
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

        if (timeChacker.audioSource == null || !timeChacker.audioSource.isPlaying) 
        {
            return; 
        }

        float currentElapsedTime = (float)timeChacker.elapsedTime; 

        SpawnInfoBundle? spawnInfo = spawnerSelector.GetNoteAndSpawnerForCurrentTime(currentElapsedTime);

        if (spawnInfo.HasValue)
        {
            SpawnNote(spawnInfo.Value.NoteData, spawnInfo.Value.SpawnerTransform, spawnInfo.Value.Tempo);
        }
    }

    /// <summary>
    /// 단일 노트를 생성하고 초기 속성을 설정합니다.
    /// </summary>
    /// <param name="noteInfo">생성할 노트의 데이터.</param>
    /// <param name="selectedSpawner">노트를 스폰할 Spawner Transform.</param>
    /// <param name="tempo">현재 음악의 템포.</param>
    void SpawnNote(NoteInfo noteInfo, Transform selectedSpawner, float tempo)
    {
        // 노트를 생성할 때 스포너의 위치를 기준으로 생성합니다.
        // Quaternion.identity는 회전이 없는 상태(기본값)를 의미합니다.
        GameObject spawnedNote = Instantiate(notePrefab, selectedSpawner.position, Quaternion.identity); 
        NoteMover noteMover = spawnedNote.GetComponent<NoteMover>();

        // --- 여기서 Z축 기준으로 0, 90, 180, 270도 중 랜덤 회전을 적용합니다. ---
        // 1. 회전시킬 각도들을 배열로 정의합니다.
        float[] rotationAngles = { 0f, 90f, 180f, 270f };
        
        // 2. 배열에서 무작위로 하나의 각도를 선택합니다.
        // Random.Range(min, max)는 min 이상 max 미만의 정수를 반환하므로,
        // rotationAngles.Length를 max 값으로 사용하면 배열의 모든 인덱스에 접근할 수 있습니다.
        float randomZRotation = rotationAngles[Random.Range(0, rotationAngles.Length)];
        
        // 3. 선택된 각도를 사용하여 노트의 Z축 회전을 설정합니다.
        // Quaternion.Euler(x, y, z)는 오일러 각도(일반적인 각도 값)를 유니티의 회전값(쿼터니언)으로 변환해줍니다.
        // X, Y축 회전은 0으로 유지하고 Z축에만 랜덤 각도를 적용합니다.
        spawnedNote.transform.rotation = Quaternion.Euler(0, 0, randomZRotation);
        // --- 랜덤 회전 적용 끝 ---


        if (noteMover != null)
        {
            noteMover.InitializeNote(
                selectedSpawner,       
                tempo,                 
                noteInfo.time,          
                noteInfo,               
                timeChacker             
            );
        }
        else
        {
            Debug.LogWarning("생성된 노트 프리팹에 NoteMover 컴포넌트가 없습니다! 노트 이동이 불가능합니다.", spawnedNote);
            Destroy(spawnedNote); 
        }

        // --- 추가된 부분: NoteJudger 초기화 ---
        NoteJudger noteJudger = spawnedNote.GetComponent<NoteJudger>();
        if (noteJudger != null)
        {
            // NoteMover 인스턴스를 NoteJudger에 전달합니다.
            noteJudger.Initialize(noteMover); 
        }
        // ------------------------------------
    }
}