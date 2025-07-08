using UnityEngine;

public class ReactiveObject : MonoBehaviour
{
    public float pushForce = 5f;
    public float returnForce = 50f;
    public float damping = 5f;
    public float returnDelay = 1f;

    private Rigidbody rb;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isReturning = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        originalPosition = transform.position;
        originalRotation = transform.rotation;

        rb.useGravity = false;
        rb.isKinematic = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isReturning)
        {
            Vector3 pushDir = (transform.position - collision.contacts[0].point).normalized;
            rb.AddForce(pushDir * pushForce, ForceMode.Impulse);

            CancelInvoke(nameof(StartReturn));
            Invoke(nameof(StartReturn), returnDelay);
        }
    }

    private void StartReturn()
    {
        isReturning = true;
    }

    void FixedUpdate()
    {
        if (isReturning)
        {
            Vector3 toOriginal = originalPosition - transform.position;
            Vector3 springForce = toOriginal * returnForce - rb.linearVelocity * damping;
            rb.AddForce(springForce, ForceMode.Force);

            Quaternion toRot = Quaternion.Inverse(transform.rotation) * originalRotation;
            toRot.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;
            Vector3 angVel = angle * axis.normalized;
            rb.AddTorque(angVel * returnForce - rb.angularVelocity * damping, ForceMode.Force);

            // 정밀하게 돌아왔는지 체크
            if (toOriginal.magnitude < 0.01f && rb.linearVelocity.magnitude < 0.05f)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                transform.position = originalPosition;
                transform.rotation = originalRotation;
                isReturning = false;
            }
        }
    }
}