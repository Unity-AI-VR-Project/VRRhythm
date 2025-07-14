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
    public NoteDirection requiredDirection;

    public Vector3 calculatedTargetPos;

    public SaberNoteType NoteType;
}

public struct SpawnInfoBundle
{
    public NoteInfo NoteData;
    public Transform SpawnerTransform;
    public float Tempo;
    public Vector3 CalculatedTargetPos;
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
    private SaberNoteType _lastAssignedNoteType = SaberNoteType.Right;

    public float PLAYABLE_X_MIN = -1f;
    public float PLAYABLE_X_MAX = 1f;
    public float PLAYABLE_Y_MIN = 0.7f;
    public float PLAYABLE_Y_MAX = 1.4f;
    public float PLAYABLE_Z = 1.5f;

    public float copyNoteDataSecOffset = 0.125f;
    public float moveAmount = 0.35f;

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
                    foreach (var note in beat.notes)
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
        NoteDirection.Any // 'Any' 방향도 포함하는 것이 좋습니다.
    };

        SaberNoteType[] possibleHands = {
        SaberNoteType.Left,
        SaberNoteType.Right
    };

        float lastNoteTime = -1.0f;

        for (int i = 0; i < _allNotes.Count; i++)
        {
            // 구조체이므로, 원본을 직접 가져와서 수정합니다.
            // 또는, 복사본을 만들어서 수정하고 다시 리스트에 할당하는 방식도 가능합니다.
            // 여기서는 복사본을 만들어서 수정하고 다시 할당하는 방식으로 구현합니다.
            // (직접 수정하는 방식은 C# 7.2 이상의 ref struct 또는 Span<T>와 관련되어 복잡해질 수 있으므로 이 방식이 일반적입니다.)
            NoteInfo currentNote = _allNotes[i]; // 원본의 복사본을 가져옴

            bool isCloseToLastNote = (currentNote.time - lastNoteTime <= copyNoteDataSecOffset && lastNoteTime != -1.0f);

            lastNoteTime = currentNote.time; // 현재 노트의 시간을 마지막 노트 시간으로 업데이트

            if (isCloseToLastNote)
            {
                currentNote.requiredDirection = _lastAssignedDirection; // 복사본 수정
                currentNote.NoteType = _lastAssignedNoteType; // 복사본 수정
                                                              // note.time = lastNoteTime; // 이 부분은 JSON에서 읽어온 time 값을 사용하므로 변경할 필요 없습니다.
            }
            else
            {
                int randomDirectionIndex = Random.Range(0, possibleDirections.Length);
                currentNote.requiredDirection = possibleDirections[randomDirectionIndex]; // 복사본 수정

                int randomHandIndex = Random.Range(0, possibleHands.Length);
                currentNote.NoteType = possibleHands[randomHandIndex]; // 복사본 수정
            }

            // 복사본의 변경사항을 원본 리스트에 다시 저장
            _allNotes[i] = currentNote;

            // 다음 노트를 위해 현재 할당된 방향/타입을 업데이트
            _lastAssignedDirection = currentNote.requiredDirection;
            _lastAssignedNoteType = currentNote.NoteType;
        }
    }


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
        // === 중요 수정: PLAYABLE_X_MIN과 PLAYABLE_X_MAX를 매번 수정하지 않고 임시 변수를 사용 ===
        float currentPlayableXMin = PLAYABLE_X_MIN;
        float currentPlayableXMax = PLAYABLE_X_MAX;

        if (currentNote.NoteType == SaberNoteType.Left)
        {
            currentPlayableXMax = PLAYABLE_X_MAX / 4f; // 왼손 노트는 플레이 가능한 영역을 왼쪽 1/4로 제한
            currentPlayableXMin = PLAYABLE_X_MIN; // 왼쪽 전체 영역을 기준으로 함
        }
        else // Right
        {
            currentPlayableXMin = PLAYABLE_X_MIN / 4f; // 오른손 노트는 플레이 가능한 영역을 오른쪽 1/4로 제한
            currentPlayableXMax = PLAYABLE_X_MAX; // 오른쪽 전체 영역을 기준으로 함
        }

        Vector3 defaultTargetPos = new Vector3(
            Random.Range(currentPlayableXMin + 0.1f, currentPlayableXMax - 0.1f),
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
            float moveAmount = this.moveAmount;
            Vector3 finalCalculatedPos = basePos;

            switch (currentNote.requiredDirection)
            {
                case NoteDirection.Up:
                    float intendedY_Up = basePos.y + moveAmount;
                    if (intendedY_Up > PLAYABLE_Y_MAX)
                    {
                        finalCalculatedPos.y = basePos.y - moveAmount;
                    }
                    else
                    {
                        finalCalculatedPos.y = intendedY_Up;
                    }
                    break;
                case NoteDirection.Down:
                    float intendedY_Down = basePos.y - moveAmount;
                    if (intendedY_Down < PLAYABLE_Y_MIN)
                    {
                        finalCalculatedPos.y = basePos.y + moveAmount;
                    }
                    else
                    {
                        finalCalculatedPos.y = intendedY_Down;
                    }
                    break;
                case NoteDirection.Left:
                    float intendedX_Left = basePos.x - moveAmount;
                    if (intendedX_Left < currentPlayableXMin)
                    { // === 수정: PLAYABLE_X_MIN 대신 currentPlayableXMin 사용 ===
                        finalCalculatedPos.x = basePos.x + moveAmount;
                    }
                    else
                    {
                        finalCalculatedPos.x = intendedX_Left;
                    }
                    break;
                case NoteDirection.Right:
                    float intendedX_Right = basePos.x + moveAmount;
                    if (intendedX_Right > currentPlayableXMax)
                    { // === 수정: PLAYABLE_X_MAX 대신 currentPlayableXMax 사용 ===
                        finalCalculatedPos.x = basePos.x - moveAmount;
                    }
                    else
                    {
                        finalCalculatedPos.x = intendedX_Right;
                    }
                    break;
            }

            // eScenes 스위치 문은 현재 로직과 관련이 없으며 Dead Code로 보입니다.
            // 제거하거나 주석 처리하는 것이 좋습니다.
            // Define.eScenes a = Define.eScenes.InGame;
            // switch (a)
            // {
            //     case eScenes.Title: break;
            //     case eScenes.Lobby: break;
            //     case eScenes.InGame: break;
            //     default: throw new ArgumentOutOfRangeException();
            // }

            // 최종 클램핑 시에도 임시 변수를 사용
            finalCalculatedPos.x = Mathf.Clamp(finalCalculatedPos.x, currentPlayableXMin, currentPlayableXMax); // === 수정 ===
            finalCalculatedPos.y = Mathf.Clamp(finalCalculatedPos.y, PLAYABLE_Y_MIN, PLAYABLE_Y_MAX);
            finalCalculatedPos.z = PLAYABLE_Z;

            return finalCalculatedPos;
        }

        return defaultTargetPos;
    }

    private Transform FindAppropriateSpawner(NoteInfo currentNote)
    {
        if (spawners == null || spawners.Count == 0)
        {
            Debug.LogError("SpawnerSelector: 스포너가 할당되지 않았습니다! 최소한 하나의 스포너를 할당해야 합니다.", this);
            return null;
        }

        // === 핵심 수정: 스포너가 하나일 경우 항상 첫 번째 스포너를 반환 ===
        if (spawners.Count == 1)
        {
            _lastUsedSpawnerIndex = 0; // 항상 0번 스포너 사용
            return spawners[0];
        }

        // 스포너가 여러 개일 경우에만 기존 로직을 따릅니다.
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