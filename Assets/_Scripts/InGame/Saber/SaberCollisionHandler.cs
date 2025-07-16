using UnityEngine;
using Define;

public class SaberCollisionHandler : MonoBehaviour
{
    [SerializeField] private Saber _saber;

    private NoteJudger _noteJudgerInstance;
    private MusicSynchronizer _musicTimeChecker;

    /// <summary>
    /// 스크립트 인스턴스가 로드될 때 호출되며, 필요한 컴포넌트 참조를 설정하고 유효성을 검사합니다.
    /// </summary>
    void Start()
    {
        if (_saber == null)
        {
            _saber = GetComponent<Saber>();
        }

        if (_saber == null)
        {
            Debug.LogError("SaberCollisionHandler: Saber 컴포넌트를 찾을 수 없습니다. 이 스크립트는 Saber 컴포넌트와 함께 사용되어야 합니다.", this);
            enabled = false;
        }
        if (GameManager.Instance.sceneController.currentScene != eScenes.InGame)
        {
            return;
        }

        if (NoteManager.Instance != null)
        {
            _noteJudgerInstance = NoteManager.Instance.GetNoteJudger();
            _musicTimeChecker = NoteManager.Instance.musicTimeChecker;
        }
        else
        {
            Debug.LogError("SaberCollisionHandler: NoteManager.Instance가 초기화되지 않았습니다. NoteJudger를 가져올 수 없습니다.", this);
            enabled = false;
        }

        if (_musicTimeChecker == null)
        {
            Debug.LogError("SaberCollisionHandler: MusicTimeChecker 참조를 가져올 수 없습니다. 판정 처리를 할 수 없습니다.", this);
            enabled = false;
        }
    }

    /// <summary>
    /// 콜라이더가 다른 트리거 콜라이더에 진입했을 때 호출됩니다.
    /// 노트와의 충돌을 감지하고 NoteJudger에게 판정 처리를 위임합니다.
    /// </summary>
    /// <param name="other">충돌한 다른 콜라이더입니다.</param>
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Note"))
        {
            Note noteComponent = other.GetComponent<Note>();
            if (noteComponent == null)
            {
                Debug.LogWarning($"SaberCollisionHandler: 충돌한 오브젝트({other.name})에 Note 컴포넌트가 없습니다.", other);
                NoteManager.Instance?.ReturnPooledNote(other.gameObject);
                return;
            }

            NoteMovement noteMover = other.GetComponent<NoteMovement>();
            if (noteMover == null)
            {
                Debug.LogWarning($"SaberCollisionHandler: 충돌한 오브젝트({other.name})에 NoteMover 컴포넌트가 없습니다.", other);
                NoteManager.Instance?.ReturnPooledNote(other.gameObject);
                return;
            }

            if (_noteJudgerInstance == null)
            {
                _noteJudgerInstance = NoteManager.Instance.GetNoteJudger();
                if (_noteJudgerInstance == null)
                {
                    Debug.LogError("SaberCollisionHandler: NoteJudger 인스턴스가 할당되지 않았습니다. 판정 처리를 건너뜁니다.");
                    NoteManager.Instance?.ReturnPooledNote(other.gameObject);
                    return;
                }
            }

            if (_musicTimeChecker == null)
            {
                _musicTimeChecker = NoteManager.Instance.musicTimeChecker;
                if(_musicTimeChecker == null)
                {
                    Debug.LogError("SaberCollisionHandler: musicTimeChecker 인스턴스가 할당되지 않았습니다. 판정 처리를 건너뜁니다.");
                    NoteManager.Instance?.ReturnPooledNote(other.gameObject);
                    return;
                }
            }

            _noteJudgerInstance.JudgeAndProcessNote(other.gameObject, _saber, other, _musicTimeChecker);
        }
        if (other.TryGetComponent<ObjectButton>(out ObjectButton objectButton))
        {
            objectButton.OnButtonClick();
            Destroy(objectButton.gameObject); // 트리거 오브젝트 제거
        }
    }
}