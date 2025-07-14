using System;
using UnityEngine;
using Define; // Define 네임스페이스가 필요합니다 (예: InGameState enum).

// 게임 점수, 콤보, 체력 등의 정보를 관리하는 매니저 클래스
public class InGameManager : MonoBehaviour
{
    // 인게임 인스턴스 접근 프로퍼티 (싱글톤 패턴)
    public static InGameManager Instance
    {
        get
        {
            if (m_instance == null)
            {
                // 현재 존재하는 InGameManager를 찾아 할당
                m_instance = FindAnyObjectByType<InGameManager>();
                if (m_instance == null)
                {
                    // 씬에 인스턴스가 없을 경우 새로 생성 (선택 사항, 보통은 씬에 미리 배치)
                    GameObject obj = new GameObject("InGameManager");
                    m_instance = obj.AddComponent<InGameManager>();
                    Debug.LogWarning("InGameManager: 씬에 인스턴스가 없어 새로 생성했습니다. 일반적으로 씬에 미리 배치하는 것을 권장합니다.");
                }
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

    public int Miss { get; private set; }

    // 플레이어 체력
    public float PlayerHealth { get; private set; } // 프로퍼티 이름 변경 (public 필드와의 혼동 방지)

    // 게임 진행 체크
    public bool IsStarted { get; private set; } // 프로퍼티 이름 변경

    // 게임 진행상황 변경 시 발생하는 이벤트
    public event Action<InGameState> OnStarted;

    // 점수가 변경되었을 때 발생하는 이벤트
    public event Action<int> OnScoreChanged;

    // 콤보 수치가 변경되었을 때 발생하는 이벤트
    public event Action<int> OnComboChanged;

    // 미스 수치가 변경 되었을 떄
    public event Action<int> OnMissChanged;



    // 오브젝트가 활성화될 때 호출
    private void Awake()
    {
        // 인게임 매니저 중복 생성 막는 처리
        if (m_instance != null && m_instance != this)
        {
            Destroy(gameObject);
            return;
        }
        m_instance = this; // 현재 인스턴스를 싱글톤으로 설정

        // 씬 전환 시 파괴되지 않도록 설정 (필요한 경우)
        // DontDestroyOnLoad(gameObject); 

        // 초기 체력 설정
        PlayerHealth = 100;
        Score = 0;
        Combo = 1;
        MaxCombo = 8;
        Miss = 0;
        IsStarted = false; // 초기에는 게임이 시작되지 않은 상태
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
        PlayerHealth -= damage;
        // 체력이 0 이하가 되면 게임 오버 처리 등 추가 가능
        if (PlayerHealth <= 0)
        {
            PlayerHealth = 0;
            Debug.Log("플레이어 체력 0! 게임 오버!");
            // TODO: 게임 오버 로직 호출
        }
    }

    // 게임 종료 시 최대 콤보에 따른 보너스 점수 적용
    public void ApplyComboBonus()
    {
        // 예: 최대 콤보 수 x 10 만큼 보너스 점수 추가
        int bonus = MaxCombo * 10;
        AddScore(bonus);
        Debug.Log($"Max Combo Bonus Applied: {bonus}");
    }

    public void MissUpdate()
    {
        Miss++;
        OnMissChanged?.Invoke(Miss);
    }

    // 게임 시작을 알리는 메서드 (외부에서 호출)
    public void StartGame()
    {
        if (!IsStarted) // 이미 시작되지 않았다면
        {
            IsStarted = true;
            OnStarted?.Invoke(InGameState.Playing); // 구독자들에게 게임 시작 알림
            Debug.Log("게임 시작 이벤트 발생!");
        }
        else
        {
            Debug.LogWarning("InGameManager: 이미 게임이 시작된 상태입니다.");
        }
    }
}