using Define;
using System;
using UnityEngine;

public class ChatUI : MonoBehaviour
{
    [SerializeField] private Transform chatParent;

    private void Awake()
    {
        GameManager.Instance.chatManager.chatUI = this;
    }

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

    public void AddChat(string content, int sentiment)
    {
        ChatObjectData data = new ChatObjectData(DateTime.Now, content, sentiment);
        ChatLog log = new ChatLog(GameManager.Instance.sceneController.currentScene ,data);
        GameManager.Instance.dataManager.chatLogs.Add(log);
        GameObject chat = GameManager.Instance.chatManager.CreateChatObject(log,chatParent);
        chat.GetComponent<ChatObject>().SetChatObject(log.chatObjectData);
    }

    private void LoadChat()
    {
        initializeChat();
        eScenes current = GameManager.Instance.sceneController.currentScene;
        foreach (ChatLog log in GameManager.Instance.dataManager.chatLogs)
        {
            if (log.scene == current)
            {
                GameObject chat = GameManager.Instance.chatManager.CreateChatObject(log,chatParent);
                chat.GetComponent<ChatObject>().SetChatObject(log.chatObjectData);
            }
        }
    }
}
