using UnityEngine;
using UnityEngine.Pool;
using System.Collections;
using System.Collections.Generic;

// 파티클 오브젝트 풀을 관리하는 클래스
public class ParticlePoolManager : MonoBehaviour
{
    // 외부에서 접근 가능한 싱글턴 인스턴스
    public static ParticlePoolManager instance
    {
        get
        {
            // 인스턴스가 없으면 씬에서 검색하여 할당
            if (m_instance == null)
            {
                m_instance = FindAnyObjectByType<ParticlePoolManager>();
            }
            return m_instance;
        }
    }

    // 실제 인스턴스를 저장하는 정적 필드
    private static ParticlePoolManager m_instance;

    // 키에 따라 다양한 파티클 프리팹을 저장할 수 있는 구조체
    [System.Serializable]
    public class ParticleEntry
    {
        public string key;              // 파티클 식별 키
        public ParticleSystem prefab;  // 해당 키에 대응하는 파티클 프리팹
    }

    [Header("등록된 파티클 프리팹들")]
    public ParticleEntry[] particlePrefabs; // 인스펙터에서 설정 가능한 프리팹 배열

    // 키별로 오브젝트 풀을 관리하는 딕셔너리
    private Dictionary<string, ObjectPool<ParticleSystem>> particlePools;

    private void Awake()
    {
        // 중복 싱글턴 방지: 만약 다른 인스턴스라면 제거
        if (instance != this)
        {
            Destroy(gameObject);
        }

        // 딕셔너리 초기화
        particlePools = new Dictionary<string, ObjectPool<ParticleSystem>>();

        // 각 프리팹마다 오브젝트 풀 생성
        foreach (var entry in particlePrefabs)
        {
            string key = entry.key;
            ParticleSystem prefab = entry.prefab;

            // 오브젝트 풀 생성 및 딕셔너리에 등록
            particlePools[key] = new ObjectPool<ParticleSystem>(
                createFunc: () =>
                {
                    ParticleSystem ps = Instantiate(prefab);     // 새 파티클 인스턴스 생성
                    ps.gameObject.SetActive(false);              // 기본적으로 비활성화
                    return ps;
                },
                actionOnGet: (ps) => ps.gameObject.SetActive(true),    // 풀에서 꺼낼 때 활성화
                actionOnRelease: (ps) => ps.gameObject.SetActive(false), // 반환 시 비활성화
                actionOnDestroy: (ps) => Destroy(ps.gameObject),        // 풀 자체 삭제 시 객체 제거
                collectionCheck: false,
                defaultCapacity: 10 // 초기 풀 크기 설정
            );
           
        }
    }

    /// <summary>
    /// 해당 키의 파티클을 지정된 위치에 재생
    /// </summary>
    public void SpawnParticle(string key, Vector3 position)
    {
        // 키가 등록되지 않았으면 경고 로그 출력 후 종료
        if (!particlePools.ContainsKey(key))
        {
            Debug.LogWarning($"[ParticlePoolManager] 등록되지 않은 파티클 키: {key}");
            return;
        }

        // 풀에서 파티클 인스턴스를 꺼내 위치 지정 후 재생
        var pool = particlePools[key];
        ParticleSystem ps = pool.Get();
        ps.transform.position = position;

        ps.Play();

        // 파티클 재생이 끝난 후 자동으로 반환하는 코루틴 시작
        StartCoroutine(ReturnAfterDelay(ps, pool, ps.main.duration));
    }

    // 일정 시간 후 파티클을 풀에 반환하는 코루틴
    private IEnumerator ReturnAfterDelay(ParticleSystem ps, ObjectPool<ParticleSystem> pool, float delay)
    {
        yield return new WaitForSeconds(delay);
        pool.Release(ps);
    }
}