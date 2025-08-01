using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필요합니다.

public class TitleButton : MonoBehaviour
{
    [Header("충돌 설정")]
    [SerializeField] private string targetTag = "Saber"; // 충돌을 감지할 오브젝트의 태그
    [SerializeField] private string nextSceneName = "YourNextScene"; // 이동할 다음 씬 이름

    [Header("페이드 아웃 캔버스")]
    [SerializeField] private CanvasFader[] targetCanvasFaders; // 페이드 아웃시킬 CanvasFader 컴포넌트 배열 참조

    private bool hasTriggered = false; // 중복 트리거 방지 플래그
    private int completedFades = 0; // 페이드 아웃이 완료된 캔버스 개수
    AudioSource audio;
    public AudioClip clip;

    void Start()
    {
        audio = GetComponent<AudioSource>();
        // 각 CanvasFader의 페이드 아웃 완료 이벤트에 씬 전환 카운터 메서드를 구독합니다.
        // CanvasFader.onFadeComplete += LoadNextScene; 이 방식은 여러 캔버스에 적합하지 않습니다.
        // 각 CanvasFader가 완료될 때마다 이벤트를 개별적으로 처리하고,
        // 모든 캔버스의 페이드 아웃이 완료되면 씬을 전환하도록 변경합니다.

        // CanvasFader가 할당되지 않았다면 경고 메시지 출력
        if (targetCanvasFaders == null || targetCanvasFaders.Length == 0)
        {
            Debug.LogWarning("CanvasFader가 할당되지 않았거나 배열이 비어있습니다. 인스펙터에서 할당해주세요.", this);
        }
    }

    void OnEnable()
    {
        // 스크립트가 활성화될 때마다 이벤트 구독을 초기화합니다.
        CanvasFader.onFadeComplete += OnCanvasFadeComplete;
        completedFades = 0; // 활성화될 때마다 카운터 초기화
        hasTriggered = false; // 활성화될 때마다 트리거 플래그 초기화
    }

    void OnDisable()
    {
        // 스크립트가 비활성화될 때 이벤트 구독을 해제하여 메모리 누수를 방지합니다.
        CanvasFader.onFadeComplete -= OnCanvasFadeComplete;
    }

    /// <summary>
    /// CanvasFader의 페이드 아웃이 완료될 때 호출되는 메서드.
    /// 모든 캔버스의 페이드 아웃이 완료되면 씬을 전환합니다.
    /// </summary>
    private void OnCanvasFadeComplete()
    {
        completedFades++;
        if (targetCanvasFaders != null && completedFades >= targetCanvasFaders.Length)
        {
            // 모든 캔버스의 페이드 아웃이 완료되면 씬 전환
            LoadNextScene();
        }
        else if (targetCanvasFaders == null || targetCanvasFaders.Length == 0)
        {
            // CanvasFader가 할당되지 않은 경우, 즉시 씬 전환
            LoadNextScene();
        }
    }

    // Is Trigger가 체크된 Collider와 충돌했을 때 호출됩니다.
    void OnTriggerEnter(Collider other)
    {
        audio.PlayOneShot(clip);
        // 이미 트리거되었거나 목표 태그가 아닌 경우 무시
        if (hasTriggered || !other.CompareTag(targetTag))
        {
            return;
        }

        Debug.Log(other.name + "와 충돌! Canvas 페이드 아웃 및 씬 전환 시작.");
        hasTriggered = true; // 중복 트리거 방지
        completedFades = 0; // 페이드 아웃 카운터 초기화

        // CanvasFader 배열이 할당되어 있고 비어있지 않다면 모든 캔버스 페이드 아웃 시작
        if (targetCanvasFaders != null && targetCanvasFaders.Length > 0)
        {
            foreach (CanvasFader fader in targetCanvasFaders)
            {
                if (fader != null)
                {
                    fader.StartFadeOut();
                }
                else
                {
                    Debug.LogWarning("targetCanvasFaders 배열에 null 참조가 있습니다. 확인해주세요.", this);
                    // null인 경우에도 카운트를 증가시켜 다음 캔버스 처리에 영향을 주지 않도록 함
                    completedFades++;
                }
            }
            // 모든 캔버스 중에 유효한 fader가 하나도 없어서 OnCanvasFadeComplete가 호출되지 않을 경우를 대비
            if (completedFades >= targetCanvasFaders.Length)
            {
                LoadNextScene();
            }
        }
        else
        {
            // CanvasFader가 없으면 바로 씬 전환
            Debug.LogWarning("CanvasFader가 없어 바로 씬을 전환합니다. CanvasFader를 할당해주세요.");
            LoadNextScene();
        }
    }

    /// <summary>
    /// 다음 씬으로 이동합니다.
    /// </summary>
    private void LoadNextScene()
    {
        // 씬 이름이 비어있지 않은지 확인
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("다음 씬 이름이 설정되지 않았습니다! 인스펙터에서 'Next Scene Name'을 설정해주세요.");
            return;
        }

        // 빌드 설정에 씬이 추가되어 있는지 확인하는 것이 좋습니다.
        // SceneUtility.GetBuildIndexByScenePath는 에디터에서만 잘 작동하므로,
        // 빌드된 게임에서는 SceneManager.GetSceneByName 등의 방법을 사용하는 것이 더 안전합니다.
        // 여기서는 편의상 GetBuildIndexByScenePath를 유지합니다.
        int nextSceneIndex = SceneUtility.GetBuildIndexByScenePath(nextSceneName);
        if (nextSceneIndex == -1)
        {
            Debug.LogError($"'{nextSceneName}' 씬이 빌드 설정에 추가되어 있지 않거나 경로가 잘못되었습니다! Scene을 빌드 설정에 추가해주세요.");
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}