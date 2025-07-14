using UnityEngine;
using Define; // Define 네임스페이스가 필요합니다 (예: SaberNoteType, NoteDirection).

/// <summary>
/// 노트 오브젝트의 이동 및 생명주기를 관리합니다.
/// Z축은 BPM 기반의 고정 속도로 이동하며, X/Y축은 목표 위치에 고정됩니다.
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
    // private Vector3 _setPointPosition; // 더 이상 중간 경유점으로 사용되지 않음
    private Vector3 _targetPosition; // Final target position (판정선 위치)
    private Vector3 _exitPosition; // 노트가 완전히 사라질 최종 지점 (targetPos 이후)

    // === BPM 및 속도 설정 ===
    private float _bpm;
    [SerializeField, Tooltip("노트가 SpawnPos.z에서 TargetPos.z까지 이동하는 데 걸리는 비트 수.")]
    private float _preSpawnBeats = 4.0f;

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
        _noteComponent = GetComponent<Note>();
        _noteRenderer = GetComponent<Renderer>();

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
        _isInitialized = false;

        _bpm = bpm;
        TargetMusicTime = targetMusicTime;
        _musicTimeChecker = musicTimeChecker;
        _targetPosition = targetPos;

        if (_noteComponent != null)
        {
            _noteType = _noteComponent.requiredNoteType;
        }
        else
        {
            Debug.LogError("NoteMovement: InitializeNote 호출 시 Note 컴포넌트가 누락되었습니다. 노트 초기화 실패.", this);
            return;
        }

        InitializePositions(spawnerParent);
        CalculateMovementParameters();

        _noteActualStartTime = TargetMusicTime - _zTravelTimeSpawnToTarget;
        _noteRemovalTime = TargetMusicTime + _zTravelTimeTargetToExit;

        _cachedTransform.position = _spawnPosition;

        ApplyNoteColor(_noteType);

        _isInitialized = true;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 노트를 풀로 반환하기 전 초기 상태로 리셋합니다.
    /// </summary>
    public void ResetNote()
    {
        _isInitialized = false;
        _cachedTransform.position = Vector3.zero;
        _cachedTransform.rotation = Quaternion.identity;
        if (_noteComponent != null)
        {
            _noteComponent.ResetNote();
        }
        gameObject.SetActive(false);
    }

    void Update()
    {
        if (!_isInitialized || _musicTimeChecker == null) return;

        float currentMusicTime = _musicTimeChecker.currentTimeDSP;
        float timeElapsedFromStart = currentMusicTime - _noteActualStartTime;

        Vector3 currentPosition;

        // X/Y 위치는 항상 _targetPosition의 X/Y에 고정됩니다.
        currentPosition.x = _targetPosition.x;
        currentPosition.y = _targetPosition.y;

        // Z축 이동만 시간에 따라 Lerp 또는 선형 이동을 처리합니다.
        if (timeElapsedFromStart < _zTravelTimeSpawnToTarget)
        {
            float progressSpawnToTarget = (_zTravelTimeSpawnToTarget > 0)
                                        ? Mathf.Max(0f, timeElapsedFromStart / _zTravelTimeSpawnToTarget)
                                        : 0f;
            currentPosition.z = Mathf.Lerp(_spawnPosition.z, _targetPosition.z, progressSpawnToTarget);
        }
        else // 노트가 Target을 지나 Exit까지 이동하는 구간
        {
            float timeAfterTarget = timeElapsedFromStart - _zTravelTimeSpawnToTarget;
            float progressTargetToExit = (_zTravelTimeTargetToExit > 0)
                                        ? Mathf.Min(1f, timeAfterTarget / _zTravelTimeTargetToExit)
                                        : 0f;

            // Z는 Target에서 Exit까지 직접 계산하여 연속적인 이동 구현
            currentPosition.z = _targetPosition.z + (_zMoveDirection * (_postTargetExitDistance * progressTargetToExit));
        }

        _cachedTransform.position = currentPosition;

        CheckForPoolReturn(currentMusicTime);
    }

    /// <summary>
    /// 노트의 초기 위치(_spawnPosition)와 풀로 반환될 최종 지점(_exitPosition)을 계산합니다.
    /// _setPointPosition은 더 이상 사용되지 않습니다.
    /// </summary>
    /// <param name="spawnerParent">노트가 스폰되는 스포너의 Transform (Z 위치만 참고).</param>
    private void InitializePositions(Transform spawnerParent)
    {
        // _spawnPosition의 X/Y는 _targetPosition의 X/Y와 동일하게 설정합니다.
        // Z축은 고정된 먼 거리 (플레이어 시점 기준)
        float fixedSpawnZ = 40f;

        _spawnPosition = new Vector3(_targetPosition.x, _targetPosition.y, fixedSpawnZ);

        // _setPointPosition은 더 이상 중간 경유점으로 사용되지 않습니다.
        // 필요하다면 _targetPosition과 동일하게 설정하여 단순화할 수 있습니다.
        // _setPointPosition = _targetPosition; 

        // _exitPosition은 _targetPosition을 지나 _postTargetExitDistance 만큼 더 이동한 지점
        _zMoveDirection = Mathf.Sign(_targetPosition.z - _spawnPosition.z); // 대부분 -1이 될 것입니다.
        _exitPosition = _targetPosition + new Vector3(0, 0, _zMoveDirection * _postTargetExitDistance);

        _totalZDistanceSpawnToTarget = Mathf.Abs(_targetPosition.z - _spawnPosition.z);
        _totalZDistanceTargetToExit = Mathf.Abs(_exitPosition.z - _targetPosition.z);
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

        float beatDuration = 60f / _bpm;

        _zTravelTimeSpawnToTarget = _preSpawnBeats * beatDuration;

        float zSpeed = (_zTravelTimeSpawnToTarget > 0) ? _totalZDistanceSpawnToTarget / _zTravelTimeSpawnToTarget : 0f;
        _zTravelTimeTargetToExit = (zSpeed > 0) ? _postTargetExitDistance / zSpeed : 0f;

        if (_zTravelTimeSpawnToTarget <= 0)
        {
            Debug.LogError("NoteMovement: Z축 이동 시간 (Spawn->Target)이 0이거나 음수입니다. 'PreSpawnBeats'와 'Bpm' 설정을 확인하세요.", this);
            _zTravelTimeTargetToExit = 0;
        }
    }

    /// <summary>
    /// 이 메서드는 더 이상 복잡한 X/Y 보간을 수행하지 않습니다.
    /// X/Y 위치는 `Update`에서 `_targetPosition`의 X/Y에 직접 고정됩니다.
    /// </summary>
    /// <param name="segmentProgress">현재 Z축 이동 구간 내에서의 진행도 (0.0 ~ 1.0).</param>
    /// <returns>현재 계산된 X/Y 위치.</returns>
    private Vector3 GetXYPositionAlongPath(float segmentProgress)
    {
        // X/Y 위치는 targetPosition에 고정되므로, 이 메서드는 더 이상 복잡한 계산을 하지 않습니다.
        // 필요하다면 _targetPosition.x, _targetPosition.y를 직접 반환해도 됩니다.
        // 현재 로직에서는 사실상 이 함수가 필요 없어지므로 Update에서 직접 접근하는 것이 더 효율적입니다.
        // 현재는 하위 호환성을 위해 유지하되, 리턴 값은 고정된 Z를 포함하지 않도록 주의합니다.
        return new Vector3(_targetPosition.x, _targetPosition.y, 0);
    }


    /// <summary>
    /// 노트가 _noteRemovalTime에 도달했는지 확인하고 오브젝트 풀로 반환합니다.
    /// </summary>
    /// <param name="currentMusicTime">MusicSynchronizer에서 제공하는 현재 음악 시간.</param>
    private void CheckForPoolReturn(float currentMusicTime)
    {
        if (currentMusicTime >= _noteRemovalTime)
        {
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