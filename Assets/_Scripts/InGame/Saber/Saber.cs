using UnityEngine;

public class Saber : MonoBehaviour
{
    private Vector3 _previousPosition; // 이전 프레임의 사벨 위치
    private Vector3 _currentVelocity;  // 현재 프레임의 사벨 속도 (방향과 속력)

    [Header("Saber Settings")]
    [Tooltip("스윙 방향 계산을 위한 최소 이동 거리 (노이즈 필터링).")]
    public float minMovementThreshold = 0.01f;

    void Awake()
    {
        // 게임 시작 시 현재 위치를 이전 위치로 초기화
        _previousPosition = transform.position;
    }

    void Update()
    {
        // FixedUpdate에서 물리적인 움직임을 처리하는 것이 더 정확하지만,
        // 간단한 방향 계산을 위해 Update에서도 가능합니다.
        // Rigidbody를 사용한다면 FixedUpdate에서 처리하는 것이 좋습니다.

        Vector3 currentPosition = transform.position;
        Vector3 deltaPosition = currentPosition - _previousPosition;

        // 사벨이 실제로 움직였을 때만 속도 업데이트
        if (deltaPosition.magnitude > minMovementThreshold)
        {
            _currentVelocity = deltaPosition / Time.deltaTime; // 속도 = 거리 / 시간
        }
        else
        {
            _currentVelocity = Vector3.zero; // 움직임이 없으면 속도 0
        }

        _previousPosition = currentPosition; // 다음 프레임을 위해 현재 위치를 이전 위치로 저장
    }

    /// <summary>
    /// 사벨이 휘둘러진 방향 벡터를 반환합니다.
    /// 이 벡터는 정규화된(Normalized) 벡터입니다.
    /// </summary>
    /// <returns>사벨의 스윙 방향 벡터.</returns>
    public Vector3 GetSwingDirection()
    {
        // 현재 속도 벡터의 방향을 반환
        if (_currentVelocity.magnitude > 0)
        {
            return _currentVelocity.normalized;
        }
        else
        {
            return Vector3.zero; // 움직임이 없으면 (0,0,0) 반환
        }
    }

    /// <summary>
    /// 사벨의 현재 속도(Vector3)를 반환합니다.
    /// </summary>
    /// <returns>사벨의 현재 속도 벡터.</returns>
    public Vector3 GetVelocity()
    {
        return _currentVelocity;
    }
}