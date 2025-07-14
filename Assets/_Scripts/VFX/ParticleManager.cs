using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleManager : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static ParticleManager Instance { get; private set; }

    [Header("Particle System Prefabs")]
    // 사용할 파티클 시스템 프리팹들을 여기에 할당합니다.
    // 각 프리팹의 이름을 키로 사용하여 접근할 수 있도록 Dictionary를 사용합니다.
    public List<ParticleSystemInfo> particleSystemInfos;

    // 내부적으로 사용될 파티클 프리팹 딕셔너리
    private Dictionary<string, GameObject> particlePrefabs = new Dictionary<string, GameObject>();

    [System.Serializable]
    public class ParticleSystemInfo
    {
        public string particleName; // 파티클의 고유 이름 (예: "HitEffect", "Explosion")
        public GameObject particlePrefab; // 해당 파티클 시스템 프리팹
    }

    private void Awake()
    {
        // 싱글톤 인스턴스 초기화
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 파티클 프리팹들을 딕셔너리에 로드
        foreach (var info in particleSystemInfos)
        {
            if (!particlePrefabs.ContainsKey(info.particleName))
            {
                particlePrefabs.Add(info.particleName, info.particlePrefab);
            }
            else
            {
                Debug.LogWarning($"ParticleManager: Duplicate particle name found: {info.particleName}. Please ensure unique names for particle systems.");
            }
        }
    }

    /// <summary>
    /// 지정된 종류의 파티클을 특정 위치와 회전으로 생성하고 재생합니다.
    /// </summary>
    /// <param name="particleType">재생할 파티클의 종류 (ParticleSystemInfo에 등록된 이름)</param>
    /// <param name="position">파티클이 생성될 월드 위치</param>
    /// <param name="rotation">파티클이 생성될 회전 (옵션, 기본값 Quaternion.identity)</param>
    public void PlayParticle(string particleType, Vector3 position, Quaternion rotation = default(Quaternion))
    {
        if (rotation == default(Quaternion))
        {
            rotation = Quaternion.identity; // 기본 회전값 설정
        }

        if (particlePrefabs.ContainsKey(particleType))
        {
            GameObject particlePrefab = particlePrefabs[particleType];

            // 파티클 시스템 인스턴스 생성
            // 여기서는 간단하게 Instantiate를 사용하지만, 성능을 위해 오브젝트 풀링을 사용하는 것이 좋습니다.
            GameObject particleGO = Instantiate(particlePrefab, position, rotation);
            ParticleSystem ps = particleGO.GetComponent<ParticleSystem>();

            if (ps != null)
            {
                ps.Play();

                // 파티클 시스템이 재생을 마치면 자동으로 파괴되도록 코루틴 시작
                StartCoroutine(DestroyParticleWhenFinished(ps, particleGO));
            }
            else
            {
                Debug.LogWarning($"ParticleManager: The prefab '{particleType}' does not contain a ParticleSystem component.");
                Destroy(particleGO); // 파티클 시스템이 없으면 바로 파괴
            }
        }
        else
        {
            Debug.LogWarning($"ParticleManager: Particle type '{particleType}' not found. Please check your particleSystemInfos list.");
        }
    }

    /// <summary>
    /// 파티클 시스템이 재생을 마친 후 해당 게임 오브젝트를 파괴합니다.
    /// 오브젝트 풀링을 사용할 경우, 이 메서드를 통해 오브젝트를 풀로 반환하는 로직으로 대체됩니다.
    /// </summary>
    /// <param name="particleSystem">재생 중인 ParticleSystem 컴포넌트</param>
    /// <param name="particleGameObject">파괴할 게임 오브젝트</param>
    /// <returns></returns>
    private IEnumerator DestroyParticleWhenFinished(ParticleSystem particleSystem, GameObject particleGameObject)
    {
        // 파티클 시스템이 재생을 마치기를 기다립니다.
        // main.duration은 loop가 아닐 때의 재생 시간, startLifetime은 loop일 때의 예상 최대 시간
        // loop 파티클 시스템의 경우 IsAlive()를 사용하는 것이 더 정확할 수 있습니다.
        while (particleSystem.isPlaying)
        {
            yield return null;
        }

        // 파티클 시스템이 재생을 마쳤으면 게임 오브젝트를 파괴합니다.
        // 여기에 오브젝트 풀로 반환하는 로직을 구현할 수 있습니다.
        if (particleGameObject != null)
        {
            Destroy(particleGameObject);
        }
    }

    // 오버로드 메서드: Vector3만으로 파티클을 재생할 경우
    public void PlayParticle(string particleType, Vector3 position)
    {
        PlayParticle(particleType, position, Quaternion.identity);
    }
}