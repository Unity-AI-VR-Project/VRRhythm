using UnityEngine;
using System.Collections;

/// <summary>
/// 노트 오브젝트가 DeadZone이랑 Saber에 닿았을 때 처리하는 스크립트
/// - DeadZone에 닿으면 바로 풀에 반환
/// - Saber에 닿았을 때 일정 조건을 만족하면 효과 처리 후 풀에 반환
/// </summary>
public class NoteCleanUp : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // 데드존에 닿았을 경우: 바로 풀에 반환
        if (other.CompareTag("DeadZone"))
        {
            NotePoolManager.Instance.ReturnNote(gameObject);
            return;
        }

        // 세이버(Saber)에 닿았을 경우
        if (other.CompareTag("Saber"))
        {
            // 충돌 방향 계산 (세이버에서 노트 방향)
            Vector3 hitDir = (transform.position - other.transform.position).normalized;
            // 로컬 좌표계로 변환
            Vector3 localHitDir = transform.InverseTransformDirection(hitDir);

            // 오른쪽에서 왼쪽으로 베었을 경우만 타당한 커트로 인식 (x > 0.7f 기준)
            if (localHitDir.x > 0.7f)
            {
                Vector3 cutPoint = transform.position;     // 절단 위치
                Vector3 cutNormal = transform.forward;     // 절단 기준 방향

                // 현재 Note를 복제해서 효과 처리용으로 사용
                GameObject clone = Instantiate(gameObject, transform.position, transform.rotation);
                clone.transform.localScale = transform.localScale;

                // 복제된 오브젝트에서는 충돌 처리 및 풀 반환 스크립트 제거
                Destroy(clone.GetComponent<NoteCleanUp>()); // 충돌 중복 방지
                Destroy(clone.GetComponent<Collider>());    // 충돌 방지용 콜라이더 제거

                // 절단 효과 적용 (외부 Cutter 클래스 활용)
                //Cutter.Cut(clone, cutPoint, cutNormal);

                // 절단된 복제 오브젝트는 2초 후 제거
                Destroy(clone, 2f);

                // 원본 Note는 잠시 후 풀에 반환
                StartCoroutine(DelayedReturn());
            }
        }
    }

    /// <summary>
    /// 잠시 대기 후 Note를 풀에 반환
    /// </summary>
    IEnumerator DelayedReturn()
    {
        yield return new WaitForSeconds(0.5f); // 잠시 대기
        NotePoolManager.Instance.ReturnNote(gameObject); // 풀에 반환
    }
}