using UnityEngine;
using Define; // Define 네임스페이스가 필요합니다 (예: SaberNoteType, NoteDirection).

/// <summary>
/// 노트 오브젝트의 이동 및 생명주기를 관리합니다.
/// Z축은 BPM 기반의 고정 속도로 이동하며, X/Y축은 Lerp를 따라 움직여 기믹을 구현합니다.
/// </summary>
public class NoteMovement : MonoBehaviour
{
    // === 외부에서 주입받을 참조들 ===
    private MusicSynchronizer _musicTimeChecker;

    // === 노트 관련 정보 (초기화 시 Note 컴포넌트에서 가져옴) ===
    private Note _noteComponent; // 노트 컴포넌트 참조 (Note.cs 스크립트가 필요합니다)
    private SaberNoteType _noteType; // 이 노트에 필요한 손 타입 (SaberNoteType.Left, Right)

    // === 내부에서 사용할 위치 및 이동 계산 값 ===
    private Vector3 _spawnPosition;
    private Vector3 _setPointPosition; // 중간 경유점 (X/Y Lerp에 사용)
    private Vector3 _targetPosition; // Final target position (판정선 위치)
    private Vector3 _exitPosition; // 노트가 완전히 사라질 최종 지점 (targetPos 이후)

    // === BPM 및 속도 설정 ===
    private float _bpm;
    [SerializeField, Tooltip("노트가 SpawnPos.z에서 TargetPos.z까지 이동하는 데 걸리는 비트 수.")]
    private float _preSpawnBeats = 4.0f; // 이 값이 NoteSpawnerTime에서 SpawnerSelector로 전달됩니다.

    [SerializeField, Tooltip("노트가 TargetPos.z를 지나쳐 완전히 사라지는 데 걸리는 추가 거리.")]
    private float _postTargetExitDistance = 10.0f; // 타겟 지점을 통과한 후 추가로 이동할 Z 거리 (유니티 단위)

    // === 타이밍 관련 변수 ===
    public float TargetMusicTime { get; private set; } // 노트가 TargetPosition에 도달해야 하는 음악 시간 (JSON의 time)
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

    // 캐시된 트랜스폼 및 렌더러 (성능 최적화)
    private Transform _cachedTransform;
    private Renderer _noteRenderer;

    void Awake()
    {
        _cachedTransform = transform;
        _noteComponent = GetComponent<Note>(); // 노트에 Note.cs 컴포넌트가 있어야 합니다.
        _noteRenderer = GetComponent<Renderer>(); // 노트에 Renderer 컴포넌트가 있어야 합니다 (색상 변경용).

        if (_noteComponent == null)
        {
            Debug.LogError("NoteMovement: Note 컴포넌트가 없습니다. 이 스크립트는 Note 컴포넌트와 함께 사용되어야 합니다.", this);
            enabled = false;
        }
        if (_noteRenderer == null)
        {
            Debug.LogWarning("NoteMovement: Renderer 컴포넌트가 없습니다. 노트 색상을 변경할 수 없습니다.", this);
        }
    }

