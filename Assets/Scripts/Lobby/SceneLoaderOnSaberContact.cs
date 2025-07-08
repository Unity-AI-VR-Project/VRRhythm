using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 추가

public class SceneLoaderOnSaberContact : MonoBehaviour
{
    // 각 패널 인덱스에 매핑될 씬 이름을 배열로 정의합니다.
    // 인스펙터에서 직접 씬 이름을 입력해야 합니다.
    [Header("Scene Settings")]
    public string[] sceneNames;

    [Tooltip("씬 전환 시 중복 트리거를 방지하기 위한 쿨다운 시간")]
    [SerializeField] private float sceneLoadCooldown = 2f;
    private float _lastSceneLoadTime = -999f; // 마지막 씬 로드 시간 기록

    private void OnTriggerEnter(Collider other)
    {
        // "Saber" 태그를 가진 오브젝트에 의해서만 트리거되도록 합니다.
        if (!other.CompareTag("Saber"))
        {
            return;
        }

        // 쿨다운 시간 내에 중복 씬 로드 방지
        if (Time.time - _lastSceneLoadTime < sceneLoadCooldown)
        {
            Debug.LogWarning("[SceneLoaderOnSaberContact] 씬 로드 쿨다운 중입니다. 잠시 후 다시 시도하세요.");
            return;
        }

        _lastSceneLoadTime = Time.time; // 씬 로드 시도 시간 기록

        // Check 스크립트에서 현재 선택된 패널의 인덱스를 가져옵니다.
        int selectedPanelIndex = SongSelect.currentIndex;

        // 유효한 씬 인덱스인지 확인
        if (selectedPanelIndex >= 0 && selectedPanelIndex < sceneNames.Length)
        {
            string sceneToLoad = sceneNames[selectedPanelIndex];

            // 씬이 실제로 존재하는지 확인 (선택 사항이지만 안전을 위해 권장)
            // 빌드 설정에 추가된 씬만 SceneUtility.GetAllScenePaths()로 확인 가능
            // Editor Only: UnityEditor.SceneManagement.EditorSceneManager.GetSceneByPath
            // Runtime: 씬 빌드 설정에 추가된 모든 씬의 이름은 직접 관리해야 합니다.

            // 실제 씬 로드
            Debug.Log($"[SceneLoaderOnSaberContact] 인덱스 {selectedPanelIndex}에 해당하는 씬 '{sceneToLoad}' 로드를 시도합니다.");
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError($"[SceneLoaderOnSaberContact] 유효하지 않은 패널 인덱스입니다: {selectedPanelIndex}. 씬 이름을 확인할 수 없습니다.");
        }
    }

    /* 만약 특정 씬으로 이동하기 전에 추가적인 확인이 필요하다면 아래와 같은 메서드를 활용할 수 있습니다.
    public void LoadSelectedScene()
    {
        OnTriggerEnter(null); // 더미 호출 (적절한 방식은 아님)
        // 대신 씬 로드를 직접 호출하는 Public 메서드를 제공하는 것이 좋습니다.
        // 예를 들어 UI 버튼 클릭 등에 연결할 때 유용합니다.
    }
    */
}