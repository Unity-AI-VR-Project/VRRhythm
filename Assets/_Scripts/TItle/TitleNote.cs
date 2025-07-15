using UnityEngine;

public class TitleNote : MonoBehaviour
{
    public float speed;
    public Vector3 directionVector;

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
            // SFX & VFX
            Destroy(gameObject);
        }
    }
}
