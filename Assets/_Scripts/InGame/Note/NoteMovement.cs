using UnityEngine;
using Define;

/// <summary>
/// 노트 오브젝트의 이동 및 생명주기를 관리합니다.
/// Z축은 BPM 기반의 고정 속도로 이동하며, X/Y축은 Lerp를 따라 움직여 기믹을 구현합니다.
/// </summary>
public class NoteMovement : MonoBehaviour
{
    // === 외부에서 주입받을 참조들 ===
    private MusicSynchronizer _musicTimeChecker;

    // === 노트 관련 정보 (초기화 시 Note 컴포넌트에서 가져옴) ===
    private Note _noteComponent; // 노트 컴포넌트 참조
    private SaberNoteType _noteType; // 이 노트에 필요한 손 타입 (SaberNoteType.Left, Right)

    // === 내부에서 사용할 위치 및 이동 계산 값 ===
    private Vector3 _spawnPosition;
    private Vector3 _setPointPosition; // 중간 경유점
    private Vector3 _targetPosition; // Final target position
    private Vector3 _exitPosition; // 노트가 완전히 사라질 최종 지점 (targetPos 이후)

    // === BPM 및 속도 설정 ===
    private float _bpm;
    [SerializeField, Tooltip("노트가 SpawnPos.z에서 TargetPos.z까지 이동하는 데 걸리는 비트 수.")]
    private float _preSpawnBeats = 4.0f; 
    [SerializeField, Tooltip("노트가 TargetPos.z를 지나쳐 완전히 사라지는 데 걸리는 추가 거리.")]
    private float _postTargetExitDistance = 10.0f; // 타겟 지점을 통과한 후 추가로 이동할 Z 거리 (유니티 단위)

    // === 타이밍 관련 변수 ===
    public float TargetMusicTime { get { return _targetMusicTime; } private set { _targetMusicTime = value; } }
    private float _targetMusicTime; // 노트가 TargetPosition에 도달해야 하는 음악 시간
    private float _noteActualStartTime; // 노트가 SpawnPos에서 이동을 시작하는 실제 음악 시간 (BPM 고려)
    private float _noteRemovalTime; // 노트가 풀로 반환될 실제 음악 시간

    private float _totalZDistanceSpawnToTarget; // SpawnPos.z에서 TargetPos.z까지의 전체 Z 거리 (양수 값)
    private float _totalZDistanceTargetToExit; // TargetPos.z에서 ExitPos.z까지의 전체 Z 거리 (양수 값)
    private float _zTravelTimeSpawnToTarget; // SpawnPos에서 TargetPos까지 Z축 이동에 걸리는 총 시간
    private float _zTravelTimeTargetToExit; // TargetPos에서 ExitPos까지 Z축 이동에 걸리는 총 시간
    
    // Z축의 실제 이동 방향 (노트가 플레이어에게서 멀리서 오면 -1, 멀어지면 1)
    private float _zMoveDirection; 

    // === 상태 플래그 ===
    private bool _isInitialized = false;

    // 캐시된 트랜스폼 (성능 최적화)
    private Transform _cachedTransform;
    private Renderer _noteRenderer;

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

    public void InitializeNote(Transform spawnerParent, float bpm, float targetMusicTime, MusicSynchronizer musicTimeChecker, Vector3 targetPos)
    {
        _isInitialized = false;

        _bpm = bpm;
        _targetMusicTime = targetMusicTime;
        _musicTimeChecker = musicTimeChecker;
        _targetPosition = targetPos;

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
        
        _noteActualStartTime = _targetMusicTime - _zTravelTimeSpawnToTarget; 
        _noteRemovalTime = _targetMusicTime + _zTravelTimeTargetToExit; // Target을 지나 Exit까지 가는 시간

        _cachedTransform.position = _spawnPosition;
        ApplyNoteColor(_noteType);

        _isInitialized = true;

    }

    public void ResetNote()
    {
        _isInitialized = false;
        _cachedTransform.position = Vector3.zero; 
        _cachedTransform.rotation = Quaternion.identity;
    }

