using UnityEngine;
using Define; // Define 네임스페이스 사용

[RequireComponent(typeof(SaberCollisionHandler))]
public class Saber : MonoBehaviour
{
    private Vector3 _previousPosition; // 이전 프레임의 사벨 위치
    private Vector3 _currentVelocity;  // 현재 프레임의 사벨 속도 (방향과 속력)
    private Quaternion _previousRotation; // 이전 프레임의 사벨 회전 (회전 속도 계산에 유용)

    [Header("Saber Configuration")]
    [Tooltip("이 세이버가 왼손 세이버인지 오른손 세이버인지 지정합니다.")]
    public SaberNoteType saberNoteType; // 유니티 에디터에서 선택할 수 있는 퍼블릭 필드

    [Header("Saber Settings")]
    [Tooltip("스윙 방향 계산을 위한 최소 이동 거리 (노이즈 필터링).")]
    public float minMovementThreshold = 0.01f;
    [Tooltip("스윙 방향 계산을 위한 최소 속도 임계값 (매우 느린 움직임 필터링).")]
    public float minVelocityThreshold = 0.05f; // 추가: 속도 기반 필터링

    void Awake()
    {
        _previousPosition = transform.position; // 게임 시작 시 현재 위치를 이전 위치로 초기화
        _previousRotation = transform.rotation; // 게임 시작 시 현재 회전을 이전 회전으로 초기화
    }

    void Update()
    {
        Vector3 currentPosition = transform.position;
        Quaternion currentRotation = transform.rotation;

        Vector3 deltaPosition = currentPosition - _previousPosition;

        // 사벨이 실제로 움직였을 때만 속도 업데이트
        // minMovementThreshold와 minVelocityThreshold를 함께 사용하여 더 정확한 스윙 감지
        if (deltaPosition.magnitude > minMovementThreshold)
        {
            _currentVelocity = deltaPosition / Time.deltaTime; // 속도 = 거리 / 시간
            if (_currentVelocity.magnitude < minVelocityThreshold) // 추가: 속도 자체도 임계값보다 낮으면 무시
            {
                _currentVelocity = Vector3.zero;
            }
        }
        else
        {
            _currentVelocity = Vector3.zero; // 움직임이 없으면 속도 0
        }

        _previousPosition = currentPosition; // 다음 프레임을 위해 현재 위치를 이전 위치로 저장
        _previousRotation = currentRotation; // 다음 프레임을 위해 현재 회전을 이전 회전으로 저장
    }

    /// <summary>
    /// 사벨이 휘둘러진 방향 벡터를 반환합니다.
    /// 이 벡터는 정규화된(Normalized) 벡터입니다.
    /// </summary>
    /// <returns>사벨의 스윙 방향 벡터.</returns>
    public Vector3 GetSwingDirection()
    {
        // 현재 속도 벡터의 방향을 반환
        // _currentVelocity가 이미 minVelocityThreshold를 통해 필터링되었으므로,
        // 여기서는 0보다 큰지 여부만 확인하면 됩니다.
        if (_currentVelocity.sqrMagnitude > 0) // .sqrMagnitude는 Magnitude보다 계산 비용이 적습니다.
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

    /// <summary>
    /// 사벨의 현재 각속도(Angular Velocity) 벡터를 반환합니다.
    /// 이 값은 주로 회전 스윙 점수 계산에 사용될 수 있습니다.
    /// </summary>
    /// <returns>사벨의 각속도 벡터.</returns>
    public Vector3 GetAngularVelocity()
    {
        // 이전 회전에서 현재 회전으로의 변화를 Quaternion.Inverse * currentRotation으로 얻습니다.
        // 이 쿼터니언을 통해 각도와 축을 추출하고 Time.deltaTime으로 나누어 각속도를 계산합니다.
        Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(_previousRotation);
        deltaRotation.ToAngleAxis(out float angleInDegrees, out Vector3 rotationAxis);

        if (angleInDegrees > 180f)
        {
            angleInDegrees -= 360f;
        }

        // 라디안으로 변환하고 시간에 대한 변화율 계산
        return rotationAxis.normalized * (angleInDegrees * Mathf.Deg2Rad / Time.deltaTime);
    }
}