using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Define;

// 게임의 점수, 콤보, 타이머, 체력 등의 UI를 관리하는 클래스
public class InGameUIManager : UIManagerBase
{
    // UI 요소들 (인스펙터에서 할당)
    public TextMeshProUGUI scoreText;    // 점수 텍스트
    public TextMeshProUGUI comboText;    // 콤보 텍스트
    public Image timerImage;             // 타이머 이미지 (프로그래스 바 형태)
    public Image comboImage;             // 콤보 이미지 (이전 코드에 있었으므로 유지)
    public TextMeshProUGUI timeText;     // 남은 시간 텍스트
                                         // MusicSynchronizer에서 음악 시간을 가져오므로 AudioSource 직접 참조는 제거하거나 MusicSynchronizer 참조로 변경
                                         // public AudioSource audioSource; // 직접 참조 대신 MusicSynchronizer를 통해 접근 권장
    public GameObject endCanvas;         // 게임 종료 캔버스 (게임 오버 시 활성화)
    public TextMeshProUGUI endScoreText; // 게임 종료 시 점수 텍스트
    public TextMeshProUGUI endComboText; // 게임 종료 시 최고 콤보 텍스트
    public TextMeshProUGUI endMissText;  // 게임 종료 시 미스 수치 텍스트


    public TextMeshProUGUI missText;

    // MusicSynchronizer 참조 추가 (음악 시간 동기화용)
    public MusicSynchronizer musicSynchronizer;

    public Image hpBar;                  // 체력바 이미지

    // 시작 체력은 InGameManager에서 가져오므로 별도 저장 변수 필요 없음
    // private float startHP; 

    // 음악 타이머 관련 변수
    private float totalSongTime;         // 음악 전체 재생 시간
    // currentTime은 musicSynchronizer.currentMusicTime을 사용하므로 별도 변수 필요 없음
    // private float currentTime; 

    void Awake()
    {
        // MusicSynchronizer 참조를 동적으로 찾습니다.
        if (musicSynchronizer == null)
        {
            musicSynchronizer = FindAnyObjectByType<MusicSynchronizer>();
            if (musicSynchronizer == null)
            {
                Debug.LogError("InGameUIManager: MusicSynchronizer를 찾을 수 없습니다. 씬에 MusicSynchronizer가 있는지 확인해주세요.");
                enabled = false;
                return;
            }
        }
    }

    void Start()
    {
        // MusicSynchronizer에서 총 음악 시간을 가져옵니다.
        if (musicSynchronizer != null && musicSynchronizer.GetComponent<AudioSource>() != null && musicSynchronizer.GetComponent<AudioSource>().clip != null)
        {
            totalSongTime = musicSynchronizer.GetComponent<AudioSource>().clip.length;
        }
        else
        {
            Debug.LogWarning("InGameUIManager: MusicSynchronizer 또는 AudioSource 클립을 찾을 수 없습니다. 타이머가 작동하지 않을 수 있습니다.");
            totalSongTime = 0f; // 기본값 설정
        }




        // InGameManager가 제대로 초기화되었는지 확인 후 이벤트 구독 및 UI 초기화
        if (InGameManager.Instance != null)
        {
            InGameManager.Instance.OnScoreChanged += UpdateScoreText;
            InGameManager.Instance.OnComboChanged += UpdateComboText;
            InGameManager.Instance.OnMissChanged += UpdateMissText;
            InGameManager.Instance.OnSongEnded += ShowEndCanvas;

            // UI 텍스트 초기화
            UpdateScoreText(InGameManager.Instance.Score);
            UpdateComboText(InGameManager.Instance.Combo);
            UpdateMissText(InGameManager.Instance.Miss);
        }
        else
        {
            Debug.LogError("InGameUIManager: InGameManager 인스턴스를 찾을 수 없습니다. UI 업데이트가 제한될 수 있습니다.");
            enabled = false; // 매니저 없이는 UI 작동이 어려우므로 비활성화
        }
    }

