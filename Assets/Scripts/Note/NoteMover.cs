using System;
using UnityEngine;
using System.Collections;

/// <summary>
/// 노트 오브젝트를 이동시키는 클래스
/// </summary>
public class NoteMover : MonoBehaviour
{
    public Transform spawner;
    
    // === 위치 설정 ===
    private Transform spawnPoint;   // 노트가 생성되는 시작 위치 (지점 C)
    private Transform setPoint;     // 노트가 처음 멈추는 지점 (지점 A)
    private Transform targetPoint;  // 노트가 연주 타이밍에 도달해야 하는 위치 (지점 B)


    [Header("Tempo Settings")]
    public static float bpm = 102f;              // 비트당 분당 박자 수 (1분에 몇 박자)
    public float beatsPerNote = 2f;       // 노트가 생성되고 도달하기까지 걸리는 비트 수

    [Range(0f, 1f)]
    public float moveToA_Ratio = 0.4f;    // 오브젝트 이동 시 C→A 구간이 전체에서 차지하는 비율

    [Header("Stop Settings")]

    // 내부 변수들
    private float beatTime = 60f / bpm;        // 1비트당 시간 (초)
    private int noteIndex = 0;     // 현재까지 생성된 노트 수
    private float timer = 0f;      // 시간 누적용 타이머

    private void OnEnable()
    {
        // 위치 포인트 자동 설정
        if (spawner != null)
        {
            AssignPointsFromParent(spawner);
        }
        else
        {
            Debug.LogWarning("movementPointsParent가 설정되지 않았습니다.");
        }
        
        StartCoroutine(MoveNote_CAB(gameObject));
        Invoke("DestroyObject", 4f);
    }

    void DestroyObject()
    {
        Destroy(gameObject);
    }


    // 노트가 C → A → B로 정해진 시간 동안 이동
    IEnumerator MoveNote_CAB(GameObject note)
    {
        float totalTravelTime = beatTime * beatsPerNote;  // 전체 이동 시간

        float moveToA_Duration = totalTravelTime * moveToA_Ratio;         // C → A 이동 시간
        float moveToB_Duration = totalTravelTime * (1f - moveToA_Ratio);  // A → B 이동 시간

        // C → A 이동
        yield return StartCoroutine(MoveSegment(note, spawnPoint.position, setPoint.position, moveToA_Duration));

        // A → B 이동 (연주 시점 도달 이후에는 계속 전진)
        yield return StartCoroutine(MoveSegment(note, setPoint.position, targetPoint.position, moveToB_Duration, true));
    }

    // 오브젝트를 from → to까지 duration 시간 동안 이동
    IEnumerator MoveSegment(GameObject obj, Vector3 from, Vector3 to, float duration, bool keepGoingAfter = false)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            float easedT = Mathf.Pow(t, 0.60f);
            obj.transform.position = Vector3.Lerp(from, to, easedT);  // 선형 보간을 통한 위치 이동
            yield return null;
        }

        // B 지점 도달 이후 계속 전진하도록 설정된 경우
        if (keepGoingAfter)
        {
            Vector3 dir = (to - from).normalized;  // 이동 방향 벡터
            float speed = Vector3.Distance(from, to) / duration; // 이동 속도 계산

            while (true)
            {
                obj.transform.position += dir * speed * Time.deltaTime;
                yield return null;
            }
        }
    }
    void AssignPointsFromParent(Transform parent)
    {
        foreach (Transform child in parent)
        {
            switch (child.name)
            {
                case "SpawnPos":
                    spawnPoint = child;
                    break;
                case "SetPos":
                    setPoint = child;
                    break;
                case "TargetPos":
                    targetPoint = child;
                    break;
            }
        }

        // 할당되지 않은 경우 경고
        if (!spawnPoint) Debug.LogWarning("SpawnPos 자식 Transform을 찾지 못했습니다.");
        if (!setPoint) Debug.LogWarning("SetPos 자식 Transform을 찾지 못했습니다.");
        if (!targetPoint) Debug.LogWarning("TargetPos 자식 Transform을 찾지 못했습니다.");
    }
}
