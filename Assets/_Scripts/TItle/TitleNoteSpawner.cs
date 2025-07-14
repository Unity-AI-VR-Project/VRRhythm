using UnityEngine;

public class TitleNoteSpawner : MonoBehaviour
{
    [SerializeField] private GameObject titleNote;
    public float noteSpawnDelay;
    public float bpm = 120f;
    [SerializeField] private float targetBeatDivision = 8f;
    [SerializeField] private float spawnBeatDivision = 4f;
    [SerializeField] private float noteLifeTime;
    [SerializeField] private float noteSpeed;
    [SerializeField] private Vector3 noteDirection;

    private const float targetDistance = 10f;

    private void Awake()
    {
        float secondsPerBeat = 60f / bpm;

        noteLifeTime = secondsPerBeat / (targetBeatDivision / 4f);

        noteSpeed = targetDistance / noteLifeTime;

        noteSpawnDelay = 60f / spawnBeatDivision;
    }

    public void NoteCreate()
    {
        TitleNote note = Instantiate(titleNote, transform.position,Quaternion.identity).GetComponent<TitleNote>();
        note.speed = noteSpeed;
        note.directionVector = noteDirection;
        Destroy(note.gameObject, noteLifeTime*1.5f);
        Invoke("NoteCreate", noteSpawnDelay);
    }
}
