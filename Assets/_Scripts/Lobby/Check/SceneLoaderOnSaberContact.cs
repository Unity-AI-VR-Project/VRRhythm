using UnityEngine;
using UnityEngine.SceneManagement; // SceneManager를 사용하기 위해 추가

public class SceneLoaderOnSaberContact : MonoBehaviour
{
    // === 옵저버 패턴을 위한 이벤트 선언 ===
    // 씬 로드를 요청할 때 외부에 알리는 이벤트
    public static event System.Action<string> OnSceneLoadRequested;
    // ======================================

    [Header("씬 로드 설정")]
    [Tooltip("CanvasMover의 음악 클립 인덱스에 매칭되는 씬 이름들을 순서대로 할당하세요.")]
    public string[] scenesToLoad; // 각 음악 인덱스에 매칭되는 씬 이름 배열

    [Header("충돌 감지 쿨다운")]
    [Tooltip("Saber와의 충돌 후 다음 충돌을 감지할 때까지의 시간 (초)")]
    [SerializeField] private float cooldown = 1.0f; // 버튼 재입력을 막기 위한 쿨다운 설정
    private float _lastTriggerTime = -999f; // 마지막 트리거 발생 시간

    void Start()
    {
        // CanvasMover 인스턴스 참조를 가져옵니다.
        // CanvasMover._instance가 private이기 때문에 직접 접근할 수 없습니다.
        // public static getter를 CanvasMover에 추가하거나, FindFirstObjectByType 사용해야 합니다.
        // 여기서는 FindFirstObjectByType 사용하는 것이 가장 간단합니다.
        // (단, 씬에 CanvasMover 컴포넌트가 하나만 있거나, 오디오 재생을 담당하는 CanvasMover를 명확히 찾을 수 있어야 합니다.)
        CanvasMover masterCanvasMover = FindFirstObjectByType<CanvasMover>();

        if (masterCanvasMover != null)
        {
            if (masterCanvasMover.clips != null && scenesToLoad.Length != masterCanvasMover.clips.Length)
            {
                Debug.LogWarning($"[SceneLoaderOnSaberContact] 'Scenes To Load' 배열의 길이가 CanvasMover의 'Clips' 배열 길이 ({masterCanvasMover.clips.Length})와 다릅니다. 이는 예상치 못한 동작을 유발할 수 있습니다.");
            }
        }
        else
        {
            Debug.LogError("[SceneLoaderOnSaberContact] 씬에서 CanvasMover 인스턴스를 찾을 수 없습니다! 'Clips' 배열 길이 검사를 건너뜝니다.");
        }

        Debug.Log("[SceneLoaderOnSaberContact] 스크립트가 준비되었습니다. 'Saber'와 충돌 시 현재 선택된 곡에 해당하는 씬을 로드 요청합니다.");
    }

    /// <summary>
    /// 콜라이더가 트리거로 설정된 오브젝트에 다른 콜라이더가 진입했을 때 호출됩니다.
    /// </summary>
    /// <param name="other">충돌한 다른 콜라이더</param>
    private void OnTriggerEnter(Collider other)
    {
        // 충돌한 오브젝트의 태그가 "Saber"인지 확인
        if (!other.CompareTag("Saber"))
        {
            return; // Saber가 아니면 무시
        }

        // 쿨다운 시간 확인 (연속적인 트리거 방지)
        if (Time.time - _lastTriggerTime < cooldown)
        {
            return; // 쿨다운 중이면 무시
        }

        _lastTriggerTime = Time.time; // 마지막 트리거 시간 업데이트

        // CanvasMover에서 현재 선택된 곡의 인덱스를 가져옵니다.
        // CanvasMover.currentSongIndex는 static이므로 직접 접근 가능합니다.
        int currentSongIndex = CanvasMover.currentSongIndex;

        // 해당 인덱스에 매칭되는 씬 이름을 찾습니다.
        if (scenesToLoad != null && currentSongIndex >= 0 && currentSongIndex < scenesToLoad.Length)
        {
            string sceneNameToLoad = scenesToLoad[currentSongIndex];
            Debug.Log($"[SceneLoaderOnSaberContact] Saber와의 충돌 감지! 현재 곡 인덱스 {currentSongIndex}에 해당하는 '{sceneNameToLoad}' 씬 로드를 요청합니다.");

            // === 씬 로드 요청 이벤트를 외부에 알림 ===
            OnSceneLoadRequested?.Invoke(sceneNameToLoad);
            // ==========================================
        }
        else
        {
            Debug.LogWarning($"[SceneLoaderOnSaberContact] 유효하지 않은 곡 인덱스 ({currentSongIndex}) 이거나 'Scenes To Load' 배열이 할당되지 않았습니다. 씬 로드를 할 수 없습니다.");
        }
    }
}