using UnityEngine;

public class ParticleTest : MonoBehaviour
{

    private void Update()
    {


        if (Input.GetKeyDown(KeyCode.Space))
        {
            ParticleManager.Instance.PlayParticle("Red", transform.position, Quaternion.identity);
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Saber"))
        {

            ParticleManager.Instance.PlayParticle("Select"/*"Red,Blue*/, transform.position, Quaternion.identity);


        }
    }
}
