using UnityEngine;
using Define;

/// <summary>
/// 노트 오브젝트의 이동 및 생명주기를 관리합니다.
/// Z축은 BPM 기반의 고정 속도로 이동하며, X/Y축은 Lerp를 따라 움직여 기믹을 구현합니다.
/// </summary>
public class NoteMovement : MonoBehaviour
{
    // === 외부에서 주입받을 참조들 ===
    private MusicTimeChecker _musicTimeChecker;

    // === 노트 관련 정보 (초기화 시 Note 컴포넌트에서 가져옴) ===
    private Note _noteComponent; // 노트 컴포넌트 참조
    private SaberNoteType _noteType; // 이 노트에 필요한 손 타입 (SaberNoteType.Left, Right)

    // === 내부에서 사용할 위치 및 이동 계산 값 ===
    private Vector3 _spawnPosition;
    private Vector3 _setPointPosition;
    private Vector3 _targetPosition; // Final target position

    // === BPM 및 속도 설정 ===
    private float _bpm;
    [SerializeField, Tooltip("노트가 SpawnPos.z에서 TargetPos.z까지 이동하는 데 걸리는 비트 수.")]
    private float _preSpawnBeats = 4.0f;

    [SerializeField, Tooltip("SetPoint까지의 Z축 거리가 너무 짧을 때, Y축 기믹이 재생될 최소 시간 (초).")]
    private float _minSetPointAnimTime = 0.2f;

    // === 타이밍 관련 변수 ===
    public float TargetMusicTime { get { return _targetMusicTime; } private set { _targetMusicTime = value; } }
    private float _targetMusicTime; // 노트가 TargetPosition에 도달해야 하는 음악 시간
    private float _noteActualStartTime; // 노트가 SpawnPos에서 이동을 시작하는 실제 음악 시간 (BPM 고려)

    private float _totalZDistance; // SpawnPos.z에서 TargetPos.z까지의 전체 Z 거리
    private float _zTravelTime; // SpawnPos에서 TargetPos까지 Z축 이동에 걸리는 총 시간
    private float _zSpeed; // Z축 이동 속도 (미터/초)

    private float _timeToSetPoint; // SpawnPos에서 SetPoint까지 Z축 이동에 걸리는 시간
    private float _timeFromSetPointToTarget; // SetPoint에서 TargetPos까지 Z축 이동에 걸리는 시간

    // === 상태 플래그 ===
    private bool _isInitialized = false;
    private bool _hasEnteredSetPointStage = false; // SetPoint Y축 이동 단계에 진입했는지 여부

    // 캐시된 트랜스폼 (성능 최적화)
    private Transform _cachedTransform;
    private Renderer _noteRenderer;

    /// <summary>
    /// NoteMover 인스턴스 초기화 시 필요한 컴포넌트 참조를 캐시합니다.
    /// </summary>
    void Awake()
    {
        _cachedTransform = transform;
        _noteComponent = GetComponent<Note>();
        _noteRenderer = GetComponent<Renderer>();

        if (_noteComponent == null)
        {
            Debug.LogError("NoteMovement: Note 컴포넌트가 없습니다. 이 스크립트는 Note 컴포넌트와 함께 사용되어야 합니다.", this);
            enabled = false;
        }
    }

