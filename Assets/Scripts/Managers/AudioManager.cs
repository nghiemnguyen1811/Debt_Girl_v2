using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// Central audio manager that organizes and plays different categories of audio: music, sound effects, footsteps, and mood sounds.
/// </summary>
public class AudioManager : SingletonMonobehaviour<AudioManager>
{
    #region === Serialized Fields ===

    [Header("Audio Sources")]
    [SerializeField] private AudioSource[] musics;
    [SerializeField] private AudioSource[] sounds;
    [SerializeField] private AudioSource[] footstepSounds;
    [SerializeField] private AudioSource[] moodSounds;

    [Header("Audio Mixers")]
    [SerializeField] private AudioMixerGroup volumeMixer;
    [SerializeField] private AudioMixerGroup musicMixer;
    [SerializeField] private AudioMixerGroup soundMixer;

    #endregion

    #region === Runtime Fields ===

    private Dictionary<AudioGroup, AudioSource[]> audioGroups;
    private float volumeVol;
    private float musicVol;
    private float soundVol;

    #endregion

    #region === Unity Events ===

    // Populate dictionary mapping enum to AudioSource arrays
    private void Start()
    {
        audioGroups = new Dictionary<AudioGroup, AudioSource[]>
        {
            { AudioGroup.Music, musics },
            { AudioGroup.Sound, sounds },
            { AudioGroup.Footstep, footstepSounds },
            { AudioGroup.Mood, moodSounds }
        };
    }

    #endregion

    #region === Internal Utilities ===

    // Check if the index is valid for the specified array
    private bool IsValidIndex(int index, AudioSource[] array)
    {
        return array != null && index >= 0 && index < array.Length;
    }

    // Play a sound from a group with optional stop before playing
    private void PlayFromGroup(AudioGroup group, int index, bool stopFirst = false)
    {
        if (!audioGroups.TryGetValue(group, out var sources))
        {
            Debug.LogWarning($"[AudioManager] Group {group} not found.");
            return;
        }

        if (!IsValidIndex(index, sources))
        {
            Debug.LogWarning($"[AudioManager] Invalid index {index} in group {group}");
            return;
        }

        if (stopFirst)
            sources[index]?.Stop();

        sources[index]?.Play();
    }

    // Stop a specific source in a group
    private void StopFromGroup(AudioGroup group, int index)
    {
        if (audioGroups.TryGetValue(group, out var sources) && IsValidIndex(index, sources))
            sources[index]?.Stop();
    }

    // Stop all sounds in a group
    private void StopAllFromGroup(AudioGroup group)
    {
        if (!audioGroups.TryGetValue(group, out var sources)) return;

        foreach (var source in sources)
        {
            if (source == null) continue;
            source?.Stop();
        }
    }

    #endregion

    #region === Music Control ===

    /// <summary>
    /// Stops all current music
    /// </summary>
    public void StopMusic()
    {
        StopAllFromGroup(AudioGroup.Music);
    }

    /// <summary>
    /// Stops all current music and plays one by index.
    /// </summary>
    public void PlayMusic(int index)
    {
        StopAllFromGroup(AudioGroup.Music);
        PlayFromGroup(AudioGroup.Music, index);
    }

    #endregion

    #region === Interaction Sound Control ===

    /// <summary>
    /// Play a UI or interaction sound.
    /// </summary>
    public void PlayInteractSound(int index)
    {
        StopSound(index);
        PlayFromGroup(AudioGroup.Sound, index, true);
    }

    /// <summary>
    /// Stop a sound effect by index.
    /// </summary>
    public void StopSound(int index)
    {
        StopFromGroup(AudioGroup.Sound, index);
    }

    #endregion

    #region === Footstep Sound Control ===

    /// <summary>
    /// Play a footstep sound without interrupting others.
    /// </summary>
    public void PlayFootstep(int index)
    {
        PlayFromGroup(AudioGroup.Footstep, index);
    }

    /// <summary>
    /// Stop a footstep sound.
    /// </summary>
    public void StopFootstep(int index)
    {
        StopFromGroup(AudioGroup.Footstep, index);
    }

    #endregion

    #region === Mood Sound Control ===

    /// <summary>
    /// Play a mood-related ambient or character sound.
    /// </summary>
    public void PlayMoodSound(int index)
    {
        StopMoodSound(index);
        PlayFromGroup(AudioGroup.Mood, index, true);
    }

    /// <summary>
    /// Stop a mood sound.
    /// </summary>
    public void StopMoodSound(int index)
    {
        StopFromGroup(AudioGroup.Mood, index);
    }

    #endregion

    #region === Volume Control ===

    public void SetVolumeSlider()
    {
        volumeMixer.audioMixer.SetFloat("VolumeVol", SettingsManager.Instance.volumeVolSlider.value);
        volumeVol = SettingsManager.Instance.volumeVolSlider.value;
    }

    public void SetMusicSlider()
    {
        musicMixer.audioMixer.SetFloat("MusicVol", SettingsManager.Instance.musicVolSlider.value);
        musicVol = SettingsManager.Instance.musicVolSlider.value;
    }

    public void SetSoundSlider()
    {
        soundMixer.audioMixer.SetFloat("SoundVol", SettingsManager.Instance.soundVolSlider.value);
        soundVol = SettingsManager.Instance.soundVolSlider.value;
    }

    public float GetVolumeVol()
    {
        return volumeVol;
    }

    public float GetMusicVol()
    {
        return musicVol;
    }

    public float GetSoundVol()
    {
        return soundVol;
    }

    #endregion
}
