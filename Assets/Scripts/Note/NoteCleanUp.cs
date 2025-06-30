using UnityEngine;

public class NoteCleanUp : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // 세이버에서 날아온 방향 계산
        Vector3 hitDir = (transform.position - other.transform.position).normalized;

        // 로컬 기준으로 변환
        Vector3 localHitDir = transform.InverseTransformDirection(hitDir);

        if (other.CompareTag("DeadZone") || other.CompareTag("Saber") && localHitDir.x > 0.7f)
        {
            NotePoolManager.Instance.ReturnNote(gameObject);
         
            return;
        }

        //    if (other.CompareTag("Saber"))
        //    {



        //        if (localHitDir.x > 0.7f)
        //        {
        //            NotePoolManager.Instance.ReturnNote(gameObject);
        //            Debug.Log("노트의 위쪽(Local 기준)에서 세이버에 맞음 → 풀로 반환됨");
        //        }
        //    }
        //}
    }
}
