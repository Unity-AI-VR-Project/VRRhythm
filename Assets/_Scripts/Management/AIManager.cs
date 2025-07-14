using UnityEngine;

public class AIManager : ManagerBase
{
    public WhisperController whisperController;
    public SAController saController;
    
    protected override void Awake()
    {
        base.Awake();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            whisperController.record.StartRecord();
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            whisperController.record.StopRecord();
        }
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
        if (saController == null)
        {
            saController = GameManager.Instance.FindComponent<SAController>(typeof(SAController), transform);
        }
    }
}
