using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

public class TickActionModels : MonoBehaviour
{
    public enum TickActionTypes
    {
        None,
        MoveToTarget
    }

    public enum TickActionTargetTypes
    {
        None,
        NotSameGroupEntity,
        SameGroupEntity,
        Manual
    }


    public static void AssignEntityTickAction(CellEntity cellEntity)
    {
        if (cellEntity == null)
        {
            Debug.LogWarning("CellEntity is null; cannot assign tick action.");
            return;
        }

        switch (cellEntity.TickActionType)
        {
            case TickActionTypes.MoveToTarget:
                cellEntity.TickAction = MoveToTarget;
                break;
        }
    }

    private static IEnumerator MoveToTarget(CellEntity cellEntity)
    {
        cellEntity.NumberOfTickActionsPerformed++;

        Vector2Int currentPosition = cellEntity.Position;

        Dictionary<CellEntity, (List<Vector2Int>, bool)> entityPaths = new Dictionary<CellEntity, (List<Vector2Int>, bool)>();

        // Selected target to move towards
        (CellEntity selectedTarget, List<Vector2Int> shortestPath, bool foundUnblockedPath) = (null, null, false);

        // Target position to move towards for Manual
        Vector2Int targetPosition = cellEntity.TickActionManualTarget;

        switch (cellEntity.TickActionTargetType)
        {
            case TickActionTargetTypes.NotSameGroupEntity:
                foreach (CellEntity entity in LevelController.Instance.CellEntities)
                {
                    if (entity.EntityGroupType != cellEntity.EntityGroupType && entity != cellEntity)
                    {
                        (List<Vector2Int>, bool) pathToEntity = CalculatePathToTarget(LevelController.Instance.CurrentHole, cellEntity, entity.Position, cellEntity.CellTypesCanMoveOn);
                        entityPaths.Add(entity, pathToEntity);
                    }
                }
                break;
            case TickActionTargetTypes.SameGroupEntity:
                foreach (CellEntity entity in LevelController.Instance.CellEntities)
                {
                    if (entity.EntityGroupType == cellEntity.EntityGroupType && entity != cellEntity)
                    {
                        (List<Vector2Int>, bool) pathToEntity = CalculatePathToTarget(LevelController.Instance.CurrentHole, cellEntity, entity.Position, cellEntity.CellTypesCanMoveOn);
                        entityPaths.Add(entity, pathToEntity);
                    }
                }
                break;
            case TickActionTargetTypes.Manual:
                (List<Vector2Int>, bool) pathToTarget = CalculatePathToTarget(LevelController.Instance.CurrentHole, cellEntity, targetPosition, cellEntity.CellTypesCanMoveOn);

                selectedTarget = null;
                shortestPath = pathToTarget.Item1;
                foundUnblockedPath = pathToTarget.Item2;
                break;
        }

        if (cellEntity.TickActionTargetType == TickActionTargetTypes.NotSameGroupEntity || cellEntity.TickActionTargetType == TickActionTargetTypes.SameGroupEntity)
        {
            foreach (KeyValuePair<CellEntity, (List<Vector2Int>, bool)> kvp in entityPaths)
            {
                (CellEntity entity, List<Vector2Int> path, bool isUnblocked) = (kvp.Key, kvp.Value.Item1, kvp.Value.Item2);

                if (isUnblocked && (shortestPath == null || path.Count < shortestPath.Count))
                {
                    selectedTarget = entity;
                    shortestPath = path;
                    foundUnblockedPath = true;
                }
                else if (!foundUnblockedPath && (shortestPath == null || path.Count < shortestPath.Count))
                {
                    selectedTarget = entity;
                    shortestPath = path;
                }
            }
        }

        switch (cellEntity.TickActionTargetType)
        {
            case TickActionTargetTypes.NotSameGroupEntity:
            case TickActionTargetTypes.SameGroupEntity:
                if (selectedTarget != null)
                {
                    Debug.Log($"Selected target: {selectedTarget.Position} with path length: {shortestPath.Count}");

                    for (int i = 0; i < cellEntity.MovementRange; i++)
                    {
                        if (i > shortestPath.Count - 1)
                        {
                            break;
                        }

                        yield return AnimationModels.MoveSnap(cellEntity.EntityObject.transform, new Vector3(shortestPath[i].x, shortestPath[i].y, cellEntity.EntityObject.transform.position.z));

                        int cellIndex = LevelController.Instance.CurrentHole.HoleLayout.FindIndex(holeCell => holeCell.Position == cellEntity.Position);
                        LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntity = null;

                        cellEntity.Position = shortestPath[i];

                        cellIndex = LevelController.Instance.CurrentHole.HoleLayout.FindIndex(holeCell => holeCell.Position == cellEntity.Position);
                        LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntity = cellEntity;

                        cellEntity.NumberOfTickActionsFailed = 0;
                    }
                }
                else
                {
                    Debug.Log("No valid target found.");
                }

                if (selectedTarget != null && cellEntity.Position == selectedTarget.Position)
                {
                    Debug.Log("The CellEntity has reached its target position.");
                    cellEntity.NumberOfTickActionsFailed = 0;
                    TickManager.Instance.Unsubscribe(cellEntity, cellEntity.TickAction, cellEntity.TickType);
                    Debug.Log($"Unsubscribed Entity at: {cellEntity.Position}");
                }
                break;
            case TickActionTargetTypes.Manual:
                if (shortestPath != null)
                {
                    Debug.Log($"Target position: {targetPosition} with path length: {shortestPath.Count}");

                    for (int i = 0; i < cellEntity.MovementRange; i++)
                    {
                        if (i > shortestPath.Count - 1)
                        {
                            break;
                        }

                        yield return AnimationModels.MoveSnap(cellEntity.EntityObject.transform, new Vector3(shortestPath[i].x, shortestPath[i].y, cellEntity.EntityObject.transform.position.z));

                        int cellIndex = LevelController.Instance.CurrentHole.HoleLayout.FindIndex(holeCell => holeCell.Position == cellEntity.Position);
                        LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntity = null;

                        cellEntity.Position = shortestPath[i];

                        cellIndex = LevelController.Instance.CurrentHole.HoleLayout.FindIndex(holeCell => holeCell.Position == cellEntity.Position);
                        LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntity = cellEntity;

                        cellEntity.NumberOfTickActionsFailed = 0;
                    }
                }

                if (cellEntity.Position == targetPosition)
                {
                    Debug.Log("The CellEntity has reached its target position.");
                    cellEntity.NumberOfTickActionsFailed = 0;
                    TickManager.Instance.Unsubscribe(cellEntity, cellEntity.TickAction, cellEntity.TickType);
                    Debug.Log($"Unsubscribed Entity at: {cellEntity.Position}");
                }
                break;
        }

        // Didn't change position and not at target position, so count as failed attempt
        if (cellEntity.Position == currentPosition)
        {
            cellEntity.NumberOfTickActionsFailed++;

            if (cellEntity.NumberOfTickActionsFailed > 1)
            {
                Debug.Log("The CellEntity failed to reach their taret position.");
                cellEntity.NumberOfTickActionsFailed = 0;
                TickManager.Instance.Unsubscribe(cellEntity, cellEntity.TickAction, cellEntity.TickType);
                Debug.Log($"Unsubscribed Entity at: {cellEntity.Position}");
            }
        }
    }

