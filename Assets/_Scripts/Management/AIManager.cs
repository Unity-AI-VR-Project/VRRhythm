using UnityEngine;

public class AIManager : ManagerBase
{
    public RunWhisper whisperController;
    public SAController saController;
    
    protected override void Awake()
    {
        base.Awake();
    }

    private void Update()
    {
        if (whisperController == null)
        {
            UnityEngine.Debug.LogError("AIManager: whisperController가 할당되지 않았습니다. Inspector에서 RunWhisper 컴포넌트를 할당해주세요.");
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!whisperController.GetAudioProcessor().IsRecording && !whisperController.IsProcessingAudio)
            {
                whisperController.StartRecordingPublic();
            }
        }

        if (whisperController.GetAudioProcessor().IsRecording)
        {
            if (Input.GetKeyUp(KeyCode.Space) ||
                (Time.time - whisperController.GetAudioProcessor().RecordingStartTime >= whisperController.GetAudioProcessor().maxRecordingSeconds))
            {
                whisperController.StopRecordingPublic();
            }
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            if (!whisperController.GetAudioProcessor().IsRecording && !whisperController.IsProcessingAudio)
            {
                whisperController.TestModelPerformancePublic();
            }
        }
    }

    protected override void Initialize()
    {
        InitializeWhisper();

        base.Initialize();
    }

    private void InitializeWhisper()
    {
        if (saController == null)
        {
            saController = GameManager.Instance.FindComponent<SAController>(typeof(SAController), transform);
        }
    }
}
