using System;
using System.Linq;
using UnityEngine;
public class AudioManager : Singleton<AudioManager>
{
    [SerializeField] private AudioGroup[] audioGroups;

    private void Start()
    {
        foreach (AudioGroup audioGroup in audioGroups)
        {
            audioGroup.source = gameObject.AddComponent<AudioSource>();
        }
    }

    public void PlayAudioGroup(string name, bool restartIfPlaying = true)
    {
        AudioGroup group = Array.Find(audioGroups, group => group.name == name);
        if (group == null)
        {
            Debug.LogWarning($"group {name} not found");
            return;
        }
        if (restartIfPlaying && group.source.isPlaying) return;

        Sound targetSound = group.sounds[UnityEngine.Random.Range(0, group.sounds.Length)];
        group.source.loop = targetSound.loop;
        group.source.clip = targetSound.clip;
        group.source.volume = targetSound.volume;
        group.source.Play();
    }
    public void StopAudioGroup(string name)
    {
        AudioGroup group = Array.Find(audioGroups, group => group.name == name);
        if (group == null)
        {
            Debug.LogWarning($"group {name} not found");
            return;
        }
        group.source.Stop();
    }

    public void StopAllButPlay(string[] names)
    {
        AudioGroup[] stopGroups = audioGroups;
        foreach (string name in names)
        {
            stopGroups = stopGroups.Where(group => group.name != name).ToArray();
            PlayAudioGroup(name, restartIfPlaying: false);
        }
        foreach (AudioGroup group in stopGroups)
        {
            group.source.Stop();
        }
    }
}

[Serializable]
class AudioGroup
{
    public string name;
    [HideInInspector] public AudioSource source;

    public Sound[] sounds;
}

[Serializable]
class Sound
{
    public AudioClip clip;
    public bool loop;
    [Range(0f, 1f)] public float volume;
}