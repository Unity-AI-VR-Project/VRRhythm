using UnityEngine;
using System.Collections;

public class NoteMover : MonoBehaviour
{
    public Transform pointA; // SetPoint (30, 0, 0)
    public Transform pointB; // Target (0, 0, 0)
    public Transform spawnPoint; // Start (30, -5, 0)

    public float bpm = 130f; // 음악 BPM
    public AudioSource audioSource;
    public float moveToA_Duration = 0.3f; // C → A까지 소요 시간

    public int maxNoteCount = 500;

    private float beatTime;             // 1박자 시간 (초)
    private float moveToB_Duration;     // A → B 이동 시간 (계산됨)
    private int noteIndex = 0;
    private bool readyToGenerate = false;

    void Start()
    {
        beatTime = 60f / bpm;

        // A→B로 갈 시간이 0이 되면 오류 발생 → 방어 로직
        if (moveToA_Duration >= beatTime)
        {
            Debug.LogError("C→A 시간이 beatTime보다 크거나 같으면 안 됩니다.");
            return;
        }

        moveToB_Duration = beatTime - moveToA_Duration;

        // 첫 노트는 C → A 이동 후 음악 시작
        SpawnFirstNote();
    }

    void Update()
    {
        if (!readyToGenerate || !audioSource.isPlaying) return;

        float totalTravelTime = beatTime; // 전체 이동 시간 = C→A + A→B

        // 현재 오디오 재생 시간에 따라 노트 미리 생성
        while (noteIndex < maxNoteCount &&
               audioSource.time >= beatTime * noteIndex - totalTravelTime)
        {
            SpawnNoteWithTiming();
            noteIndex++;
        }
    }

    // 최초 노트: C → A → B 로 이동하며, A 도착 후 음악 재생
    void SpawnFirstNote()
    {
        GameObject note = NotePoolManager.Instance.SpawnNote(spawnPoint.position);
        StartCoroutine(FirstNoteRoutine(note));
    }

    IEnumerator FirstNoteRoutine(GameObject note)
    {
        // 1단계: C → A 이동
        yield return StartCoroutine(Move(note, spawnPoint.position, pointA.position, moveToA_Duration));

        // 음악 시작
        audioSource.Play();
        readyToGenerate = true;

        // 2단계: A → B 이동
        yield return StartCoroutine(Move(note, pointA.position, pointB.position, moveToB_Duration));

        NotePoolManager.Instance.ReturnNote(note);
    }

    // 일반 노트: C → A → B 이동 (총 duration은 beatTime으로 정확히 맞춤)
    void SpawnNoteWithTiming()
    {
        GameObject note = NotePoolManager.Instance.SpawnNote(spawnPoint.position);
        StartCoroutine(FullMoveRoutine(note));
    }

    IEnumerator FullMoveRoutine(GameObject note)
    {
        // 1단계: C → A 이동
        yield return StartCoroutine(Move(note, spawnPoint.position, pointA.position, moveToA_Duration));

        // 2단계: A → B 이동
        yield return StartCoroutine(Move(note, pointA.position, pointB.position, moveToB_Duration));

        NotePoolManager.Instance.ReturnNote(note);
    }

    // 이동 코루틴 (보간 이동)
    IEnumerator Move(GameObject obj, Vector3 from, Vector3 to, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            obj.transform.position = Vector3.Lerp(from, to, t);
            yield return null;
        }
        obj.transform.position = to;
    }
}
