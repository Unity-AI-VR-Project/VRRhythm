using UnityEngine;
using System.Collections.Generic;

public class NotePoolManager : MonoBehaviour
{
    public static NotePoolManager Instance;  // 싱글톤 인스턴스

    public GameObject notePrefab;            // 생성할 노트의 프리팹
    public int poolSize = 50;                // 초기 풀에 넣어둘 노트 수

    private Queue<GameObject> notePool = new Queue<GameObject>();  // 노트를 저장할 큐(오브젝트 풀)

    void Awake()
    {
        Instance = this;  // 싱글톤 초기화

        // 풀에 미리 notePrefab을 poolSize만큼 생성하여 비활성화한 후 저장
        for (int i = 0; i < poolSize; i++)
        {
            GameObject obj = Instantiate(notePrefab);
            obj.SetActive(false);
            notePool.Enqueue(obj);
        }
    }

    // 노트를 풀에서 꺼내 원하는 위치에 소환
    public GameObject SpawnNote(Vector3 position)
    {
        GameObject obj = null;

        if (notePool.Count > 0)
        {
            // 대기 중인 노트가 있으면 꺼내서 재사용
            obj = notePool.Dequeue();
        }
        else
        {
            // 풀이 비었으면 새로 생성 (필요에 따라 허용 여부 결정 가능)
            obj = Instantiate(notePrefab);
        }

        // 위치 설정 및 활성화
        obj.transform.position = position;
        obj.SetActive(true);
        return obj;
    }

    // 노트를 다시 풀에 반환
    public void ReturnNote(GameObject obj)
    {
        obj.SetActive(false);       // 비활성화 후
        notePool.Enqueue(obj);      // 풀에 다시 넣음
    }
}