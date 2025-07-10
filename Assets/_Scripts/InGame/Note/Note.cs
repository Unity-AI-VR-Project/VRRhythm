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

    [HideInInspector] public Mesh OriginalMesh;
    [HideInInspector] public Material[] OriginalMaterials;

    private System.Type _originalColliderType;
    private bool _originalColliderIsTrigger;
    private Vector3 _originalBoxColliderSize;

    /// <summary>
    /// 노트 오브젝트가 로드될 때 컴포넌트들을 캐시하고 원본 메쉬 및 콜라이더 정보를 저장합니다.
    /// </summary>
    private void Awake()
    {
        _noteTransform = transform;
        _meshFilter = GetComponent<MeshFilter>();
        _noteMovement = GetComponent<NoteMovement>();
        _meshRenderer = GetComponent<MeshRenderer>();

        if (OriginalMesh == null && _meshFilter != null)
        {
            OriginalMesh = _meshFilter.sharedMesh;
        }

        if (OriginalMaterials == null && _meshRenderer != null)
        {
            OriginalMaterials = _meshRenderer.sharedMaterials;
        }

        Collider initialCollider = GetComponent<Collider>();
        if (initialCollider != null)
        {
            _originalColliderType = initialCollider.GetType();
            _originalColliderIsTrigger = initialCollider.isTrigger;

            if (initialCollider is BoxCollider boxCol)
            {
                _originalBoxColliderSize = boxCol.size;
            }
        }
    }

    /// <summary>
    /// 노트를 오브젝트 풀에서 재활용할 때 초기 상태로 재설정합니다.
    /// 메쉬, 콜라이더, 컴포넌트 활성화 상태 등을 원본으로 복원합니다.
    /// </summary>
    public void ResetNote()
    {
        if (_meshFilter != null && OriginalMesh != null)
        {
            _meshFilter.mesh = OriginalMesh;
        }

        if (_meshRenderer != null && OriginalMaterials != null)
        {
            _meshRenderer.materials = OriginalMaterials;
        }

        Collider[] allCurrentColliders = GetComponents<Collider>();
        foreach (var col in allCurrentColliders)
        {
            if (col != null)
            {
                Destroy(col);
            }
        }

        if (_originalColliderType != null)
        {
            Collider newCollider = gameObject.AddComponent(_originalColliderType) as Collider;
            if (newCollider != null)
            {
                newCollider.isTrigger = _originalColliderIsTrigger;

                if (newCollider is BoxCollider boxCol)
                {
                    boxCol.size = _originalBoxColliderSize;
                }
                newCollider.enabled = true;
            }
        }
        else
        {
            Debug.LogWarning($"Note {gameObject.name}: 원래 콜라이더 타입 정보가 없습니다. 콜라이더 복원 실패.", this);
        }

        if (_noteMovement != null) _noteMovement.enabled = true;
    }
}