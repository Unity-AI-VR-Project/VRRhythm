using UnityEngine;

public class Test : MonoBehaviour
{
     public RotationManager manager;
    float time;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        time = 0;
    }

    // Update is called once per frame
    void Update()
    {time += Time.deltaTime;
        if(time>3)
        {
            Debug.Log("¹ßµ¿");
            manager.SetRotationSpeed(0, -100f);

        }
        
    }
}
