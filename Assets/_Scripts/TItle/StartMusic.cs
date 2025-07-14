using UnityEngine;

public class StartMusic : MonoBehaviour
{


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InGameManager.Instance.StartGame();
    }

}
