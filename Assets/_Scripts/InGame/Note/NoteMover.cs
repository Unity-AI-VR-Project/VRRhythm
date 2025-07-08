using UnityEngine;

/// <summary>
/// 노트 오브젝트의 이동 및 생명주기를 관리합니다.
/// Z축은 BPM 기반의 고정 속도로 이동하며, X/Y축은 AnimationCurve를 따라 움직여 기믹을 구현합니다.
/// </summary>
public class NoteMover : MonoBehaviour
{
    // 스폰, 중간, 타겟 지점을 포함하는 부모 Transform (Unity 에디터에서 할당)
    public Transform SpawnerParent { get; private set; } 

    // === 내부에서 사용할 위치 값 (InitializeNote에서 초기화) ===
    private Vector3 _spawnPosition;     
    private Vector3 _setPointPosition;  
    private Vector3 _targetPosition;    

    [Header("Tempo & Speed Settings")]
    [Tooltip("음악의 분당 비트 수 (BPM). 이 값은 외부 (예: NoteSpawnerTime)에서 설정됩니다.")]
    public float Bpm { get; private set; } 
    [Tooltip("노트가 SpawnPos.z에서 TargetPos.z까지 이동하는 데 걸리는 비트 수.")]
    public float PreSpawnBeats = 4.0f;    

    [Header("X/Y Curve Movement Settings")]
    [Tooltip("노트의 Z축 이동 진행도(0~1)에 따른 X 위치 변화를 정의하는 커브.")]
    public AnimationCurve XCurve = AnimationCurve.Linear(0, 0, 1, 0); 
    [Tooltip("노트의 Z축 이동 진행도(0~1)에 따른 Y 위치 변화를 정의하는 커브.")]
    public AnimationCurve YCurve = AnimationCurve.Linear(0, 0, 1, 0); 
    [Tooltip("X/Y 커브가 적용될 최대 이동 폭을 조절합니다. 커브의 Y값이 1일 때 이 값만큼 이동합니다.")]
    public float CurveMagnitude = 1.0f; 
    
    [Tooltip("SetPoint까지의 Z축 거리가 너무 짧을 때, Y축 기믹이 재생될 최소 시간 (초).")]
    public float MIN_SETPOINT_ANIM_TIME = 0.2f; // 예를 들어 0.2초는 확보

    [Header("Note Data & Timing")]
    public float TargetMusicTime { get; private set; } 
    public NoteInfo NoteData { get; private set; } 
    public MusicTimeChacker MusicTimeChacker { get; private set; } 

    // === 내부 계산용 변수 ===
    private float _totalZDistance;         
    private float _zTravelTime;            
    private float _zSpeed;                 

    private bool _hasTriggeredSetPoint = false; 
    private float _currentZProgress; 
    
    private bool _isInitialized = false;

    // --- 시간 관련 변수 ---
    private float _noteActualStartTime; // 노트가 SpawnPos에 스폰되어 이동을 시작하는 실제 음악 시간
    private float _timeToSetPoint;      // SpawnPos에서 SetPos까지 Z축 이동에 걸리는 시간
    private float _timeFromSetPointToTarget; // SetPos에서 TargetPos까지 Z축 이동에 걸리는 시간


    /// <summary>
    /// 이 메서드는 NoteSpawnerTime에서 노트를 스폰한 직후에 호출되어야 합니다.
    /// 노트 이동에 필요한 모든 초기 데이터를 설정하고 계산을 시작합니다.
    /// </summary>
    public void InitializeNote(Transform spawnerParent, float bpm, float targetMusicTime, NoteInfo noteData, MusicTimeChacker musicTimeChacker)
    {
        SpawnerParent = spawnerParent;
        Bpm = bpm;
        TargetMusicTime = targetMusicTime;
        NoteData = noteData;
        MusicTimeChacker = musicTimeChacker;
    
        InitializePositions();
        

        CalculateMovementParameters();
        
        
        _noteActualStartTime = TargetMusicTime - _zTravelTime; // 노트의 실제 이동 시작 시간 계산

        CalculateSetPointParameters(); // SetPoint 관련 시간 계산
        
        transform.position = _spawnPosition; // 초기 위치 설정

        _isInitialized = true; 
        ApplySetPointGimmick(NoteData); 
    }

