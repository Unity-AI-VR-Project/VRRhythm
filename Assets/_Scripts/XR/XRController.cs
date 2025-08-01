using UnityEngine;
using UnityEngine.InputSystem;
using Define;
public class XRController : MonoBehaviour
{
    [SerializeField] private InputAction leftTriggerAction;
    [SerializeField] private InputAction rightTriggerAction;

    private float currentLeftTriggerValue = 0f;
    private float currentRightTriggerValue = 0f;
    private bool isBothTriggersPressed = false;

    private void Awake()
    {
        if (leftTriggerAction != null)
        {
            leftTriggerAction.performed += ctx => currentLeftTriggerValue = ctx.ReadValue<float>();
            leftTriggerAction.canceled += ctx => currentLeftTriggerValue = 0f; 
        }
        if (rightTriggerAction != null)
        {
            rightTriggerAction.performed += ctx => currentRightTriggerValue = ctx.ReadValue<float>();
            rightTriggerAction.canceled += ctx => currentRightTriggerValue = 0f;
        }
    }

    void Update()
    {
        if (GameManager.Instance.sceneController.currentScene == eScenes.Title)
            return;
        if (currentLeftTriggerValue > 0.8f && currentRightTriggerValue > 0.8f && !isBothTriggersPressed)
        {
            isBothTriggersPressed = true;
            Debug.Log("양쪽 트리거 모두 0.8 이상 눌림! 녹음 시작.");

            if (GameManager.Instance != null && GameManager.Instance.aiManager != null && GameManager.Instance.aiManager.whisperController != null)
            {
                GameManager.Instance.aiManager.whisperController.StartRecordingPublic();
            }
            else
            {
                Debug.LogError("GameManager 또는 관련 컴포넌트가 제대로 초기화되지 않았습니다. StartRecordingPublic 호출 실패.");
            }
        }
        else if ((currentLeftTriggerValue <= 0.8f || currentRightTriggerValue <= 0.8f) && isBothTriggersPressed)
        {
            isBothTriggersPressed = false;
            Debug.Log("양쪽 트리거 눌림 해제.");
            GameManager.Instance.aiManager.whisperController.StopRecordingPublic();

        }
    }

    void OnEnable()
    {
        if (leftTriggerAction != null)
            leftTriggerAction.Enable();
        if (rightTriggerAction != null)
            rightTriggerAction.Enable();
    }

    void OnDisable()
    {
        if (leftTriggerAction != null)
            leftTriggerAction.Disable();
        if (rightTriggerAction != null)
            rightTriggerAction.Disable();
    }
}
