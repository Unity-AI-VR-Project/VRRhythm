using UnityEngine;
using System.Collections.Generic;

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
}

public struct SpawnInfoBundle
{
    public NoteInfo NoteData;
    public Transform SpawnerTransform;
    public float Tempo; // SpawnerSelector에서 BPM도 함께 넘겨줍니다.
}


public class SpawnerSelector : MonoBehaviour
{
    [Header("JSON Data & Spawner Setup")]
    [Tooltip("재생할 음악 정보가 담긴 JSON 파일 (TextAsset으로 드래그 앤 드롭).")]
    public TextAsset musicJsonFile; 
    [Tooltip("SpawnPos, SetPos, TargetPos Transform을 자식으로 포함하는 부모 GameObject들. 각 스포너는 이 목록의 한 요소입니다.")]
    public List<Transform> spawners; 

    [Tooltip("노트가 실제 스폰되어야 하는 시점보다 얼마나 미리 스폰될 것인지 (비트 단위).")]
    public float NotePreSpawnBeats; // NoteSpawnerTime으로부터 주입받을 값

    private RootData _currentSongData; 
    private List<NoteInfo> _allNotes; 
    private int _nextNoteIndex = 0; 

    private string _lastSpawnedNoteBand = null;
    private float _lastSpawnedNoteTime = -1.0f; 
    private int _lastUsedSpawnerIndex = -1; 

    void Awake()
    {
        LoadMusicData();
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
                    _allNotes.AddRange(beat.notes);
                }
            }
            _allNotes.Sort((n1, n2) => n1.time.CompareTo(n2.time)); 

            Debug.Log($"SpawnerSelector: 음악 데이터 로드 성공: '{_currentSongData.metadata.band_group}' (BPM: {_currentSongData.metadata.tempo}) - 총 {_allNotes.Count}개 노트.", this);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SpawnerSelector: JSON 파일 로드/파싱 오류: {e.Message}. JSON 파일 내용을 확인하세요.", this);
        }
    }

    /// <summary>
    /// 현재 음악 시간과 노트 프리팹 정보를 기반으로 다음 스폰할 노트 정보와
    /// 해당 노트를 스폰할 스포너의 Transform을 반환합니다.
    /// 이 메서드 내부에서 preSpawnTime을 계산합니다.
    /// </summary>
    /// <param name="currentTime">현재 음악 진행 시간 (초).</param>
    /// <returns>스폰할 노트 정보와 스포너 Transform이 담긴 SpawnInfoBundle? (null 허용).</returns>
    public SpawnInfoBundle? GetNoteAndSpawnerForCurrentTime(float currentTime) // preSpawnTime 인자 제거
    {
        if (_allNotes == null || _currentSongData == null || _currentSongData.metadata == null || _nextNoteIndex >= _allNotes.Count)
        {
            return null; 
        }

        NoteInfo nextNote = _allNotes[_nextNoteIndex];
        
        // --- 이 부분이 수정되었습니다 ---
        // 템포 정보는 SpawnerSelector가 직접 관리하므로 여기서 preSpawnTime을 계산합니다.
        float tempo = _currentSongData.metadata.tempo;
        if (tempo == 0f) 
        {
            Debug.LogWarning("SpawnerSelector: 템포가 유효하지 않습니다 (0). 노트 스폰 시간을 계산할 수 없습니다.", this);
            return null;
        }
        float preSpawnTime = NotePreSpawnBeats * (60f / tempo); 
        // --- 수정 끝 ---

        if (currentTime >= nextNote.time - preSpawnTime)
        {
            Transform selectedSpawner = GetNextAvailableSpawnerTransform(nextNote); 

            if (selectedSpawner == null) 
            {
                return null; 
            }

            SpawnInfoBundle bundle = new SpawnInfoBundle
            {
                NoteData = nextNote,
                SpawnerTransform = selectedSpawner,
                Tempo = tempo // 계산된 템포도 함께 넘겨줍니다.
            };

            _nextNoteIndex++; 
            _lastSpawnedNoteBand = nextNote.band;
            _lastSpawnedNoteTime = nextNote.time;

            return bundle;
        }

        return null; 
    }

    private Transform GetNextAvailableSpawnerTransform(NoteInfo currentNote)
    {
        if (spawners == null || spawners.Count == 0)
        {
            Debug.LogError("SpawnerSelector: 스포너가 할당되지 않았습니다! 최소한 하나의 스포너를 할당해야 합니다.", this);
            return null; 
        }

        int numSpawners = spawners.Count;
        int startIndex = (_lastUsedSpawnerIndex + 1) % numSpawners; 

        if (_lastUsedSpawnerIndex == -1 || _lastSpawnedNoteTime == -1.0f)
        {
            _lastUsedSpawnerIndex = startIndex; 
            return spawners[startIndex];
        }

        for (int i = 0; i < numSpawners; i++)
        {
            int potentialSpawnerIndex = (startIndex + i) % numSpawners;

            bool requiresDifferentSpawner = false;

            if (currentNote.band == _lastSpawnedNoteBand)
            {
                requiresDifferentSpawner = true;
            }

            if (currentNote.time - _lastSpawnedNoteTime < 0.2f)
            {
                requiresDifferentSpawner = true;
            }

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
        Debug.LogWarning("SpawnerSelector: 템포 데이터가 아직 로드되지 않았거나 유효하지 않습니다. 기본값 0f를 반환합니다.");
        return 0f; 
    }
}