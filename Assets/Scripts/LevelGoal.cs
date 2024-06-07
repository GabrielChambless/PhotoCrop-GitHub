using UnityEngine;

[System.Serializable]
public class LevelGoal
{
    public enum GoalTypes
    {
        FillEntireGrid,
        WithinShapeLimit,
        WithinCropLimit
    }

    [SerializeField] private GoalTypes goalType;
    [SerializeField] private string description;
    [SerializeField] private bool isCompleted;

    [SerializeField] private int shapeLimit;
    [SerializeField] private int cropLimit;

    public GoalTypes GoalType => goalType;
    public string Description => description;
    public bool IsCompleted
    {
        get => isCompleted;
        set => isCompleted = value;
    }

    public bool CheckIfWithinShapeLimit(int numberOfShapesUsed)
    {
        if (goalType == GoalTypes.WithinShapeLimit)
        {
            isCompleted = numberOfShapesUsed <= shapeLimit;
        }

        return isCompleted;
    }

    public bool CheckIfWithinCropLimit(int numberOfCropsUsed)
    {
        if (goalType == GoalTypes.WithinCropLimit)
        {
            isCompleted = numberOfCropsUsed <= cropLimit;
        }

        return isCompleted;
    }
}
