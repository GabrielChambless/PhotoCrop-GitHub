using UnityEngine;

[CreateAssetMenu(fileName = "New SettingsData", menuName = "Settings/SettingsData")]
[System.Serializable]
public class SettingsData : ScriptableObject
{
    [Header("Resolution Settings")]
    public ResolutionPresetData[] ResolutionPresets;

    [Header("Audio Settings")]
    public float MasterVolume = 1f;
    public float SfxVolume = 1f;
    public float MusicVolume = 1f;
    public float VoiceVolume = 1f;

    [Header("Graphics Settings")]
    public int QualityLevel = 2;
    public int ResolutionIndex = 0;

    public void ApplyResolution(int index)
    {
        if (index < 0 || index >= ResolutionPresets.Length)
        {
            Debug.LogWarning("Invalid resolution preset index.");
            return;
        }

        ResolutionPresetData preset = ResolutionPresets[index];
        Screen.SetResolution(preset.Width, preset.Height, preset.FullScreenMode);
        ResolutionIndex = index;

        if (CameraController.Instance != null && GameStats.UsePixelation)
        {
            CameraController.Instance.SetPixelationTexture(preset.PixelationRenderTexture);
            GameStats.PixelationResolution = new Vector2Int(preset.PixelationRenderTexture.width, preset.PixelationRenderTexture.height);
        }
    }

    public void ApplyVolume(AudioSettingsManager audioSettingsManager)
    {
        audioSettingsManager.SetMasterVolume(MasterVolume);
        audioSettingsManager.SetSFXVolume(SfxVolume);
        audioSettingsManager.SetMusicVolume(MusicVolume);
        audioSettingsManager.SetVoiceVolume(VoiceVolume);
    }

    public void ApplyQuality(int index)
    {
        QualityLevel = index;
        QualitySettings.SetQualityLevel(QualityLevel);
    }
}