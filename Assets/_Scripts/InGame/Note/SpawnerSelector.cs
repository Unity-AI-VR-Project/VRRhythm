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
    [Tooltip("인접 노트가 PLAYABLE 영역 밖으로 벗어날 수 있는 최대 허용 거리")]
    public float outOfBoundsTolerance = 0.2f;

    [Header("Note Spawning Logic Settings")]
    [Tooltip("이전 노트와 현재 노트의 시간 차이가 이 값보다 작으면 인접 노트로 간주합니다. (단위: 초)")]
    public float copyNoteDataSecOffset = 0.120f;
    [Tooltip("인접 노트가 이전 노트로부터 이동할 거리 (단위: 유니티 단위)")]
    public float moveAmount = 0.3f;
    [Tooltip("인접 노트가 최소한 떨어져 있어야 하는 거리 (겹침 방지용, 노트 반지름의 2배 정도)")]
    public float minNoteSeparationDistance = 0.25f;
    [Tooltip("0.5박자에 해당하는 시간 간격 (템포에 따라 동적으로 계산됨)")]
    private float _halfBeatDuration;

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
            // 0.5박자 시간 계산: (60 / BPM) * 0.5
            _halfBeatDuration = (60f / _currentSongData.metadata.tempo) * 1.8f;

            // copyNoteDataSecOffset 계산 (이전과 동일)
            copyNoteDataSecOffset = (_halfBeatDuration / 2) - 0.01f; // 0.25박자보다 약간 작게
            if (copyNoteDataSecOffset < 0.05f) copyNoteDataSecOffset = 0.05f;

            Debug.Log($"SpawnerSelector: 템포 {_currentSongData.metadata.tempo}, 0.5박자 시간: {_halfBeatDuration:F3}s, 인접 노트 시간 임계치: {copyNoteDataSecOffset:F3}s", this);

        }
        else
        {
            Debug.LogWarning("SpawnerSelector: 템포 데이터가 없거나 0이어서 copyNoteDataSecOffset과 _halfBeatDuration을 기본값으로 설정합니다.", this);
            copyNoteDataSecOffset = 0.125f; // 기본값 유지
            _halfBeatDuration = 0.25f; // 기본 템포 120BPM 기준 0.5박자 (60/120 * 0.5 = 0.25)
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
    /// 0.5박자 이내의 간격이 있는 노트는 이전 노트와 다른 손 타입을 갖도록 합니다.
    /// </summary>
    private void AssignDirectionsToNotes()
    {
        NoteDirection[] possibleDirections = {
            NoteDirection.Up,
            NoteDirection.Down,
            NoteDirection.Left,
            NoteDirection.Right
        };

        SaberNoteType[] possibleHands = {
            SaberNoteType.Left,
            SaberNoteType.Right
        };

        float lastNoteTime = -1.0f;

        for (int i = 0; i < _allNotes.Count; i++)
        {
            NoteInfo currentNote = _allNotes[i];

            // 이전 노트로부터의 시간 간격 계산
            float timeSinceLastNote = (lastNoteTime != -1.0f) ? (currentNote.time - lastNoteTime) : float.MaxValue;

            // _halfBeatDuration; // Start()에서 이미 계산되어 있음

            // 첫 노트이거나, 이전 노트로부터 충분히 멀리 떨어진 경우 (0.5박자 이상)
            if (timeSinceLastNote > _halfBeatDuration || lastNoteTime == -1.0f)
            {
                // 완전히 새로운 노트로 간주하고 랜덤 방향/손 할당
                int randomDirectionIndex = Random.Range(0, possibleDirections.Length);
                currentNote.requiredDirection = possibleDirections[randomDirectionIndex];

                int randomHandIndex = Random.Range(0, possibleHands.Length);
                currentNote.NoteType = possibleHands[randomHandIndex];
            }
            // copyNoteDataSecOffset 보다 크지만 0.5박자 이하로 떨어진 경우
            else if (timeSinceLastNote > copyNoteDataSecOffset && timeSinceLastNote <= _halfBeatDuration)
            {
                // 이전 노트와 다른 손 타입을 강제 할당
                currentNote.NoteType = (_lastAssignedNoteType == SaberNoteType.Left) ? SaberNoteType.Right : SaberNoteType.Left;
                // 방향은 여전히 랜덤하게 할당 (양손 번갈아 치는 상황을 고려)
                int randomDirectionIndex = Random.Range(0, possibleDirections.Length);
                currentNote.requiredDirection = possibleDirections[randomDirectionIndex];
            }
            // copyNoteDataSecOffset 이내로 가까운 인접 노트인 경우
            else // timeSinceLastNote <= copyNoteDataSecOffset
            {
                // 이전 노트의 방향과 손 타입을 그대로 따름
                currentNote.requiredDirection = _lastAssignedDirection;
                currentNote.NoteType = _lastAssignedNoteType;
            }

            _allNotes[i] = currentNote; // 구조체이므로 변경사항을 리스트에 다시 할당

            // 다음 노트를 위해 현재 노트의 정보 저장
            _lastAssignedDirection = currentNote.requiredDirection;
            _lastAssignedNoteType = currentNote.NoteType;
            lastNoteTime = currentNote.time;
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

        float preSpawnTime = NotePreSpawnBeats * (60f / tempo);
        float noteAbsoluteTime = nextNote.time;

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
                SpawnerTransform = mainSpawner,
                Tempo = tempo,
                CalculatedTargetPos = nextNote.calculatedTargetPos
            };

            _nextNoteIndex++;
            _lastSpawnedNoteBand = nextNote.band;
            _lastSpawnedNoteTime = nextNote.time;
            _lastCalculatedTargetPos = nextNote.calculatedTargetPos; // 최종 계산된 위치를 업데이트

            return bundle;
        }

        return null; // 아직 스폰할 시간이 아님
    }

    /// <summary>
    /// 노트의 최종 목표 위치를 계산합니다. 인접 노트 여부와 방향에 따라 위치를 조정합니다.
    /// 인접 노트는 PLAYABLE 범위를 벗어날 수 있지만, outOfBoundsTolerance 이내로 제한됩니다.
    /// </summary>
    private Vector3 CalculateTargetPosition(NoteInfo currentNote)
    {
        // === 변경된 부분 시작 ===
        // 각 손에 대한 플레이 가능 X축 범위를 명확히 정의합니다.
        // PLAYABLE_X_MIN과 PLAYABLE_X_MAX는 전체 플레이 가능 영역의 X축입니다.
        // 중앙값을 기준으로 각 손의 영역을 나눕니다.
        float midX = (PLAYABLE_X_MIN + PLAYABLE_X_MAX) / 2f;

        float targetXMin, targetXMax;

        if (currentNote.NoteType == SaberNoteType.Left)
        {
            // 왼손 노트는 중앙선부터 왼쪽까지
            targetXMin = PLAYABLE_X_MIN;
            targetXMax = midX;
        }
        else // SaberNoteType.Right
        {
            // 오른손 노트는 중앙선부터 오른쪽까지
            targetXMin = midX;
            targetXMax = PLAYABLE_X_MAX;
        }
        // === 변경된 부분 끝 ===


        // 확장된 클램프 범위 계산 (PLAYABLE + outOfBoundsTolerance)
        // 이 확장 범위도 이제 targetXMin/Max를 기반으로 합니다.
        float extendedXMin = targetXMin - outOfBoundsTolerance;
        float extendedXMax = targetXMax + outOfBoundsTolerance;
        float extendedYMin = PLAYABLE_Y_MIN - outOfBoundsTolerance;
        float extendedYMax = PLAYABLE_Y_MAX + outOfBoundsTolerance;


        // 인접 노트 로직
        if (_lastSpawnedNoteTime != -1.0f && (currentNote.time - _lastSpawnedNoteTime <= copyNoteDataSecOffset))
        {
            Vector3 basePos = _lastCalculatedTargetPos; // 이전에 스폰된 노트의 목표 위치
            Vector3 desiredPos = basePos; // 희망하는 이동 방향으로의 위치

            // 1. 주축 방향으로 moveAmount만큼 이동 시도
            switch (currentNote.requiredDirection)
            {
                case NoteDirection.Up: desiredPos.y += moveAmount; break;
                case NoteDirection.Down: desiredPos.y -= moveAmount; break;
                case NoteDirection.Left: desiredPos.x -= moveAmount; break;
                case NoteDirection.Right: desiredPos.x += moveAmount; break;
            }

            // 2. 확장된 경계 내로 클램프
            Vector3 clampedToExtendedBoundsPos = new Vector3(
                Mathf.Clamp(desiredPos.x, extendedXMin, extendedXMax),
                Mathf.Clamp(desiredPos.y, extendedYMin, extendedYMax),
                PLAYABLE_Z
            );

            // 3. 이전 노트와의 겹침 확인 (minNoteSeparationDistance 미확보 시)
            if (Vector3.Distance(clampedToExtendedBoundsPos, basePos) < minNoteSeparationDistance)
            {
                Debug.LogWarning($"SpawnerSelector: 인접 노트 겹침 발생. Smart Placement (outOfBounds 허용) 시도. Base: {basePos}, Desired: {desiredPos}, ClampedExtended: {clampedToExtendedBoundsPos}");

                Vector3 finalPos = basePos; // 시작은 이전 노트 위치에서

                // 이전 노트와 최소 분리 거리를 강제로 확보하도록 조정
                // 이때, 경계는 무시하고 오직 이전 노트와의 분리만 목표로 합니다.
                // 이후 다시 확장된 경계 내로 클램프합니다.
                switch (currentNote.requiredDirection)
                {
                    case NoteDirection.Up:
                        finalPos.y = basePos.y + minNoteSeparationDistance;
                        break;
                    case NoteDirection.Down:
                        finalPos.y = basePos.y - minNoteSeparationDistance;
                        break;
                    case NoteDirection.Left:
                        finalPos.x = basePos.x - minNoteSeparationDistance;
                        break;
                    case NoteDirection.Right:
                        finalPos.x = basePos.x + minNoteSeparationDistance;
                        break;
                }

                // 최종적으로 계산된 위치를 확장된 영역 내에서 다시 클램프
                finalPos.x = Mathf.Clamp(finalPos.x, extendedXMin, extendedXMax);
                finalPos.y = Mathf.Clamp(finalPos.y, extendedYMin, extendedYMax);
                finalPos.z = PLAYABLE_Z;

                // Debug.Log($"Smart Placement Result (Extended): {finalPos}");
                return finalPos;
            }

            // 겹치지 않으면 확장된 경계 내의 위치 반환
            return clampedToExtendedBoundsPos;
        }
        else // 인접 노트가 아닐 경우 (시간 간격이 충분히 멀 때)
        {
            Vector3 targetPos = Vector3.zero;
            // 인접하지 않은 노트는 이전 노트와 일정 거리 이상 떨어지도록 시도
            float minInitialSeparation = moveAmount * 2.5f;

            int maxAttempts = 10;
            for (int i = 0; i < maxAttempts; i++)
            {
                targetPos = new Vector3(
                    // 변경된 부분: currentPlayableXMin, currentPlayableXMax 대신 targetXMin, targetXMax 사용
                    Random.Range(targetXMin, targetXMax),
                    Random.Range(PLAYABLE_Y_MIN, PLAYABLE_Y_MAX),
                    PLAYABLE_Z
                );

                if (_lastCalculatedTargetPos != Vector3.zero &&
                    Vector3.Distance(targetPos, _lastCalculatedTargetPos) < minInitialSeparation)
                {
                    continue; // 겹치면 다시 시도
                }
                else
                {
                    break; // 겹치지 않으면 종료
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
        // PLAYABLE 영역 시각화 (녹색)
        Gizmos.color = Color.green;
        Vector3 minBounds = new Vector3(PLAYABLE_X_MIN, PLAYABLE_Y_MIN, PLAYABLE_Z);
        Vector3 maxBounds = new Vector3(PLAYABLE_X_MAX, PLAYABLE_Y_MAX, PLAYABLE_Z);
        Vector3 center = (minBounds + maxBounds) / 2f;
        Vector3 size = maxBounds - minBounds;
        Gizmos.DrawWireCube(center, size);

        // 확장된 허용 범위 시각화 (노란색)
        // OnDrawGizmos에서는 실제 노트의 NoteType을 알 수 없으므로,
        // 전체 PLAYABLE 영역을 기준으로 확장 범위를 그립니다.
        Gizmos.color = Color.yellow;
        Vector3 extendedMinBoundsGlobal = new Vector3(PLAYABLE_X_MIN - outOfBoundsTolerance, PLAYABLE_Y_MIN - outOfBoundsTolerance, PLAYABLE_Z);
        Vector3 extendedMaxBoundsGlobal = new Vector3(PLAYABLE_X_MAX + outOfBoundsTolerance, PLAYABLE_Y_MAX + outOfBoundsTolerance, PLAYABLE_Z);
        Vector3 extendedCenterGlobal = (extendedMinBoundsGlobal + extendedMaxBoundsGlobal) / 2f;
        Vector3 extendedSizeGlobal = extendedMaxBoundsGlobal - extendedMinBoundsGlobal;
        Gizmos.DrawWireCube(extendedCenterGlobal, extendedSizeGlobal);


        // X축 손 영역 분할 시각화 (하늘색) - 중앙선
        Gizmos.color = Color.cyan;
        float centerX = (PLAYABLE_X_MIN + PLAYABLE_X_MAX) / 2f;
        Vector3 leftHandLineStart = new Vector3(centerX, PLAYABLE_Y_MIN, PLAYABLE_Z);
        Vector3 leftHandLineEnd = new Vector3(centerX, PLAYABLE_Y_MAX, PLAYABLE_Z);
        Gizmos.DrawLine(leftHandLineStart, leftHandLineEnd);

        // 마지막 계산된 타겟 위치 시각화 (빨간색 구)
        if (_lastCalculatedTargetPos != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_lastCalculatedTargetPos, 0.1f);
        }
    }
}