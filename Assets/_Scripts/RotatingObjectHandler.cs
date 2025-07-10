using UnityEngine;
using UnityEngine.Events;

public class RotatingObjectHandler : MonoBehaviour
{
    public Vector3 offset;
    public Vector3 rotationSpeed;
    private bool isReverseRotating = false;

    [System.Serializable]
    public class RotationDirectionEvent : UnityEvent<bool> { }
    public RotationDirectionEvent onRotationDirectionChanged;

    private Quaternion previousRotation;
    private Renderer objectRenderer;

    [Header("Gradient Settings")]
    public Gradient clockwiseGradient;
    public Gradient counterClockwiseGradient;

    public float gradientCycleSpeed = 0.2f; // 얼마나 빠르게 색이 그래디언트에서 흐를지 (초당 변화율)

    private bool currentDirectionClockwise = true;
    private float directionTime = 0f;

    void Start()
    {
        transform.rotation *= Quaternion.Euler(offset);
        previousRotation = transform.rotation;

        objectRenderer = GetComponent<Renderer>();
    }

    void Update()
    {
        if (!isReverseRotating)
        {
            RotateAndApplyGradient();
        }
    }

    public void RotateAndApplyGradient()
    {
        // 회전 적용
        transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);

        // 회전 방향 감지
        float prevZ = previousRotation.eulerAngles.z;
        float currentZ = transform.rotation.eulerAngles.z;
        float deltaZ = Mathf.DeltaAngle(prevZ, currentZ);

        bool clockwise = deltaZ > 0f;

        // 회전 방향이 바뀌었을 때 이벤트 및 타이머 리셋
        if (clockwise != currentDirectionClockwise)
        {
            currentDirectionClockwise = clockwise;
            directionTime = 0f;

            onRotationDirectionChanged?.Invoke(clockwise);
        }

        // 타이머 갱신 (0~1 루프)
        directionTime += Time.deltaTime * gradientCycleSpeed;
        if (directionTime > 1f) directionTime -= 1f;

        // 현재 방향에 맞는 그래디언트 적용
        Color newColor = currentDirectionClockwise
            ? clockwiseGradient.Evaluate(directionTime)
            : counterClockwiseGradient.Evaluate(directionTime);

        if (objectRenderer != null)
        {
            objectRenderer.material.color = newColor;
        }

        previousRotation = transform.rotation;
    }

    public void ReversRotate(float x, float y, float z)
    {
        isReverseRotating = true;
        transform.Rotate(x, y, z);

        float prevZ = previousRotation.eulerAngles.z;
        float currentZ = transform.rotation.eulerAngles.z;
        float deltaZ = Mathf.DeltaAngle(prevZ, currentZ);
        bool clockwise = deltaZ > 0f;

        if (clockwise != currentDirectionClockwise)
        {
            currentDirectionClockwise = clockwise;
            directionTime = 0f;

            onRotationDirectionChanged?.Invoke(clockwise);
        }

        directionTime += Time.deltaTime * gradientCycleSpeed;
        if (directionTime > 1f) directionTime -= 1f;

        Color newColor = currentDirectionClockwise
            ? clockwiseGradient.Evaluate(directionTime)
            : counterClockwiseGradient.Evaluate(directionTime);

        if (objectRenderer != null)
        {
            objectRenderer.material.color = newColor;
        }

        previousRotation = transform.rotation;
    }
}
