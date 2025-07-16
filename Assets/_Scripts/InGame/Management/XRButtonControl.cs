using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

public class XRButtonControl : MonoBehaviour
{
    private InputDevice rightController;
    public GameObject UserCanvas;

    private bool wasPressed = false;

    private void Start()
    {
        TryInitializeController();
    }

    private void TryInitializeController()
    {
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
        if (devices.Count > 0)
        {
            rightController = devices[0];
            Debug.Log("오른손 컨트롤러 연결됨: " + rightController.name);
        }
    }

    private void Update()
    {
        if (!rightController.isValid)
        {
            TryInitializeController(); // 재시도
            return;
        }

        bool isPressed = false;
        if (rightController.TryGetFeatureValue(CommonUsages.secondaryButton, out isPressed))
        {
            if (isPressed && !wasPressed)
            {
                ToggleCanvas(); // B 버튼을 눌렀을 때만 실행 (한 번만)
            }
            wasPressed = isPressed;
        }
    }

    private void ToggleCanvas()
    {
        if (GameManager.Instance.sceneController.currentScene == Define.eScenes.InGame)
            return;
        if (UserCanvas != null)
        {
            UserCanvas.SetActive(!UserCanvas.activeSelf);
        }
    }
}