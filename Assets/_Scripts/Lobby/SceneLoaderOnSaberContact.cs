using UnityEngine;
using UnityEngine.SceneManagement; // SceneManager를 사용하기 위해 추가

public class SceneLoaderOnSaberContact : MonoBehaviour
{
    // === 옵저버 패턴을 위한 이벤트 선언 ===
    // 씬 로드를 요청할 때 외부에 알리는 이벤트
    public static event System.Action<string> OnSceneLoadRequested;
    // ======================================

    [Tooltip("충돌 시 로드할 씬의 이름을 입력하세요.")]
    public string sceneToLoad = "GameScene"; // 로드할 씬 이름

    [Header("충돌 감지 쿨다운")]
    [Tooltip("Saber와의 충돌 후 다음 충돌을 감지할 때까지의 시간 (초)")]
    [SerializeField] private float cooldown = 1.0f; // 버튼 재입력을 막기 위한 쿨다운 설정
    private float _lastTriggerTime = -999f; // 마지막 트리거 발생 시간

    void Start()
    {
        Debug.Log($"[SceneLoaderOnSaberContact] 스크립트가 준비되었습니다. 'Saber'와 충돌 시 '{sceneToLoad}' 씬을 로드 요청합니다.");
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

        Debug.Log($"[SceneLoaderOnSaberContact] Saber와의 충돌 감지! '{sceneToLoad}' 씬 로드를 요청합니다.");

        // === 씬 로드 요청 이벤트를 외부에 알림 ===
        OnSceneLoadRequested?.Invoke(sceneToLoad);
        // ==========================================
    }
}