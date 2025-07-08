using UnityEngine;
using System.Collections.Generic;
using System.Linq; // OrderBy 사용을 위해 추가

// NoteJudger.cs에 정의된 JudgementType.NoteDirection enum을 사용하기 위해 필요
// 만약 NoteJudger가 다른 네임스페이스에 있다면 using NoteJudgerNamespace; 와 같이 추가해야 합니다.

[System.Serializable]
public class RootData
{
    public Metadata metadata;
    public List<BeatData> beats;
}

[System.Serializable]
public class Metadata
{
    public float tempo;
    public string time_resolution_unit;
    public string band_group;
}

[System.Serializable]
public class BeatData
{
    public int beat_index;
    public float beat_time;
    public List<NoteInfo> notes;
}

[System.Serializable]
public class NoteInfo
{
    public float time;
    public string band;
    public int relative_pos_in_beat;
    public float strength;
    // 이 필드는 JSON에서 직접 파싱되지 않고, SpawnerSelector에서 할당됩니다.
    public NoteJudger.NoteDirection requiredDirection; 
}

public struct SpawnInfoBundle
{
    public NoteInfo NoteData;
    public Transform SpawnerTransform;
    public float Tempo;
}

public class SpawnerSelector : MonoBehaviour
{
    [Header("JSON Data & Spawner Setup")]
    [Tooltip("재생할 음악 정보가 담긴 JSON 파일 (TextAsset으로 드래그 앤 드롭).")]
    public TextAsset musicJsonFile; 
    [Tooltip("SpawnPos, SetPos, TargetPos Transform을 자식으로 포함하는 부모 GameObject들. 각 스포너는 이 목록의 한 요소입니다.")]
    public List<Transform> spawners; 

    [Tooltip("새로운 노트가 생성되기 전에 음악 박자로 얼마나 일찍 스폰되어야 하는지를 지정합니다. 이 값은 NoteMover에서 가져옵니다.")]
    [HideInInspector] public float NotePreSpawnBeats;

    private RootData _currentSongData; 
    private List<NoteInfo> _allNotes; // 모든 노트를 시간 순서대로 저장
    private int _nextNoteIndex = 0; 

    private string _lastSpawnedNoteBand = null;
    private float _lastSpawnedNoteTime = -1.0f; 
    private int _lastUsedSpawnerIndex = -1; 
    private NoteJudger.NoteDirection _lastAssignedDirection = NoteJudger.NoteDirection.Any; // 마지막으로 할당된 노트 방향

    void Awake()
    {
        LoadMusicData();
        // 스포너 리스트가 비어있는지 확인
        if (spawners == null || spawners.Count == 0)
        {
            Debug.LogError("SpawnerSelector: spawners 리스트가 비어 있습니다. 스포너 Transform을 할당해주세요.", this);
            enabled = false;
        }
    }

    private void LoadMusicData()
    {
        if (musicJsonFile == null)
        {
            Debug.LogError("SpawnerSelector: 'musicJsonFile'이 할당되지 않았습니다! JSON 파일을 Unity 에디터에서 할당해주세요.", this);
            return;
        }

        try
        {
            _currentSongData = JsonUtility.FromJson<RootData>(musicJsonFile.text);
            
            if (_currentSongData == null || _currentSongData.metadata == null || _currentSongData.beats == null)
            {
                Debug.LogError("SpawnerSelector: JSON 데이터를 파싱하는 데 실패했습니다. JSON 파일의 형식을 확인하세요.", this);
                return;
            }

            _allNotes = new List<NoteInfo>();
            foreach (var beat in _currentSongData.beats)
            {
                if (beat.notes != null)
                {
                    // 각 비트 내의 노트들을 _allNotes에 추가
                    foreach(var note in beat.notes)
                    {
                        _allNotes.Add(note);
                    }
                }
            }
            // 모든 노트를 절대 시간 기준으로 정렬
            // JSON의 time 필드는 beat_time에 상대적인 시간일 수 있으므로, 정확한 절대 시간을 계산하여 정렬해야 합니다.
            // 여기서는 간단히 note.time 필드를 기준으로 정렬합니다. (필요시 beat_time + note.time으로 절대 시간 계산 후 정렬)
            _allNotes.Sort((n1, n2) => n1.time.CompareTo(n2.time)); 

            // 모든 노트에 requiredDirection 할당
            AssignDirectionsToNotes();

            Debug.Log($"SpawnerSelector: 음악 데이터 로드 성공: '{_currentSongData.metadata.band_group}' (BPM: {_currentSongData.metadata.tempo}) - 총 {_allNotes.Count}개 노트.", this);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SpawnerSelector: JSON 파일 로드/파싱 오류: {e.Message}. JSON 파일 내용을 확인하세요.", this);
        }
    }

