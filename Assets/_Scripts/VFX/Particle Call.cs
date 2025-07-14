using UnityEngine;

public class ParticleCall : MonoBehaviour
{
    public string particleName;


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Saber"))
        {

            ParticleManager.Instance.PlayParticle(particleName, transform.position, Quaternion.identity);


        }
    }
}
