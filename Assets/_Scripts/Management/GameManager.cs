using Unity.VisualScripting;

public class GameManager : Singleton<GameManager>
{
    public SceneController sceneController;
    public UIManagerBase currentUIManager;

    bool isInitialized = false;

    protected override void Awake()
    {
        base.Awake();
        if (!isInitialized)
        {
            Initialize();
        }
    }

    private void Initialize()
    {
        InitializeSceneController();

        isInitialized = true;
    }

    private void InitializeSceneController()
    {
        if (TryGetComponent<SceneController>(out SceneController controller))
        {
            sceneController = controller;
        }
        else
        {
            sceneController = transform.AddComponent<SceneController>();
        }
    }

    public void RegisterUIManager(UIManagerBase uiManager)
    {
        if (currentUIManager != uiManager)
        {
            currentUIManager = uiManager;
        }
    }

    public void UnregisterUIManager(UIManagerBase uiManager)
    {
        if (currentUIManager == uiManager)
        {
            currentUIManager = null;
        }
    }
}