    private void AssignDirectionsToNotes()
    {
        NoteJudger.NoteDirection[] possibleDirections = {
            NoteJudger.NoteDirection.Up,
            NoteJudger.NoteDirection.Down,
            NoteJudger.NoteDirection.Left,
            NoteJudger.NoteDirection.Right,
        };

        float lastNoteTime = -1.0f;
        NoteJudger.NoteDirection currentAssignedDirection = NoteJudger.NoteDirection.Any;

        for (int i = 0; i < _allNotes.Count; i++)
        {
            NoteInfo note = _allNotes[i];
            
            // 2. SpawnInfoBundle값을 보낼때 이전 노트와 NoteData 차이가 짧거나 같으면 방향값을 똑같이 하고 바로 옆 spawners에 값을 보내기
            // '0.2f'는 예시 값이며, 게임 플레이에 따라 조정 필요
            bool isCloseToLastNote = (note.time - lastNoteTime < 0.2f && lastNoteTime != -1.0f); 

            if (isCloseToLastNote)
            {
                // 이전 노트와 매우 가깝다면 이전 노트와 같은 방향 할당
                note.requiredDirection = currentAssignedDirection;
            }
            else
            {
                // 가깝지 않다면 랜덤 방향 할당
                int randomIndex = Random.Range(0, possibleDirections.Length);
                note.requiredDirection = possibleDirections[randomIndex];
            }

            // 할당된 방향을 업데이트하여 다음 노트에 사용
            currentAssignedDirection = note.requiredDirection;
            lastNoteTime = note.time;

            _allNotes[i] = note; // 구조체이므로 변경사항을 다시 할당
        }
    }


    /// <summary>
    /// 현재 음악 시간과 노트 프리팹 정보를 기반으로 다음 스폰할 노트 정보와
    /// 해당 노트를 스폰할 스포너의 Transform을 반환합니다.
    /// 이 메서드 내부에서 preSpawnTime을 계산합니다.
    /// </summary>
    /// <param name="currentTime">현재 음악 진행 시간 (초).</param>
    /// <returns>스폰할 노트 정보와 스포너 Transform이 담긴 SpawnInfoBundle? (null 허용).</returns>
    public SpawnInfoBundle? GetNoteAndSpawnerForCurrentTime(float currentTime)
    {
        if (_allNotes == null || _currentSongData == null || _currentSongData.metadata == null || _nextNoteIndex >= _allNotes.Count)
        {
            return null; 
        }

        NoteInfo nextNote = _allNotes[_nextNoteIndex];
        
        float tempo = _currentSongData.metadata.tempo;
        if (tempo == 0f) 
        {
            Debug.LogWarning("SpawnerSelector: 템포가 유효하지 않습니다 (0). 노트 스폰 시간을 계산할 수 없습니다.", this);
            return null;
        }
        float preSpawnTime = NotePreSpawnBeats * (60f / tempo); 

        // JSON의 note.time은 beat_time에 상대적인 시간일 수 있으므로,
        // 정확한 절대 시간을 계산해야 합니다.
        // 이 부분은 JSON 데이터의 'time' 필드가 절대 시간인지, 상대 시간인지에 따라 달라집니다.
        // 현재 JSON은 'time'이 절대 시간처럼 보이지만, 'beat_time'도 있으므로 확인 필요.
        // 여기서는 'time'을 노트의 절대 시간으로 가정하고 진행합니다.
        float noteAbsoluteTime = nextNote.time; 

        if (currentTime >= noteAbsoluteTime - preSpawnTime)
        {
            // FindAppropriateSpawner에 현재 노트의 방향 정보를 전달하여 스포너 선택에 활용할 수 있습니다.
            // 현재 FindAppropriateSpawner는 band와 시간 차이만 고려하므로, 필요시 확장하세요.
            Transform selectedSpawner = FindAppropriateSpawner(nextNote); 

            if (selectedSpawner == null) 
            {
                return null; 
            }

            SpawnInfoBundle bundle = new SpawnInfoBundle
            {
                NoteData = nextNote, // requiredDirection이 이미 할당된 NoteData
                SpawnerTransform = selectedSpawner,
                Tempo = tempo
            };

            _nextNoteIndex++; 
            _lastSpawnedNoteBand = nextNote.band;
            _lastSpawnedNoteTime = nextNote.time; // 마지막으로 스폰된 노트의 절대 시간

            return bundle;
        }

        return null; 
    }

