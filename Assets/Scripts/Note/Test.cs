using UnityEngine;

public class Test : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private float speed;
   
    void Start()
    {
        speed = 10;
    }

    // Update is called once per frame
    void Update()
    {
       
        transform.position += new Vector3(0, 0, 1f)*Time.deltaTime*speed;
        
    }
}
