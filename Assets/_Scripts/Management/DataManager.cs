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
        // 1. StreamingAssets 폴더 내의 MusicList.json 파일을 먼저 로드합니다.
        //string filePath = Path.Combine(Application.streamingAssetsPath, "MusicList.json");
        TextAsset temp = Resources.Load<TextAsset>("MusicList");
        string jsonString = temp.text;
        musicData = JsonUtility.FromJson<MusicData>(jsonString);
        //if (File.Exists(filePath))
        //{
        //    string jsonString = File.ReadAllText(filePath);
        //    musicData = JsonUtility.FromJson<MusicData>(jsonString); // MusicData 객체에 파싱
        //    Debug.Log($"DataManager: MusicList.json 로드 성공. 총 {musicData.Music.Length}개 음악 목록.");
        //}
        //else
        //{
        //    Debug.LogError("DataManager: MusicList.json 파일을 찾을 수 없습니다: " + filePath);
        //}

        // MusicData가 성공적으로 로드되었다면, 개별 음악 데이터 로드를 시작합니다.
        if (musicData != null)
            LoadMusicRootData();
    }

    private void LoadMusicRootData()
    {
        // MusicData.Music 배열의 길이에 맞춰 RootData 배열을 초기화합니다.
        musicRootDatas = new RootData[musicData.Music.Length];

        // 각 음악 파일 (0.json, 1.json 등)을 순회하며 로드합니다.
        for (int i = 0; i < musicData.Music.Length; i++)
        {
            // "MusicData" 폴더 안에서 "{i}.json" 형태의 파일을 찾습니다.
            //string filePath = Path.Combine(Application.streamingAssetsPath, "MusicData", $"{i}.json");
            TextAsset temp = Resources.Load<TextAsset>("MusicData/" + i);
            string jsonString = temp.text;
            RootData rootData = JsonUtility.FromJson<RootData>(jsonString);
            musicRootDatas[i] = rootData; // 로드된 RootData를 배열에 저장
            //if (File.Exists(filePath))
            //{
            //    string jsonString = File.ReadAllText(filePath);
            //    RootData rootData = JsonUtility.FromJson<RootData>(jsonString);
            //    musicRootDatas[i] = rootData; // 로드된 RootData를 배열에 저장
            //    Debug.Log($"DataManager: MusicData/{i}.json 로드 성공. 곡명: {rootData.metadata.band_group}, BPM: {rootData.metadata.tempo}");
            //}
            //else
            //{
            //    Debug.LogError($"DataManager: MusicData/{i}.json 파일을 찾을 수 없습니다: {filePath}");
            //}
        }
        Debug.Log($"DataManager: 총 {musicRootDatas.Length}개의 개별 음악 데이터 로드 완료.");
    }
}
