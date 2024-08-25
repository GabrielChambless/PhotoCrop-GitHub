using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Shape
{
    public GameObject ShapeObject;
    public Vector2Int Dimensions;
    public List<ShapeCell> ShapeLayout;

    public Shape(ShapeData shapeData)
    {
        Dimensions = shapeData.Dimensions;
        ShapeLayout = new List<ShapeCell>();

        foreach (ShapeCell originalCell in shapeData.ShapeLayout)
        {
            ShapeCell newCell = new ShapeCell
            {
                Position = originalCell.Position,
                IsFilled = originalCell.IsFilled,
                CellContentType = originalCell.CellContentType
            };

            ShapeLayout.Add(newCell);
        }
    }

    public Shape(List<ShapeCell> shapeCells, Vector2Int dimensions)
    {
        Dimensions = dimensions; 
        ShapeLayout = new List<ShapeCell>();

        foreach (ShapeCell originalCell in shapeCells)
        {
            ShapeCell newCell = new ShapeCell
            {
                Position = originalCell.Position, 
                IsFilled = originalCell.IsFilled,  
                CellContentType = originalCell.CellContentType 
            };

            newCell.ShapeCellObject = originalCell.ShapeCellObject;
            newCell.CellEntities = originalCell.CellEntities;

            ShapeLayout.Add(newCell);  
        }
    }


    public static (Shape, GameObject) CreateShapeAndObjectFromData(ShapeData shapeData)
    {
        Shape newShape = new Shape(shapeData);

        GameObject newShapeObject = new GameObject("ShapeObject");

        foreach (ShapeCell cell in newShape.ShapeLayout)
        {
            if (cell.CellContentType != GameStats.CellContentTypes.Empty)
            {
                GameObject shapeCellObject = Object.Instantiate(LevelSelectorController.Instance.SelectedLevelData.CellObjectPrefab(cell.CellContentType));

                cell.ShapeCellObject = shapeCellObject;

                shapeCellObject.transform.SetParent(newShapeObject.transform);

                float worldTypeOffsetZ = 0f;

                switch (LevelSelectorController.Instance.SelectedLevelData.World)
                {
                    case LevelData.WorldType.FundamentalShapes:
                        worldTypeOffsetZ = 0.5f;
                        break;
                    case LevelData.WorldType.Chess:
                        worldTypeOffsetZ = 0f;
                        break;
                }

                shapeCellObject.transform.localPosition = new Vector3(cell.Position.x, cell.Position.y, worldTypeOffsetZ);

                if (shapeData.CellEntityLayout[newShape.ShapeLayout.IndexOf(cell)] != null)
                {
                    int indexOfCellEntity = newShape.ShapeLayout.IndexOf(cell);

                    CellEntity cellEntity = new CellEntity(shapeData.CellEntityLayout[indexOfCellEntity]);
                    cellEntity.EntityObject = Object.Instantiate(shapeData.CellEntityLayout[indexOfCellEntity].EntityObject);
                    cellEntity.EntityObject.transform.SetParent(cell.ShapeCellObject.transform);
                    cellEntity.EntityObject.transform.position = shapeCellObject.transform.position - (Vector3.forward / 2);
                    cell.CellEntities.Add(cellEntity);
                }
            }
        }

        return (newShape, newShapeObject);
    }

    public static void RotateClockwise(Shape shape)
    {
        List<ShapeCell> newLayout = new List<ShapeCell>();
        int n = shape.Dimensions.x;

        int center = n / 2;     // Translation to index

        // Populate the new layout with default cells
        for (int i = 0; i < n * n; i++)
        {
            newLayout.Add(new ShapeCell
            {
                Position = new Vector2Int(i % n - center, i / n - center),
                IsFilled = false,
                CellContentType = GameStats.CellContentTypes.Empty
            });
        }

        // Rotate each cell and place it in the new layout
        foreach (ShapeCell cell in shape.ShapeLayout)
        {
            if (!cell.IsFilled)
            {
                continue;
            }

            Vector2Int oldPos = cell.Position;
            Vector2Int newPos = new Vector2Int(oldPos.y, -oldPos.x);
            int newIndex = (newPos.y + center) * n + (newPos.x + center);

            // Check bounds
            if (newIndex >= 0 && newIndex < n * n)
            {
                newLayout[newIndex] = new ShapeCell
                {
                    Position = newPos,
                    IsFilled = cell.IsFilled,
                    CellContentType = cell.CellContentType,
                    CellEntities = cell.CellEntities
                };

                if (cell.ShapeCellObject != null)
                {
                    newLayout[newIndex].ShapeCellObject = cell.ShapeCellObject;
                }
            }
        }

        shape.ShapeLayout = newLayout;
    }

    public static bool CanPlaceShape(Hole hole, Shape shape, Vector2Int mouseGridPosition)
    {
        foreach (ShapeCell shapeCell in shape.ShapeLayout)
        {
            if (shapeCell.CellContentType == GameStats.CellContentTypes.Empty)
            {
                continue;
            }

            // The position of the cell should be directly relative to the mouse grid position
            Vector2Int targetPosition = new Vector2Int(mouseGridPosition.x + shapeCell.Position.x, mouseGridPosition.y + shapeCell.Position.y);

            // Check bounds
            if (targetPosition.x < 0 || targetPosition.x >= hole.GridSize.x
                || targetPosition.y < 0 || targetPosition.y >= hole.GridSize.y)
            {
                return false;
            }

            // Check if the position is empty
            int holeIndex = targetPosition.y * (int)hole.GridSize.x + targetPosition.x;

            if (hole.HoleLayout[holeIndex].CellContentType != GameStats.CellContentTypes.Empty)
            {
                return false;
            }
        }

        // If all checks passed, place the shape
        foreach (ShapeCell shapeCell in shape.ShapeLayout)
        {
            if (shapeCell.CellContentType == GameStats.CellContentTypes.Empty)
            {
                continue;
            }

            Vector2Int targetPosition = new Vector2Int(mouseGridPosition.x + shapeCell.Position.x, mouseGridPosition.y + shapeCell.Position.y);
            int holeIndex = targetPosition.y * hole.GridSize.x + targetPosition.x;

            // Update the hole layout
            hole.HoleLayout[holeIndex].CellContentType = shapeCell.CellContentType;
            hole.HoleLayout[holeIndex].IsFilled = true;

            for (int i = shapeCell.CellEntities.Count - 1; i >= 0; i--)
            {
                shapeCell.CellEntities[i].Position = hole.HoleLayout[holeIndex].Position;
                hole.HoleLayout[holeIndex].CellEntities.Add(shapeCell.CellEntities[i]);

                if (LevelController.Instance != null)
                {
                    Debug.Log("cell entities count: " + LevelController.Instance.CellEntities.Count);
                    LevelController.Instance.AddToCellEntities(shapeCell.CellEntities[i]);
                    Debug.Log("cell entities count: " + LevelController.Instance.CellEntities.Count);
                }

                shapeCell.CellEntities.RemoveAt(i);
            }
        }

        return true;
    }
}
