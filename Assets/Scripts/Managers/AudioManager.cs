using System.Collections.Generic;
using UnityEngine;

public class AudioManager : SingletonMonobehaviour<AudioManager>
{
    [Header(" Elements ")]
    [SerializeField] private AudioSource[] musics;
    [SerializeField] private AudioSource[] sounds;
    [SerializeField] private AudioSource[] footstepSounds;
    [SerializeField] private AudioSource[] moodSounds;

    private Dictionary<AudioGroup, AudioSource[]> audioGroups;

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

    // ------------------------------
    // Generic Methods
    // ------------------------------
    private bool IsValidIndex(int index, AudioSource[] array)
    {
        return array != null && index >= 0 && index < array.Length;
    }

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

    private void StopFromGroup(AudioGroup group, int index)
    {
        if (audioGroups.TryGetValue(group, out var sources) && IsValidIndex(index, sources))
            sources[index]?.Stop();
    }

    private void StopAllFromGroup(AudioGroup group)
    {
        if (!audioGroups.TryGetValue(group, out var sources)) return;

        foreach (var source in sources)
            source?.Stop();
    }


    // ======== Music ========
    public void PlayMusic(int index)
    {
        StopAllFromGroup(AudioGroup.Music);
        PlayFromGroup(AudioGroup.Music, index);
    }
    // ================================



    // ======== Interact Sound ========
    public void PlayInteractSound(int index)
    {
        StopSound(index);
        PlayFromGroup(AudioGroup.Sound, index, true);
    }

    public void StopSound(int index)
    {
        StopFromGroup(AudioGroup.Sound, index);
    }
    // ================================



    // ======== Footstep Sound ========
    public void PlayFootstep(int index)
    {
        PlayFromGroup(AudioGroup.Footstep, index);
    }

    public void StopFootstep(int index)
    {
        StopFromGroup(AudioGroup.Footstep, index);
    }
    // ================================



    // ======== Mood Sound ========
    public void PlayMoodSound(int index)
    {
        StopMoodSound(index);
        PlayFromGroup(AudioGroup.Mood, index, true);
    }

    public void StopMoodSound(int index)
    {
        StopFromGroup(AudioGroup.Mood, index);
    }
    // ================================
}
