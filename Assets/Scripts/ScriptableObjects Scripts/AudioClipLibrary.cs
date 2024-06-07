using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New AudioClipLibrary", menuName = "AudioClipLibrary")]
[System.Serializable]
public class AudioClipLibrary : ScriptableObject
{
    public enum AudioClipNames
    {
        BackgroundMusic,
        PlaceShape
    }

    [System.Serializable]
    public struct AudioClipData
    {
        public AudioClipNames Name;
        public AudioClip Clip;
    }

    [SerializeField] private List<AudioClipData> audioClips = new List<AudioClipData>();

    public List<AudioClipData> AudioClips => audioClips;
}
