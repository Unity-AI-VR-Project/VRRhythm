using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRRayInteractor))]
public class ToggleRay : MonoBehaviour
{
    public XRRayInteractor rayInteractor;
    private InputDevice rightController;
    private bool wasTouched = false;
    public GameObject blad;

    private void Start()
    {
        rayInteractor = GetComponent<XRRayInteractor>();
        rayInteractor.enabled = false; // 시작 시 비활성화

        // 오른손 컨트롤러 초기화
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, devices);
        if (devices.Count > 0)
        {
            rightController = devices[0];
            Debug.Log(rightController.name);
        }
    }

    private void Update()
    {
        if (GameManager.Instance.sceneController.currentScene == Define.eScenes.InGame)
            return;
        if (!rightController.isValid)
            return;

        bool isTouched;
        if (rightController.TryGetFeatureValue(CommonUsages.primary2DAxisTouch, out isTouched))
        {
            if (isTouched != wasTouched) // 변화가 있을 때만 처리
            {
                rayInteractor.enabled = isTouched;
                blad.SetActive(!isTouched);
            }
            wasTouched = isTouched;
        }
    }
}