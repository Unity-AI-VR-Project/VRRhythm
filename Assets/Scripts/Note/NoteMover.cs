using UnityEngine;
using System.Collections;

// 노트 생성과 이동을 제어하는 클래스
public class NoteMover : MonoBehaviour
{
    // === 위치 설정 ===
    public Transform spawnPoint;   // 노트가 생성되는 시작 위치 (지점 C)
    public Transform setPoint;     // 노트가 잠시 경유하는 지점 (지점 A)
    public Transform targetPoint;  // 노트가 최종적으로 도달해야 하는 위치 (지점 B)

    public AudioSource audio;      // 음악 재생을 위한 오디오 소스

    [Header("Tempo Settings")]
    public float bpm = 120f;              // 음악의 BPM (1분에 몇 박자)
    public float beatsPerNote = 2f;       // 노트가 생성된 뒤 몇 박자 후에 도착할지 설정

    [Range(0f, 1f)]
    public float moveToA_Ratio = 0.4f;    // 전체 이동 중 C→A 구간이 차지하는 비율

    [Header("Spawn Settings")]
    public int maxNoteCount = 100;              // 최대 생성할 노트 개수
    public float spawnIntervalInBeats = 1.0f;   // 노트 생성 간격 (단위: 박자)

    [Header("Stop Settings")]
    [Range(0f, 30f)]
    public float stopSpawnBeforeEnd = 5f; // 음악이 끝나기 X초 전부터는 노트 생성 중단

    // 내부 변수들
    private float beatTime;        // 한 박자의 지속 시간 (초)
    private float spawnInterval;   // 박자 기반 간격을 초 단위로 환산한 값
    private int noteIndex = 0;     // 생성한 노트 개수
    private float timer = 0f;      // 생성 타이머 누적값

    // 시작 시 호출
    void Start()
    {
        beatTime = 60f / bpm;  // BPM → 초 단위 환산
        spawnInterval = beatTime * spawnIntervalInBeats;  // 생성 주기 계산
    }

    // 매 프레임마다 실행
    void Update()
    {
        if (noteIndex >= maxNoteCount) return; // 최대 개수 도달 시 종료

        // 첫 노트는 음악 없이도 생성해야 하므로 예외 처리
        if (noteIndex > 0)
        {
            // 음악이 멈췄거나, 음악이 곧 끝날 경우 생성 중단
            if (!audio.isPlaying || (audio.clip != null && audio.clip.length - audio.time <= stopSpawnBeforeEnd))
                return;
        }

        // 시간 누적하여 spawnInterval마다 노트 생성
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer -= spawnInterval;
            SpawnNote();  // 노트 생성
            noteIndex++;  // 생성한 노트 수 증가
        }
    }

    // 노트를 생성하고 음악 재생을 시작
    void SpawnNote()
    {
        // 첫 노트 생성 시 음악 재생
        if (noteIndex == 0 && audio != null && !audio.isPlaying)
        {
            audio.Play();
        }

        // 풀에서 노트를 가져와 시작 위치에 배치
        GameObject note = NotePoolManager.Instance.SpawnNote(spawnPoint.position);

        // 노트 이동 시작 (C → A → B)
        StartCoroutine(MoveNote_CAB(note));
    }

    // 노트를 C→A→B로 일정 시간에 맞춰 이동
    IEnumerator MoveNote_CAB(GameObject note)
    {
        float totalTravelTime = beatTime * beatsPerNote;  // 전체 이동 시간

        float moveToA_Duration = totalTravelTime * moveToA_Ratio;         // C → A 이동 시간
        float moveToB_Duration = totalTravelTime * (1f - moveToA_Ratio);  // A → B 이동 시간

        // C → A 이동
        yield return StartCoroutine(MoveSegment(note, spawnPoint.position, setPoint.position, moveToA_Duration));

        // A → B 이동 (도착 후에는 계속 앞으로 직진)
        yield return StartCoroutine(MoveSegment(note, setPoint.position, targetPoint.position, moveToB_Duration, true));
    }

    // 한 구간을 duration 시간 동안 이동
    IEnumerator MoveSegment(GameObject obj, Vector3 from, Vector3 to, float duration, bool keepGoingAfter = false)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            obj.transform.position = Vector3.Lerp(from, to, t);  // 선형 보간으로 부드럽게 이동
            yield return null;
        }

        // B 도착 이후에도 같은 방향으로 계속 이동
        if (keepGoingAfter)
        {
            Vector3 dir = (to - from).normalized;  // 이동 방향 단위 벡터
            float speed = Vector3.Distance(from, to) / duration; // 동일한 속도 유지

            while (true)
            {
                obj.transform.position += dir * speed * Time.deltaTime;
                yield return null;
            }
        }
    }
}