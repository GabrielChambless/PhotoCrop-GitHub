using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New AudioClipLibrary", menuName = "AudioClipLibrary")]
[System.Serializable]
public class AudioClipLibrary : ScriptableObject
{
    public enum AudioClipNames
    {
        TitleMusic,
        DefaultShapePlace,
        BrickClick1,
        BrickClick2,
        BrickClick3
    }

    public enum AudioTypes
    {
        Music,
        SFX,
        Voice
    }

    [System.Serializable]
    public struct AudioClipData
    {
        public AudioTypes AudioType;
        public AudioClipNames Name;
        public AudioClip Clip;
    }

    [SerializeField] private List<AudioClipData> audioClips = new List<AudioClipData>();

    public List<AudioClipData> AudioClips => audioClips;
}
