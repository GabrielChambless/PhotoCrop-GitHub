using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New HoleData", menuName = "HoleData")]
[System.Serializable]
public class HoleData : ScriptableObject
{
    [SerializeField] private Vector2Int gridSize;
    [SerializeField] private List<HoleCell> holeLayout = new List<HoleCell>();
    [SerializeField] private List<CellEntityData> cellEntityLayout = new List<CellEntityData>();


    public Vector2Int GridSize => gridSize;
    public List<HoleCell> HoleLayout => holeLayout;
    public List<CellEntityData> CellEntityLayout => cellEntityLayout;
}