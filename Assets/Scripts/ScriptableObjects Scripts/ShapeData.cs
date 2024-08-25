using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New ShapeData", menuName = "ShapeData")]
[System.Serializable]
public class ShapeData : ScriptableObject
{
    [SerializeField] private Vector2Int dimensions = new Vector2Int(3, 3);
    [SerializeField] private List<ShapeCell> shapeLayout = new List<ShapeCell>();
    [SerializeField] private List<CellEntityData> cellEntityLayout = new List<CellEntityData>();

    public Vector2Int Dimensions
    {
        get => dimensions;
        private set
        {
            dimensions = EnsureOddDimensions(value);
        }
    }

    public List<ShapeCell> ShapeLayout => shapeLayout;
    public List<CellEntityData> CellEntityLayout => cellEntityLayout;

    private void OnValidate()
    {
        Dimensions = dimensions;  // This will enforce odd dimensions during editor changes
    }

    private Vector2Int EnsureOddDimensions(Vector2Int dimensions)
    {
        if (dimensions.x % 2 == 0)
        {
            dimensions.x += 1;
        }
        if (dimensions.y % 2 == 0)
        {
            dimensions.y += 1;
        }

        if (dimensions.x > 5)
        {
            dimensions.x = 5;
        }
        if (dimensions.y > 5)
        {
            dimensions.y = 5;
        }

        if (dimensions.x != dimensions.y)
        {
            dimensions.y = dimensions.x;
        }

        return dimensions;
    }
}