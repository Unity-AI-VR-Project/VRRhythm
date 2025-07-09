using UnityEngine;
using Define;
/// <summary>
/// 노트 오브젝트의 이동 및 생명주기를 관리합니다.
/// Z축은 BPM 기반의 고정 속도로 이동하며, X/Y축은 AnimationCurve를 따라 움직여 기믹을 구현합니다.
/// </summary>
public class NoteMover : MonoBehaviour
{
    // 스폰, 중간, 타겟 지점을 포함하는 부모 Transform (Unity 에디터에서 할당)
    public Transform SpawnerParent { get; private set; } 
    public NoteJudger.NoteDirection requiredDirection;
    // 새로 추가: 이 노트에 필요한 손 타입
    public NoteType noteType { get; private set; } // NoteJudger에서도 참조할 수 있도록 public 유지

    // === 내부에서 사용할 위치 값 (InitializeNote에서 초기화) ===
    private Vector3 _spawnPosition;     
    private Vector3 _setPointPosition;  
    public Vector3 TargetPosition { get; private set; }    

    [Header("Tempo & Speed Settings")]
    [Tooltip("음악의 분당 비트 수 (BPM). 이 값은 외부 (예: NoteSpawnerTime)에서 설정됩니다.")]
    public float Bpm { get; private set; } 
    [Tooltip("노트가 SpawnPos.z에서 TargetPos.z까지 이동하는 데 걸리는 비트 수.")]
    public float PreSpawnBeats = 4.0f;    

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
    // NoteType 파라미터 추가
    public void InitializeNote(Transform spawnerParent, float bpm, float targetMusicTime, NoteInfo noteData, MusicTimeChacker musicTimeChacker, Vector3 targetPos, NoteType NoteType)
    {
        SpawnerParent = spawnerParent;
        Bpm = bpm;
        TargetMusicTime = targetMusicTime;
        NoteData = noteData;
        MusicTimeChacker = musicTimeChacker;
        TargetPosition = targetPos; // 외부에서 받은 TargetPos 할당
        requiredDirection = NoteData.requiredDirection;
        this.noteType = NoteType; // 새로 추가: NoteType 할당
    
        InitializePositions(); 
        CalculateMovementParameters(); 
        
        _noteActualStartTime = TargetMusicTime - _zTravelTime; 

        CalculateSetPointParameters(); 
        
        transform.position = _spawnPosition; 

        _isInitialized = true; 
        ApplySetPointGimmick(NoteData); // NoteType 색상 변경 로직 포함
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
    /// SpawnerParent의 자식 Transform에서 SpawnPos, SetPos를 찾아 위치를 저장합니다.
    /// TargetPos는 이제 SpawnerSelector에서 직접 전달받습니다.
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

        _spawnPosition = (tempSpawn != null) ? tempSpawn.position : Vector3.zero;
        if (tempSpawn == null) Debug.LogError($"NoteMover: Spawner '{SpawnerParent.name}'에서 'SpawnPos' 자식 Transform을 찾지 못했습니다. 노트 스폰 위치가 0,0,0이 됩니다.", this); 

        _setPointPosition = (tempSet != null) ? tempSet.position : Vector3.zero;
        if (tempSet == null) Debug.LogWarning($"NoteMover: Spawner '{SpawnerParent.name}'에서 'SetPos' 자식 Transform을 찾지 못했습니다. 중간 기믹이 작동하지 않을 수 있습니다.", this);
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
        _totalZDistance = TargetPosition.z - _spawnPosition.z; 
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
        if (_setPointPosition == Vector3.zero || Mathf.Abs(_zSpeed) < 0.001f || _zTravelTime <= 0 || 
            (Mathf.Sign(_zSpeed) * (_setPointPosition.z - _spawnPosition.z) < 0))
        {
            _timeToSetPoint = 0; 
            _timeFromSetPointToTarget = _zTravelTime; 
            return;
        }

        float zDistanceToSetPoint = _setPointPosition.z - _spawnPosition.z;
        
        _timeToSetPoint = Mathf.Max(MIN_SETPOINT_ANIM_TIME, Mathf.Abs(zDistanceToSetPoint) / Mathf.Abs(_zSpeed));
        _timeToSetPoint = Mathf.Min(_timeToSetPoint, _zTravelTime);

        _timeFromSetPointToTarget = _zTravelTime - _timeToSetPoint;
    }


    /// <summary>
    /// Update 루프의 유효성을 검사합니다.
    /// </summary>
    private bool IsValidSetup()
    {
        if (_spawnPosition == Vector3.zero && TargetPosition == Vector3.zero) 
        {
             Debug.LogError("NoteMover: 스폰/타겟 위치가 올바르게 초기화되지 않았습니다. SpawnerParent 설정을 확인하거나, SpawnerSelector에서 TargetPos를 제대로 전달했는지 확인하세요.", this);
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
    /// _spawnPosition.z에서 _setPointPosition.z까지 Y축 상승, 그 이후에는 TargetPosition.xy로 이동합니다.
    /// X/Y 커브 기믹은 제거되었습니다.
    /// </summary>
    /// <param name="currentTime">현재 음악 시간 (초).</param>
    private void UpdateXYPositionByCurveWithStages(float currentTime)
    {
        Vector3 currentPosition = transform.position; 

        float timeElapsedOverall = currentTime - _noteActualStartTime;

        if (_timeToSetPoint > 0 && timeElapsedOverall <= _timeToSetPoint)
        {
            float segmentTimeProgress = Mathf.Clamp01(timeElapsedOverall / _timeToSetPoint);

            float currentY = Mathf.Lerp(_spawnPosition.y, _setPointPosition.y, segmentTimeProgress);
            
            transform.position = new Vector3(
                _spawnPosition.x, 
                currentY,         
                currentPosition.z 
            );
        }
        else 
        {
            float timeElapsedInSecondStage = timeElapsedOverall - _timeToSetPoint;
            float progressInSecondStage = (_timeFromSetPointToTarget > 0) ? Mathf.Clamp01(timeElapsedInSecondStage / _timeFromSetPointToTarget) : 0f;

            float startX = (_timeToSetPoint > 0) ? _setPointPosition.x : _spawnPosition.x;
            float startY = (_timeToSetPoint > 0) ? _setPointPosition.y : _spawnPosition.y;

            float currentX = Mathf.Lerp(startX, TargetPosition.x, progressInSecondStage);
            float currentY = Mathf.Lerp(startY, TargetPosition.y, progressInSecondStage);

            transform.position = new Vector3(
                currentX, 
                currentY, 
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
        }
    }

    /// <summary>
    /// 노트가 _targetPosition을 지나쳐 화면 밖으로 나갔는지 확인하고 제거합니다.
    /// 이 함수는 이제 TargetPosition 필드를 사용합니다.
    /// </summary>
    private void CheckAndDestroyNote(float currentTime)
    {
        const float destroyOffset = 5.0f; 
    
        if (_totalZDistance > 0) 
        {
            if (currentTime - TargetMusicTime > 1.0f && transform.position.z > (TargetPosition.z + destroyOffset)) 
            {
                Destroy(gameObject);
            }
        }
        else 
        {
            if (currentTime - TargetMusicTime > 1.0f && transform.position.z < (TargetPosition.z - destroyOffset)) 
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
            switch (noteType) // 저장된 NoteNoteType 사용
            {
                case NoteType.LeftHand:
                    noteRenderer.material.color = Color.red;
                    break;
                case NoteType.RightHand:
                    noteRenderer.material.color = Color.blue;
                    break;
                case NoteType.Any:
                    
                    break; 
            }
        }
    }
}