using UnityEngine;
using System;

public class GameManager : Singleton<GameManager>
{
    public SceneController sceneController;
    public UIManagerBase currentUIManager;
    public SoundManager soundManager;
    public ChatManager chatManager;
    public DataManager dataManager;
    public AIManager aiManager;

    bool isInitialized = false;
    public bool isDeveloping = true; // 자동생성 됐을때 기본값이 true로 다른 기능 테스트에 방해되지 않도록 설정

    protected override void Awake()
    {
        base.Awake();
        if (!isInitialized && !isDeveloping)
        {
            Initialize();
        }
        DontDestroyOnLoad(gameObject);
    }

    private void Initialize()
    {
        InitializeManager();

        isInitialized = true;
    }


    private void InitializeManager()
    {
        if (sceneController == null)
        {
            sceneController = FindComponent<SceneController>(typeof(SceneController), transform);
        }
        if (soundManager == null)
        {
            soundManager = FindComponent<SoundManager>(typeof(SoundManager), transform);
        }
        if (chatManager == null)
        {
            chatManager = FindComponent<ChatManager>(typeof(ChatManager), transform);
        }
        if (dataManager == null)
        {
            dataManager = FindComponent<DataManager>(typeof(DataManager), transform);
        }
        if (aiManager == null)
        {
            //aiManager = FindComponent<AIManager>(typeof(AIManager), transform);
        }
    }

    public T FindComponent<T>(Type component,Transform parent)
    {
        Debug.Log($"{component.Name}가 {parent.name}의 하위 객체에 존재하지 않습니다.\n {parent.name}의 하위 객체로 {component.Name}을 생성합니다.");
        foreach (Transform child in parent)
        {
            if (TryGetComponent<T>(out T instance))
            {
                return instance;
            }
        }
        GameObject sceneObj = new GameObject(typeof(T).Name, typeof(T));
        sceneObj.transform.SetParent(parent);
        sceneObj.transform.localPosition = Vector3.zero;

        return sceneObj.GetComponent<T>();
    }

    public void RegisterUIManager(UIManagerBase uiManager)
    {
        if (currentUIManager != uiManager)
        {
            currentUIManager = uiManager;
        }
    }

    public void UnregisterUIManager(UIManagerBase uiManager)
    {
        if (currentUIManager == uiManager)
        {
            currentUIManager = null;
        }
    }
}
