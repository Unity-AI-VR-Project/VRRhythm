using UnityEngine;
using System.Collections;

/// <summary>
/// 노트 오브젝트가 DeadZone이나 Saber에 닿을 때 처리하는 스크립트
/// - DeadZone에 닿으면 즉시 풀로 반환
/// - Saber에 닿으면 방향을 판별해 절단 효과 처리 후 풀로 반환
/// </summary>
public class NoteCleanUp : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // 데드존에 닿았을 경우: 바로 풀로 반환
        if (other.CompareTag("DeadZone"))
        {
            NotePoolManager.Instance.ReturnNote(gameObject);
            return;
        }

        // 세이버(Saber)에 닿았을 경우
        if (other.CompareTag("Saber"))
        {
            // 충돌 방향 계산 (세이버에서 날아온 방향)
            Vector3 hitDir = (transform.position - other.transform.position).normalized;
            // 로컬 공간 기준으로 변환
            Vector3 localHitDir = transform.InverseTransformDirection(hitDir);

            // 왼쪽에서 오른쪽으로 오는 타격만 인식 (x > 0.7f 조건)
            if (localHitDir.x > 0.7f)
            {
                Vector3 cutPoint = transform.position;     // 절단 위치
                Vector3 cutNormal = transform.forward;     // 절단 기준 방향

                // 현재 Note의 복사본 생성 (절단 효과용)
                GameObject clone = Instantiate(gameObject, transform.position, transform.rotation);
                clone.transform.localScale = transform.localScale;
               

                // 복사된 오브젝트에는 충돌 처리, 풀 반환 스크립트 제거
                Destroy(clone.GetComponent<NoteCleanUp>()); // 충돌 중복 방지
                Destroy(clone.GetComponent<Collider>());    // 충돌 방지용 콜라이더 제거

                // 복사본에 절단 적용 (파편 등 효과 발생)
                Cutter.Cut(clone, cutPoint, cutNormal);

                // 절단된 복사본은 2초 후 삭제
                Destroy(clone, 2f);

                // 원본 Note는 잠시 후 풀로 반환
                StartCoroutine(DelayedReturn());
            }
        }
    }

    /// <summary>
    /// 절단 연출 후 일정 시간 기다리고 Note 풀에 반환
    /// </summary>
    IEnumerator DelayedReturn()
    {
        yield return new WaitForSeconds(0.5f); // 연출 시간 확보
        NotePoolManager.Instance.ReturnNote(gameObject); // 풀로 복귀
    }
}