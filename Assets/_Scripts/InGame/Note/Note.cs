// Note.cs

using UnityEngine;
using Define;

public class Note : MonoBehaviour
{
    public NoteDirection requiredDirection;
    public SaberNoteType requiredNoteType;

    private Transform _noteTransform;
    private MeshFilter _meshFilter;
    private NoteMovement _noteMovement;
    private MeshRenderer _meshRenderer;
    private Collider _noteCollider; // 콜라이더 참조 추가
    // 절단 효과를 제거한다면 아래 변수들은 더 이상 필요 없습니다.
    // [HideInInspector] public Mesh OriginalMesh;
    // [HideInInspector] public Material[] OriginalMaterials;
    // private System.Type _originalColliderType;
    // private bool _originalColliderIsTrigger;
    // private Vector3 _originalBoxColliderSize;

    /// <summary>
    /// 노트 오브젝트가 로드될 때 컴포넌트들을 캐시합니다.
    /// </summary>
    private void Awake()
    {
        _noteTransform = transform;
        _meshFilter = GetComponent<MeshFilter>();
        _noteMovement = GetComponent<NoteMovement>();
        _meshRenderer = GetComponent<MeshRenderer>();
        _noteCollider = GetComponent<Collider>();

        // 절단 효과 관련 주석 처리된 부분은 완전히 제거하는 것이 깔끔합니다.
        // 현재는 주석 처리된 상태로 불필요한 코드입니다.
    }

    /// <summary>
    /// 노트를 오브젝트 풀에서 재활용할 때 초기 상태로 재설정합니다.
    /// NoteMovement 컴포넌트의 활성화 및 기본 상태를 복원합니다.
    /// </summary>
    public void ResetNote()
    {
        // 절단 효과 관련 주석 처리된 부분은 완전히 제거하는 것이 깔끔합니다.
        // 현재는 주석 처리된 상태로 불필요한 코드입니다.

        if (_noteMovement != null)
        {
            _noteMovement.enabled = true; // NoteMovement 컴포넌트 다시 활성화
                                          // NoteMovement 내부의 초기화 메서드(InitializeNote)는
                                          // NoteManager.SpawnNote()와 같은 스포너 로직에서 호출해야 합니다.
                                          // ResetNote는 활성화만 담당하거나, NoteMovement 내부의 상태를 재설정하는 역할을 합니다.

            // 필요하다면 NoteMovement의 초기 상태를 여기서 재설정할 수도 있습니다.
            // 예: _noteMovement.ResetMovementState(); // NoteMovement 스크립트에 이 메서드 구현
        }
        else
        {
            Debug.LogWarning("Note: NoteMovement 컴포넌트를 찾을 수 없습니다. NoteMovement가 이 게임 오브젝트에 없거나, Awake 시점에 누락되었습니다.", this);
        }

        if (_noteCollider != null)
        {
            _noteCollider.enabled = true;
        }
        else
        {
            Debug.LogWarning("Note: ResetNote 호출 시 Collider 컴포넌트를 찾을 수 없습니다.", this);
        }
        // 노트 오브젝트 자체의 위치, 회전, 스케일을 초기화
        _noteTransform.localPosition = Vector3.zero; // 로컬 위치 초기화
        _noteTransform.localRotation = Quaternion.identity; // 로컬 회전 초기화
        _noteTransform.localScale = _noteTransform.localScale;

        // 만약 노트의 색상이 외부에서 변경될 수 있다면, 여기서 기본 색상으로 되돌릴 수 있습니다.
        // 하지만 ApplyNoteColor가 NoteMovement에서 호출되므로, 굳이 여기서 할 필요는 없을 수 있습니다.
    }
}