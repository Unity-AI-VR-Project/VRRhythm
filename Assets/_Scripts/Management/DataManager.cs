using Define;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DataManager : ManagerBase
{
    public List<ChatLog> chatLogs = new List<ChatLog>();
    public MusicData musicData;
    public RootData[] musicRootDatas;
    public int selectedMusicNumber;

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
        if (musicData != null)
            LoadMusicRootData();
    }

    private void LoadMusicRootData()
    {
        musicRootDatas = new RootData[musicData.Music.Length];
        for (int i = 0; i<musicData.Music.Length; i++)
        {
            string filePath = Path.Combine(Application.streamingAssetsPath, "MusicData",$"{i}.json");

            if (File.Exists(filePath))
            {
                string jsonString = File.ReadAllText(filePath);
                RootData rootData = JsonUtility.FromJson<RootData>(jsonString);
                musicRootDatas[i] = rootData;
            }
        }
    }
}