    /// <summary>
    /// 매 프레임 호출됩니다. 노트의 위치를 업데이트하고 기믹 및 제거 조건을 확인합니다.
    /// </summary>
    void Update()
    {
        if (!_isInitialized || !IsValidSetup()) return;

        float currentMusicTime = (float)MusicTimeChacker.elapsedTime; 

        UpdateZPosition(currentMusicTime);
        UpdateXYPositionByCurveWithStages(currentMusicTime);
        CheckSetPointTrigger(currentMusicTime);
        CheckAndDestroyNote(currentMusicTime);
    }

    /// <summary>
    /// SpawnerParent의 자식 Transform에서 SpawnPos, SetPos, TargetPos를 찾아 위치를 저장합니다.
    /// </summary>
    private void InitializePositions()
    {
        if (SpawnerParent == null)
        {
            Debug.LogError("NoteMover: 'InitializePositions' 호출 시 'SpawnerParent'가 null입니다. 초기화 순서를 확인하세요.", this);
            return; 
        }

        Transform tempSpawn = SpawnerParent.Find("SpawnPos");
        Transform tempSet = SpawnerParent.Find("SetPos");
        Transform tempTarget = SpawnerParent.Find("TargetPos");

        _spawnPosition = (tempSpawn != null) ? tempSpawn.position : Vector3.zero;
        if (tempSpawn == null) Debug.LogError($"NoteMover: Spawner '{SpawnerParent.name}'에서 'SpawnPos' 자식 Transform을 찾지 못했습니다. 노트 스폰 위치가 0,0,0이 됩니다.", this); 

        _setPointPosition = (tempSet != null) ? tempSet.position : Vector3.zero;
        if (tempSet == null) Debug.LogWarning($"NoteMover: Spawner '{SpawnerParent.name}'에서 'SetPos' 자식 Transform을 찾지 못했습니다. 중간 기믹이 작동하지 않을 수 있습니다.", this);

        _targetPosition = (tempTarget != null) ? tempTarget.position : Vector3.zero;
        if (tempTarget == null) Debug.LogError($"NoteMover: Spawner '{SpawnerParent.name}'에서 'TargetPos' 자식 Transform을 찾지 못했습니다. 노트 이동이 불가능하며 0,0,0으로 도착할 수 있습니다.", this);
    }

