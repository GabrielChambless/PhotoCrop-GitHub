using UnityEngine;

[CreateAssetMenu(fileName = "New ResolutionPresetData", menuName = "Settings/ResolutionPresetData")]
public class ResolutionPresetData : ScriptableObject
{
    [SerializeField] private string presetName;
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private FullScreenMode fullScreenMode;
    [SerializeField] private RenderTexture pixelationRenderTexture;

    public string PresetName => presetName;
    public int Width => width;
    public int Height => height;
    public FullScreenMode FullScreenMode => fullScreenMode;
    public RenderTexture PixelationRenderTexture => pixelationRenderTexture;
}