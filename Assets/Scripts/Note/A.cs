using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 게임의 점수, 콤보, 타이머, 체력 등의 UI를 관리하는 클래스
public class A : MonoBehaviour
{
    // UI 요소들 (인스펙터에서 할당)
    public TextMeshProUGUI scoreText;    // 점수 텍스트
    public TextMeshProUGUI comboText;    // 콤보 텍스트
    public Image timerImage;             // 타이머 이미지 (프로그래스 바 형태)
    public TextMeshProUGUI timeText;     // 남은 시간 텍스트
    public AudioSource audioSource;      // 음악 재생용 AudioSource
    public Image hpBar;                  // 체력바 이미지

    // 체력 관련 변수
    float startHP;                       // 시작 체력 (GameManager에서 가져옴)
  

    // 음악 타이머 관련 변수
    private float totalSongTime;         // 음악 전체 재생 시간
    private float currentTime;           // 현재 남은 시간

    void Start()
    {
        // 시작 시 체력 초기화
        startHP = GameManager.instance.playerHealth;
      

        // 음악 길이 설정
        if (audioSource != null && audioSource.clip != null)
        {
            totalSongTime = audioSource.clip.length;
            currentTime = totalSongTime;
        }
        else
        {
            Debug.LogWarning("오디오 클립이 없음.");
        }

        // 시작 시 콤보 텍스트는 숨김
        comboText.gameObject.SetActive(false);

        // 점수 및 콤보 변경 시 UI 갱신 이벤트 등록
        GameManager.instance.OnScoreChanged += UpdateScoreText;
        GameManager.instance.OnComboChanged += UpdateComboText;

        // UI 텍스트 초기화
        UpdateScoreText(GameManager.instance.Score);
        UpdateComboText(GameManager.instance.Combo);
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
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;

            // 타이머 바 채우기 비율 조정
            float fillAmount = Mathf.Clamp01(currentTime / totalSongTime);
            timerImage.fillAmount = fillAmount;

            // 텍스트로 시간 표시 (분:초)
            int totalSeconds = Mathf.CeilToInt(currentTime);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            timeText.text = string.Format("{0}:{1:00}", minutes, seconds);
        }
        else
        {
            // 시간이 다 되면 0으로 고정
            timeText.text = "0:00";
            timerImage.fillAmount = 0f;
        }
    }

    // 체력바 UI 갱신 함수
    void UpdateHP()
    {
        float fillAmount = Mathf.Clamp01(GameManager.instance.playerHealth / startHP);
        hpBar.fillAmount = fillAmount;
    }


    // 점수 변경 시 텍스트 갱신 (이벤트 핸들러)
    void UpdateScoreText(int score)
    {
        scoreText.text = "Score: " + score;
    }

    // 콤보 변경 시 텍스트 갱신 및 토글 (이벤트 핸들러)
    void UpdateComboText(int combo)
    {
        if (combo > 0)
        {
            comboText.gameObject.SetActive(true);
            comboText.text = "X " + combo;
        }
        else
        {
            comboText.gameObject.SetActive(false);
        }
    }

    // 오브젝트가 파괴될 때 이벤트 해제 (메모리 누수 방지)
    private void OnDestroy()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnScoreChanged -= UpdateScoreText;
            GameManager.instance.OnComboChanged -= UpdateComboText;
        }
    }
}