    public static List<Vector2Int> CalculatePossibleMoves(Hole hole, CellEntity cellEntity, List<GameStats.CellContentTypes> validContentTypes = null, bool shouldAlternate = false)
    {
        List<Vector2Int> possibleMoves = new List<Vector2Int>();
        int indexTracker = 0;

        foreach (GameStats.DirectionTypes direction in cellEntity.DirectionsCanMove)
        {
            Vector2Int currentPosition = cellEntity.Position;
            Vector2Int directionVector = GetDirectionVector(direction);

            while (true)
            {
                currentPosition += directionVector;

                if (shouldAlternate && validContentTypes != null)
                {
                    if (!IsPositionValid(hole, currentPosition, validContentTypes, validContentTypes[indexTracker]))
                    {
                        break;
                    }

                    indexTracker = (indexTracker + 1) % validContentTypes.Count;
                }
                else
                {
                    if (!IsPositionValid(hole, currentPosition, validContentTypes))
                    {
                        break;
                    }
                }

                possibleMoves.Add(currentPosition);
            }
        }

        return possibleMoves;
    }

    public static (List<Vector2Int> path, bool isUnblocked) CalculatePathToTarget(Hole hole, CellEntity cellEntity, Vector2Int targetPosition, List<GameStats.CellContentTypes> validContentTypes = null, bool shouldAlternate = false)
    {
        Queue<(Vector2Int position, int indexTracker)> queue = new Queue<(Vector2Int position, int indexTracker)>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue((cellEntity.Position, 0));
        cameFrom[cellEntity.Position] = cellEntity.Position;
        visited.Add(cellEntity.Position);

        Vector2Int farthestReachable = cellEntity.Position;
        float closestDistance = Vector2Int.Distance(cellEntity.Position, targetPosition);
        bool isUnblocked = false;

        while (queue.Count > 0)
        {
            var (current, indexTracker) = queue.Dequeue();

            if (current == targetPosition)
            {
                farthestReachable = current;
                isUnblocked = true;
                break;
            }

            foreach (GameStats.DirectionTypes direction in cellEntity.DirectionsCanMove)
            {
                Vector2Int next = current + GetDirectionVector(direction);

                if (!visited.Contains(next))
                {
                    if (shouldAlternate && validContentTypes != null)
                    {
                        GameStats.CellContentTypes nextContentType = validContentTypes[indexTracker];

                        if (IsPositionValid(hole, next, validContentTypes, nextContentType))
                        {
                            queue.Enqueue((next, (indexTracker + 1) % validContentTypes.Count));
                            cameFrom[next] = current;
                            visited.Add(next);

                            float distanceToTarget = Vector2Int.Distance(next, targetPosition);
                            if (distanceToTarget < closestDistance)
                            {
                                farthestReachable = next;
                                closestDistance = distanceToTarget;
                            }
                        }
                    }
                    else
                    {
                        if (IsPositionValid(hole, next, validContentTypes))
                        {
                            queue.Enqueue((next, indexTracker));
                            cameFrom[next] = current;
                            visited.Add(next);

                            float distanceToTarget = Vector2Int.Distance(next, targetPosition);
                            if (distanceToTarget < closestDistance)
                            {
                                farthestReachable = next;
                                closestDistance = distanceToTarget;
                            }
                        }
                    }
                }
            }
        }

        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int step = targetPosition;


        Debug.Log("Farthest reachable: " + farthestReachable);

        // If the target position is not reachable, use the farthest reachable position
        if (!cameFrom.ContainsKey(step))
        {
            Debug.Log("Couldn't find a path to the target. Returning the path to the farthest reachable position.");
            step = farthestReachable;
            isUnblocked = false;
        }

        while (step != cellEntity.Position)
        {
            path.Add(step);
            step = cameFrom[step];
        }

        //path.Add(cellEntity.Position);  // Add the starting position
        path.Reverse();

        return (path, isUnblocked);
    }

