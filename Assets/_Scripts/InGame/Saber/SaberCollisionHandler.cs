using UnityEngine;
using Define; // Define 네임스페이스가 없다면 제거하거나 적절히 수정해주세요.

public class SaberCollisionHandler : MonoBehaviour
{
    [SerializeField] private Saber _saber;
    [SerializeField] private AudioClip _hitSoundClip; // 노트 충돌 시 재생할 효과음 클립

    private NoteJudger _noteJudgerInstance;
    private MusicSynchronizer _musicTimeChecker;

    /// <summary>
    /// 스크립트 인스턴스가 로드될 때 호출되며, 필요한 컴포넌트 참조를 설정하고 유효성을 검사합니다.
    /// </summary>
    void Start()
    {
        // Saber 컴포넌트 참조 확인 및 설정
        if (_saber == null)
        {
            _saber = GetComponent<Saber>();
        }

        if (_saber == null)
        {
            Debug.LogError("SaberCollisionHandler: Saber 컴포넌트를 찾을 수 없습니다. 이 스크립트는 Saber 컴포넌트와 함께 사용되어야 합니다.", this);
            enabled = false;
            return; // 에러 발생 시 더 이상 진행하지 않음
        }

        // 현재 씬이 InGame이 아닐 경우 조기 리턴
        if (GameManager.Instance.sceneController.currentScene != eScenes.InGame)
        {
            return;
        }

        // NoteManager 관련 참조 설정
        if (NoteManager.Instance != null)
        {
            _noteJudgerInstance = NoteManager.Instance.GetNoteJudger();
            _musicTimeChecker = NoteManager.Instance.musicTimeChecker;
        }
        else
        {
            Debug.LogError("SaberCollisionHandler: NoteManager.Instance가 초기화되지 않았습니다. NoteJudger를 가져올 수 없습니다.", this);
            enabled = false;
            return;
        }

        // MusicTimeChecker 참조 확인
        if (_musicTimeChecker == null)
        {
            Debug.LogError("SaberCollisionHandler: MusicTimeChecker 참조를 가져올 수 없습니다. 판정 처리를 할 수 없습니다.", this);
            enabled = false;
            return;
        }

        // 효과음 클립이 할당되지 않았다면 경고
        if (_hitSoundClip == null)
        {
            Debug.LogWarning("SaberCollisionHandler: 노트 충돌 효과음 클립이 할당되지 않았습니다. 효과음이 재생되지 않습니다.", this);
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

            // NoteJudger 및 MusicTimeChecker 참조가 Start에서 제대로 초기화되었는지 다시 확인 (안전 장치)
            if (_noteJudgerInstance == null)
            {
                _noteJudgerInstance = NoteManager.Instance?.GetNoteJudger();
                if (_noteJudgerInstance == null)
                {
                    Debug.LogError("SaberCollisionHandler: NoteJudger 인스턴스가 할당되지 않았습니다. 판정 처리를 건너뜁니다.");
                    NoteManager.Instance?.ReturnPooledNote(other.gameObject);
                    return;
                }
            }

            if (_musicTimeChecker == null)
            {
                _musicTimeChecker = NoteManager.Instance?.musicTimeChecker;
                if (_musicTimeChecker == null)
                {
                    Debug.LogError("SaberCollisionHandler: musicTimeChecker 인스턴스가 할당되지 않았습니다. 판정 처리를 건너킵니다.");
                    NoteManager.Instance?.ReturnPooledNote(other.gameObject);
                    return;
                }
            }
            GameManager.Instance.soundManager.PlaySFX(_hitSoundClip);
            _noteJudgerInstance.JudgeAndProcessNote(other.gameObject, _saber, other, _musicTimeChecker);
        }

        // 버튼 오브젝트 처리
        if (other.TryGetComponent<ObjectButton>(out ObjectButton objectButton))
        {
            objectButton.OnButtonClick();
            Destroy(objectButton.gameObject); // 트리거 오브젝트 제거
        }
    }
}