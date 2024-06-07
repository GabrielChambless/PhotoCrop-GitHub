using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource voiceSource;
    [SerializeField] private AudioClipLibrary audioClipLibrary;

    private Dictionary<AudioClipLibrary.AudioClipNames, AudioClip> audioClipDictionary
        = new Dictionary<AudioClipLibrary.AudioClipNames, AudioClip>();

    private AudioClipLibrary.AudioClipNames currentAudioClipName;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        foreach (AudioClipLibrary.AudioClipData entry in audioClipLibrary.AudioClips)
        {
            if (!audioClipDictionary.ContainsKey(entry.Name))
            {
                audioClipDictionary.Add(entry.Name, entry.Clip);
            }
        }

        PlayMusic(AudioClipLibrary.AudioClipNames.BackgroundMusic, true);
    }

    public void PlayMusic(AudioClipLibrary.AudioClipNames clipName, bool shouldLoop)
    {
        if (audioClipDictionary.TryGetValue(clipName, out AudioClip clip))
        {
            if (musicSource.isPlaying && currentAudioClipName == clipName)
            {
                return;
            }

            if (musicSource.isPlaying)
            {
                musicSource.Stop();
            }

            currentAudioClipName = clipName;
            musicSource.clip = clip;
            musicSource.loop = shouldLoop;
            musicSource.Play();
            return;
        }

        Debug.LogWarning($"Music clip '{clipName}' not found!");
    }

    public void StopMusic()
    {
        musicSource.Stop();
    }

    public void PlaySFX(AudioClipLibrary.AudioClipNames clipName)
    {
        if (audioClipDictionary.TryGetValue(clipName, out AudioClip clip))
        {
            sfxSource.PlayOneShot(clip);
            return;
        }

        Debug.LogWarning($"SFX clip '{clipName}' not found!");
    }
}