    public static List<Vector2Int> FindAdjacentCellsWithEntities(Hole hole, CellEntity cellEntity)
    {
        List<Vector2Int> adjacentPositions = GetAdjacentPositions(cellEntity.Position);
        List<Vector2Int> positionsWithEntities = new List<Vector2Int>();

        foreach (Vector2Int position in adjacentPositions)
        {
            HoleCell cell = hole.HoleLayout.FirstOrDefault(c => c.Position == position);

            if (cell != null && cell.CellEntity != null)
            {
                positionsWithEntities.Add(position);
            }
        }

        return positionsWithEntities;
    }

    private static List<Vector2Int> GetAdjacentPositions(Vector2Int position)
    {
        return new List<Vector2Int>
        {
            position + new Vector2Int(0, 1),    // Up
            position + new Vector2Int(0, -1),   // Down
            position + new Vector2Int(1, 0),    // Right
            position + new Vector2Int(-1, 0),   // Left
            position + new Vector2Int(1, 1),    // UpRight
            position + new Vector2Int(-1, 1),   // UpLeft
            position + new Vector2Int(1, -1),   // DownRight
            position + new Vector2Int(-1, -1)   // DownLeft
        };
    }
    private static Vector2Int GetDirectionVector(GameStats.DirectionTypes direction)
    {
        switch (direction)
        {
            case GameStats.DirectionTypes.Up:
                return new Vector2Int(0, 1);
            case GameStats.DirectionTypes.Down:
                return new Vector2Int(0, -1);
            case GameStats.DirectionTypes.Right:
                return new Vector2Int(1, 0);
            case GameStats.DirectionTypes.Left:
                return new Vector2Int(-1, 0);
            case GameStats.DirectionTypes.UpRight:
                return new Vector2Int(1, 1);
            case GameStats.DirectionTypes.UpLeft:
                return new Vector2Int(-1, 1);
            case GameStats.DirectionTypes.DownRight:
                return new Vector2Int(1, -1);
            case GameStats.DirectionTypes.DownLeft:
                return new Vector2Int(-1, -1);
            default:
                return Vector2Int.zero;
        }
    }

    private static bool IsPositionValid(Hole hole, Vector2Int position, List<GameStats.CellContentTypes> validContentTypes, GameStats.CellContentTypes specificContentType = GameStats.CellContentTypes.Empty)
    {
        HoleCell cell = hole.HoleLayout.FirstOrDefault(c => c.Position == position);

        if (validContentTypes != null && specificContentType != GameStats.CellContentTypes.Empty)
        {
            return cell != null && (cell.CellEntity == null || cell.CellEntity.CanSharePosition) && validContentTypes.Contains(cell.CellContentType) && cell.CellContentType == specificContentType;
        }
        else if (validContentTypes != null && specificContentType == GameStats.CellContentTypes.Empty)
        {
            return cell != null && (cell.CellEntity == null || cell.CellEntity.CanSharePosition) && validContentTypes.Contains(cell.CellContentType);
        }

        return cell != null && (cell.CellEntity == null || cell.CellEntity.CanSharePosition);
    }
}