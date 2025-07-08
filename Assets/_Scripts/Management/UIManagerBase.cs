using UnityEngine;

public abstract class UIManagerBase : MonoBehaviour
{
    protected virtual void Awake()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterUIManager(this);
        }

        InitializeUI();
    }

    protected virtual void InitializeUI()
    {

    }

    protected virtual void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterUIManager(this);
    }
}
