using System.Collections.Generic;

[System.Serializable]
public class LevelProgressData
{
    public LevelData.WorldType World;
    public int WorldLevelNumber;
    public List<LevelGoal.GoalTypes> GoalsCompleted;

    public LevelProgressData(LevelData.WorldType world, int worldLevelNumber, List<LevelGoal.GoalTypes> goalsCompleted)
    {
        World = world;
        WorldLevelNumber = worldLevelNumber;
        GoalsCompleted = new List<LevelGoal.GoalTypes>(goalsCompleted);
    }
}
