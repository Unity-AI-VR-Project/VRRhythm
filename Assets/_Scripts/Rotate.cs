using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

public class Rotate : MonoBehaviour
{
    [System.Serializable]
    public class RotatingObject
    {
        public Transform target;
        public Vector3 offset;  // 시작 회전 오프셋
        public Material materialToChange; // 회전 방향 따라 색상 바꾸는 대상 메터리얼

        [HideInInspector] public Quaternion previousRotation;
    }

    public List<RotatingObject> objectsToRotate;
    public Vector3 rotationSpeed = new Vector3(0f, 0f, 0.5f); // 도/초 기준 회전 속도

    public Color clockwiseColor = Color.green;
    public Color counterClockwiseColor = Color.red;

    void Start()
    {
        foreach (var obj in objectsToRotate)
        {
            if (obj.target != null)
            {
                obj.target.rotation *= Quaternion.Euler(obj.offset);
                obj.previousRotation = obj.target.rotation;
            }
        }
    }

    void Update()
    {
        foreach (var obj in objectsToRotate)
        {
            if (obj.target == null) continue;

            // 회전 적용
            obj.target.Rotate(rotationSpeed * Time.deltaTime, Space.Self);

            // 회전 방향 판단 (Z축 기준)
            float prevZ = obj.previousRotation.eulerAngles.z;
            float currentZ = obj.target.rotation.eulerAngles.z;

            float deltaZ = Mathf.DeltaAngle(prevZ, currentZ);

            bool clockwise = deltaZ > 0f;

            // 색상 변경 예시
            if (obj.materialToChange != null)
            {
                obj.materialToChange.color = clockwise ? clockwiseColor : counterClockwiseColor;
            }

            // 현재 회전 저장
            obj.previousRotation = obj.target.rotation;
        }
    }
}