    void Update()
    {
        if (!_isInitialized || _musicTimeChecker == null) return;

        float currentMusicTime = (float)_musicTimeChecker.elapsedTime;
        float timeElapsedFromStart = currentMusicTime - _noteActualStartTime;

        Vector3 currentPosition;

        if (timeElapsedFromStart < _zTravelTimeSpawnToTarget)
        {
            // Spawn에서 Target까지 이동 중
            float progressSpawnToTarget = (_zTravelTimeSpawnToTarget > 0) ? (timeElapsedFromStart / _zTravelTimeSpawnToTarget) : 0f;
            currentPosition.z = Mathf.Lerp(_spawnPosition.z, _targetPosition.z, progressSpawnToTarget);
            currentPosition.x = GetXYPositionAlongPath(progressSpawnToTarget).x;
            currentPosition.y = GetXYPositionAlongPath(progressSpawnToTarget).y;
        }
        else
        {
            // Target을 지나 Exit까지 이동 중
            float timeAfterTarget = timeElapsedFromStart - _zTravelTimeSpawnToTarget;
            float progressTargetToExit = (_zTravelTimeTargetToExit > 0) ? (timeAfterTarget / _zTravelTimeTargetToExit) : 0f;
            
            // X/Y는 Target에서 Exit까지 선형 보간
            currentPosition.x = Mathf.Lerp(_targetPosition.x, _exitPosition.x, progressTargetToExit);
            currentPosition.y = Mathf.Lerp(_targetPosition.y, _exitPosition.y, progressTargetToExit);
            // Z는 Target에서 Exit까지 선형 보간 (Mathf.Lerp는 여기서도 클램프되므로 주의!)
            // 직접 계산으로 변경
            currentPosition.z = _targetPosition.z + (_zMoveDirection * (_postTargetExitDistance * progressTargetToExit));

            // 또는: SetPoint-Target과 동일한 방식으로 Lerp를 확장
            // currentPosition.z = Mathf.Lerp(_targetPosition.z, _exitPosition.z, progressTargetToExit);
            // 하지만 이것도 결국 progressTargetToExit가 1을 넘으면 _exitPosition에 멈춥니다.
            // 따라서 Z축은 직접 계산하는 것이 가장 명확합니다.
        }

        _cachedTransform.position = currentPosition;

        CheckForPoolReturn(currentMusicTime);

        // 디버그 로그 (실시간 위치 및 시간 확인)
        // Debug.Log($"Update: Time={currentMusicTime:F2}, Pos={_cachedTransform.position}, Prog={(currentMusicTime - _noteActualStartTime) / (_zTravelTimeSpawnToTarget + _zTravelTimeTargetToExit):F2}");
    }

    private void InitializePositions(Transform spawnerParent)
    {
        float spawnPosXMin = -2f; 
        float spawnPosXMax = 2f;  
        float spawnPosYMin = 0f;  
        float spawnPosYMax = 3f;  

        float randomX = Random.Range(spawnPosXMin, spawnPosXMax);
        float randomY = Random.Range(spawnPosYMin, spawnPosYMax);

        // Z축이 플레이어로부터 멀리(큰 Z값)에서 가까이(작은 Z값)로 이동한다고 가정
        float fixedSpawnZ = 40f; // 예시값. 실제 게임 환경에 맞춰야 합니다.
        // _targetPosition은 InitializeNote에서 주입받음 (예: Z=0f)

        _spawnPosition = new Vector3(randomX, randomY, fixedSpawnZ); 

        // SetPointPosition은 Spawn과 Target의 Z 중간 지점
        _setPointPosition = new Vector3(
            Mathf.Lerp(_spawnPosition.x, _targetPosition.x, 0.5f),
            Mathf.Lerp(_spawnPosition.y, _targetPosition.y, 0.5f), // <-- 이 부분을 수정
            Mathf.Lerp(_spawnPosition.z, _targetPosition.z, 0.5f)
        );

        // ExitPosition은 TargetPosition을 지나 _postTargetExitDistance 만큼 더 이동한 지점
        // Z축 이동 방향을 고려하여 계산
        _zMoveDirection = Mathf.Sign(_targetPosition.z - _spawnPosition.z); // 음수면 Z 감소 방향 (앞으로)
        _exitPosition = _targetPosition + new Vector3(0, 0, _zMoveDirection * _postTargetExitDistance);

        _totalZDistanceSpawnToTarget = Mathf.Abs(_targetPosition.z - _spawnPosition.z); 
        _totalZDistanceTargetToExit = Mathf.Abs(_exitPosition.z - _targetPosition.z); 
    }