    /// <summary>
    /// Z축 이동에 필요한 매개변수 (속도, 시간 등)를 계산합니다.
    /// </summary>
    private void CalculateMovementParameters()
    {
        if (Bpm <= 0) 
        {
            Debug.LogError("NoteMover: BPM 값이 0보다 작거나 같습니다. 유효한 BPM을 설정하세요. BPM=" + Bpm, this);
            _zTravelTime = 0; 
            _zSpeed = 0;
            return;
        }
        
        float beatDuration = 60f / Bpm;
        _totalZDistance = _targetPosition.z - _spawnPosition.z; 
        _zTravelTime = PreSpawnBeats * beatDuration; 

        if (_zTravelTime <= 0) 
        {
            Debug.LogError("NoteMover: Z축 이동 시간이 0이거나 음수입니다. 'PreSpawnBeats'와 'Bpm' 설정을 확인하세요.", this);
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
        // SetPos가 유효하지 않거나, Z축 속도가 0이거나, 총 이동 시간이 0이면 SetPoint 기믹을 스킵합니다.
        // 또한, SetPos.z가 SpawnPos.z보다 뒤에 있거나 같은 경우 (Z축 이동 방향과 일치하지 않는 경우)에도 스킵합니다.
        if (_setPointPosition == Vector3.zero || Mathf.Abs(_zSpeed) < 0.001f || _zTravelTime <= 0 || 
            (Mathf.Sign(_zSpeed) * (_setPointPosition.z - _spawnPosition.z) < 0))
        {
            _timeToSetPoint = 0; 
            _timeFromSetPointToTarget = _zTravelTime; 
            return;
        }

        float zDistanceToSetPoint = _setPointPosition.z - _spawnPosition.z;
        
        // Z축 거리를 기반으로 SetPoint까지의 시간을 계산합니다.
        // 계산된 시간이 MIN_SETPOINT_ANIM_TIME보다 작으면 그 값을 사용 (최소 시간 확보)
        _timeToSetPoint = Mathf.Max(MIN_SETPOINT_ANIM_TIME, Mathf.Abs(zDistanceToSetPoint) / Mathf.Abs(_zSpeed));
        
        // SetPoint까지의 시간이 전체 이동 시간보다 길다면 클램프 (오류 방지)
        _timeToSetPoint = Mathf.Min(_timeToSetPoint, _zTravelTime);

        // SetPoint에서 TargetPoint까지 남은 시간
        _timeFromSetPointToTarget = _zTravelTime - _timeToSetPoint;

        // 디버깅을 위한 로그 (선택 사항)
        // Debug.Log($"SetPoint Params: Spawn.Z={_spawnPosition.z:F2}, Set.Z={_setPointPosition.z:F2}, Target.Z={_targetPosition.z:F2}");
        // Debug.Log($"SetPoint Params: Calculated TimeToSet={Mathf.Abs(zDistanceToSetPoint) / Mathf.Abs(_zSpeed):F2}, Final TimeToSet={_timeToSetPoint:F2}");
        // Debug.Log($"SetPoint Params: ZSpeed={_zSpeed:F2}, _zTravelTime={_zTravelTime:F2}, TimeFromSetToTarget={_timeFromSetPointToTarget:F2}");
    }


    /// <summary>
    /// Update 루프의 유효성을 검사합니다.
    /// </summary>
    private bool IsValidSetup()
    {
        if (MusicTimeChacker == null)
        {
            Debug.LogError("NoteMover: 'MusicTimeChacker'가 할당되지 않았습니다. 노트가 움직이지 않습니다.", this);
            return false;
        }
        if (_spawnPosition == Vector3.zero && _targetPosition == Vector3.zero) 
        {
             Debug.LogError("NoteMover: 스폰/타겟 위치가 올바르게 초기화되지 않았습니다. SpawnerParent 설정을 확인하세요.", this);
             return false;
        }
        return true;
    }

    /// <summary>
    /// 현재 음악 시간을 기반으로 노트의 Z축 위치를 업데이트합니다.
    /// </summary>
    private void UpdateZPosition(float currentTime)
    {
        float elapsedTimeSinceNoteStart = currentTime - _noteActualStartTime;

        _currentZProgress = (_zTravelTime > 0) ? (elapsedTimeSinceNoteStart / _zTravelTime) : 0f; 

        float currentZ = _spawnPosition.z + (_totalZDistance * _currentZProgress);
    
        transform.position = new Vector3(transform.position.x, transform.position.y, currentZ);
    }

    /// <summary>
    /// _spawnPosition.z에서 _setPointPosition.z까지 Y축 상승, 그 이후에는 X/Y 커브 기믹 적용.
    /// </summary>
    /// <param name="currentTime">현재 음악 시간 (초).</param>
    private void UpdateXYPositionByCurveWithStages(float currentTime)
    {
        Vector3 currentPosition = transform.position; // 현재 Z값을 유지하기 위해 가져옵니다.

        // 노트가 실제 이동을 시작한 시점부터 현재까지의 경과 시간
        float timeElapsedOverall = currentTime - _noteActualStartTime;

        // --- 1단계: SpawnPos.z ~ SetPos.z 구간 (Y축 상승) ---
        // _timeToSetPoint가 0보다 크고, 현재 경과 시간이 1단계 시간을 초과하지 않았을 때
        if (_timeToSetPoint > 0 && timeElapsedOverall <= _timeToSetPoint)
        {
            // 1단계 구간 내에서의 시간 진행도 (0.0 ~ 1.0)
            float segmentTimeProgress = Mathf.Clamp01(timeElapsedOverall / _timeToSetPoint);

            // Y축은 SpawnPos.y에서 SetPos.y까지 선형 보간 (원하는 커브를 사용해도 됨)
            float currentY = Mathf.Lerp(_spawnPosition.y, _setPointPosition.y, segmentTimeProgress);
            
            transform.position = new Vector3(
                _spawnPosition.x, // 이 구간에서는 X 변화 없음 (SpawnPos.x로 고정)
                currentY,         // 계산된 Y 위치
                currentPosition.z 
            );
        }
        // --- 2단계: SetPos.z ~ TargetPos.z 구간 (X/Y 커브 기믹) ---
        // 1단계가 완료되었거나 (SetPos 기믹이 없거나 스킵된 경우 포함), 총 경과 시간이 1단계 시간을 초과했을 때
        else 
        {
            // 2단계 구간 내에서의 시간 진행도 (0.0 ~ 1.0)
            // 2단계의 시작 시점으로부터 현재까지의 시간
            float timeElapsedInSecondStage = timeElapsedOverall - _timeToSetPoint;
            float progressInSecondStage = (_timeFromSetPointToTarget > 0) ? Mathf.Clamp01(timeElapsedInSecondStage / _timeFromSetPointToTarget) : 0f;

            float xOffset = XCurve.Evaluate(progressInSecondStage) * CurveMagnitude;
            float yOffset = YCurve.Evaluate(progressInSecondStage) * CurveMagnitude;

            transform.position = new Vector3(
                _setPointPosition.x + xOffset, // SetPos의 X를 기준으로 X 오프셋 적용
                _setPointPosition.y + yOffset, // SetPos의 Y를 기준으로 Y 오프셋 적용
                currentPosition.z 
            );
        }
    }

    /// <summary>
    /// 노트가 _setPointPosition을 통과했는지 확인하고 기믹을 발동합니다.
    /// </summary>
    private void CheckSetPointTrigger(float currentTime)
    {
        if (_setPointPosition == Vector3.zero || _hasTriggeredSetPoint) return; 

        float timeElapsedOverall = currentTime - _noteActualStartTime;
        
        bool passedSetPoint = timeElapsedOverall >= _timeToSetPoint;

        if (passedSetPoint)
        {
            _hasTriggeredSetPoint = true;
            //Debug.Log($"노트 ({NoteData.band})가 SetPos 지점({_setPointPosition.z:.2f})을 통과했습니다. (현재 Z: {transform.position.z:.2f}, 음악 시간: {currentTime:.2f})", this);
        }
    }

    /// <summary>
    /// 노트가 _targetPosition을 지나쳐 화면 밖으로 나갔는지 확인하고 제거합니다.
    /// </summary>
    private void CheckAndDestroyNote(float currentTime)
    {
        const float destroyOffset = 5.0f; 
    
        if (_totalZDistance > 0) // 노트가 앞으로 이동하는 경우
        {
            if (currentTime - TargetMusicTime > 1.0f && transform.position.z > (_targetPosition.z + destroyOffset)) 
            {
                Destroy(gameObject);
            }
        }
        else // 노트가 뒤로 이동하는 경우 (Z축 값이 감소)
        {
            if (currentTime - TargetMusicTime > 1.0f && transform.position.z < (_targetPosition.z - destroyOffset)) 
            {
                Destroy(gameObject);
            }
        }
    }

    /// <summary>
    /// _setPointPosition 통과 시 발동할 기믹 로직을 정의합니다.
    /// </summary>
    private void ApplySetPointGimmick(NoteInfo note)
    {
        Renderer noteRenderer = GetComponent<Renderer>();
        if (noteRenderer != null)
        {
            switch (note.band)
            {
                case "low":
                    noteRenderer.material.color = Color.cyan;
                    break;
                case "mid":
                    noteRenderer.material.color = Color.magenta;
                    break;
                case "high":
                    noteRenderer.material.color = Color.yellow;
                    break;
                default:
                    noteRenderer.material.color = Color.white;
                    break;
            }
        }
    }
}