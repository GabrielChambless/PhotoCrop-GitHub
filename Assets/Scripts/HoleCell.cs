using UnityEngine;

[System.Serializable]
public class HoleCell
{
    public GameObject HoleCellObject;
    public Vector2Int Position;
    public bool IsFilled;
    public GameStats.CellContentTypes CellContentType;
    public CellEntity CellEntity;
}