    /// <summary>
    /// 이 메서드는 NoteSpawnerTime에서 노트를 스폰한 직후에 호출되어야 합니다.
    /// 노트 이동에 필요한 모든 초기 데이터를 설정하고 계산을 시작합니다.
    /// </summary>
    /// <param name="spawnerParent">스폰, 중간, 타겟 지점을 포함하는 부모 Transform.</param>
    /// <param name="bpm">음악의 분당 비트 수 (BPM).</param>
    /// <param name="targetMusicTime">노트가 타겟 위치에 도달해야 하는 음악 시간 (초).</param>
    /// <param name="musicTimeChecker">현재 음악 시간을 제공하는 MusicTimeChecker 인스턴스.</param>
    /// <param name="targetPos">노트가 최종적으로 도달할 월드 좌표.</param>
    public void InitializeNote(Transform spawnerParent, float bpm, float targetMusicTime, MusicTimeChecker musicTimeChecker, Vector3 targetPos)
    {
        // 초기화 플래그 리셋
        _isInitialized = false;
        _hasEnteredSetPointStage = false;

        // 외부 참조 할당
        _bpm = bpm;
        _targetMusicTime = targetMusicTime;
        _musicTimeChecker = musicTimeChecker;
        _targetPosition = targetPos;

        // Note 컴포넌트에서 필요한 정보 가져오기
        if (_noteComponent != null)
        {
            _noteType = _noteComponent.requiredNoteType;
        }
        else
        {
            Debug.LogError("NoteMovement: InitializeNote 호출 시 Note 컴포넌트가 누락되었습니다.", this);
            return;
        }

        // 위치 설정 및 계산
        InitializePositions(spawnerParent);
        CalculateMovementParameters();
        _noteActualStartTime = _targetMusicTime - _zTravelTime; // 노트 이동 시작 시간
        CalculateSetPointParameters();

        // 초기 위치 설정
        _cachedTransform.position = _spawnPosition;

        // 노트 색상 적용 (NoteType에 따라)
        ApplyNoteColor(_noteType);

        _isInitialized = true;
    }

    /// <summary>
    /// 노트가 오브젝트 풀로 반환될 때 호출되어 상태를 초기화합니다.
    /// </summary>
    public void ResetNote()
    {
        _isInitialized = false;
        _hasEnteredSetPointStage = false;
        _cachedTransform.position = Vector3.zero; // 안전을 위해 위치 초기화
        _cachedTransform.rotation = Quaternion.identity; // 회전 초기화

        // 필요한 경우, 렌더러와 콜라이더를 다시 활성화합니다.
        // NoteManager에서 HandleNoteCutVisuals가 비활성화하므로, 풀에서 꺼낼 때 다시 활성화해야 함.
        // 이 로직은 NoteManager의 OnGetNoteFromPool에서 처리하는 것이 더 적절합니다.
    }


    /// <summary>
    /// 매 프레임 호출됩니다. 노트의 위치를 업데이트하고 제거 조건을 확인합니다.
    /// </summary>
    void Update()
    {
        if (!_isInitialized || _musicTimeChecker == null) return;

        float currentMusicTime = (float)_musicTimeChecker.elapsedTime;
        float timeElapsedOverall = currentMusicTime - _noteActualStartTime;

        // Z축 위치 업데이트 (BPM 기반 선형 이동)
        UpdateZPosition(timeElapsedOverall);

        // X/Y축 위치 업데이트 (SetPoint를 기준으로 2단계 이동)
        UpdateXYPositionByStages(timeElapsedOverall);

        // 노트 제거 조건 확인
        CheckForPoolReturn(currentMusicTime);
    }

    /// <summary>
    /// SpawnerParent의 자식 Transform에서 SpawnPos, SetPos를 찾아 위치를 저장합니다.
    /// </summary>
    /// <param name="spawnerParent">스폰 위치를 포함하는 부모 트랜스폼.</param>
    private void InitializePositions(Transform spawnerParent)
    {
        Transform tempSpawn = spawnerParent.Find("SpawnPos");
        Transform tempSet = spawnerParent.Find("SetPos");

        _spawnPosition = (tempSpawn != null) ? tempSpawn.position : Vector3.zero;
        if (tempSpawn == null) Debug.LogError($"NoteMovement: Spawner '{spawnerParent.name}'에서 'SpawnPos' 자식 Transform을 찾지 못했습니다. 노트 스폰 위치가 0,0,0이 됩니다.", this);

        _setPointPosition = (tempSet != null) ? tempSet.position : Vector3.zero;
        if (tempSet == null) Debug.LogWarning($"NoteMovement: Spawner '{spawnerParent.name}'에서 'SetPos' 자식 Transform을 찾지 못했습니다. 중간 기믹이 작동하지 않을 수 있습니다.", this);
    }

