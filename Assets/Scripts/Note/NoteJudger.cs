using UnityEngine;

// 노트 판정을 담당하는 클래스
public class NoteJudger : MonoBehaviour
{
    private Vector3 spawnPosition;      // 노트가 생성된 초기 위치
    private bool isActive = false;      // 현재 노트가 판정 가능한 상태인지 여부

    [Header("판정 거리 기준 (단위: Unity Units)")]
    public float perfectRange = 0.1f;   // Perfect 판정 거리 기준
    public float excellentRange = 0.2f; // Excellent 판정 거리 기준
    public float goodRange = 0.3f;      // Good 판정 거리 기준
    public float normalRange = 0.5f;    // Normal 판정 거리 기준
    public float autoMissRange = 1.0f;  // 이보다 멀어지면 자동 Miss 처리

    private GameManager gameManager;    // GameManager 참조

    // 노트가 활성화될 때 실행 (풀링 방식이라면 재활성 시 호출됨)
    void OnEnable()
    {
        spawnPosition = transform.position;  // 노트의 시작 위치 저장
        isActive = true;                     // 판정 가능한 상태로 설정

        // GameManager가 연결되지 않았으면 찾아서 연결
        if (gameManager == null)
            gameManager = FindAnyObjectByType<GameManager>();
    }

    // 매 프레임마다 호출
    void Update()
    {
        if (!isActive) return;

        // 현재 위치와 생성 위치 간의 거리 계산
        float distance = Vector3.Distance(transform.position, spawnPosition);

        // 일정 거리 이상 벗어나면 자동 Miss 처리 (OnDisable이 호출됨)
        if (distance > autoMissRange)
        {
            gameObject.SetActive(false);
        }
    }

    // 노트가 비활성화될 때 호출됨 (자동/수동 비활성화 모두 포함)
    void OnDisable()
    {
        if (!isActive) return;

        // 최종 거리 기준으로 판정 결과 계산
        float distance = Vector3.Distance(transform.position, spawnPosition);
        string judgement = GetJudgement(distance);

        // 판정 결과에 따라 점수 및 콤보 처리
        ApplyJudgement(judgement);
        isActive = false;
    }

    // 거리 값에 따라 판정 결과 문자열 반환
    string GetJudgement(float distance)
    {
        if (distance <= perfectRange) return "Perfect";
        if (distance <= excellentRange) return "Excellent";
        if (distance <= goodRange) return "Good";
        if (distance <= normalRange) return "Normal";
        return "Miss";
    }

    // 판정 결과에 따라 점수 추가 및 콤보/피해 처리
    void ApplyJudgement(string result)
    {
        switch (result)
        {
            case "Perfect":
            case "Excellent":
            case "Good":
                gameManager.AddScore(GetScore(result)); // 점수 추가
                gameManager.AddCombo();                 // 콤보 증가
                break;

            case "Normal":
            case "Miss":
                gameManager.AddScore(GetScore(result)); // 점수 추가
                gameManager.ResetCombo();               // 콤보 초기화

                if (result == "Miss")
                    gameManager.TakeDamage(20);         // 체력 감소
                break;
        }

        // 디버그 출력 (판정 결과 로그)
        Debug.Log($"[{gameObject.name}] 판정 결과: {result}");
    }

    // 판정 결과에 따라 점수를 반환
    int GetScore(string result)
    {
        return result switch
        {
            "Perfect" => 100,
            "Excellent" => 70,
            "Good" => 50,
            "Normal" => 20,
            _ => 0
        };
    }
}