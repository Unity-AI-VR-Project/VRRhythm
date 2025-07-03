using UnityEngine;

public class RotationManager : MonoBehaviour
{
    [Tooltip("회전 핸들러가 붙은 GameObjects 배열")]
    public GameObject[] gameObjects;

    /// <summary>
    /// Z축 회전 속도 설정 (단일 float 인수)
    /// </summary>
    public void SetRotationSpeed(int index, float zSpeed)
    {
        var handler = GetHandler(index);
        if (handler != null)
        {
            handler.rotationSpeed = new Vector3(0f, 0f, zSpeed);
        }
    }

   

    /// <summary>
    /// RotatingObjectHandler 가져오기
    /// </summary>
    public RotatingObjectHandler GetHandler(int index)
    {
        if (index < 0 || index >= gameObjects.Length)
        {
            Debug.LogWarning($"[RotationManager] 인덱스 {index}가 범위를 벗어났습니다.");
            return null;
        }

        var handler = gameObjects[index].GetComponent<RotatingObjectHandler>();
        if (handler == null)
        {
            Debug.LogWarning($"[RotationManager] {gameObjects[index].name}에 RotatingObjectHandler가 없습니다.");
        }

        return handler;
    }
}
