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
            MusicSlider.value = Mathf.InverseLerp(-80f, 0f, musicVolume);
        }

        float sfxVolume;
        if (sfxMixer.audioMixer.GetFloat("SFXVolume", out sfxVolume))
        {
            SfxSlider.value = Mathf.InverseLerp(-80f, 0f, sfxVolume);
        }
    }

    private void SetMusicVolume(float sliderValue)
    {
        AudioMixerGroup musicMixer = GameManager.Instance.soundManager.GetMusicMixerGroup();
        float mixerVolume = Mathf.Lerp(-80f, 0f, sliderValue);
        musicMixer.audioMixer.SetFloat("MusicVolume", mixerVolume);
    }

    private void SetSfxVolume(float sliderValue)
    {
        AudioMixerGroup sfxMixer = GameManager.Instance.soundManager.GetSFXMixerGroup();
        float mixerVolume = Mathf.Lerp(-80f, 0f, sliderValue);
        sfxMixer.audioMixer.SetFloat("SFXVolume", mixerVolume);
    }
}
