using UnityEngine;

public class AIManager : ManagerBase
{
    WhisperController whisperController;
    
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Initialize()
    {
        InitializeWhisper();

        base.Initialize();
    }

    private void InitializeWhisper()
    {
        if (whisperController == null)
        {
            whisperController = GameManager.Instance.FindComponent<WhisperController>(typeof(WhisperController), transform);
        }
    }
}
