using UnityEngine;

public class TitleNote : MonoBehaviour
{
    public float speed;
    public Vector3 directionVector;
    public AudioClip noteCutClip;

    void Start()
    {
        
    }

    void Update()
    {
        transform.Translate(directionVector * speed * Time.deltaTime);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Saber"))
        {
            GameManager.Instance.soundManager.PlaySFX(noteCutClip);
            Destroy(transform.GetChild(0).gameObject);
            Destroy(gameObject);
        }
    }
}