    private void CalculateMovementParameters()
    {
        if (_bpm <= 0)
        {
            Debug.LogError("NoteMovement: BPM 값이 0보다 작거나 같습니다. 유효한 BPM을 설정하세요. BPM=" + _bpm, this);
            _zTravelTimeSpawnToTarget = 0;
            _zTravelTimeTargetToExit = 0;
            return;
        }

        float beatDuration = 60f / _bpm; 
        _zTravelTimeSpawnToTarget = _preSpawnBeats * beatDuration;

        // Target에서 Exit까지 이동하는 시간은 Z축 속도를 유지하면서 계산
        float zSpeed = _totalZDistanceSpawnToTarget / _zTravelTimeSpawnToTarget;
        _zTravelTimeTargetToExit = _postTargetExitDistance / zSpeed;

        if (_zTravelTimeSpawnToTarget <= 0)
        {
            Debug.LogError("NoteMovement: Z축 이동 시간이 0이거나 음수입니다. 'PreSpawnBeats'와 'Bpm' 설정을 확인하세요.", this);
            _zTravelTimeTargetToExit = 0;
        }
    }

    /// <summary>
    /// 전체 진행도(totalProgress)에 따라 X/Y 위치를 2단계 Lerp로 계산합니다.
    /// 이 함수는 이제 totalProgress가 1.0을 넘어설 때 사용되지 않습니다.
    /// `Update` 함수에서 `progressSpawnToTarget` 또는 `progressTargetToExit`를 직접 넘겨받아 사용합니다.
    /// </summary>
    /// <param name="segmentProgress">현재 세그먼트(Spawn->SetPoint 또는 SetPoint->Target) 내에서의 진행도 (0.0 ~ 1.0).</param>
    /// <returns>현재 계산된 X/Y 위치.</returns>
    private Vector3 GetXYPositionAlongPath(float segmentProgress)
    {
        Vector3 currentXY;

        // SetPoint가 Spawn-Target 전체 Z 이동 중 어느 비율에 있는지 정의
        float setPointZProgressRatio = 0.5f; 

        if (segmentProgress <= setPointZProgressRatio)
        {
            // Spawn에서 SetPoint까지의 X/Y 진행도
            float subSegmentProgress = (setPointZProgressRatio > 0) ? segmentProgress / setPointZProgressRatio : 0f;
            currentXY = Vector3.Lerp(_spawnPosition, _setPointPosition, subSegmentProgress);
        }
        else
        {
            // SetPoint에서 Target까지의 X/Y 진행도
            float remainingProgressRange = 1.0f - setPointZProgressRatio; 
            float subSegmentProgress = (remainingProgressRange > 0) ? (segmentProgress - setPointZProgressRatio) / remainingProgressRange : 0f;
            currentXY = Vector3.Lerp(_setPointPosition, _targetPosition, subSegmentProgress);
        }

        return new Vector3(currentXY.x, currentXY.y, 0); // Z축은 따로 계산하므로 0 반환
    }


    /// <summary>
    /// 노트가 _noteRemovalTime에 도달했는지 확인하고 풀로 반환합니다.
    /// </summary>
    private void CheckForPoolReturn(float currentMusicTime)
    {
        if (currentMusicTime >= _noteRemovalTime)
        {
            NoteManager.Instance?.ReturnPooledNote(gameObject);
        }
    }

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
                default:
                    _noteRenderer.material.color = Color.white; 
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