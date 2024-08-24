using System.Collections.Generic;
using UnityEngine;

public static class GameStats
{
    public enum CellContentTypes
    {
        Empty,
        Wall,
        RedCell,
        GreenCell,
        BlueCell,
        YellowCell,
        WhiteCell,
        BlackCell
    }

    public enum DirectionTypes
    {
        Up,
        Down,
        Right,
        Left,
        UpRight,
        UpLeft,
        DownRight,
        DownLeft
    }


    // Make a method to debug to the console when the GameData JSON is saved and loaded

    public static bool DebugMode;

    public static bool UsePixelation = false;
    public static Vector2Int PixelationResolution = new Vector2Int(512, 288);

    public static List<LevelData> LevelsFinishedThisSession = new List<LevelData>();

    public static bool World1Unlocked = true;
    public static bool World2Unlocked = true;


    public static GameData GetDataToSave()
    {
        GameData gameData = new GameData();

        // Settings Data
        gameData.SettingsData_MasterVolume = MenuController.Instance.SettingsData.MasterVolume;
        gameData.SettingsData_SfxVolume = MenuController.Instance.SettingsData.SfxVolume;
        gameData.SettingsData_MusicVolume = MenuController.Instance.SettingsData.MusicVolume;
        gameData.SettingsData_VoiceVolume = MenuController.Instance.SettingsData.VoiceVolume;
        gameData.SettingsData_QualityLevel = MenuController.Instance.SettingsData.QualityLevel;
        gameData.SettingsData_ResolutionIndex = MenuController.Instance.SettingsData.ResolutionIndex;

        // Level Progress Data
        foreach (LevelData levelData in LevelsFinishedThisSession)
        {
            List<LevelGoal.GoalTypes> goalsCompleted = new List<LevelGoal.GoalTypes>();

            foreach (LevelGoal goal in levelData.LevelGoals)
            {
                if (goal.IsCompleted)
                {
                    goalsCompleted.Add(goal.GoalType);
                }
            }

            LevelProgressData levelProgressData = new LevelProgressData(levelData.World, levelData.WorldLevelNumber, goalsCompleted);
            gameData.AllLevelProgressData.Add(levelProgressData);
        }

        LevelsFinishedThisSession.Clear();

        return gameData;
    }

    public static void ApplyLoadedGameData(GameData gameData)
    {
        // Settings Data
        MenuController.Instance.SettingsData.MasterVolume = gameData.SettingsData_MasterVolume;
        MenuController.Instance.SettingsData.SfxVolume = gameData.SettingsData_SfxVolume;
        MenuController.Instance.SettingsData.MusicVolume = gameData.SettingsData_MusicVolume;
        MenuController.Instance.SettingsData.VoiceVolume = gameData.SettingsData_VoiceVolume;
        MenuController.Instance.SettingsData.QualityLevel = gameData.SettingsData_QualityLevel;
        MenuController.Instance.SettingsData.ResolutionIndex = gameData.SettingsData_ResolutionIndex;

        // Level Progress Data
        foreach (LevelProgressData levelProgressData in gameData.AllLevelProgressData)
        {
            LevelData levelData = LevelDataLibrary.Instance.GetLevelDataByWorldAndLevelNumber(levelProgressData.World, levelProgressData.WorldLevelNumber);

            if (levelData != null)
            {
                foreach (LevelGoal goal in levelData.LevelGoals)
                {
                    goal.IsCompleted = levelProgressData.GoalsCompleted.Contains(goal.GoalType);
                }
            }
        }
    }
}