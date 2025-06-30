using UnityEngine;
using System.Collections;

// 노트 생성과 이동을 제어하는 클래스
public class NoteMover : MonoBehaviour
{
    public Transform spawnPoint;      // 노트가 생성되는 시작 위치 (지점 C)
    public Transform setPoint;        // 노트가 경유하는 중간 위치 (지점 A)
    public Transform targetPoint;     // 노트가 도착하는 최종 위치 (지점 B)
    public AudioSource audio;         // 음악 재생을 위한 오디오 소스

    [Header("Tempo Settings")]
    public float bpm = 120f;                 // 곡의 BPM (분당 박자 수)
    public float beatsPerNote = 2f;          // 노트가 생성 후 몇 박자 뒤에 도착해야 하는지

    [Range(0f, 1f)]
    public float moveToA_Ratio = 0.4f;       // 전체 이동 시간 중 C → A에 사용되는 비율

    [Header("Spawn Settings")]
    public int maxNoteCount = 100;           // 생성할 최대 노트 수
    public float spawnIntervalInBeats = 1.0f; // 노트 생성 간격 (단위: 박자)

    // 내부 변수들
    private float beatTime;         // 한 박자에 걸리는 시간 (초)
    private float spawnInterval;    // 노트 생성 간격을 초로 환산한 값
    private int noteIndex = 0;      // 현재 생성한 노트 수
    private float timer = 0f;       // 시간 누적용 타이머

    // 초기화 시 호출
    void Start()
    {
        beatTime = 60f / bpm;                           // BPM을 초 단위로 환산
        spawnInterval = beatTime * spawnIntervalInBeats; // 노트 생성 주기 설정
    }

    // 매 프레임마다 실행
    void Update()
    {
        if (noteIndex >= maxNoteCount) return;  // 최대 개수 도달 시 종료

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer -= spawnInterval;
            SpawnNote();   // 노트 생성
            noteIndex++;   // 생성 수 증가
        }
    }

    // 노트 생성 시 호출
    void SpawnNote()
    {
        // 첫 노트 생성 시 음악 재생
        if (noteIndex == 0 && audio != null && !audio.isPlaying)
        {
            audio.Play();
        }

        // 노트 풀에서 가져와 시작 위치에 배치
        GameObject note = NotePoolManager.Instance.SpawnNote(spawnPoint.position);

        // 노트 이동 코루틴 실행 (C → A → B)
        StartCoroutine(MoveNote_CAB(note));
    }

    // 노트를 C → A → B로 이동시키는 코루틴
    IEnumerator MoveNote_CAB(GameObject note)
    {
        float totalTravelTime = beatTime * beatsPerNote; // 전체 이동 시간

        float moveToA_Duration = totalTravelTime * moveToA_Ratio;         // C → A 시간
        float moveToB_Duration = totalTravelTime * (1f - moveToA_Ratio);  // A → B 시간

        yield return StartCoroutine(MoveSegment(note, spawnPoint.position, setPoint.position, moveToA_Duration));
        yield return StartCoroutine(MoveSegment(note, setPoint.position, targetPoint.position, moveToB_Duration, true));
    }

    // 한 구간(from → to)을 일정 시간 동안 이동시키는 함수
    IEnumerator MoveSegment(GameObject obj, Vector3 from, Vector3 to, float duration, bool keepGoingAfter = false)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            obj.transform.position = Vector3.Lerp(from, to, t);  // 선형 보간
            yield return null;
        }

        // B에 도달한 이후에도 같은 방향으로 계속 이동 (연출용)
        if (keepGoingAfter)
        {
            Vector3 dir = (to - from).normalized;
            float speed = Vector3.Distance(from, to) / duration;

            while (true)
            {
                obj.transform.position += dir * speed * Time.deltaTime;
                yield return null;
            }
        }
    }
}