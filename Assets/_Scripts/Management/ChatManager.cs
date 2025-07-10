using UnityEngine;
using Define;

public class ChatManager : ManagerBase
{
    [SerializeField] GameObject[] chatPrefabs;

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Initialize()
    {
        base.Initialize();
    }

    public void CreateChatObject(ChatObjectData chatObjectData) 
    {
        int prefabType = 0;
        switch (GameManager.Instance.sceneController.currentScene)  
        {
            case eScenes.Title: // 타이틀에서는 채팅창 사용 X
                return;
            case eScenes.Lobby:
                prefabType = 0;
                break;
            case eScenes.InGame:
                prefabType = 1;
                break;
        }
        Instantiate(chatPrefabs[prefabType]);
    }
}
