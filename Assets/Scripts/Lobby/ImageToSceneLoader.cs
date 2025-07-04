using UnityEngine;
using UnityEngine.SceneManagement;

public class ImageToSceneLoader : MonoBehaviour
{
    [Tooltip("이 이미지를 선택하면 이동할 씬 이름")]
    public int sceanNumber;

    [SerializeField]
    private float cooldown = 1f;
    private float lastHitTime = -999f;

    private void OnTriggerEnter(Collider other)
    {
      
        if (!other.CompareTag("Saber")) return;

        if (Time.time - lastHitTime < cooldown) return;

        lastHitTime = Time.time;

        Debug.Log($"[Scene Load] Loading scene: {sceanNumber}");
        
        SceneManager.LoadScene(sceanNumber);
    }
}
