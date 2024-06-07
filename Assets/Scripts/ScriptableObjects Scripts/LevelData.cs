using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New LevelData", menuName = "Level/LevelData")]
[System.Serializable]
public class LevelData : ScriptableObject
{
    public enum WorldType
    {
        FundamentalShapes,
        Chess
    }

    [SerializeField] private WorldType world;
    [SerializeField] private int worldLevelNumber;
    [SerializeField] private GameObject levelBoardObject;
    [SerializeField] private HoleData holeData;
    [SerializeField] private List<ShapeData> shapeDataList = new List<ShapeData>();
    [SerializeField] private List<LevelGoal> levelGoals = new List<LevelGoal>();

    public WorldType World => world;
    public int WorldLevelNumber => worldLevelNumber;
    public GameObject LevelBoardObject => levelBoardObject;
    public HoleData HoleData => holeData; 
    public List<ShapeData> ShapeDataList => shapeDataList;
    public List<LevelGoal> LevelGoals => levelGoals;
}