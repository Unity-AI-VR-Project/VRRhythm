using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ButtonSetting : MonoBehaviour
{
    [Header("이동 설정")]
    [Tooltip("오브젝트가 도달할 목표 위치입니다.")]
    public Vector3 targetPosition;

    [Tooltip("오브젝트의 이동 속도입니다.")]
    [Range(0.1f, 10.0f)] // 속도 조절을 위한 슬라이더
    public float moveSpeed = 1.0f;

    private void Start()
    {
        Transform player = FindAnyObjectByType<XRController>().transform;
        targetPosition.y = player.position.y+.5f;
    }

    private void Update()
    {
        // 현재 위치와 목표 위치가 다를 경우에만 이동
        if (transform.position != targetPosition)
        {
            // 현재 위치에서 목표 위치까지 moveSpeed 속도로 이동
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        }
    }
}