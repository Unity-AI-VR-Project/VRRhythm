using System.Collections.Generic; // 큐(Queue)를 사용하기 위해 추가
using UnityEngine;
using Define;
using TMPro;

[RequireComponent(typeof(SaberCollisionHandler))]
public class Saber : MonoBehaviour
{
    // 이전 위치와 속도 대신 스윙 궤적 기록을 위한 멤버 변수
    private Queue<Vector3> _positionHistory = new Queue<Vector3>();
    [Header("Saber Settings")]
    [Tooltip("스윙 방향 계산을 위해 기록할 이전 프레임 위치의 개수.")]
    public int positionHistoryLength = 10; // 보통 5~15 프레임 정도가 적절

    [Tooltip("스윙 방향으로 간주될 최소 이동 거리. (노이즈 필터링)")]
    public float minSwingDistance = 0.05f; // 너무 작은 움직임은 스윙으로 간주하지 않음

    // 이전 프레임의 위치는 더 이상 필요 없지만, 유지를 원한다면 그대로 두세요.
    // private Vector3 _previousPosition; 
    // private Vector3 _currentVelocity; 
    private Quaternion _previousRotation;

    // 마지막으로 유효하게 계산된 스윙 방향 (움직임이 없을 때 대비)
    private Vector3 _lastValidSwingDirection = Vector3.forward;

    [Header("Saber Configuration")]
    [Tooltip("이 세이버가 왼손 세이버인지 오른손 세이버인지 지정합니다.")]
    public SaberNoteType saberNoteType; // 유니티 에디터에서 선택할 수 있는 퍼블릭 필드

    void Awake()
    {
        // _previousPosition = transform.position; // 더 이상 필요 없을 수 있음
        _previousRotation = transform.rotation; // 게임 시작 시 현재 회전을 이전 회전으로 초기화
    }

    void FixedUpdate() // FixedUpdate에서 물리적인 움직임을 기록하는 것이 더 안정적입니다.
    {
        // 현재 위치를 기록 큐에 추가
        _positionHistory.Enqueue(transform.position);

        // 큐의 길이가 설정된 길이보다 길면 가장 오래된 위치를 제거
        if (_positionHistory.Count > positionHistoryLength)
        {
            _positionHistory.Dequeue();
        }

        // 각속도 계산을 위해 이전 회전 업데이트
        _previousRotation = transform.rotation;
    }

    /// <summary>
    /// 사벨이 휘둘러진 방향 벡터를 반환합니다.
    /// 이 벡터는 정규화된(Normalized) 벡터입니다.
    /// 스윙 시작점부터 현재 위치까지의 궤적 방향을 계산합니다.
    /// </summary>
    /// <returns>사벨의 스윙 방향 벡터.</returns>
    public Vector3 GetSwingDirection()
    {
        // 큐에 충분한 데이터가 있는지 확인
        if (_positionHistory.Count < positionHistoryLength)
        {
            // 아직 데이터가 충분치 않다면, 마지막으로 유효한 방향 또는 기본값 반환
            // 또는 Vector3.zero를 반환하여 아직 유효한 스윙이 아님을 나타낼 수도 있습니다.
            return _lastValidSwingDirection;
        }

        // 가장 오래된 위치 (스윙 시작점으로 간주)
        Vector3 startPosition = _positionHistory.Peek();
        // 현재 위치 (스윙 끝점으로 간주)
        Vector3 endPosition = transform.position;

        // 스윙 벡터 계산 (끝점 - 시작점)
        Vector3 swingVector = endPosition - startPosition;

        // 최소 스윙 거리를 넘었을 때만 유효한 스윙으로 간주하고 방향 업데이트
        if (swingVector.magnitude > minSwingDistance)
        {
            _lastValidSwingDirection = swingVector.normalized;
        }

        return _lastValidSwingDirection; // 마지막 유효한 스윙 방향 반환
    }

    /// <summary>
    /// 사벨의 현재 속도(Vector3)를 반환합니다. (더 이상 주 판정에 사용되지 않음)
    /// </summary>
    /// <returns>사벨의 현재 속도 벡터.</returns>
    public Vector3 GetVelocity()
    {
        // 이 함수는 더 이상 GetSwingDirection에서 주되게 사용되지 않으므로, 
        // 외부에서 필요할 경우를 위해 그대로 두거나 제거할 수 있습니다.
        // 여기서는 단순 참고용으로 마지막 두 프레임의 속도를 계산하여 반환합니다.
        if (_positionHistory.Count >= 2)
        {
            Vector3[] positions = _positionHistory.ToArray();
            Vector3 currentPos = positions[positions.Length - 1];
            Vector3 prevPos = positions[positions.Length - 2];
            return (currentPos - prevPos) / Time.fixedDeltaTime;
        }
        return Vector3.zero;
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
        return rotationAxis.normalized * (angleInDegrees * Mathf.Deg2Rad / Time.fixedDeltaTime); // FixedUpdate에서 호출되므로 Time.fixedDeltaTime 사용
    }
}