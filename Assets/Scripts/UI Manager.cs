using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    // 점수 텍스트 UI
    public TextMeshProUGUI scoreText;
    // 콤보 텍스트 UI
    public TextMeshProUGUI comboText;
    // 타이머 UI 이미지 (프로그래스 바 형태)
    public Image timerImage;
    // 남은 시간 텍스트 UI
    public TextMeshProUGUI timeText;
    // 음악 재생용 오디오 소스
    public AudioSource audioSource;

    // 전체 노래 길이
    private float totalSongTime;
    // 현재 남은 시간
    private float currentTime;

    // 현재 점수와 콤보 수
    private int score = 0;
    private int combo = 0;

    void Start()
    {
        // 오디오 소스와 클립이 존재하면 총 시간 초기화
        if (audioSource != null && audioSource.clip != null)
        {
            totalSongTime = audioSource.clip.length;
            currentTime = totalSongTime;
        }
        else
        {
            Debug.LogWarning("오디오 소스나 클립이 할당되지 않았습니다.");
        }

        // 시작 시 콤보 텍스트는 숨김
        comboText.gameObject.SetActive(false);

        UpdateScoreText();
        UpdateComboText();
    }

    void Update()
    {
        HandleInput(); // 키 입력 처리
        UpdateTimer(); // 타이머 업데이트
    }

    void HandleInput()
    {
        // O 키를 누르면 점수 100 추가
        if (Input.GetKeyDown(KeyCode.O))
        {
            score += 100;
            UpdateScoreText();
        }

        // P 키를 누르면 콤보 증가
        if (Input.GetKeyDown(KeyCode.P))
        {
            // 콤보가 0이면 콤보 UI 표시
            if (combo == 0)
            {
                comboText.gameObject.SetActive(true);
            }

            combo += 1;
            UpdateComboText();
        }

        // D 키를 누르면 콤보 리셋 및 UI 숨김
        if (Input.GetKeyDown(KeyCode.D))
        {
            combo = 0;
            comboText.gameObject.SetActive(false);
        }
    }

    void UpdateTimer()
    {
        if (currentTime > 0)
        {
            // 남은 시간 감소
            currentTime -= Time.deltaTime;

            // 프로그레스 바 채우기 비율 계산
            float fillAmount = Mathf.Clamp01(currentTime / totalSongTime);
            timerImage.fillAmount = fillAmount;

            // 남은 시간 계산 및 표시
            int totalSeconds = Mathf.CeilToInt(currentTime);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            timeText.text = string.Format("{0}:{1:00}", minutes, seconds);
        }
        else
        {
            // 시간이 다 되었을 때 표시
            timeText.text = "0:00";
            timerImage.fillAmount = 0f;
        }
    }

    // 점수 텍스트 갱신
    void UpdateScoreText()
    {
        scoreText.text = "Score: " + score;
    }

    // 콤보 텍스트 갱신
    void UpdateComboText()
    {
        comboText.text = "X " + combo;
    }
}