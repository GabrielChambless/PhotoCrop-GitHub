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
    [SerializeField] private GameObject wallCellPrefab;
    [SerializeField] private GameObject redCellPrefab;
    [SerializeField] private GameObject greenCellPrefab;
    [SerializeField] private GameObject blueCellPrefab;
    [SerializeField] private GameObject yellowCellPrefab;
    [SerializeField] private GameObject whiteCellPrefab;
    [SerializeField] private GameObject blackCellPrefab;
    [SerializeField] private List<LevelGoal> levelGoals = new List<LevelGoal>();

    public WorldType World => world;
    public int WorldLevelNumber => worldLevelNumber;
    public GameObject LevelBoardObject => levelBoardObject;
    public HoleData HoleData => holeData;
    public List<ShapeData> ShapeDataList => shapeDataList;
    public List<LevelGoal> LevelGoals => levelGoals;

    public GameObject CellObjectPrefab(GameStats.CellContentTypes cellContentType)
    {
        switch (cellContentType)
        {
            case GameStats.CellContentTypes.RedCell:
                return redCellPrefab;
            case GameStats.CellContentTypes.GreenCell:
                return greenCellPrefab;
            case GameStats.CellContentTypes.BlueCell:
                return blueCellPrefab;
            case GameStats.CellContentTypes.YellowCell:
                return yellowCellPrefab;
            case GameStats.CellContentTypes.WhiteCell:
                return whiteCellPrefab;
            case GameStats.CellContentTypes.BlackCell:
                return blackCellPrefab;
            default:
                return wallCellPrefab;
        }
    }
}