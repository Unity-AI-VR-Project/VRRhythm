using UnityEngine;

public abstract class ManagerBase : MonoBehaviour
{
    protected bool isInitialized = false;
    protected virtual void Awake()
    {
        if (!isInitialized)
        {
            Initialize();
        }
    }

    protected virtual void Initialize()
    {
        isInitialized = true;
    }
}
