using Define;
using UnityEngine;

public class ChatUI : MonoBehaviour
{
    [SerializeField] private Transform chatParent;

    private void OnEnable()
    {
        LoadChat();
    }

    private void initializeChat()
    {
        foreach(Transform child in chatParent)
        {
            Destroy(child.gameObject);
        }
    }

    private void LoadChat()
    {
        initializeChat();
        eScenes current = GameManager.Instance.sceneController.currentScene;
        foreach (ChatLog log in GameManager.Instance.dataManager.chatLogs)
        {
            if (log.scene == current)
            {
                GameObject chat = GameManager.Instance.chatManager.CreateChatObject(log);
                chat.transform.parent = chatParent;
                chat.GetComponent<ChatObject>().SetChatObject(log.chatObjectData);
            }
        }
    }
}
