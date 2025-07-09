using UnityEngine;

public class DestroyAfterDelay : MonoBehaviour
{
    [Tooltip("오브젝트가 파괴되기까지의 지연 시간 (초).")]
    public float delay = 3.0f; // 기본 3초로 설정, 필요에 따라 유니티 에디터에서 조정 가능

    void Start()
    {
        // 'delay' 시간 후에 이 GameObject를 파괴합니다.
        Destroy(gameObject, delay);
    }
}