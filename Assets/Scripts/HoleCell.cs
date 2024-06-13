using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HoleCell
{
    public GameObject HoleCellObject;
    public Vector2Int Position;
    public bool IsFilled;
    public GameStats.CellContentTypes CellContentType;
    public List<CellEntity> CellEntities = new List<CellEntity>();
}
