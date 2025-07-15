using Define;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DataManager : ManagerBase
{
    public List<ChatLog> chatLogs = new List<ChatLog>();
    public MusicData musicData;
    protected override void Awake()
    {
        base.Awake();
    }

    protected override void Initialize()
    {
        base.Initialize();
        LoadMusicData();
    }

    private void LoadMusicData()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "MusicList.json");

        if (File.Exists(filePath))
        {
            string jsonString = File.ReadAllText(filePath);
            musicData = JsonUtility.FromJson<MusicData>(jsonString);
        }
        else
        {
            Debug.LogError("파일을 찾을 수 없습니다: " + filePath);
        }
    }
}
