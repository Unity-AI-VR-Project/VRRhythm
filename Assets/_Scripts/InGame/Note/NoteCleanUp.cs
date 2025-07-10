using UnityEngine;
using System.Collections; // Coroutine을 사용할 경우 필요

/// <summary>
/// 노트 오브젝트의 충돌을 감지하고, 해당 오브젝트를 NoteManager의 풀에 반환하는 역할을 합니다.
/// 세이버와의 판정 로직은 SaberCollisionHandler와 NoteJudger에서 처리합니다.
/// </summary>
public class NoteCleanUp : MonoBehaviour
{
    /// <summary>
    /// 다른 콜라이더와 트리거 충돌이 발생했을 때 호출됩니다.
    /// </summary>
    /// <param name="other">충돌한 다른 콜라이더.</param>
    void OnTriggerEnter(Collider other)
    {
        // DeadZone 태그를 가진 오브젝트와 닿았을 경우
        if (other.CompareTag("DeadZone"))
        {
            // NoteManager가 초기화되어 있는지 확인
            if (NoteManager.Instance != null)
            {
                // DeadZone에 닿았으므로, 판정에 실패한 것으로 간주하고 풀에 반환합니다.
                // NoteManager의 ReturnPooledNote에 miss 여부를 전달할 수 있도록 확장하거나,
                // 여기서 InGameManager에 직접 Miss 처리를 알릴 수 있습니다.
                // 여기서는 NoteManager.Instance.ReturnPooledNote를 호출하며,
                // NoteManager나 InGameManager에서 DeadZone으로 인한 Miss 처리를 별도로 할 수 있도록 유도합니다.

                // DeadZone 통과는 미스 판정으로 연결되므로, InGameManager에 Miss를 알리고 콤보 초기화
                InGameManager.Instance?.ResetCombo();
                InGameManager.Instance?.TakeDamage(20); // Miss 데미지 적용

                // 파티클 스폰 (Miss)
                ParticlePoolManager.Instance?.SpawnParticle("Miss", transform.position);

                // 노트 시각적 요소 비활성화 및 풀 반환
                NoteManager.Instance.HandleNoteCutVisuals(gameObject, transform.position, Vector3.zero, true); // isMissed: true 전달
            }
            else
            {
                Debug.LogWarning("NoteCleanUp: NoteManager 인스턴스를 찾을 수 없어 노트를 풀에 반환하지 못했습니다. Destroy 호출.", this);
                Destroy(gameObject); // 비상 처리: 풀이 없으면 그냥 파괴
            }
            return;
        }

        // Saber 태그를 가진 오브젝트와 닿았을 경우
        // 이 로직은 SaberCollisionHandler에서 이미 처리하고 있으므로,
        // 여기서는 아무것도 하지 않습니다.
        // SaberCollisionHandler가 이 충돌을 감지하고 NoteJudger에 판정을 요청할 것입니다.
        // NoteJudger는 판정 후 NoteManager에 시각 효과 및 풀 반환을 요청합니다.
        // 따라서 이 스크립트에서는 Saber 태그에 대한 추가적인 처리가 필요 없습니다.
    }

    // DelayedReturn 코루틴은 이제 필요 없습니다.
    // Saber와의 충돌은 NoteJudger -> NoteManager에서 직접 풀 반환을 처리하며,
    // DeadZone과의 충돌도 여기서 직접 NoteManager를 통해 반환합니다.
}