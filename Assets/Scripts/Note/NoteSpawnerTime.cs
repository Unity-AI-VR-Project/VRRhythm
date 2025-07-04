using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable]
public class NoteTimer
{
    [SerializeField]
    public List<List<float>> times;
}

public class NoteSpawnerTime : MonoBehaviour
{
    public MusicTimeChacker timeChacker;
    
    public GameObject notePrefab;
    public double[] spawnIndex;
    
    public string fileName;
    
    private int counter;

    public NoteTimer timer;

    void Start()
    {
        TextAsset data = Resources.Load<TextAsset>(fileName);
        timer = JsonUtility.FromJson<NoteTimer>(data.text);
        

        if (data == null)
        {
            Debug.LogError("텍스트 파일을 불러올 수 없습니다.");
            return;
        }

        // string[] timeStrings = data.text.Split(new char[] {'\n'}, StringSplitOptions.RemoveEmptyEntries);
        Debug.LogError(data.text);
        string[] timeStrings = data.text.Split('\n');
        spawnIndex = new double[timeStrings.Length];
        
        
        for (int i = 0; i < timeStrings.Length; i++)
        {
            if (double.TryParse(timeStrings[i], out double time))
            {
                spawnIndex[i] = time;
            }
            else
            {
                Debug.LogWarning($"잘못된 숫자 형식 (index {i}): {timeStrings[i]}");
            }
        }

        counter = 0;
    }

    void Update()
    {
        if (counter < spawnIndex.Length)
        {
            if (timeChacker.elapsedTime > spawnIndex[counter])
            {
                Debug.Log("노트 생성" + spawnIndex[counter]);
                SpawnNote();
                counter++;
            }
        }
        else
        {
            Debug.Log("생성할 인덱스가 없습니다.");
        }
        
    }
    

    void SpawnNote()
    {
        Instantiate(notePrefab, transform.position, transform.rotation);
    }
}