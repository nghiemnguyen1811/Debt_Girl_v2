using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : SingletonMonobehaviour<SettingsManager>
{
    [Header(" UI ")]
    public Slider volumeVolSlider;
    public Slider musicVolSlider;
    public Slider soundVolSlider;

    void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        volumeVolSlider.value = AudioManager.Instance.GetVolumeVol();
        musicVolSlider.value = AudioManager.Instance.GetMusicVol();
        soundVolSlider.value = AudioManager.Instance.GetSoundVol();
    }

    public void SetVolumeSlider()
    {
        AudioManager.Instance.SetVolumeSlider();
    }

    public void SetMusicSlider()
    {
        AudioManager.Instance.SetMusicSlider();
    }

    public void SetSoundSlider()
    {
        AudioManager.Instance.SetSoundSlider();
    }
}
