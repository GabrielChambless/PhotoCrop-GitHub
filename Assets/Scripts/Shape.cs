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

            ShapeLayout.Add(newCell);  
        }
    }


    public static (Shape, GameObject) CreateShapeAndObjectFromData(ShapeData shapeData, GameObject shapeCellPrefab)
    {
        Shape newShape = new Shape(shapeData);

        GameObject newShapeObject = new GameObject("ShapeObject");

        foreach (ShapeCell cell in newShape.ShapeLayout)
        {
            if (cell.CellContentType != GameStats.CellContentTypes.Empty)
            {
                GameObject shapeCellObject = Object.Instantiate(shapeCellPrefab);

                cell.ShapeCellObject = shapeCellObject;

                shapeCellObject.transform.SetParent(newShapeObject.transform);
                shapeCellObject.transform.localPosition = new Vector3(cell.Position.x, cell.Position.y, 0f);

                // TODO; don't change materials like this
                switch (cell.CellContentType)
                {
                    case GameStats.CellContentTypes.RedCell:
                        shapeCellObject.GetComponent<Renderer>().material.color = Color.red;
                        break;
                    case GameStats.CellContentTypes.GreenCell:
                        shapeCellObject.GetComponent<Renderer>().material.color = Color.green;
                        break;
                    case GameStats.CellContentTypes.BlueCell:
                        shapeCellObject.GetComponent<Renderer>().material.color = Color.blue;
                        break;
                    case GameStats.CellContentTypes.YellowCell:
                        shapeCellObject.GetComponent<Renderer>().material.color = Color.yellow;
                        break;
                    case GameStats.CellContentTypes.WhiteCell:
                        shapeCellObject.GetComponent<Renderer>().material.color = Color.white;
                        break;
                    case GameStats.CellContentTypes.BlackCell:
                        shapeCellObject.GetComponent<Renderer>().material.color = Color.black;
                        break;
                    default:
                        shapeCellObject.GetComponent<Renderer>().material.color = Color.gray;
                        break;
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
                    CellContentType = cell.CellContentType
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
        }

        return true;
    }
}
