using UnityEngine;
using UnityEngine.UI;
using TMPro;

// UI를 관리하는 클래스
public class UiManager : MonoBehaviour
{
    // 점수, 콤보, 타이머 텍스트, HP 바 등 UI 요소들
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;
    public Image timerImage;
    public TextMeshProUGUI timeText;
    public AudioSource audioSource;
    public Image hpBar;

    // HP 정보
    float startHP;
    float currentHP;

    // 음악 재생 시간 정보
    private float totalSongTime;
    private float currentTime;

    // 점수 및 콤보
    private int score = 0;
    private int combo = 0;

    // 초기화
    void Start()
    {
        // HP 초기값 설정
        startHP = 100;
        currentHP = 100;

        // 오디오 클립이 제대로 설정되었는지 확인하고 총 길이 측정
        if (audioSource != null && audioSource.clip != null)
        {
            totalSongTime = audioSource.clip.length;
            currentTime = totalSongTime;
        }
        else
        {
            Debug.LogWarning("오디오 소스나 클립이 할당되지 않았습니다.");
        }

        // 콤보 텍스트는 기본적으로 비활성화
        comboText.gameObject.SetActive(false);

        // 초기 점수 및 콤보 텍스트 표시
        UpdateScoreText();
        UpdateComboText();
    }

    // 매 프레임마다 호출됨
    void Update()
    {
        HandleInput();   // 입력 처리
        UpdateTimer();   // 타이머 업데이트
    }

    // 키보드 입력 처리
    void HandleInput()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            // 점수 100점 추가
            score += 100;
            UpdateScoreText();
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            // 콤보가 0이면 텍스트 다시 표시
            if (combo == 0)
            {
                comboText.gameObject.SetActive(true);
            }

            // 콤보 증가
            combo += 1;
            UpdateComboText();
        }

        if (Input.GetKeyDown(KeyCode.D))
        {
            // 콤보 초기화 및 UI 숨기기
            combo = 0;
            comboText.gameObject.SetActive(false);
        }

        if (Input.GetKeyDown(KeyCode.F))
        {
            // HP 10 감소 및 UI 업데이트
            currentHP -= 10;
            UpdateHP();
        }
    }

    // 타이머 및 진행률 바 업데이트
    void UpdateTimer()
    {
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime;

            float fillAmount = Mathf.Clamp01(currentTime / totalSongTime);
            timerImage.fillAmount = fillAmount;

            int totalSeconds = Mathf.CeilToInt(currentTime);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            // mm:ss 형식으로 텍스트 출력
            timeText.text = string.Format("{0}:{1:00}", minutes, seconds);
        }
        else
        {
            // 시간이 다 되었을 경우
            timeText.text = "0:00";
            timerImage.fillAmount = 0f;
        }
    }

    // HP 게이지 업데이트
    void UpdateHP()
    {
        float fillAmount = Mathf.Clamp01(currentHP / startHP);
        hpBar.fillAmount = fillAmount;
    }

    // 점수 텍스트 업데이트
    void UpdateScoreText()
    {
        scoreText.text = "Score: " + score;
    }

    // 콤보 텍스트 업데이트
    void UpdateComboText()
    {
        comboText.text = "X " + combo;
    }
}