    private Transform FindAppropriateSpawner(NoteInfo currentNote)
    {
        if (spawners == null || spawners.Count == 0)
        {
            Debug.LogError("SpawnerSelector: 스포너가 할당되지 않았습니다! 최소한 하나의 스포너를 할당해야 합니다.", this);
            return null; 
        }

        int numSpawners = spawners.Count;
        int startIndex = (_lastUsedSpawnerIndex + 1) % numSpawners; 

        // 첫 노트 처리
        if (_lastUsedSpawnerIndex == -1) // _lastSpawnedNoteTime == -1.0f는 AssignDirectionsToNotes에서 처리
        {
            _lastUsedSpawnerIndex = startIndex; 
            return spawners[startIndex];
        }

        // 2. 노트와 NoteData 차이가 짧거나 같으면 바로 옆 spawners에 값을 보내기
        // SpawnerSelector의 AssignDirectionsToNotes에서 이미 방향을 결정했으므로,
        // 여기서는 스포너 선택 로직만 남습니다.
        // '바로 옆' 스포너 선택 로직은 'band' 또는 'relative_pos_in_beat'와 같은
        // 노트의 위치 관련 데이터와 스포너의 물리적 위치를 매핑해야 합니다.
        // 현재는 단순히 다음 인덱스의 스포너를 선택하도록 유지합니다.
        
        // 이전에 스폰된 노트와 현재 노트의 시간 간격이 짧은 경우, 스포너를 특정 방식으로 선택할 수 있습니다.
        // 예를 들어, 이전 스포너의 바로 옆 스포너를 우선적으로 선택하는 로직
        // 이 로직은 스포너의 물리적 배치가 중요합니다. (예: 왼쪽 -> 중앙 -> 오른쪽)
        bool isCloseToLastNote = (currentNote.time - _lastSpawnedNoteTime < 0.2f); // SpawnerSelector의 _lastSpawnedNoteTime 사용

        if (isCloseToLastNote)
        {
            // 이전 스포너의 '바로 옆' 스포너를 찾습니다.
            // 이 로직은 spawners 리스트의 순서가 물리적 위치와 일치한다고 가정합니다.
            int nextAdjacentSpawnerIndex = (_lastUsedSpawnerIndex + 1) % numSpawners;
            _lastUsedSpawnerIndex = nextAdjacentSpawnerIndex;
            return spawners[nextAdjacentSpawnerIndex];
        }

        // 일반적인 스포너 선택 로직 (기존 코드와 유사)
        for (int i = 0; i < numSpawners; i++)
        {
            int potentialSpawnerIndex = (startIndex + i) % numSpawners;

            bool requiresDifferentSpawner = false;

            if (currentNote.band == _lastSpawnedNoteBand)
            {
                requiresDifferentSpawner = true;
            }
            // `currentNote.time - _lastSpawnedNoteTime < 0.2f` 조건은 위에서 isCloseToLastNote로 처리

            if (potentialSpawnerIndex == _lastUsedSpawnerIndex && requiresDifferentSpawner)
            {
                continue; 
            }
            
            _lastUsedSpawnerIndex = potentialSpawnerIndex; 
            return spawners[potentialSpawnerIndex];
        }

        Debug.LogWarning("SpawnerSelector: 조건에 맞는 스포너를 찾지 못했습니다. 다음 순서의 스포너를 강제로 사용합니다. 스포너 개수나 조건 검토가 필요할 수 있습니다.", this);
        int fallbackIndex = startIndex; 
        _lastUsedSpawnerIndex = fallbackIndex; 
        return spawners[fallbackIndex];
    }

    public float GetTempo()
    {
        if (_currentSongData != null && _currentSongData.metadata != null)
        {
            return _currentSongData.metadata.tempo;
        }
        return 120f; // 기본값
    }
}