    /// <summary>
    /// 노트가 오브젝트 풀에서 활성화될 때 호출되어 초기화됩니다.
    /// </summary>
    /// <param name="spawnerParent">노트가 스폰되는 스포너의 Transform (초기 Z 위치 계산에 사용).</param>
    /// <param name="bpm">현재 음악의 BPM.</param>
    /// <param name="targetMusicTime">노트가 판정선에 도달해야 할 음악 시간 (JSON의 'time' 값).</param>
    /// <param name="musicTimeChecker">MusicSynchronizer 인스턴스 (시간 동기화용).</param>
    /// <param name="targetPos">노트가 도달해야 할 최종 목표 위치 (판정선 위치).</param>
    public void InitializeNote(Transform spawnerParent, float bpm, float targetMusicTime, MusicSynchronizer musicTimeChecker, Vector3 targetPos)
    {
        _isInitialized = false; // 초기화 시작 시 플래그 리셋

        _bpm = bpm;
        TargetMusicTime = targetMusicTime; // JSON의 time 값
        _musicTimeChecker = musicTimeChecker;
        _targetPosition = targetPos; // 판정선 위치

        if (_noteComponent != null)
        {
            _noteType = _noteComponent.requiredNoteType; // Note 컴포넌트에서 노트 타입 가져오기
        }
        else
        {
            Debug.LogError("NoteMovement: InitializeNote 호출 시 Note 컴포넌트가 누락되었습니다. 노트 초기화 실패.", this);
            return;
        }

        // 위치 설정 및 이동 관련 파라미터 계산
        InitializePositions(spawnerParent); // 스포너 부모 Transform을 사용하여 시작 위치 및 경로 설정
        CalculateMovementParameters();

        // 노트가 SpawnPosition에서 이동을 시작해야 하는 음악 시간
        // targetMusicTime (판정선 도달 시간) - _zTravelTimeSpawnToTarget (Spawn-Target 이동 시간)
        _noteActualStartTime = TargetMusicTime - _zTravelTimeSpawnToTarget;

        // 노트가 풀로 반환될 시간 (targetMusicTime + Target-Exit 이동 시간)
        _noteRemovalTime = TargetMusicTime + _zTravelTimeTargetToExit;

        // 노트의 초기 위치를 SpawnPosition으로 설정
        _cachedTransform.position = _spawnPosition;

        // 노트 색상 적용
        ApplyNoteColor(_noteType);

        _isInitialized = true; // 초기화 완료 플래그 설정
        gameObject.SetActive(true); // 혹시 비활성화되어 있었다면 활성화
    }

    /// <summary>
    /// 노트를 풀로 반환하기 전 초기 상태로 리셋합니다.
    /// </summary>
    public void ResetNote()
    {
        _isInitialized = false; // 초기화 플래그 해제
        _cachedTransform.position = Vector3.zero; // 위치 리셋 (필요시)
        _cachedTransform.rotation = Quaternion.identity; // 회전 리셋
        // Note 컴포넌트의 추가적인 리셋 로직 호출 가능 (예: 충돌 감지 비활성화)
        if (_noteComponent != null)
        {
            _noteComponent.ResetNote(); // Note.cs에 ResetNote() 메서드가 있다고 가정
        }
        gameObject.SetActive(false); // 풀로 반환되기 전에 비활성화
    }

    void Update()
    {
        // 초기화되지 않았거나 MusicSynchronizer가 없다면 아무것도 하지 않습니다.
        if (!_isInitialized || _musicTimeChecker == null) return;

        // MusicSynchronizer의 현재 DSP 시간을 가져옵니다. (음악 재생 시작 시점을 0으로 하는 경과 시간)
        float currentMusicTime = _musicTimeChecker.currentTimeDSP;

        // 노트가 실제로 이동을 시작하는 시간부터 얼마나 지났는지 계산
        float timeElapsedFromStart = currentMusicTime - _noteActualStartTime;

        Vector3 currentPosition;

        // 1. 노트가 Spawn에서 Target까지 이동하는 구간 (음수 시간을 포함하여 처리)
        // timeElapsedFromStart가 0보다 작으면 (아직 _noteActualStartTime에 도달하지 않았다면)
        // 0으로 간주하여 _spawnPosition에 고정되도록 합니다.
        if (timeElapsedFromStart < _zTravelTimeSpawnToTarget)
        {
            // Lerp의 진행도는 0에서 1 사이로 클램프되어야 합니다.
            // timeElapsedFromStart가 음수일 때 progressSpawnToTarget이 음수가 되므로,
            // Math.Max(0f, ...)를 사용하여 음수값을 0으로 고정합니다.
            float progressSpawnToTarget = (_zTravelTimeSpawnToTarget > 0)
                                        ? Mathf.Max(0f, timeElapsedFromStart / _zTravelTimeSpawnToTarget)
                                        : 0f;

            currentPosition.z = Mathf.Lerp(_spawnPosition.z, _targetPosition.z, progressSpawnToTarget);
            currentPosition.x = GetXYPositionAlongPath(progressSpawnToTarget).x;
            currentPosition.y = GetXYPositionAlongPath(progressSpawnToTarget).y;
        }
        // 2. 노트가 Target을 지나 Exit까지 이동하는 구간
        else
        {
            float timeAfterTarget = timeElapsedFromStart - _zTravelTimeSpawnToTarget;
            // progressTargetToExit도 0에서 1 사이로 클램프됩니다.
            float progressTargetToExit = (_zTravelTimeTargetToExit > 0)
                                        ? Mathf.Min(1f, timeAfterTarget / _zTravelTimeTargetToExit)
                                        : 0f; // 1 이상이면 1로 고정, 0 미만이면 0으로 고정

            // X/Y는 Target에서 Exit까지 선형 보간
            currentPosition.x = Mathf.Lerp(_targetPosition.x, _exitPosition.x, progressTargetToExit);
            currentPosition.y = Mathf.Lerp(_targetPosition.y, _exitPosition.y, progressTargetToExit);

            // Z는 Target에서 Exit까지 직접 계산하여 연속적인 이동 구현
            currentPosition.z = _targetPosition.z + (_zMoveDirection * (_postTargetExitDistance * progressTargetToExit));
        }

        // 계산된 위치로 노트 트랜스폼 업데이트
        _cachedTransform.position = currentPosition;

        // 노트가 풀로 반환될 시간인지 확인
        CheckForPoolReturn(currentMusicTime);

        // 디버그 로그 (실시간 위치 및 시간 확인)
        // Debug.Log($"Note Update: CurrentTime={currentMusicTime:F2}, NoteStart={_noteActualStartTime:F2}, NoteRemoval={_noteRemovalTime:F2}, Pos={_cachedTransform.position}");
    }

