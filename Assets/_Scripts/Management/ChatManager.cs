using UnityEngine;
using Define;

public class ChatManager : ManagerBase
{
    [SerializeField] GameObject[] chatPrefabs;
    public ChatUI chatUI;
    protected override void Awake()
    {
        base.Awake();
    }

    private void OnEnable()
    {
        if (chatUI == null)
        {
            chatUI = FindAnyObjectByType<ChatUI>();
        }
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    public GameObject CreateChatObject(ChatLog chatLog,Transform parent)
    {
        int prefabType = 0;
        switch (chatLog.scene)
        {
            case eScenes.Title: // 타이틀에서는 채팅창 사용 X
                return null;
            case eScenes.Lobby:
                prefabType = 0;
                break;
            case eScenes.InGame:
                prefabType = 1;
                break;
        }
        RectTransform obj = Instantiate(chatPrefabs[prefabType],parent).GetComponent<RectTransform>();

        obj.anchoredPosition = Vector3.zero;
        obj.localRotation = Quaternion.identity;
        obj.localScale = Vector3.one;
        return obj.gameObject;
    }
}
