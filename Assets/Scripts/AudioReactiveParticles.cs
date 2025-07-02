using UnityEngine;

[RequireComponent(typeof(ParticleSystem))]
public class AudioReactiveParticles : MonoBehaviour
{
    public AudioAnalyzer analyzer;
    private ParticleSystem ps;
    private ParticleSystem.MainModule mainModule;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        mainModule = ps.main;
    }

    void Update()
    {
        float bass = analyzer.spectrumData[1] * 100f; // 저음 대역
        float mid = analyzer.spectrumData[15] * 100f; // 중음 대역

        mainModule.startSize = Mathf.Clamp(bass + mid, 0.1f, 5f);
        mainModule.startSpeed = Mathf.Clamp(bass * 10f, 1f, 10f);
    }
}
