using UnityEngine;

public abstract class ManagerBase : MonoBehaviour
{
    protected bool isInitialized = false;

    // ManagerBase의 Awake는 하위 클래스에서 기본적으로 호출됩니다.
    protected virtual void Awake()
    {
        if (!isInitialized)
        {
            Initialize();
        }
    }

    // ManagerBase의 Initialize는 하위 클래스에서 오버라이드하여 구체적인 초기화 로직을 구현합니다.
    protected virtual void Initialize()
    {
        isInitialized = true;
        Debug.Log($"{this.GetType().Name} 초기화 완료.");
    }
}