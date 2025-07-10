using UnityEngine;
using Define;

public class Note : MonoBehaviour
{
    public NoteDirection requiredDirection;
    public SaberNoteType requiredNoteType;

    private Transform noteTransform;
    private Collider noteCollider;
    private NoteMovement noteMovement;

    private void Awake()
    {
        noteTransform = transform;
        noteCollider = GetComponent<Collider>();
        noteMovement = GetComponent<NoteMovement>();
    }

    public void ResetNote()
    {
        if (noteCollider != null) noteCollider.enabled = true;
        if (noteMovement != null) noteMovement.enabled = true;
        gameObject.SetActive(true);
    }
}