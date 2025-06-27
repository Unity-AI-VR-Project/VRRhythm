using UnityEngine;
using System.Collections.Generic;

public class NotePoolManager : MonoBehaviour
{
    public static NotePoolManager Instance;

    public GameObject notePrefab;
    public int poolSize = 50;

    private Queue<GameObject> notePool = new Queue<GameObject>();

    void Awake()
    {
        Instance = this;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(notePrefab);
            obj.SetActive(false);
            notePool.Enqueue(obj);
        }
    }

    public GameObject SpawnNote(Vector3 position)
    {
        GameObject obj = null;

        if (notePool.Count > 0)
        {
            obj = notePool.Dequeue();
        }
        else
        {
            // 풀을 초과할 경우 새로 생성 (선택)
            obj = Instantiate(notePrefab);
        }

        obj.transform.position = position;
        obj.SetActive(true);
        return obj;
    }

    public void ReturnNote(GameObject obj)
    {
        obj.SetActive(false);
        notePool.Enqueue(obj);
    }
}
