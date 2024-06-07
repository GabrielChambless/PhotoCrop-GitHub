using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MenuController : MonoBehaviour
{
    public static MenuController Instance { get; private set; }

    [SerializeField] private GameObject mainMenuObject;
    [SerializeField] private GameObject levelPauseMenuObject;
    [SerializeField] private GameObject levelFinishMenuObject;

    // Settings Menu
    public SettingsData SettingsData => settingsData;
    [SerializeField] private SettingsData settingsData;
    [SerializeField] private GameObject settingsMenuObject;
    [SerializeField] private AudioSettingsManager audioSettingsManager;

    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider voiceVolumeSlider;
    [SerializeField] private Button applyButton;


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

        applyButton.onClick.AddListener(ApplySettings);

        masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);
        musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        voiceVolumeSlider.onValueChanged.AddListener(SetVoiceVolume);
    }

    private void Start()
    {
        PopulateResolutionDropdown();
        PopulateQualityDropdown();
        LoadCurrentSettings();
        settingsData.ApplyQuality(qualityDropdown.value);
        settingsData.ApplyResolution(resolutionDropdown.value);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && SceneStateManager.Instance.CurrentSceneState == SceneStateManager.SceneStates.LevelSelector
            && (GameStateManager.Instance.CurrentGameState == GameStateManager.GameStates.Settings || GameStateManager.Instance.CurrentGameState == GameStateManager.GameStates.WorldSelector))
        {
            GameStateManager.Instance.CurrentGameState = GameStateManager.GameStates.MainMenu;
            ToggleMainMenu();
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && SceneStateManager.Instance.CurrentSceneState == SceneStateManager.SceneStates.Level && !LevelController.Instance.LevelIsCompleted
            && GameStateManager.Instance.CurrentGameState != GameStateManager.GameStates.LevelFinish && GameStateManager.Instance.CurrentGameState != GameStateManager.GameStates.LevelFinishMenu)
        {
            ToggleLevelPauseMenu();
        }
        else if (Input.GetKeyDown(KeyCode.Escape) && SceneStateManager.Instance.CurrentSceneState == SceneStateManager.SceneStates.Level && LevelController.Instance.LevelIsCompleted
            && GameStateManager.Instance.CurrentGameState != GameStateManager.GameStates.Level && GameStateManager.Instance.CurrentGameState != GameStateManager.GameStates.LevelPauseMenu)
        {
            ToggleLevelFinishMenu();
        }
    }

    public void GoToLevelSelector()
    {
        if (SceneStateManager.Instance.CurrentSceneState != SceneStateManager.SceneStates.LevelSelector)
        {
            SceneStateManager.Instance.CurrentSceneState = SceneStateManager.SceneStates.LevelSelector;

            mainMenuObject.SetActive(false);
            levelPauseMenuObject.SetActive(false);
            levelFinishMenuObject.SetActive(false);

            StartCoroutine(SceneStateManager.Instance.LoadSceneAsync(nameof(SceneStateManager.SceneStates.LevelSelector), false, () => LevelSelectorController.Instance.InstantiateWorldController()));
            return;
        }

        // Already Main Menu
        ToggleMainMenu();
        GameStateManager.Instance.CurrentGameState = GameStateManager.GameStates.WorldSelector;
        LevelSelectorController.Instance.InstantiateWorldController();
    }

    public void GoToSettings()
    {
        GameStateManager.Instance.CurrentGameState = GameStateManager.GameStates.Settings;

        mainMenuObject.SetActive(false);
        levelPauseMenuObject.SetActive(false);
        levelFinishMenuObject.SetActive(false);

        settingsMenuObject.SetActive(!settingsMenuObject.activeSelf);
    }

    public void QuitApplication()
    {
        Application.Quit();
    }

    public void ToggleMainMenu()
    {
        settingsMenuObject.SetActive(false);
        levelPauseMenuObject.SetActive(false);
        levelFinishMenuObject.SetActive(false);

        mainMenuObject.SetActive(!mainMenuObject.activeSelf);
    }

    // Level Pause Menu
    public void ToggleLevelPauseMenu()
    {
        if (!levelPauseMenuObject.activeSelf)
        {
            GameStateManager.Instance.CurrentGameState = GameStateManager.GameStates.LevelPauseMenu;
        }
        else
        {
            GameStateManager.Instance.CurrentGameState = GameStateManager.GameStates.Level;
        }

        mainMenuObject.SetActive(false);
        settingsMenuObject.SetActive(false);
        levelFinishMenuObject.SetActive(false);

        levelPauseMenuObject.SetActive(!levelPauseMenuObject.activeSelf);
    }

    public void RestartLevel()
    {
        if (LevelController.Instance == null || LevelController.Instance.IsSettingUpLevel)
        {
            return;
        }

        mainMenuObject.SetActive(false);
        settingsMenuObject.SetActive(false);
        levelPauseMenuObject.SetActive(false);
        levelFinishMenuObject.SetActive(false);

        LevelController.Instance.RestartLevel();
    }

    // Level Finish
    public void ToggleLevelFinishMenu()
    {
        if (!levelFinishMenuObject.activeSelf)
        {
            GameStateManager.Instance.CurrentGameState = GameStateManager.GameStates.LevelFinishMenu;
        }
        else
        {
            GameStateManager.Instance.CurrentGameState = GameStateManager.GameStates.LevelFinish;
        }

        mainMenuObject.SetActive(false);
        settingsMenuObject.SetActive(false);
        levelPauseMenuObject.SetActive(false);

        levelFinishMenuObject.SetActive(!levelFinishMenuObject.activeSelf);
    }

    public void GoToNextLevel()
    {
        LevelData nextLevelData = LevelDataLibrary.Instance.GetLevelDataByWorldAndLevelNumber(LevelSelectorController.Instance.SelectedLevelData.World, LevelSelectorController.Instance.SelectedLevelData.WorldLevelNumber + 1);

        if (nextLevelData != null)
        {
            LevelSelectorController.Instance.SetLevelData(nextLevelData);
            LevelSelectorController.Instance.SetHoleData(nextLevelData.HoleData);
            LevelSelectorController.Instance.SetShapeData(nextLevelData.ShapeDataList);

            mainMenuObject.SetActive(false);
            levelPauseMenuObject.SetActive(false);
            levelFinishMenuObject.SetActive(false);

            LevelController.Instance.RestartLevel();
            return;
        }

        // Last available level in the current world was completed
        GoToLevelSelector();
    }

    // Settings Menu
    public void SetMasterVolume(float volume)
    {
        settingsData.MasterVolume = LinearToDecibel(volume);
        audioSettingsManager.SetMasterVolume(volume);
    }

    public void SetSFXVolume(float volume)
    {
        settingsData.SfxVolume = LinearToDecibel(volume);
        audioSettingsManager.SetSFXVolume(volume);
    }

    public void SetMusicVolume(float volume)
    {
        settingsData.MusicVolume = LinearToDecibel(volume);
        audioSettingsManager.SetMusicVolume(volume);
    }

    public void SetVoiceVolume(float volume)
    {
        settingsData.VoiceVolume = LinearToDecibel(volume);
        audioSettingsManager.SetVoiceVolume(volume);
    }

    public void ApplySettings()
    {
        settingsData.ApplyQuality(qualityDropdown.value);
        settingsData.ApplyResolution(resolutionDropdown.value);

        GameDataManager.Instance.SaveGameData();
    }


    // Internal Methods
    private void LoadCurrentSettings()
    {
        masterVolumeSlider.value = DecibelToLinear(settingsData.MasterVolume);
        sfxVolumeSlider.value = DecibelToLinear(settingsData.SfxVolume);
        musicVolumeSlider.value = DecibelToLinear(settingsData.MusicVolume);
        voiceVolumeSlider.value = DecibelToLinear(settingsData.VoiceVolume);

        resolutionDropdown.value = settingsData.ResolutionIndex;
        qualityDropdown.value = settingsData.QualityLevel;
    }

    private void PopulateResolutionDropdown()
    {
        resolutionDropdown.ClearOptions();

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

        foreach (ResolutionPresetData preset in settingsData.ResolutionPresets)
        {
            options.Add(new TMP_Dropdown.OptionData(preset.PresetName));
        }

        resolutionDropdown.AddOptions(options);
    }

    private void PopulateQualityDropdown()
    {
        qualityDropdown.ClearOptions();

        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();

        for (int i = 0; i < QualitySettings.names.Length; i++)
        {
            options.Add(new TMP_Dropdown.OptionData(QualitySettings.names[i]));
        }

        qualityDropdown.AddOptions(options);
    }

    private float LinearToDecibel(float linear)
    {
        return Mathf.Lerp(-80f, 0f, linear);
    }

    private float DecibelToLinear(float db)
    {
        return Mathf.InverseLerp(-80f, 0f, db);
    }
}
