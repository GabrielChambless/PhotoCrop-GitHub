using System;
using System.Collections.Generic;

[System.Serializable]
public class GameData
{
    // Settings Data
    public float SettingsData_MasterVolume = 1f;
    public float SettingsData_SfxVolume = 1f;
    public float SettingsData_MusicVolume = 1f;
    public float SettingsData_VoiceVolume = 1f;
    public int SettingsData_QualityLevel = 1;
    public int SettingsData_ResolutionIndex = 0;

    // Level Progress Data
    public List<LevelProgressData> AllLevelProgressData = new List<LevelProgressData>();
}