    void Update()
    {
        // 매 프레임 타이머 및 체력바 갱신
        UpdateTimer();
        UpdateHP();
    }

    // 타이머 UI 갱신 함수
    void UpdateTimer()
    {
        if (musicSynchronizer == null || totalSongTime <= 0)
        {
            // Debug.LogWarning("UpdateTimer: MusicSynchronizer 또는 총 음악 시간이 유효하지 않습니다.");
            return; // 유효하지 않은 경우 업데이트 중단
        }

        // MusicSynchronizer의 현재 음악 재생 시간(currentMusicTime)을 사용합니다.
        // 이 시간은 AudioSource.time과 동일합니다.
        float currentPlaybackTime = musicSynchronizer.currentMusicTime;
        float remainingTime = totalSongTime - currentPlaybackTime;

        if (remainingTime > 0)
        {
            // 타이머 바 채우기 비율 조정 (Clamp01은 0~1 사이로 값을 제한)
            float fillAmount = Mathf.Clamp01(remainingTime / totalSongTime);
            if (timerImage != null)
            {
                timerImage.fillAmount = fillAmount;
            }

            // 텍스트로 시간 표시 (분:초)
            int totalSeconds = Mathf.CeilToInt(remainingTime); // 남은 시간을 올림하여 정수로 변환
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            if (timeText != null)
            {
                timeText.text = string.Format("{0}:{1:00}", minutes, seconds);
            }
        }
        else // 시간이 다 되었거나 음수일 경우 (음악 종료)
        {
            if (timeText != null)
            {
                timeText.text = "0:00"; // 0으로 고정
            }
            if (timerImage != null)
            {
                timerImage.fillAmount = 0f; // 바도 0으로 고정
            }
        }
    }

    // 체력바 UI 갱신 함수
    void UpdateHP()
    {
        if (InGameManager.Instance == null || hpBar == null) return;

        // InGameManager.Instance.PlayerHealth를 직접 사용
        // InGameManager.Instance.PlayerHealth는 현재 체력, 시작 체력은 InGameManager에서 관리하는 최대 체력으로 간주
        float fillAmount = Mathf.Clamp01(InGameManager.Instance.PlayerHealth / 100f); // 최대 체력이 100이라고 가정
        hpBar.fillAmount = fillAmount;
    }

    // 점수 변경 시 텍스트 갱신 (이벤트 핸들러)
    void UpdateScoreText(int score)
    {
        if (scoreText != null)
        {
            scoreText.text = $"{score}";
        }
    }

    // 콤보 변경 시 텍스트 갱신 및 토글 (이벤트 핸들러)
    void UpdateComboText(int combo)
    {
        if (comboText != null)
        {
            
            comboText.text = $"{combo}";
            
        }
    }

    void UpdateMissText(int miss)
    {
        missText.text = $"{miss}";
    }

    void ShowEndCanvas(InGameState state)
    {
        int maxCombo = InGameManager.Instance.MaxCombo;
        int miss = InGameManager.Instance.Miss;
        int score = InGameManager.Instance.Score;


        endComboText.text = $"최고 콤보: {maxCombo}";
        endScoreText.text = $"최종 점수: {score}";
        endMissText.text = $"미스: {miss}";

        if (endCanvas != null)
        {
            endCanvas.SetActive(true);
        }
        else
        {
            Debug.LogWarning("End Canvas가 할당되지 않았습니다. 게임 종료 UI가 표시되지 않습니다.");
        }

       

    }

    // 오브젝트가 파괴될 때 이벤트 해제 (메모리 누수 방지)
    protected override void OnDestroy()
    {
        base.OnDestroy(); // UIManagerBase의 OnDestroy 호출
        // InGameManager 인스턴스가 여전히 존재하는지 확인 후 이벤트 구독 해제
        if (InGameManager.Instance != null)
        {
            InGameManager.Instance.OnScoreChanged -= UpdateScoreText;
            InGameManager.Instance.OnComboChanged -= UpdateComboText;
        }
    }

}