using UnityEngine;

public abstract class UIManagerBase : ManagerBase
{
    protected override void Awake()
    {
        base.Awake();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterUIManager(this);
        }
    }

    protected override void Initialize()
    {
        base.Initialize();
        InitializeUI();
    }

    protected virtual void InitializeUI()
    {
        if (GameManager.Instance.currentUIManager == null)
        {
            GameManager.Instance.currentUIManager = this;
        }
        else
        {
            Debug.LogWarning("UIManagerBase: Another UIManager is already registered. This may cause unexpected behavior.", this);
        }
    }

    protected virtual void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterUIManager(this);
    }
}