    /// <summary>
    /// 노트의 초기 위치(_spawnPosition), 중간 경유점(_setPointPosition),
    /// 그리고 풀로 반환될 최종 지점(_exitPosition)을 계산합니다.
    /// </summary>
    /// <param name="spawnerParent">노트가 스폰되는 스포너의 Transform (주로 X/Y 위치 기준 제공).</param>
    private void InitializePositions(Transform spawnerParent)
    {
        // SpawnerSelector에서 제공되는 SpawnerTransform의 X/Y 위치를 _spawnPosition의 기준으로 사용합니다.
        // Z축은 고정된 먼 거리 (플레이어 시점 기준)
        float fixedSpawnZ = 40f; // 이 값은 게임의 시점과 스케일에 따라 조정해야 합니다.

        // _spawnPosition은 스포너의 X/Y를 따르고 Z는 고정된 값으로 시작
        _spawnPosition = new Vector3(spawnerParent.position.x, spawnerParent.position.y, fixedSpawnZ);

        // _targetPosition은 InitializeNote에서 주입받은 값 (주로 판정선 위치)
        // _setPointPosition은 _spawnPosition과 _targetPosition의 Z축 중간 지점을 기준으로 X/Y를 보간하여 계산
        _setPointPosition = new Vector3(
            Mathf.Lerp(_spawnPosition.x, _targetPosition.x, 0.5f),
            Mathf.Lerp(_spawnPosition.y, _targetPosition.y, 0.5f),
            Mathf.Lerp(_spawnPosition.z, _targetPosition.z, 0.5f) // Z축도 중간값
        );

        // _exitPosition은 _targetPosition을 지나 _postTargetExitDistance 만큼 더 이동한 지점
        // _zMoveDirection은 노트가 Z축으로 플레이어에게 다가오는지(음수) 멀어지는지(양수)를 결정
        _zMoveDirection = Mathf.Sign(_targetPosition.z - _spawnPosition.z);
        _exitPosition = _targetPosition + new Vector3(0, 0, _zMoveDirection * _postTargetExitDistance);

        // 각 구간의 Z축 이동 거리 계산 (항상 양수)
        _totalZDistanceSpawnToTarget = Mathf.Abs(_targetPosition.z - _spawnPosition.z);
        _totalZDistanceTargetToExit = Mathf.Abs(_exitPosition.z - _targetPosition.z);

        // 디버그 (각 위치 확인)
        // Debug.Log($"Note Init Positions: Spawn={_spawnPosition}, SetPoint={_setPointPosition}, Target={_targetPosition}, Exit={_exitPosition}");
    }

