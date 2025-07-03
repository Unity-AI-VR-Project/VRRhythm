using UnityEngine;
using UnityEngine.Audio;

public class VolumManager : MonoBehaviour
{
    public AudioMixer mixer;
    public string parameterName = "BGMVolume";

    // 슬라이더에서 호출할 함수
    public void SetVolumeFromSlider(float sliderValue)
    {
        float clamped = Mathf.Clamp(sliderValue, 0.0001f, 1f); // 0 방지
        float dB = Mathf.Log10(clamped) * 20f;
        mixer.SetFloat(parameterName, dB);
    }
}