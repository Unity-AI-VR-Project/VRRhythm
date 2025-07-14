using System;
using UnityEngine;
using System.Collections.Generic;
using Define;
using System.Linq;
using Random = UnityEngine.Random;


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
    public NoteDirection requiredDirection; 

    // 새로 추가: 이 노트의 최종 목표 위치
    public Vector3 calculatedTargetPos; 

    // 새로 추가: 왼손/오른손 정보 - 이제 Enums.cs에 정의된 NoteType 사용
    public SaberNoteType NoteType; 
}

public struct SpawnInfoBundle
{
    public NoteInfo NoteData;
    public Transform SpawnerTransform;
    public float Tempo;
    public Vector3 CalculatedTargetPos; // SpawnerSelector에서 계산된 TargetPos
}

public class SpawnerSelector : MonoBehaviour
{
    [Header("JSON Data & Spawner Setup")]
    public TextAsset musicJsonFile; 
    public List<Transform> spawners; 

    [HideInInspector] public float NotePreSpawnBeats;

    private RootData _currentSongData; 
    public List<NoteInfo> _allNotes; 
    private int _nextNoteIndex = 0; 

    private string _lastSpawnedNoteBand = null;
    private float _lastSpawnedNoteTime = -1.0f; 
    private int _lastUsedSpawnerIndex = -1; 

    private Vector3 _lastCalculatedTargetPos = Vector3.zero;
    private NoteDirection _lastAssignedDirection = NoteDirection.Up; 
    // 새로 추가: 마지막으로 할당된 손 타입 - 이제 Enums.cs에 정의된 NoteType 사용
    private SaberNoteType _lastAssignedNoteType = SaberNoteType.Right; // 초기값 설정

    public float PLAYABLE_X_MIN = -1f;
    public float PLAYABLE_X_MAX = 1f;
    public float PLAYABLE_Y_MIN = 0.7f;
    public float PLAYABLE_Y_MAX = 1.4f;
    public float PLAYABLE_Z = 1.5f; // 플레이 가능한 Z 위치 (카메라와의 거리)

    public float copyNoteDataSecOffset = 0.125f; 
    public float moveAmount = 0.35f;
    
    // (이하 기존 코드 유지)
    void Awake()
    {
        LoadMusicData();
        if (spawners == null || spawners.Count == 0)
        {
            Debug.LogError("SpawnerSelector: spawners 리스트가 비어 있습니다. 스포너 Transform을 할당해주세요.", this);
            enabled = false;
        }
    }