    /// <summary>
    /// Z축 이동에 필요한 매개변수 (속도, 총 이동 시간, 총 거리)를 계산합니다.
    /// </summary>
    private void CalculateMovementParameters()
    {
        if (_bpm <= 0)
        {
            Debug.LogError("NoteMovement: BPM 값이 0보다 작거나 같습니다. 유효한 BPM을 설정하세요. BPM=" + _bpm, this);
            _zTravelTime = 0;
            _zSpeed = 0;
            return;
        }

        float beatDuration = 60f / _bpm;
        _totalZDistance = _targetPosition.z - _spawnPosition.z;
        _zTravelTime = _preSpawnBeats * beatDuration;

        if (_zTravelTime <= 0)
        {
            Debug.LogError("NoteMovement: Z축 이동 시간이 0이거나 음수입니다. 'PreSpawnBeats'와 'Bpm' 설정을 확인하세요.", this);
            _zSpeed = 0;
        }
        else
        {
            _zSpeed = _totalZDistance / _zTravelTime;
        }
    }

    /// <summary>
    /// SetPoint 관련 Z축 진행도 및 시간 파라미터를 계산합니다.
    /// 이 메서드는 _noteActualStartTime이 계산된 후에 호출되어야 합니다.
    /// </summary>
    private void CalculateSetPointParameters()
    {
        // SetPoint가 없거나, Z축 이동이 없거나, Z축 이동 방향이 잘못된 경우
        if (_setPointPosition == Vector3.zero || Mathf.Abs(_zSpeed) < 0.001f || _zTravelTime <= 0 ||
            (Mathf.Sign(_zSpeed) * (_setPointPosition.z - _spawnPosition.z) < 0))
        {
            _timeToSetPoint = 0;
            _timeFromSetPointToTarget = _zTravelTime;
            return;
        }

        float zDistanceToSetPoint = _setPointPosition.z - _spawnPosition.z;

        // SetPoint까지 이동하는 시간을 계산하고, 최소 애니메이션 시간을 보장
        _timeToSetPoint = Mathf.Max(_minSetPointAnimTime, Mathf.Abs(zDistanceToSetPoint) / Mathf.Abs(_zSpeed));
        _timeToSetPoint = Mathf.Min(_timeToSetPoint, _zTravelTime); // 총 이동 시간을 초과하지 않도록 제한

        _timeFromSetPointToTarget = _zTravelTime - _timeToSetPoint;
    }

    /// <summary>
    /// 현재 음악 시간을 기반으로 노트의 Z축 위치를 업데이트합니다.
    /// </summary>
    /// <param name="timeElapsedOverall">노트가 스폰된 후 경과한 전체 시간 (초).</param>
    private void UpdateZPosition(float timeElapsedOverall)
    {
        float currentZProgress = (_zTravelTime > 0) ? (timeElapsedOverall / _zTravelTime) : 0f;
        float currentZ = _spawnPosition.z + (_totalZDistance * currentZProgress);

        Vector3 currentPosition = _cachedTransform.position;
        _cachedTransform.position = new Vector3(currentPosition.x, currentPosition.y, currentZ);
    }

