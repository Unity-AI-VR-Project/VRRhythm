using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리를 위해 추가

// UIManagerBase가 어떤 기능을 하는지 알 수 없으므로, 필요하다면 해당 코드를 여기에 포함시키거나
// UIManagerBase의 내용에 따라 적절히 조정해주세요.
// 예시: public abstract class UIManagerBase : MonoBehaviour { protected virtual void Awake() { Debug.Log("UIManagerBase Awake"); } protected virtual void InitializeUI() { } protected virtual void OnDestroy() { } }
public class LobbyUIManager : UIManagerBase
{
    
    // 예를 들어, 노래 제목을 표시할 TextMeshProUGUI
    // public TMPro.TextMeshProUGUI songTitleText; 
    // public List<TMPro.TextMeshProUGUI> songInfoTexts; // 노래 정보 표시를 위한 텍스트 리스트 등

    // SongSelect (CanvasMover)의 OnSongChanged 이벤트를 구독하여 노래 변경을 처리할 메소드
    private void HandleSongChanged(int songIndex)
    {
        Debug.Log($"[LobbyUIManager] 노래가 {songIndex}번으로 변경되었습니다. 해당 노래에 맞는 UI를 업데이트합니다.");
        // 여기에 노래 인덱스에 따라 UI를 업데이트하는 로직을 추가할 수 있습니다.
        // 예: 노래 제목, 아티스트 정보 표시 등
        // if (songTitleText != null && songIndex >= 0 && songIndex < GetComponent<CanvasMover>().clips.Length)
        // {
        //     songTitleText.text = GetComponent<CanvasMover>().clips[songIndex].name;
        // }
    }

    // SceneLoaderOnSaberContact의 OnSceneLoadRequested 이벤트를 구독하여 씬 로드 요청을 처리할 메소드
    private void HandleSceneLoadRequested(string sceneName)
    {
        Debug.Log($"[LobbyUIManager] 씬 로드 요청을 받았습니다: {sceneName}");
        // 실제 씬 로드 로직을 수행합니다.
        SceneManager.LoadScene(sceneName);
    }

    protected override void Awake()
    {
        base.Awake();
        // 이벤트 구독
        // CanvasMover는 static 이벤트를 사용하므로, CanvasMover 인스턴스가 아닌 타입으로 구독합니다.
        CanvasMover.OnSongChanged += HandleSongChanged;
        SceneLoaderOnSaberContact.OnSceneLoadRequested += HandleSceneLoadRequested;

        InitializeUI(); // UI 초기화는 Awake 또는 Start에서 호출하는 것이 좋습니다.
    }

    protected override void InitializeUI()
    {
        base.InitializeUI();
        // 여기에 LobbyUIManager가 관리하는 UI 요소들의 초기화 로직을 추가할 수 있습니다.
        // 예를 들어, 초기 노래 정보 표시 등 (Awake에서 이미 PlayClip이 호출되었으므로, 초기 정보를 받아올 수 있음)
        // CanvasMover mover = FindObjectOfType<CanvasMover>(); // 씬에 CanvasMover가 하나만 있다고 가정
        // if (mover != null && songTitleText != null && mover.clips != null && mover.clips.Length > 0)
        // {
        //     // 초기 노래 정보 표시 (만약 currentCenterCanvasOriginalIndex가 CanvasMover에 public으로 노출되어 있다면 사용 가능)
        //     // 또는 그냥 clips[0]으로 초기화.
        //     songTitleText.text = mover.clips[0].name; // 첫 번째 곡 이름으로 초기화
        // }
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        // 메모리 누수를 방지하기 위해 이벤트를 구독 해제합니다.
        CanvasMover.OnSongChanged -= HandleSongChanged;
        SceneLoaderOnSaberContact.OnSceneLoadRequested -= HandleSceneLoadRequested;
    }
}