using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Hole
{
    public GameObject HoleObject;
    public Vector2Int GridSize;
    public List<HoleCell> HoleLayout;

    public Hole(HoleData holeData)
    {
        GridSize = holeData.GridSize;
        HoleLayout = new List<HoleCell>();

        foreach (HoleCell originalCell in holeData.HoleLayout)
        {
            HoleCell newCell = new HoleCell
            {
                Position = originalCell.Position,
                IsFilled = originalCell.IsFilled,
                CellContentType = originalCell.CellContentType
            };

            HoleLayout.Add(newCell);
        }
    }

    public static (Hole, GameObject) CreateHoleAndObjectFromData(HoleData holeData)
    {
        Hole newHole = new Hole(holeData);

        GameObject newHoleObject = new GameObject("HoleObject");

        foreach (HoleCell cell in newHole.HoleLayout)
        {
            if (cell.CellContentType != GameStats.CellContentTypes.Empty)
            {
                GameObject holeCellObject = Object.Instantiate(LevelSelectorController.Instance.SelectedLevelData.CellObjectPrefab(cell.CellContentType));

                cell.HoleCellObject = holeCellObject;

                holeCellObject.transform.SetParent(newHoleObject.transform);
                holeCellObject.transform.localPosition = new Vector3(cell.Position.x, cell.Position.y, 0f);

                if (holeData.CellEntityLayout[newHole.HoleLayout.IndexOf(cell)] != null)
                {
                    int indexOfCellEntity = newHole.HoleLayout.IndexOf(cell);

                    CellEntity cellEntity = new CellEntity(holeData.CellEntityLayout[indexOfCellEntity]);
                    cellEntity.EntityObject = Object.Instantiate(holeData.CellEntityLayout[indexOfCellEntity].EntityObject);
                    cellEntity.EntityObject.transform.position = holeCellObject.transform.position - (Vector3.forward / 2);
                    cellEntity.Position = cell.Position;
                    cell.CellEntities.Add(cellEntity);

                    if (LevelController.Instance != null)
                    {
                        LevelController.Instance.AddToCellEntities(cellEntity);
                    }
                }
            }
        }

        return (newHole, newHoleObject);
    }

    public static bool GridIsCompletelyFilled(Hole hole)
    {
        return !hole.HoleLayout.Any(cell => cell.CellContentType == GameStats.CellContentTypes.Empty);
    }

    public static List<List<HoleCell>> GroupCells(Hole hole)
    {
        int width = hole.GridSize.x;
        int height = hole.GridSize.y;

        Dictionary<Vector2Int, bool> visitedCells = new Dictionary<Vector2Int, bool>();
        List<List<HoleCell>> groups = new List<List<HoleCell>>();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Vector2Int position = new Vector2Int(x, y);
                HoleCell cell = hole.HoleLayout.FirstOrDefault(c => c.Position == position);

                if (cell == null || cell.CellContentType == GameStats.CellContentTypes.Empty || visitedCells.ContainsKey(position))
                {
                    continue;
                }

                List<HoleCell> group = new List<HoleCell>();
                FloodFill(hole, position, cell.CellContentType, visitedCells, group);

                if (group.Count > 0)
                {
                    groups.Add(group);
                }
            }
        }

        // Optionally, process groups further or store them
        foreach (List<HoleCell> subGroup in groups)
        {
            Debug.Log($"Group found with {subGroup.Count} cells of type {subGroup[0].CellContentType}");
        }

        return groups;
    }

    private static void FloodFill(Hole hole, Vector2Int pos, GameStats.CellContentTypes type, Dictionary<Vector2Int, bool> visited, List<HoleCell> group)
    {
        if (visited.ContainsKey(pos))
        {
            return;
        }

        HoleCell cell = hole.HoleLayout.FirstOrDefault(c => c.Position == pos);

        if (cell == null || cell.CellContentType != type)
        {
            return;
        }

        visited[pos] = true;
        group.Add(cell);

        List<Vector2Int> directions = new List<Vector2Int>
        {
            new Vector2Int(1, 0),  // Right
            new Vector2Int(-1, 0), // Left
            new Vector2Int(0, 1),  // Up
            new Vector2Int(0, -1)  // Down
        };

        foreach (Vector2Int dir in directions)
        {
            Vector2Int newPos = new Vector2Int(pos.x + dir.x, pos.y + dir.y);

            if (newPos.x >= 0 && newPos.x < hole.GridSize.x && newPos.y >= 0 && newPos.y < hole.GridSize.y)
            {
                FloodFill(hole, newPos, type, visited, group);
            }
        }
    }
}
