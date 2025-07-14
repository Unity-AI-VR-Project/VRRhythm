using System;
using UnityEngine;
using System.Collections.Generic;
using Define; // Define 네임스페이스가 프로젝트에 정의되어 있어야 합니다.
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
    public NoteDirection requiredDirection; // Define 네임스페이스의 enum으로 예상됩니다.

    public Vector3 calculatedTargetPos;

    public SaberNoteType NoteType; // Define 네임스페이스의 enum으로 예상됩니다.
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
    public Transform mainSpawner;

    [HideInInspector] public float NotePreSpawnBeats;

    private RootData _currentSongData;
    public List<NoteInfo> _allNotes;
    private int _nextNoteIndex = 0;

    private string _lastSpawnedNoteBand = null;
    private float _lastSpawnedNoteTime = -1.0f;

    // 마지막으로 계산된 목표 위치를 저장하여 다음 인접 노트 계산의 기준으로 사용
    private Vector3 _lastCalculatedTargetPos = Vector3.zero;
    private NoteDirection _lastAssignedDirection = NoteDirection.Up;
    private SaberNoteType _lastAssignedNoteType = SaberNoteType.Right;

    [Header("Playable Area Settings")]
    [Tooltip("노트가 나타날 플레이 가능 영역의 X 최소값")]
    public float PLAYABLE_X_MIN = -1f;
    [Tooltip("노트가 나타날 플레이 가능 영역의 X 최대값")]
    public float PLAYABLE_X_MAX = 1f;
    [Tooltip("노트가 나타날 플레이 가능 영역의 Y 최소값")]
    public float PLAYABLE_Y_MIN = 0.3f;
    [Tooltip("노트가 나타날 플레이 가능 영역의 Y 최대값")]
    public float PLAYABLE_Y_MAX = 1.1f;
    [Tooltip("노트가 나타날 플레이 가능 영역의 Z 위치 (판정선 Z)")]
    public float PLAYABLE_Z = 2f;

    [Header("Note Spawning Logic Settings")]
    [Tooltip("이전 노트와 현재 노트의 시간 차이가 이 값보다 작으면 인접 노트로 간주합니다. (단위: 초)")]
    public float copyNoteDataSecOffset = 0.120f;
    [Tooltip("인접 노트가 이전 노트로부터 이동할 거리 (단위: 유니티 단위)")]
    public float moveAmount = 0.3f; // 이 값을 조절하여 노트 간의 간격을 조절하세요.

    void Awake()
    {
        LoadMusicData();
        if (mainSpawner == null)
        {
            Debug.LogError("SpawnerSelector: mainSpawner가 할당되지 않았습니다. 스포너 Transform을 할당해주세요.", this);
            enabled = false;
        }
    }

    private void Start()
    {
        if (_currentSongData != null && _currentSongData.metadata != null && _currentSongData.metadata.tempo != 0)
        {
            // BPM에 따라 copyNoteDataSecOffset을 동적으로 계산합니다.
            // 0.5비트 간격에서 약간의 여유를 줍니다.
            copyNoteDataSecOffset = (60 / _currentSongData.metadata.tempo / 2) - 0.01f;
            // 최소값을 설정하여 너무 작아지는 것을 방지
            if (copyNoteDataSecOffset < 0.05f) copyNoteDataSecOffset = 0.05f;
        }
        else
        {
            Debug.LogWarning("SpawnerSelector: 템포 데이터가 없거나 0이어서 copyNoteDataSecOffset을 기본값으로 설정합니다.", this);
            copyNoteDataSecOffset = 0.125f; // 기본값 유지
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

    /// <summary>
    /// 로드된 노트 데이터에 방향과 손 타입을 할당합니다.
    /// 인접 노트 (시간 간격이 가까운 노트)는 이전 노트의 방향과 손 타입을 따릅니다.
    /// </summary>
    private void AssignDirectionsToNotes()
    {
        // NoteDirection.Any를 여기서 제거하여 스폰될 노트는 항상 특정 방향을 가지도록 합니다.
        NoteDirection[] possibleDirections = {
            NoteDirection.Up,
            NoteDirection.Down,
            NoteDirection.Left,
            NoteDirection.Right
            // NoteDirection.Any // 스폰되는 노트의 시각적 방향으로는 사용하지 않습니다.
        };

        SaberNoteType[] possibleHands = {
            SaberNoteType.Left,
            SaberNoteType.Right
        };

        float lastNoteTime = -1.0f;

        for (int i = 0; i < _allNotes.Count; i++)
        {
            NoteInfo currentNote = _allNotes[i];

            // 이전 노트와의 시간 간격이 copyNoteDataSecOffset 이하인지 확인
            bool isCloseToLastNote = (lastNoteTime != -1.0f && (currentNote.time - lastNoteTime <= copyNoteDataSecOffset));

            lastNoteTime = currentNote.time;

            if (isCloseToLastNote)
            {
                // 인접 노트일 경우, 이전 노트와 동일한 방향 및 손 타입 할당
                currentNote.requiredDirection = _lastAssignedDirection;
                currentNote.NoteType = _lastAssignedNoteType;
            }
            else
            {
                // 인접 노트가 아닐 경우, 무작위 방향 및 손 타입 할당
                // 이전과 다른 방향/손 타입을 강제하는 로직을 추가할 수도 있습니다.
                int randomDirectionIndex = Random.Range(0, possibleDirections.Length);
                currentNote.requiredDirection = possibleDirections[randomDirectionIndex];

                int randomHandIndex = Random.Range(0, possibleHands.Length);
                currentNote.NoteType = possibleHands[randomHandIndex];
            }

            // struct이므로 변경사항을 반영하기 위해 다시 할당
            _allNotes[i] = currentNote;

            // 다음 노트를 위해 현재 할당된 방향/타입 업데이트
            _lastAssignedDirection = currentNote.requiredDirection;
            _lastAssignedNoteType = currentNote.NoteType;
        }
    }

    /// <summary>
    /// 현재 시간에 스폰할 노트 정보를 반환하고, 다음 노트 인덱스를 업데이트합니다.
    /// </summary>
    /// <param name="currentTime">현재 게임 시간입니다.</param>
    /// <returns>스폰 정보 번들 또는 null을 반환합니다.</returns>
    public SpawnInfoBundle? GetNoteAndSpawnerForCurrentTime(float currentTime)
    {
        if (_allNotes == null || _currentSongData == null || _currentSongData.metadata == null || _nextNoteIndex >= _allNotes.Count)
        {
            return null; // 더 이상 스폰할 노트가 없거나 데이터가 유효하지 않음
        }

        NoteInfo nextNote = _allNotes[_nextNoteIndex];

        float tempo = _currentSongData.metadata.tempo;
        if (tempo == 0f)
        {
            Debug.LogWarning("SpawnerSelector: 템포가 유효하지 않습니다 (0). 노트 스폰 시간을 계산할 수 없습니다.", this);
            return null;
        }

        // 노트가 실제 타겟 위치에 도달하기 전에 미리 스폰되는 시간 계산
        float preSpawnTime = NotePreSpawnBeats * (60f / tempo);

        float noteAbsoluteTime = nextNote.time;

        // 현재 시간이 노트의 스폰 시점보다 크거나 같으면 스폰
        if (currentTime >= noteAbsoluteTime - preSpawnTime)
        {
            nextNote.calculatedTargetPos = CalculateTargetPosition(nextNote);
            _allNotes[_nextNoteIndex] = nextNote; // struct이므로 변경사항 반영

            if (mainSpawner == null)
            {
                Debug.LogError("SpawnerSelector: mainSpawner가 할당되지 않아 노트를 스폰할 수 없습니다.", this);
                return null;
            }

            SpawnInfoBundle bundle = new SpawnInfoBundle
            {
                NoteData = nextNote,
                SpawnerTransform = mainSpawner, // 항상 mainSpawner 사용
                Tempo = tempo,
                CalculatedTargetPos = nextNote.calculatedTargetPos
            };

            _nextNoteIndex++;
            _lastSpawnedNoteBand = nextNote.band;
            _lastSpawnedNoteTime = nextNote.time;
            // 핵심: _lastCalculatedTargetPos를 항상 실제 계산된 목표 위치로 업데이트합니다.
            _lastCalculatedTargetPos = nextNote.calculatedTargetPos;

            return bundle;
        }

        return null; // 아직 스폰할 시간이 아님
    }

/// <summary>
/// 노트의 최종 목표 위치를 계산합니다. 인접 노트 여부와 방향에 따라 위치를 조정합니다.
/// </summary>
private Vector3 CalculateTargetPosition(NoteInfo currentNote)
{
    float currentPlayableXMin = PLAYABLE_X_MIN;
    float currentPlayableXMax = PLAYABLE_X_MAX;

    // 왼손/오른손 노트에 따라 X축 플레이 가능 영역 조정
    if (currentNote.NoteType == SaberNoteType.Left)
    {
        // 왼손 노트는 플레이 가능한 영역을 왼쪽 절반으로 제한
        currentPlayableXMax = (PLAYABLE_X_MIN + PLAYABLE_X_MAX) / 2f;
    }
    else // Right (SaberNoteType.Right)
    {
        // 오른손 노트는 플레이 가능한 영역을 오른쪽 절반으로 제한
        currentPlayableXMin = (PLAYABLE_X_MIN + PLAYABLE_X_MAX) / 2f;
    }

    // 인접 노트 로직: 이전 노트와의 시간 간격이 충분히 짧은 경우
    if (_lastSpawnedNoteTime != -1.0f && (currentNote.time - _lastSpawnedNoteTime <= copyNoteDataSecOffset))
    {
        Vector3 basePos = _lastCalculatedTargetPos; // 이전에 스폰된 노트의 목표 위치를 기준
        Vector3 finalPos = basePos; // 최종 위치

        // 각 방향별로 이동을 시도하고 경계를 고려하여 위치 결정
        switch (currentNote.requiredDirection)
        {
            case NoteDirection.Up:
                // 위로 이동 가능한 최대치를 넘지 않도록
                finalPos.y = Mathf.Min(basePos.y + moveAmount, PLAYABLE_Y_MAX);
                // 만약 위로 이동하려 했으나 경계에 막혔다면, X축으로 약간 이동을 시도하여 겹침 방지
                if (Mathf.Approximately(finalPos.y, PLAYABLE_Y_MAX) && Mathf.Approximately(finalPos.y, basePos.y))
                {
                    // 위로 더 이상 갈 수 없는데, 이전과 위치가 거의 같다면 (겹친다면)
                    // 현재 손 영역 내에서 X축으로 랜덤 이동 (음수 또는 양수)
                    finalPos.x = Mathf.Clamp(basePos.x + Random.Range(-moveAmount, moveAmount), currentPlayableXMin, currentPlayableXMax);
                }
                break;
            case NoteDirection.Down:
                // 아래로 이동 가능한 최소치를 넘지 않도록
                finalPos.y = Mathf.Max(basePos.y - moveAmount, PLAYABLE_Y_MIN);
                if (Mathf.Approximately(finalPos.y, PLAYABLE_Y_MIN) && Mathf.Approximately(finalPos.y, basePos.y))
                {
                    finalPos.x = Mathf.Clamp(basePos.x + Random.Range(-moveAmount, moveAmount), currentPlayableXMin, currentPlayableXMax);
                }
                break;
            case NoteDirection.Left:
                // 왼쪽으로 이동 가능한 최소치를 넘지 않도록
                finalPos.x = Mathf.Max(basePos.x - moveAmount, currentPlayableXMin);
                if (Mathf.Approximately(finalPos.x, currentPlayableXMin) && Mathf.Approximately(finalPos.x, basePos.x))
                {
                    // 왼쪽으로 더 이상 갈 수 없는데, 이전과 위치가 거의 같다면 (겹친다면)
                    // Y축으로 랜덤 이동
                    finalPos.y = Mathf.Clamp(basePos.y + Random.Range(-moveAmount, moveAmount), PLAYABLE_Y_MIN, PLAYABLE_Y_MAX);
                }
                break;
            case NoteDirection.Right:
                // 오른쪽으로 이동 가능한 최대치를 넘지 않도록
                finalPos.x = Mathf.Min(basePos.x + moveAmount, currentPlayableXMax);
                if (Mathf.Approximately(finalPos.x, currentPlayableXMax) && Mathf.Approximately(finalPos.x, basePos.x))
                {
                    finalPos.y = Mathf.Clamp(basePos.y + Random.Range(-moveAmount, moveAmount), PLAYABLE_Y_MIN, PLAYABLE_Y_MAX);
                }
                break;
        }

        finalPos.z = PLAYABLE_Z; // Z축은 고정

        // 최종 검증: 이전 노트와 최소 거리 이상 떨어져 있는지 확인 (안전 장치)
        // 만약 위 로직으로도 겹침이 발생한다면, 이 부분에서 강제로 조절합니다.
        // 하지만 이 단계까지 왔다면 이미 문제가 크게 발생하고 있는 것이므로,
        // 위 switch 문 로직을 더 견고하게 만드는 것이 중요합니다.
        float minRequiredDistance = moveAmount * 0.8f; // 노트 크기를 고려한 최소 분리 거리
        if (Vector3.Distance(finalPos, basePos) < minRequiredDistance)
        {
            // 원하는 방향으로 충분히 이동하지 못했을 경우
            // 다른 축으로 랜덤 오프셋을 추가하여 겹침을 방지합니다.
            // 이때 랜덤 오프셋은 대각선 이동으로 보이지 않도록 주의
            Vector3 fallbackOffset = Vector3.zero;
            if (Mathf.Abs(finalPos.x - basePos.x) < 0.01f) // X축 이동이 거의 없었다면 Y축으로 이동
            {
                fallbackOffset.y = Random.Range(PLAYABLE_Y_MIN, PLAYABLE_Y_MAX); // Y축 전체에서 랜덤으로 이동
                if (fallbackOffset.y > basePos.y) fallbackOffset.y = basePos.y + minRequiredDistance;
                else fallbackOffset.y = basePos.y - minRequiredDistance;

                fallbackOffset.y = Mathf.Clamp(fallbackOffset.y, PLAYABLE_Y_MIN, PLAYABLE_Y_MAX);
                finalPos.y = fallbackOffset.y; // Y 위치를 대체
            }
            else if (Mathf.Abs(finalPos.y - basePos.y) < 0.01f) // Y축 이동이 거의 없었다면 X축으로 이동
            {
                fallbackOffset.x = Random.Range(currentPlayableXMin, currentPlayableXMax); // X축 전체에서 랜덤으로 이동
                if (fallbackOffset.x > basePos.x) fallbackOffset.x = basePos.x + minRequiredDistance;
                else fallbackOffset.x = basePos.x - minRequiredDistance;

                fallbackOffset.x = Mathf.Clamp(fallbackOffset.x, currentPlayableXMin, currentPlayableXMax);
                finalPos.x = fallbackOffset.x; // X 위치를 대체
            }
            // else: 대각선으로 움직였다면, 이 부분은 크게 문제가 되지 않을 수 있음.
            // 하지만 이전 코드에서 대각선 문제가 있었다고 했으므로, 최대한 직선 이동을 지향합니다.
        }

        return finalPos;
    }
    else // 인접 노트가 아닐 경우 (시간 간격이 충분히 멀 때)
    {
        // 이전 노트와는 완전히 독립적인 새로운 위치를 생성합니다.
        // 이 경우에도 이전 노트와의 최소 거리를 유지하는 것이 좋습니다.
        Vector3 targetPos = Vector3.zero;
        float minInitialSeparation = moveAmount * 2.5f; // 새로운 시작 노트는 이전 노트와 충분히 떨어져 있어야 함
        
        int maxAttempts = 10; // 무한 루프 방지를 위한 최대 시도 횟수
        for (int i = 0; i < maxAttempts; i++)
        {
            targetPos = new Vector3(
                Random.Range(currentPlayableXMin, currentPlayableXMax),
                Random.Range(PLAYABLE_Y_MIN, PLAYABLE_Y_MAX),
                PLAYABLE_Z
            );

            // 이전 노트가 있다면, 새로 생성된 랜덤 위치가 이전 노트와 너무 가깝지 않은지 확인
            if (_lastCalculatedTargetPos != Vector3.zero && 
                Vector3.Distance(targetPos, _lastCalculatedTargetPos) < minInitialSeparation)
            {
                continue; // 너무 가깝다면 다시 시도
            }
            else
            {
                break; // 충분히 떨어져 있다면 루프 종료
            }
        }
        return targetPos;
    }
}

    /// <summary>
    /// 현재 곡의 템포를 반환합니다.
    /// </summary>
    public float GetTempo()
    {
        if (_currentSongData != null && _currentSongData.metadata != null)
        {
            return _currentSongData.metadata.tempo;
        }
        return 120f; // 기본값
    }

    /// <summary>
    /// 유니티 에디터에서 디버깅을 위해 Gizmos를 그립니다.
    /// 플레이 가능 영역과 마지막으로 계산된 노트의 목표 위치를 시각화합니다.
    /// </summary>
    void OnDrawGizmos()
    {
        // 플레이 가능 영역 시각화 (녹색)
        Gizmos.color = Color.green;
        Vector3 minBounds = new Vector3(PLAYABLE_X_MIN, PLAYABLE_Y_MIN, PLAYABLE_Z);
        Vector3 maxBounds = new Vector3(PLAYABLE_X_MAX, PLAYABLE_Y_MAX, PLAYABLE_Z);
        Vector3 center = (minBounds + maxBounds) / 2f;
        Vector3 size = maxBounds - minBounds;
        Gizmos.DrawWireCube(center, size);

        // X축 손 영역 분할 시각화 (하늘색)
        Gizmos.color = Color.cyan;
        float centerX = (PLAYABLE_X_MIN + PLAYABLE_X_MAX) / 2f;
        Vector3 leftHandLineStart = new Vector3(centerX, PLAYABLE_Y_MIN, PLAYABLE_Z);
        Vector3 leftHandLineEnd = new Vector3(centerX, PLAYABLE_Y_MAX, PLAYABLE_Z);
        Gizmos.DrawLine(leftHandLineStart, leftHandLineEnd);

        // 마지막 계산된 타겟 위치 시각화 (빨간색 구)
        if (_lastCalculatedTargetPos != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_lastCalculatedTargetPos, 0.1f); // 작은 구로 표시
        }
    }
}