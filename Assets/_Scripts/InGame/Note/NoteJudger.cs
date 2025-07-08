using UnityEngine;

public enum JudgementType
{
    Perfect,
    Excellent,
    Good,
    Normal,
    Miss
}

public class NoteJudger : MonoBehaviour
{
    private Vector3 spawnPosition;
    private bool isActive = false;

    [Header("노트 거리 설정 (단위: Unity Units)")] // 수정됨:  Ÿ 
    public float perfectRange;
    public float excellentRange;
    public float goodRange;
    public float normalRange;
    public float autoMissRange;

    void OnEnable()
    {
        spawnPosition = transform.position;
        isActive = true;
    }

    void Update()
    {
        if (!isActive) return;

        float distance = Vector3.Distance(transform.position, spawnPosition);
        /*if (distance > autoMissRange)
        {
            DoJudgement();
        }*/
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;
        if (!other.CompareTag("Saber")) return;
        

        // 1. 충돌 방향 계산 // 수정됨: 浹  
        Vector3 hitDir = (transform.position - other.transform.position).normalized;

        // 2. 노트를 자신 로컬 좌표계로 변환 // 수정됨: Ʈ   ȯ
        Vector3 localHitDir = transform.InverseTransformDirection(hitDir);
        Debug.Log(localHitDir);

        // 3. 베기 방향 체크 (x > 0.7) // 수정됨: ޡ  üũ
        if (localHitDir.x > 0.25f)
        {
            // 판정 // 수정됨: 
            DoJudgement();
            // 잔상으로 클론 생성 // 수정됨: ܿ Ŭ 
            GameObject clone = Instantiate(gameObject, transform.position, transform.rotation);
            Destroy(clone.GetComponent<Collider>());
            Destroy(clone.GetComponent<NoteJudger>()); // 중복 제거 // 수정됨: ߺ 

            // 절단 처리 // 수정됨:  ó
            Cutter.Cut(clone, transform.position, transform.up);
            gameObject.GetComponent<Collider>().enabled = false;
            Destroy(clone, 2f);
        }
    }

    void DoJudgement()
    {
        float distance = Vector3.Distance(transform.position, spawnPosition);
        JudgementType judgement = GetJudgement(distance);
        ApplyJudgement(judgement);

        ParticlePoolManager.instance.SpawnParticle(judgement.ToString(), transform.position);

        isActive = false;
        gameObject.SetActive(false); // 노트 비활성화 // 수정됨:  Ȱȭ
    }

    JudgementType GetJudgement(float distance)
    {
        if (distance <= perfectRange) return JudgementType.Perfect;
        if (distance <= excellentRange) return JudgementType.Excellent;
        if (distance <= goodRange) return JudgementType.Good;
        if (distance <= normalRange) return JudgementType.Normal;
        return JudgementType.Miss;
    }

    void ApplyJudgement(JudgementType result)
    {
        switch (result)
        {
            case JudgementType.Perfect:
            case JudgementType.Excellent:
            case JudgementType.Good:
                InGameManager.instance.AddScore(GetScore(result));
                InGameManager.instance.AddCombo();
                break;

            case JudgementType.Normal:
                InGameManager.instance.AddScore(GetScore(result));
                InGameManager.instance.ResetCombo();
                break;

            case JudgementType.Miss:
                InGameManager.instance.AddScore(GetScore(result));
                InGameManager.instance.ResetCombo();
                InGameManager.instance.TakeDamage(20);
                break;
        }

        Debug.Log($"[{gameObject.name}] 판정 결과: {result}"); // 수정됨:  
    }

    int GetScore(JudgementType result)
    {
        return result switch
        {
            JudgementType.Perfect => 100,
            JudgementType.Excellent => 70,
            JudgementType.Good => 50,
            JudgementType.Normal => 20,
            JudgementType.Miss => 0,
            _ => 0
        };
    }
}