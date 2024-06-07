using UnityEngine;

[System.Serializable]
public class ShapeCell
{
    public GameObject ShapeCellObject;
    public Vector2Int Position;
    public bool IsFilled;
    public GameStats.CellContentTypes CellContentType = GameStats.CellContentTypes.Empty;
    public CellEntity CellEntity;
}
