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

    }

    protected virtual void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.UnregisterUIManager(this);
    }
}
