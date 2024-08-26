using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New LevelDataLibrary", menuName = "Level/LevelDataLibrary")]
[System.Serializable]
public class LevelDataLibrary : ScriptableObject
{
    public static LevelDataLibrary Instance { get; set; }

    [SerializeField] private List<LevelData> BricksLevelData = new List<LevelData>();
    [SerializeField] private List<LevelData> ChessLevelData = new List<LevelData>();

    public List<LevelGoal> GetCompletedLevelGoals(LevelData levelData)
    {
        List<LevelGoal> completedGoals = new List<LevelGoal>();

        foreach (LevelGoal goal in levelData.LevelGoals)
        {
            if (goal.IsCompleted)
            {
                completedGoals.Add(goal);
            }
        }

        return completedGoals;
    }

    public LevelData GetLevelDataByWorldAndLevelNumber(LevelData.WorldType world, int worldLevelNumber)
    {
        List<LevelData> worldLevelData = new List<LevelData>();

        switch (world)
        {
            case LevelData.WorldType.Bricks:
                worldLevelData = BricksLevelData;
                break;
            case LevelData.WorldType.Chess:
                worldLevelData = ChessLevelData;
                break;
        }

        foreach (LevelData levelData in worldLevelData)
        {
            if (levelData.WorldLevelNumber == worldLevelNumber)
            {
                return levelData;
            }
        }

        Debug.LogWarning($"Couldn't find any LevelData matching the WorldType: '{world}', LevelNumber: '{worldLevelNumber}'");
        return null;
    }
}