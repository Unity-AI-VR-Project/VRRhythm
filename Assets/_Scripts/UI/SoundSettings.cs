using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class SoundSettings : MonoBehaviour
{
    [SerializeField] private Slider MusicSlider;
    [SerializeField] private Slider SfxSlider;

    private void OnEnable()
    {
        SetSliderValue();
        MusicSlider.onValueChanged.AddListener(SetMusicVolume);
        SfxSlider.onValueChanged.AddListener(SetSfxVolume);
    }

    private void OnDisable()
    {
        MusicSlider.onValueChanged.RemoveListener(SetMusicVolume);
        SfxSlider.onValueChanged.RemoveListener(SetSfxVolume);
    }

    private void SetSliderValue()
    {
        AudioMixerGroup musicMixer = GameManager.Instance.soundManager.GetMusicMixerGroup();
        AudioMixerGroup sfxMixer = GameManager.Instance.soundManager.GetSFXMixerGroup();

        float musicVolume;
        if (musicMixer.audioMixer.GetFloat("MusicVolume", out musicVolume))
        {
            // 믹서 값이 -80이면 슬라이더를 0으로 설정
            if (musicVolume <= -79f) // 부동 소수점 비교를 위해 약간의 오차 범위 허용
            {
                MusicSlider.value = 0f;
            }
            else
            {
                MusicSlider.value = Mathf.InverseLerp(-40f, 0f, musicVolume);
            }
        }

        float sfxVolume;
        if (sfxMixer.audioMixer.GetFloat("SFXVolume", out sfxVolume))
        {
            // 믹서 값이 -80이면 슬라이더를 0으로 설정
            if (sfxVolume <= -79f) // 부동 소수점 비교를 위해 약간의 오차 범위 허용
            {
                SfxSlider.value = 0f;
            }
            else
            {
                SfxSlider.value = Mathf.InverseLerp(-40f, 0f, sfxVolume);
            }
        }
    }

    private void SetMusicVolume(float sliderValue)
    {
        AudioMixerGroup musicMixer = GameManager.Instance.soundManager.GetMusicMixerGroup();
        float mixerVolume;
        if (sliderValue == 0f)
        {
            mixerVolume = -80f; // 슬라이더 값이 0이면 믹서 볼륨을 -80으로 설정
        }
        else
        {
            mixerVolume = Mathf.Lerp(-40f, 0f, sliderValue); // 그 외에는 -40f ~ 0f 사이로 보간
        }
        musicMixer.audioMixer.SetFloat("MusicVolume", mixerVolume);
    }

    private void SetSfxVolume(float sliderValue)
    {
        AudioMixerGroup sfxMixer = GameManager.Instance.soundManager.GetSFXMixerGroup();
        float mixerVolume;
        if (sliderValue == 0f)
        {
            mixerVolume = -80f; // 슬라이더 값이 0이면 믹서 볼륨을 -80으로 설정
        }
        else
        {
            mixerVolume = Mathf.Lerp(-40f, 0f, sliderValue); // 그 외에는 -40f ~ 0f 사이로 보간
        }
        sfxMixer.audioMixer.SetFloat("SFXVolume", mixerVolume);
    }
}