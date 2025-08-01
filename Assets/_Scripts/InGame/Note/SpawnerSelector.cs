using System;
using UnityEngine;
using System.Collections.Generic;
using Define; // Define 네임스페이스가 프로젝트에 정의되어 있어야 합니다.
using Random = UnityEngine.Random;

// RootData, Metadata, BeatData, NoteInfo, SpawnInfoBundle은 
// JSON 직렬화 및 노트 데이터 구조를 정의합니다.
[Serializable]
public class RootData
{
    public Metadata metadata;
    public List<BeatData> beats;
}

[Serializable]
public class Metadata
{
    public float tempo;
    public string time_resolution_unit;
    public string band_group;
}

[Serializable]
public class BeatData
{
    public int beat_index;
    public float beat_time;
    public List<NoteInfo> notes;
}

[Serializable]
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
    public Transform mainSpawner; // 노트가 스폰될 시작 지점 Transform

    [HideInInspector] public float NotePreSpawnBeats; // MusicSynchronizer에서 설정될 노트 프리 스폰 시간 (비트 단위)

    private RootData _currentSongData; // 현재 플레이 중인 곡의 전체 노트 데이터
    public List<NoteInfo> _allNotes; // 모든 노트 정보 (정렬 및 방향 할당 완료)
    private int _nextNoteIndex = 0; // 다음에 스폰할 노트의 인덱스

    // 이전 노트 스폰 정보 (인접 노트 계산에 사용)
    private string _lastSpawnedNoteBand = null;
    private float _lastSpawnedNoteTime = -1.0f;
    private Vector3 _lastCalculatedTargetPos = Vector3.zero; // 마지막으로 계산된 목표 위치
    private NoteDirection _lastAssignedDirection = NoteDirection.Up; // 마지막으로 할당된 노트 방향
    private SaberNoteType _lastAssignedNoteType = SaberNoteType.Right; // 마지막으로 할당된 노트 손 타입

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
    public float outOfBoundsTolerance = 0.2f; // 이 값은 인접 노트의 직접적인 클램핑에 더 이상 사용되지 않음.

    [Header("Note Spawning Logic Settings")]
    [Tooltip("이전 노트와 현재 노트의 시간 차이가 이 값보다 작으면 인접 노트로 간주합니다. (단위: 초)")]
    public float copyNoteDataSecOffset = 0.120f; // BPM 기반으로 Start()에서 재계산됨
    [Tooltip("인접 노트가 이전 노트로부터 이동할 거리 (단위: 유니티 단위)")]
    public float moveAmount = 0.3f;
    [Tooltip("인접 노트가 최소한 떨어져 있어야 하는 거리 (겹침 방지용, 이 값은 이제 사용되지 않음)")]
    public float minNoteSeparationDistance = 0.25f; // 이 값은 이제 겹침 방지에 직접 사용되지 않음
    [Tooltip("0.5박자에 해당하는 시간 간격 (템포에 따라 동적으로 계산됨)")]
    private float _halfBeatDuration;

    public Transform headGear; // 플레이어 헤드기어 Transform

    void Awake()
    {
        if (mainSpawner == null)
        {
            Debug.LogError("SpawnerSelector: mainSpawner가 할당되지 않았습니다. 스포너 Transform을 할당해주세요.", this);
            enabled = false;
            return;
        }
        // DataManager에서 음악 데이터를 로드합니다.
        LoadMusicData();
    }

    private void Start()
    {
        if (_currentSongData != null && _currentSongData.metadata != null && _currentSongData.metadata.tempo != 0)
        {
            float tempo = _currentSongData.metadata.tempo;
            // 0.5박자 시간 계산: (60 / BPM) * 0.5f로 수정
            _halfBeatDuration = (60f / tempo) * 0.5f;

            // copyNoteDataSecOffset 계산 (0.25박자 간격으로 수정)
            copyNoteDataSecOffset = (60f / tempo) * 0.25f;
            if (copyNoteDataSecOffset < 0.05f) copyNoteDataSecOffset = 0.05f; // 최소값 보장

            Debug.Log($"SpawnerSelector: 템포 {tempo}, 0.5박자 시간: {_halfBeatDuration:F3}s, 인접 노트 시간 임계치 (0.25박자): {copyNoteDataSecOffset:F3}s", this);
        }
        else
        {
            Debug.LogWarning("SpawnerSelector: 템포 데이터가 없거나 0이어서 copyNoteDataSecOffset과 _halfBeatDuration을 기본값으로 설정합니다.", this);
            copyNoteDataSecOffset = 0.125f; // 기본값 유지 (약 120BPM 기준 0.25박자)
            _halfBeatDuration = 0.25f; // 기본 템포 120BPM 기준 0.5박자
        }

        // 게임 시작 시 헤드기어 위치를 기준으로 플레이 가능 Y 범위 초기 설정 (선택 사항)
        // 만약 게임 중에도 유동적으로 변하길 원한다면 Update()에만 유지해도 됩니다.
        if (headGear != null)
        {
            PLAYABLE_Y_MAX = headGear.position.y + 0.5f;
            PLAYABLE_Y_MIN = headGear.position.y - 0.5f;
        }
    }

    private void Update()
    {
        // 게임 중에도 플레이어 시야에 따라 노트 Y 스폰 위치를 유동적으로 맞추려면 사용
        if (headGear != null)
        {
            PLAYABLE_Y_MAX = headGear.position.y + 0.2f;
            PLAYABLE_Y_MIN = headGear.position.y - 0.2f;
        }
    }

    /// <summary>
    /// GameManager의 DataManager에서 선택된 음악의 RootData를 로드하고 노트를 정렬합니다.
    /// </summary>
    private void LoadMusicData()
    {
        try
        {
            // GameManager.Instance와 DataManager가 초기화되어 있고, selectedMusicNumber가 올바른 인덱스를 가리키는지 확인해야 합니다.
            _currentSongData = GameManager.Instance.dataManager.musicRootDatas[GameManager.Instance.dataManager.selectedMusicNumber];

            if (_currentSongData == null || _currentSongData.metadata == null || _currentSongData.beats == null)
            {
                Debug.LogError("SpawnerSelector: JSON 데이터를 파싱하는 데 실패했습니다. JSON 파일의 형식을 확인하거나 DataManager 초기화 상태를 확인하세요.", this);
                enabled = false;
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
            // 노트들을 시간 순서대로 정렬합니다.
            _allNotes.Sort((n1, n2) => n1.time.CompareTo(n2.time));

            AssignDirectionsToNotes(); // 방향 및 손 타입 할당 로직 호출

            Debug.Log($"SpawnerSelector: 음악 데이터 로드 성공: '{_currentSongData.metadata.band_group}' (BPM: {_currentSongData.metadata.tempo}) - 총 {_allNotes.Count}개 노트.", this);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SpawnerSelector: JSON 파일 로드/파싱 오류: {e.Message}. DataManager의 JSON 파일 내용을 확인하세요.", this);
            enabled = false;
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
            NoteDirection.Right,
            //NoteDirection.Any // 'Any' 방향도 고려한다면 추가
        };

        SaberNoteType[] possibleHands = {
            SaberNoteType.Left,
            SaberNoteType.Right
        };

        float lastNoteTime = -1.0f; // 이전 노트의 시간

        // AssignDirectionsToNotes 내에서만 사용될 임시 변수 (루프 내에서만 유효)
        NoteDirection tempLastAssignedDirection = NoteDirection.Up;
        SaberNoteType tempLastAssignedNoteType = SaberNoteType.Right;

        for (int i = 0; i < _allNotes.Count; i++)
        {
            NoteInfo currentNote = _allNotes[i];

            // 이전 노트로부터의 시간 간격 계산
            float timeSinceLastNote = (lastNoteTime != -1.0f) ? (currentNote.time - lastNoteTime) : float.MaxValue;

            // 첫 노트이거나, 이전 노트로부터 0.5박자 이상 멀리 떨어진 경우
            if (timeSinceLastNote > _halfBeatDuration || lastNoteTime == -1.0f)
            {
                // 완전히 새로운 노트로 간주하고 랜덤 방향/손 할당
                int randomDirectionIndex = Random.Range(0, possibleDirections.Length);
                currentNote.requiredDirection = possibleDirections[randomDirectionIndex];

                int randomHandIndex = Random.Range(0, possibleHands.Length);
                currentNote.NoteType = possibleHands[randomHandIndex];
            }
            // copyNoteDataSecOffset (0.25박자) 보다 크지만 0.5박자 이하로 떨어진 경우
            else if (timeSinceLastNote > copyNoteDataSecOffset && timeSinceLastNote <= _halfBeatDuration)
            {
                // 이전 노트와 다른 손 타입을 강제 할당 (양손 번갈아 치기 유도)
                currentNote.NoteType = (tempLastAssignedNoteType == SaberNoteType.Left) ? SaberNoteType.Right : SaberNoteType.Left;

                // 방향은 여전히 랜덤하게 할당 (양손 번갈아 치는 상황에 다양한 방향 부여)
                int randomDirectionIndex = Random.Range(0, possibleDirections.Length);
                currentNote.requiredDirection = possibleDirections[randomDirectionIndex];
            }
            // copyNoteDataSecOffset (0.25박자) 이내로 가까운 인접 노트인 경우
            else // timeSinceLastNote <= copyNoteDataSecOffset
            {
                // 이전 노트의 방향과 손 타입을 그대로 따름 (동일 손으로 연속해서 치는 패턴 유도)
                currentNote.requiredDirection = tempLastAssignedDirection;
                currentNote.NoteType = tempLastAssignedNoteType;
            }

            _allNotes[i] = currentNote; // NoteInfo는 struct이므로 변경사항을 리스트에 다시 할당해야 합니다.

            // 다음 노트를 위해 현재 노트의 정보 저장 (AssignDirectionsToNotes 내에서만 사용)
            tempLastAssignedDirection = currentNote.requiredDirection;
            tempLastAssignedNoteType = currentNote.NoteType;
            lastNoteTime = currentNote.time;
        }
    }

    /// <summary>
    /// 현재 시간에 스폰할 노트 정보를 반환하고, 다음 노트 인덱스를 업데이트합니다.
    /// 이 메서드는 MusicSynchronizer의 Update에서 호출됩니다.
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

        // NotePreSpawnBeats는 MusicSynchronizer에서 동적으로 설정됩니다.
        float preSpawnTime = NotePreSpawnBeats * (60f / tempo);
        float noteAbsoluteTime = nextNote.time;

        // 현재 시간이 노트 스폰 시간(노트 등장 시간 - 프리 스폰 시간)보다 크거나 같으면 스폰
        if (currentTime >= noteAbsoluteTime - preSpawnTime)
        {
            // 목표 위치 계산 및 노트 데이터에 저장
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

            _nextNoteIndex++; // 다음 노트로 인덱스 증가
            _lastSpawnedNoteBand = nextNote.band; // 마지막 스폰된 노트 정보 업데이트
            _lastSpawnedNoteTime = nextNote.time;
            _lastCalculatedTargetPos = nextNote.calculatedTargetPos; // 최종 계산된 위치를 업데이트
            // AssignDirectionsToNotes()의 다음 호출을 위한 정보도 업데이트
            _lastAssignedDirection = nextNote.requiredDirection;
            _lastAssignedNoteType = nextNote.NoteType;

            return bundle;
        }

        return null; // 아직 스폰할 시간이 아님
    }

    /// <summary>
    /// 노트의 최종 목표 위치를 계산합니다. 인접 노트 여부와 방향에 따라 위치를 조정합니다.
    /// 사용자 요청에 따라 인접 노트의 범위 제한 및 겹침 방지 로직이 제거되었습니다.
    /// </summary>
    private Vector3 CalculateTargetPosition(NoteInfo currentNote)
    {
        // 각 손에 대한 플레이 가능 X축 범위를 명확히 정의합니다.
        float midX = (PLAYABLE_X_MIN + PLAYABLE_X_MAX) / 2f;
        float targetXMin, targetXMax;

        if (currentNote.NoteType == SaberNoteType.Left)
        {
            targetXMin = PLAYABLE_X_MIN;
            targetXMax = midX;
        }
        else // SaberNoteType.Right
        {
            targetXMin = midX;
            targetXMax = PLAYABLE_X_MAX;
        }

        // 이 변수들은 이제 비인접 노트 클램핑에만 간접적으로 사용됩니다.
        // float extendedXMin = targetXMin - outOfBoundsTolerance;
        // float extendedXMax = targetXMax + outOfBoundsTolerance;
        // float extendedYMin = PLAYABLE_Y_MIN - outOfBoundsTolerance;
        // float extendedYMax = PLAYABLE_Y_MAX + outOfBoundsTolerance;

        Vector3 finalTargetPos;

        // 인접 노트 로직 (copyNoteDataSecOffset 이내의 시간 간격)
        // 사용자 요청에 따라 인접 노트 범위 제한 및 겹침 방지 로직 제거
        if (_lastSpawnedNoteTime != -1.0f && (currentNote.time - _lastSpawnedNoteTime <= copyNoteDataSecOffset))
        {
            Vector3 basePos = _lastCalculatedTargetPos; // 이전에 스폰된 노트의 목표 위치
            Vector3 desiredMoveDirection = Vector3.zero; // 원하는 이동 방향 단위 벡터

            switch (currentNote.requiredDirection)
            {
                case NoteDirection.Up: desiredMoveDirection = Vector3.up; break;
                case NoteDirection.Down: desiredMoveDirection = Vector3.down; break;
                case NoteDirection.Left: desiredMoveDirection = Vector3.left; break;
                case NoteDirection.Right: desiredMoveDirection = Vector3.right; break;
            }

            // 이전 노트의 위치에서 원하는 방향으로 moveAmount만큼 이동합니다.
            // 인접 노트에 대한 추가적인 범위 제한(클램핑)이나 겹침 방지 로직은 제거되었습니다.
            finalTargetPos = basePos + desiredMoveDirection * moveAmount;
            finalTargetPos.z = PLAYABLE_Z; // Z축은 판정선으로 고정

            return finalTargetPos;
        }
        else // 인접 노트가 아닐 경우 (시간 간격이 충분히 멀 때)
        {
            // 사용자 요청에 따라 겹침 방지 로직(이전 노트와의 최소 분리 거리 체크) 제거.
            // 노트 생성 범위 (PLAYABLE_X/Y) 내에서 완전히 무작위 위치를 찾습니다.
            finalTargetPos = new Vector3(
                Random.Range(targetXMin, targetXMax), // 해당 손의 X 범위 내에서 랜덤
                Random.Range(PLAYABLE_Y_MIN, PLAYABLE_Y_MAX),
                PLAYABLE_Z
            );
            return finalTargetPos;
        }
    }

    /// <summary>
    /// 현재 곡의 템포를 반환합니다. MusicSynchronizer에서 BPM 값을 가져갈 때 사용됩니다.
    /// </summary>
    public float GetTempo()
    {
        if (_currentSongData != null && _currentSongData.metadata != null)
        {
            return _currentSongData.metadata.tempo;
        }
        return 120f; // 데이터 로드 실패 시 기본값 (안전 장치)
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

        // 이전에는 확장된 허용 범위 시각화 (노란색)
        // 이제 인접 노트에 대한 클램핑이 없어졌으므로 이 시각화는 덜 중요할 수 있음
        Gizmos.color = Color.yellow;
        Vector3 extendedMinBoundsGlobal = new Vector3(PLAYABLE_X_MIN - outOfBoundsTolerance, PLAYABLE_Y_MIN - outOfBoundsTolerance, PLAYABLE_Z);
        Vector3 extendedMaxBoundsGlobal = new Vector3(PLAYABLE_X_MAX + outOfBoundsTolerance, PLAYABLE_Y_MAX + outOfBoundsTolerance, PLAYABLE_Z);
        Vector3 extendedCenterGlobal = (extendedMinBoundsGlobal + extendedMaxBoundsGlobal) / 2f;
        Vector3 extendedSizeGlobal = extendedMaxBoundsGlobal - extendedMinBoundsGlobal;
        Gizmos.DrawWireCube(extendedCenterGlobal, extendedSizeGlobal);

        // X축 손 영역 분할 중앙선 시각화 (하늘색)
        Gizmos.color = Color.cyan;
        float centerX = (PLAYABLE_X_MIN + PLAYABLE_X_MAX) / 2f;
        Vector3 lineStart = new Vector3(centerX, PLAYABLE_Y_MIN, PLAYABLE_Z);
        Vector3 lineEnd = new Vector3(centerX, PLAYABLE_Y_MAX, PLAYABLE_Z);
        Gizmos.DrawLine(lineStart, lineEnd);

        // 마지막 계산된 타겟 위치 시각화 (빨간색 구)
        if (_lastCalculatedTargetPos != Vector3.zero)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(_lastCalculatedTargetPos, 0.1f);

            // 겹침 방지 로직이 제거되었으므로, 이 시각화는 더 이상 겹침 방지 거리를 의미하지 않음
            // 하지만 노트의 대략적인 크기를 나타내는 용도로는 유지될 수 있습니다.
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // 오렌지색 투명
            Gizmos.DrawSphere(_lastCalculatedTargetPos, minNoteSeparationDistance);
        }
    }
}