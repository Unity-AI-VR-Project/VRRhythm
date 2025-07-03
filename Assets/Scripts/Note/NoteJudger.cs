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

    [Header("���� �Ÿ� ���� (����: Unity Units)")]
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
        if (distance > autoMissRange)
        {
            DoJudgement();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;
    if (!other.CompareTag("Saber")) return;

    // 1. �浹 ���� ���
    Vector3 hitDir = (transform.position - other.transform.position).normalized;

    // 2. ��Ʈ�� ���� �������� ��ȯ
    Vector3 localHitDir = transform.InverseTransformDirection(hitDir);

    // 3. �ޡ�� ���� üũ (x > 0.7)
    if (localHitDir.x > 0.7f)
    {
        // ����
        DoJudgement();

        // ���ܿ� Ŭ�� ����
        GameObject clone = Instantiate(gameObject, transform.position, transform.rotation);
        Destroy(clone.GetComponent<Collider>());
        Destroy(clone.GetComponent<NoteJudger>()); // �ߺ� ����

        // ���� ó��
        //Cutter.Cut(clone, transform.position, transform.up);
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
        gameObject.SetActive(false); // ���� ��Ȱ��ȭ
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

        Debug.Log($"[{gameObject.name}] ���� ���: {result}");
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