    /// <summary>
    /// 노트의 이동에 필요한 시간 관련 파라미터들을 계산합니다.
    /// </summary>
    private void CalculateMovementParameters()
    {
        if (_bpm <= 0)
        {
            Debug.LogError("NoteMovement: BPM 값이 0보다 작거나 같습니다. 유효한 BPM을 설정하세요. BPM=" + _bpm, this);
            _zTravelTimeSpawnToTarget = 0;
            _zTravelTimeTargetToExit = 0;
            return;
        }

        float beatDuration = 60f / _bpm; // 1비트의 초 단위 길이

        // Spawn에서 Target까지 Z축 이동에 걸리는 시간 (_preSpawnBeats에 기반)
        _zTravelTimeSpawnToTarget = _preSpawnBeats * beatDuration;

        // Target에서 Exit까지 Z축 이동에 걸리는 시간 (Spawn-Target과 동일한 Z축 속도를 유지)
        float zSpeed = (_zTravelTimeSpawnToTarget > 0) ? _totalZDistanceSpawnToTarget / _zTravelTimeSpawnToTarget : 0f;
        _zTravelTimeTargetToExit = (zSpeed > 0) ? _postTargetExitDistance / zSpeed : 0f;

        if (_zTravelTimeSpawnToTarget <= 0)
        {
            Debug.LogError("NoteMovement: Z축 이동 시간 (Spawn->Target)이 0이거나 음수입니다. 'PreSpawnBeats'와 'Bpm' 설정을 확인하세요.", this);
            _zTravelTimeTargetToExit = 0; // 이 경우 Target->Exit 시간도 무의미
        }
    }

    /// <summary>
    /// 전체 진행도(segmentProgress)에 따라 X/Y 위치를 2단계 Lerp로 계산합니다.
    /// 이 함수는 Z축 이동과는 독립적으로 X/Y 경로를 만듭니다.
    /// </summary>
    /// <param name="segmentProgress">현재 Z축 이동 구간 내에서의 진행도 (0.0 ~ 1.0).</param>
    /// <returns>현재 계산된 X/Y 위치.</returns>
    private Vector3 GetXYPositionAlongPath(float segmentProgress)
    {
        Vector3 currentXY;

        // _setPointPosition이 _spawnPosition과 _targetPosition의 Z축 중간에 있으므로,
        // Z축 진행도의 0.5를 기준으로 X/Y Lerp 구간을 나눕니다.
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

        return new Vector3(currentXY.x, currentXY.y, 0); // Z축은 이 함수에서 계산하지 않으므로 0 반환
    }


    /// <summary>
    /// 노트가 _noteRemovalTime에 도달했는지 확인하고 오브젝트 풀로 반환합니다.
    /// </summary>
    /// <param name="currentMusicTime">MusicSynchronizer에서 제공하는 현재 음악 시간.</param>
    private void CheckForPoolReturn(float currentMusicTime)
    {
        // 노트가 제거될 시간(_noteRemovalTime)을 지났다면 풀로 반환
        if (currentMusicTime >= _noteRemovalTime)
        {
            // NoteManager.Instance가 null이 아닌지 확인하여 잠재적 오류 방지
            NoteManager.Instance?.ReturnPooledNote(gameObject);
        }
    }

    /// <summary>
    /// 노트 타입에 따라 노트의 색상을 변경합니다.
    /// </summary>
    /// <param name="type">적용할 노트 타입.</param>
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
                    break;
                default:
                    Debug.LogWarning($"NoteMovement: 알 수 없는 SaberNoteType 값 ({type})입니다. 기본 색상 (흰색)을 사용합니다.", this);
                    _noteRenderer.material.color = Color.white;
                    break;
            }
        }
        else
        {
            Debug.LogWarning("NoteMovement: 렌더러 또는 재질이 없어 노트 색상을 적용할 수 없습니다. 노트 프리팹에 Renderer와 Material이 있는지 확인하세요.", this);
        }
    }

    /// <summary>
    /// 노트가 스폰되기 얼마나 전에 나타나야 하는지를 결정하는 비트 수를 반환합니다.
    /// SpawnerSelector에서 이 값을 가져가 스폰 시점을 계산하는 데 사용합니다.
    /// </summary>
    public float GetPreSpawnBeats()
    {
        return _preSpawnBeats;
    }
}