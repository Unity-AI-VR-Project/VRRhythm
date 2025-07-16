using Define;
using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class ChatUI : MonoBehaviour
{
    [SerializeField] private Transform chatParent;
    [SerializeField] TextMeshProUGUI displayTMP;
    private void Awake()
    {
        GameManager.Instance.chatManager.chatUI = this;
    }

    private void Start()
    {
        LoadChat();
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
        if (displayTMP != null)
        {
            displayTMP.text = $"<sprite={sentiment}>";
            StartCoroutine("DisplayEmojiTimer");
        }
    }

    private IEnumerator DisplayEmojiTimer()
    {
        yield return new WaitForSeconds(0.7f);
        displayTMP.text = "";
    }

    public void LoadChat()
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
