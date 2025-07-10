using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 필요합니다.

public class TitleButton : MonoBehaviour
{
    [Header("충돌 설정")]
    [SerializeField] private string targetTag = "Saber"; // 충돌을 감지할 오브젝트의 태그
    [SerializeField] private string nextSceneName = "YourNextScene"; // 이동할 다음 씬 이름

    [Header("페이드 아웃 캔버스")]
    [SerializeField] private CanvasFader targetCanvasFader; // 페이드 아웃시킬 CanvasFader 컴포넌트 참조

    private bool hasTriggered = false; // 중복 트리거 방지 플래그
    AudioSource audio;
    public AudioClip clip;
    void Start()
    {
        audio = GetComponent<AudioSource>();
        // 캔버스 페이드 아웃 완료 이벤트에 씬 전환 메서드를 구독합니다.
        CanvasFader.onFadeComplete += LoadNextScene;

        // targetCanvasFader가 할당되지 않았다면 경고 메시지 출력
        if (targetCanvasFader == null)
        {
            Debug.LogWarning("CanvasFader가 할당되지 않았습니다. 인스펙터에서 할당해주세요.", this);
        }
    }

    void OnDestroy()
    {
        // 스크립트가 파괴될 때 이벤트 구독을 해제하여 메모리 누수를 방지합니다.
        CanvasFader.onFadeComplete -= LoadNextScene;
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

        // CanvasFader가 할당되어 있다면 페이드 아웃 시작
        if (targetCanvasFader != null)
        {
            targetCanvasFader.StartFadeOut();
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
        int nextSceneIndex = SceneUtility.GetBuildIndexByScenePath(nextSceneName);
        if (nextSceneIndex == -1)
        {
            Debug.LogError($"'{nextSceneName}' 씬이 빌드 설정에 추가되어 있지 않거나 경로가 잘못되었습니다! Scene을 빌드 설정에 추가해주세요.");
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }
}