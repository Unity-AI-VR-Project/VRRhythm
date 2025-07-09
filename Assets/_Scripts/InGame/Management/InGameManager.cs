using System;
using UnityEngine;

// 게임 점수, 콤보, 체력 등의 정보를 관리하는 매니저 클래스
public class InGameManager : MonoBehaviour
{
    // 인게임 인스턴스 접근 프로퍼티
    public static InGameManager Instance
    {
        get
        {
            if (m_instance == null)
            {
                // 현재 존재하는 InGameManager를 찾아 할당
                m_instance = FindAnyObjectByType<InGameManager>();
            }
            return m_instance;
        }
    }

    // 단일 인게임 인스턴스를 저장하는 정적 필드
    private static InGameManager m_instance;

    // 현재 점수
    public int Score { get; private set; }

    // 현재 콤보 수치
    public int Combo { get; private set; }

    // 최고 콤보 기록
    public int MaxCombo { get; private set; }

    // 플레이어 체력
    public float playerHealth { get; private set; }

    // 점수가 변경되었을 때 발생하는 이벤트
    public event Action<int> OnScoreChanged;

    // 콤보 수치가 변경되었을 때 발생하는 이벤트
    public event Action<int> OnComboChanged;

    // 오브젝트가 활성화될 때 호출
    private void Awake()
    {
        // 인게임 매니저 중복 생성 막는 처리
        if (Instance != this)
        {
            Destroy(gameObject);
        }

        // 초기 체력 설정
        playerHealth = 100;
    }

    // 점수를 추가하는 메서드
    public void AddScore(int value)
    {
        Score += value;
        OnScoreChanged?.Invoke(Score); // 구독자들에게 점수 변경 알림
    }

    // 콤보를 1 증가시키는 메서드
    public void AddCombo()
    {
        Combo++;
        if (Combo > MaxCombo)
            MaxCombo = Combo;

        OnComboChanged?.Invoke(Combo); // 구독자들에게 콤보 변경 알림
    }

    // 콤보를 초기화하는 메서드
    public void ResetCombo()
    {
        Combo = 0;
        OnComboChanged?.Invoke(Combo);
    }

    // 플레이어가 피해를 입었을 때 호출
    public void TakeDamage(int damage)
    {
        playerHealth -= damage;
    }

    // 게임 종료 시 최대 콤보에 따른 보너스 점수 적용
    public void ApplyComboBonus()
    {
        // 예: 최대 콤보 수 x 10 만큼 보너스 점수 추가
        int bonus = MaxCombo * 10;
        AddScore(bonus);
        Debug.Log($"Max Combo Bonus Applied: {bonus}");
    }
}