    private void Start()
    {

        // 1/8 한박자 시간 - 0.01(오프셋) 보다 가까우면 인접한 노트로 인식
        copyNoteDataSecOffset = (60 / _currentSongData.metadata.tempo / 2) - 0.01f;
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
                    foreach(var note in beat.notes)
                    {
                        _allNotes.Add(note);
                    }
                }
            }
            _allNotes.Sort((n1, n2) => n1.time.CompareTo(n2.time)); 

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
        NoteDirection[] possibleDirections = {
            NoteDirection.Up,
            NoteDirection.Down,
            NoteDirection.Left,
            NoteDirection.Right,
        };

        SaberNoteType[] possibleHands = { // 새로 추가: 가능한 손 타입 - 이제 Enums.cs의 NoteType 사용
            SaberNoteType.Left,
            SaberNoteType.Right
        };

        
        float lastNoteTime = -1.0f;


        // 모든 노트를 조건에 맞게 검사하여 _allNotes에 담음
        for (int i = 0; i < _allNotes.Count; i++)
        {
            NoteInfo note = _allNotes[i];

            // 인접 노트인지 검사하는 조건
            bool isCloseToLastNote = (note.time - lastNoteTime <= copyNoteDataSecOffset && lastNoteTime != -1.0f);

            lastNoteTime = note.time;

            if (isCloseToLastNote)
            {
                // 인접 노트의 경우, 이전 노트의 방향과 손 타입 모두 그대로 사용
                note.requiredDirection = _lastAssignedDirection; 
                note.NoteType = _lastAssignedNoteType; // 손 타입도 복사
                note.time = lastNoteTime;
            }
            else
            {
                // 인접하지 않은 노트는 랜덤 방향과 랜덤 손 타입 할당
                int randomDirectionIndex = Random.Range(0, possibleDirections.Length);
                note.requiredDirection = possibleDirections[randomDirectionIndex];

                int randomHandIndex = Random.Range(0, possibleHands.Length); // 랜덤 손 타입 할당
                note.NoteType = possibleHands[randomHandIndex];
            }
            
            // _lastAssignedDirection과 _lastAssignedNoteType 갱신 (다음 노트의 로직을 위해)
            _lastAssignedDirection = note.requiredDirection;
            _lastAssignedNoteType = note.NoteType; // 손 타입도 갱신
            

            _allNotes[i] = note;
        }
    }


    public SpawnInfoBundle? GetNoteAndSpawnerForCurrentTime(float currentTime)
    {
        if (_allNotes == null || _currentSongData == null || _currentSongData.metadata == null || _nextNoteIndex >= _allNotes.Count)
        {
            return null; 
        }

        // 다음 노트의 데이터를 가져옴
        NoteInfo nextNote = _allNotes[_nextNoteIndex];
        

        float tempo = _currentSongData.metadata.tempo;
        if (tempo == 0f) 
        {
            Debug.LogWarning("SpawnerSelector: 템포가 유효하지 않습니다 (0). 노트 스폰 시간을 계산할 수 없습니다.", this);
            return null;
        }
        float preSpawnTime = NotePreSpawnBeats * (60f / tempo); 

        float noteAbsoluteTime = nextNote.time; 

        if (currentTime >= noteAbsoluteTime - preSpawnTime)
        {
            nextNote.calculatedTargetPos = CalculateTargetPosition(nextNote);
            _allNotes[_nextNoteIndex] = nextNote; 

            Transform selectedSpawner = FindAppropriateSpawner(nextNote); 

            if (selectedSpawner == null) 
            {
                return null; 
            }

            SpawnInfoBundle bundle = new SpawnInfoBundle
            {
                NoteData = nextNote,
                SpawnerTransform = selectedSpawner,
                Tempo = tempo,
                CalculatedTargetPos = nextNote.calculatedTargetPos
            };

            _nextNoteIndex++; 
            _lastSpawnedNoteBand = nextNote.band;
            _lastSpawnedNoteTime = nextNote.time;
            _lastCalculatedTargetPos = nextNote.calculatedTargetPos;

            return bundle;
        }

        return null; 
    }

    private Vector3 CalculateTargetPosition(NoteInfo currentNote)
    {
        if (currentNote.NoteType == SaberNoteType.Left)
        {
            PLAYABLE_X_MAX = PLAYABLE_X_MAX / 4; // 왼손 노트는 플레이 가능한 영역을 왼쪽 1/4로 제한
        }
        else
        {
            PLAYABLE_X_MIN = PLAYABLE_X_MIN / 4; // 오른손 노트는 플레이 가능한 영역을 오른쪽 1/4로 제한
        }

        Vector3 defaultTargetPos = new Vector3(
            Random.Range(PLAYABLE_X_MIN + 0.1f, PLAYABLE_X_MAX - 0.1f),
            Random.Range(PLAYABLE_Y_MIN + 0.1f, PLAYABLE_Y_MAX - 0.1f),
            PLAYABLE_Z // 플레이 가능한 Z 위치 (카메라와의 거리)
        );

        if (_lastSpawnedNoteTime != -1.0f && (currentNote.time - _lastSpawnedNoteTime <= copyNoteDataSecOffset))
        {
            if (currentNote.requiredDirection == NoteDirection.Any)
            {
                Debug.LogWarning("CalculateTargetPosition: 현재 노트의 requiredDirection이 Any입니다. 인접 로직 대신 랜덤 TargetPos를 사용합니다.");
                return defaultTargetPos;
            }

            Vector3 basePos = _lastCalculatedTargetPos; 
            float moveAmount = this.moveAmount; // 멤버 변수 moveAmount 사용
            Vector3 finalCalculatedPos = basePos; 

            switch (currentNote.requiredDirection)
            {
                case NoteDirection.Up:
                    float intendedY_Up = basePos.y + moveAmount;
                    if (intendedY_Up > PLAYABLE_Y_MAX) {
                        finalCalculatedPos.y = basePos.y - moveAmount;
                    } else {
                        finalCalculatedPos.y = intendedY_Up;
                    }
                    break;
                case NoteDirection.Down:
                    float intendedY_Down = basePos.y - moveAmount;
                    if (intendedY_Down < PLAYABLE_Y_MIN) {
                        finalCalculatedPos.y = basePos.y + moveAmount;
                    } else {
                        finalCalculatedPos.y = intendedY_Down;
                    }
                    break;
                case NoteDirection.Left:
                    float intendedX_Left = basePos.x - moveAmount;
                    if (intendedX_Left < PLAYABLE_X_MIN) {
                        finalCalculatedPos.x = basePos.x + moveAmount;
                    } else {
                        finalCalculatedPos.x = intendedX_Left;
                    }
                    break;
                case NoteDirection.Right:
                    float intendedX_Right = basePos.x + moveAmount;
                    if (intendedX_Right > PLAYABLE_X_MAX) {
                        finalCalculatedPos.x = basePos.x - moveAmount;
                    } else {
                        finalCalculatedPos.x = intendedX_Right;
                    }
                    break;
            }

            Define.eScenes a = Define.eScenes.InGame;

            switch (a)
            {
                case eScenes.Title:
                    break;
                case eScenes.Lobby:
                    break;
                case eScenes.InGame:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            
            finalCalculatedPos.x = Mathf.Clamp(finalCalculatedPos.x, PLAYABLE_X_MIN, PLAYABLE_X_MAX);
            finalCalculatedPos.y = Mathf.Clamp(finalCalculatedPos.y, PLAYABLE_Y_MIN, PLAYABLE_Y_MAX);
            finalCalculatedPos.z = PLAYABLE_Z; 

            //Debug.Log($"인접 노트 타겟 포지션 계산: 이전 노트 시간: {_lastSpawnedNoteTime}, 현재 노트 시간: {currentNote.time}, 요청 방향: {currentNote.requiredDirection}, 최종 타겟: {finalCalculatedPos}, 손 타입: {currentNote.NoteType}");
            return finalCalculatedPos;
        }

        //Debug.Log($"랜덤 타겟 포지션 계산: 최종 타겟: {defaultTargetPos}, 노트 타격방향: {currentNote.requiredDirection}, 손 타입: {currentNote.NoteType}");
        return defaultTargetPos; 
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

        if (_lastUsedSpawnerIndex == -1) 
        {
            _lastUsedSpawnerIndex = startIndex; 
            return spawners[startIndex];
        }

        bool isCloseToLastNoteTime = (currentNote.time - _lastSpawnedNoteTime <= 0.2f);

        if (isCloseToLastNoteTime)
        {
            int nextAdjacentSpawnerIndex = (_lastUsedSpawnerIndex + 1) % numSpawners;
            _lastUsedSpawnerIndex = nextAdjacentSpawnerIndex;
            return spawners[nextAdjacentSpawnerIndex];
        }

        for (int i = 0; i < numSpawners; i++)
        {
            int potentialSpawnerIndex = (startIndex + i) % numSpawners;

            bool requiresDifferentSpawner = false;

            if (currentNote.band == _lastSpawnedNoteBand)
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
        return 120f;
    }
}