    /// <summary>
    /// X/Y축 위치를 업데이트합니다. _spawnPosition에서 _setPointPosition을 거쳐 _targetPosition으로 이동합니다.
    /// </summary>
    /// <param name="timeElapsedOverall">노트가 스폰된 후 경과한 전체 시간 (초).</param>
    private void UpdateXYPositionByStages(float timeElapsedOverall)
    {
        Vector3 currentPosition = _cachedTransform.position;

        // 첫 번째 단계: SpawnPos에서 SetPoint까지 Y축 이동
        if (_timeToSetPoint > 0 && timeElapsedOverall <= _timeToSetPoint)
        {
            float segmentTimeProgress = Mathf.Clamp01(timeElapsedOverall / _timeToSetPoint);
            float currentY = Mathf.Lerp(_spawnPosition.y, _setPointPosition.y, segmentTimeProgress);

            _cachedTransform.position = new Vector3(
                _spawnPosition.x, // X는 스폰 위치 X 고정
                currentY,
                currentPosition.z
            );
        }
        // 두 번째 단계: SetPoint에서 TargetPos까지 X/Y축 이동 (Z축 이동과 별개)
        else
        {
            // SetPoint 단계를 처음 진입했으면 시작 위치를 SetPoint로 설정
            if (!_hasEnteredSetPointStage)
            {
                _hasEnteredSetPointStage = true;
                _cachedTransform.position = new Vector3(_setPointPosition.x, _setPointPosition.y, currentPosition.z);
            }

            float timeElapsedInSecondStage = timeElapsedOverall - _timeToSetPoint;
            float progressInSecondStage = (_timeFromSetPointToTarget > 0) ? Mathf.Clamp01(timeElapsedInSecondStage / _timeFromSetPointToTarget) : 0f;

            float currentX = Mathf.Lerp(_setPointPosition.x, _targetPosition.x, progressInSecondStage);
            float currentY = Mathf.Lerp(_setPointPosition.y, _targetPosition.y, progressInSecondStage);

            _cachedTransform.position = new Vector3(
                currentX,
                currentY,
                currentPosition.z
            );
        }
    }

    /// <summary>
    /// 노트가 _targetPosition을 지나쳐 화면 밖으로 나갔는지 확인하고 풀로 반환합니다.
    /// </summary>
    /// <param name="currentMusicTime">현재 음악 시간 (초).</param>
    private void CheckForPoolReturn(float currentMusicTime)
    {
        // 타겟 시간을 1초 초과했고, 노트가 타겟 위치에서 충분히 벗어났는지 확인
        // (Z축 방향에 따라 destroyOffset 적용)
        const float destroyOffset = 5.0f; // 노트를 풀로 반환하기 위한 추가 Z축 거리

        if (_totalZDistance > 0) // Z축 증가 방향 (뒤에서 앞으로)
        {
            if (currentMusicTime - _targetMusicTime > 1.0f && _cachedTransform.position.z > (_targetPosition.z + destroyOffset))
            {
                NoteManager.Instance?.ReturnPooledNote(gameObject);
            }
        }
        else // Z축 감소 방향 (앞에서 뒤로)
        {
            if (currentMusicTime - _targetMusicTime > 1.0f && _cachedTransform.position.z < (_targetPosition.z - destroyOffset))
            {
                NoteManager.Instance?.ReturnPooledNote(gameObject);
            }
        }
    }

    /// <summary>
    /// 노트의 NoteType에 따라 렌더러의 색상을 변경합니다.
    /// (예: 왼손 노트는 빨강, 오른손 노트는 파랑).
    /// </summary>
    /// <param name="type">노트의 SaberNoteType.</param>
    private void ApplyNoteColor(SaberNoteType type)
    {
        if (_noteRenderer != null && _noteRenderer.material != null)
        {
            switch (type)
            {
                case SaberNoteType.Left:
                    _noteRenderer.material.color = Color.red;
                    break;
                case SaberNoteType.Right:
                    _noteRenderer.material.color = Color.blue;
                    break;
                // 기타 노트 타입에 대한 처리 (예: Bomb, Any 등)
                default:
                    _noteRenderer.material.color = Color.white; // 기본 색상
                    break;
            }
        }
        else
        {
            Debug.LogWarning("NoteMovement: 렌더러 또는 재질이 없어 노트 색상을 적용할 수 없습니다.", this);
        }
    }

    public float GetPreSpawnBeats()
    {
        return _preSpawnBeats;
    }
}