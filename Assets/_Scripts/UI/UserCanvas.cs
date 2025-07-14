using UnityEngine;
using Define;

public class UserCanvas : MonoBehaviour
{
    [SerializeField] private GameObject[] buttonObjects = new GameObject[(int)eButton.Count];

    private void OnEnable()
    {
        SetButton();
    }

    private void SetButton()
    {
        InitializeButtons();
        eButton[] buttons = { };
        switch (GameManager.Instance.sceneController.currentScene)
        {
            case eScenes.Title:
                buttons = new[] { eButton.Settings,eButton.Lobby, eButton.Close,eButton.Exit };
                break;
            case eScenes.Lobby:
                buttons = new[] { eButton.Chat, eButton.Settings, eButton.Close,eButton.Exit };
                break;
            case eScenes.InGame:
                buttons = new[] { eButton.Continue, eButton.Restart, eButton.Settings, eButton.Lobby };
                break;
        }
        ActiveButtons(buttons);
    }

    public GameObject GetButtonObject(eButton button)
    {
        return buttonObjects[(int)button];
    }

    private void ActiveButtons(eButton[] buttons)
    {
        foreach (eButton button in buttons)
        {
            GameObject buttonObject = GetButtonObject(button);
            if (buttonObject != null)
            {
                buttonObject.SetActive(true);
            }
        }
    }

    private void InitializeButtons()
    {
        foreach (GameObject buttonObject in buttonObjects)
        {
            if (buttonObject != null)
            {
                buttonObject.SetActive(false);
            }
        }
    }
}
