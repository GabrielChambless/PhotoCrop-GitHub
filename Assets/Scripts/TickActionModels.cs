using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

public class TickActionModels : MonoBehaviour
{
    public enum TickActionTypes
    {
        MoveToTarget
    }

    public enum TickActionTargetTypes
    {
        NotSameGroupEntity,
        SameGroupEntity,
        RandomEntity,
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
        Dictionary<CellEntity, (List<Vector2Int>, bool)> entityPaths = new Dictionary<CellEntity, (List<Vector2Int>, bool)>();

        // Selected target to move towards
        (CellEntity selectedTarget, List<Vector2Int> shortestPath, bool foundUnblockedPath) = (null, null, false);

        switch (cellEntity.TickActionTargetType)
        {
            case TickActionTargetTypes.NotSameGroupEntity:
                foreach (CellEntity entity in LevelController.Instance.CellEntities)
                {
                    if (entity.EntityGroupType != cellEntity.EntityGroupType)
                    {
                        (List<Vector2Int>, bool) pathToEntity = CalculatePathToTarget(LevelController.Instance.CurrentHole, cellEntity, entity.Position, cellEntity.CellTypesCanMoveOn);
                        entityPaths.Add(entity, pathToEntity);
                    }
                }

                break;
            case TickActionTargetTypes.SameGroupEntity:
                foreach (CellEntity entity in LevelController.Instance.CellEntities)
                {
                    if (entity.EntityGroupType == cellEntity.EntityGroupType)
                    {
                        (List<Vector2Int>, bool) pathToEntity = CalculatePathToTarget(LevelController.Instance.CurrentHole, cellEntity, entity.Position, cellEntity.CellTypesCanMoveOn);
                        entityPaths.Add(entity, pathToEntity);
                    }
                }

                break;
            case TickActionTargetTypes.RandomEntity:
                foreach (CellEntity entity in LevelController.Instance.CellEntities)
                {
                    (List<Vector2Int>, bool) pathToEntity = CalculatePathToTarget(LevelController.Instance.CurrentHole, cellEntity, entity.Position, cellEntity.CellTypesCanMoveOn);

                    if (pathToEntity.Item2)
                    {
                        entityPaths.Add(entity, pathToEntity);
                    }
                }

                break;
            case TickActionTargetTypes.Manual:
                Vector2Int targetPosition = cellEntity.TickActionManualTarget;
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
        else if (cellEntity.TickActionTargetType == TickActionTargetTypes.RandomEntity)
        {
            if (entityPaths.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, entityPaths.Count);

                selectedTarget = entityPaths.ElementAt(randomIndex).Key;
                shortestPath = entityPaths.ElementAt(randomIndex).Value.Item1;
                foundUnblockedPath = entityPaths.ElementAt(randomIndex).Value.Item2;
            }
            else if (LevelController.Instance.CellEntities.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, LevelController.Instance.CellEntities.Count);

                selectedTarget = LevelController.Instance.CellEntities.ElementAt(randomIndex);
                shortestPath = CalculatePathToTarget(LevelController.Instance.CurrentHole, cellEntity, selectedTarget.Position, cellEntity.CellTypesCanMoveOn).path;
                foundUnblockedPath = false;
            }
        }

        switch (cellEntity.TickActionTargetType)
        {
            case TickActionTargetTypes.NotSameGroupEntity:
            case TickActionTargetTypes.SameGroupEntity:
            case TickActionTargetTypes.RandomEntity:
                if (selectedTarget != null)
                {
                    Debug.Log($"Selected target: {selectedTarget.Position} with path length: {shortestPath.Count}");
                }
                else
                {
                    Debug.Log("No valid target found.");
                }
                break;
            case TickActionTargetTypes.Manual:
                break;
        }

        // TODO; set up amation which yields the return
        yield return null;

        // movement range
        // if reached target, unsubscribe

        cellEntity.NumberOfTimesTickActionPerformed++;

        if (cellEntity.NumberOfTimesTickActionPerformed > 2)
        {
            TickManager.Instance.Unsubscribe(cellEntity, cellEntity.TickAction, cellEntity.TickType);
            Debug.Log($"Unsubscribed Entity at: {cellEntity.Position}");
        }
    }


    ////////////////////////////////////////////////////////////
    public static IEnumerator PawnPromotion(CellEntity playerPawn, List<CellEntity> rivalPieces = null)
    {
        // while loop
        // pawn checks if rival piece is forward (up y) daigonal to it, if so attack
        // if not attack, move forward (up y)

        // rival piece index tracker
        // move rival piece at index of tracker along path, with the path target being the player pawn piece
        // for loop end

        playerPawn.CurrentPath = CalculatePathToTarget(LevelController.Instance.CurrentHole, playerPawn, new Vector2Int(playerPawn.Position.x, 6)).path;

        while (playerPawn.CurrentPath.Count != 0)
        {
            playerPawn.Position = playerPawn.CurrentPath[0];
            playerPawn.CurrentPath.RemoveAt(0);
            playerPawn.EntityObject.transform.position = new Vector3(playerPawn.Position.x, playerPawn.Position.y, playerPawn.EntityObject.transform.position.z);

            yield return new WaitForSeconds(0.5f);

        }

    }
    ////////////////////////////////////////////////////////////


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
                            farthestReachable = next;
                        }
                    }
                    else
                    {
                        if (IsPositionValid(hole, next, validContentTypes))
                        {
                            queue.Enqueue((next, indexTracker));
                            cameFrom[next] = current;
                            visited.Add(next);
                            farthestReachable = next;
                        }
                    }
                }
            }
        }

        List<Vector2Int> path = new List<Vector2Int>();
        Vector2Int step = targetPosition;

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

        path.Add(cellEntity.Position);  // Add the starting position
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
            return cell != null && cell.CellEntity == null && validContentTypes.Contains(cell.CellContentType) && cell.CellContentType == specificContentType;
        }
        else if (validContentTypes != null && specificContentType == GameStats.CellContentTypes.Empty)
        {
            return cell != null && cell.CellEntity == null && validContentTypes.Contains(cell.CellContentType);
        }

        return cell != null && cell.CellEntity == null;
    }


    //public static List<Vector2Int> CalculatePathToTarget(Hole hole, CellEntity cellEntity, Vector2Int targetPosition, List<GameStats.CellContentTypes> validContentTypes = null, bool shouldAlternate = false)
    //{
    //    Queue<(Vector2Int position, int indexTracker)> queue = new Queue<(Vector2Int position, int indexTracker)>();
    //    Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();

    //    queue.Enqueue((cellEntity.Position, 0));
    //    cameFrom[cellEntity.Position] = cellEntity.Position;

    //    Vector2Int farthestReachable = cellEntity.Position;

    //    while (queue.Count > 0)
    //    {
    //        var (current, indexTracker) = queue.Dequeue();

    //        if (current == targetPosition)
    //        {
    //            farthestReachable = current;
    //            break;
    //        }

    //        foreach (GameStats.DirectionTypes direction in cellEntity.DirectionsCanMove)
    //        {
    //            Vector2Int next = current + GetDirectionVector(direction);

    //            if (shouldAlternate && validContentTypes != null)
    //            {
    //                GameStats.CellContentTypes nextContentType = validContentTypes[indexTracker];

    //                if (IsPositionValid(hole, next, validContentTypes, nextContentType))
    //                {
    //                    if (!cameFrom.ContainsKey(next))
    //                    {
    //                        queue.Enqueue((next, (indexTracker + 1) % validContentTypes.Count));
    //                        cameFrom[next] = current;
    //                        farthestReachable = next;
    //                    }
    //                }
    //            }
    //            else
    //            {
    //                if (IsPositionValid(hole, next, validContentTypes))
    //                {
    //                    if (!cameFrom.ContainsKey(next))
    //                    {
    //                        queue.Enqueue((next, indexTracker));
    //                        cameFrom[next] = current;
    //                        farthestReachable = next;
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    List<Vector2Int> path = new List<Vector2Int>();
    //    Vector2Int step = targetPosition;

    //    // If the target position is not reachable, use the farthest reachable position
    //    if (!cameFrom.ContainsKey(step))
    //    {
    //        Debug.Log("Couldn't find a path to the target. Returning the path to the farthest reachable position.");
    //        step = farthestReachable;
    //    }

    //    while (step != cellEntity.Position)
    //    {
    //        path.Add(step);
    //        step = cameFrom[step];
    //    }

    //    path.Add(cellEntity.Position);
    //    path.Reverse();

    //    foreach (var pos in path)
    //    {
    //        Debug.Log(pos);
    //    }

    //    return path;
    //}
}

// all tick actions will take place after the grid is filled or no more shapes remain

// move pawn to get promotion:
// place shape
// recalculate pawn path
// if can attack enemy piece, it does attack method (combo of moving and removing attacked piece from board)
// else if can move, pawn moves to next position in path

// this pawn's general action:
// attack diagonal enemy if possible
// if don't attack, move forward if path is possible


// in levelcontroller:
// after place shape coroutine is finished (or cropped shaped coroutine is finished), call all after shapeplaced tick actions


// spawn in hole, shapes, and cell entities
// then after everything is spawned in, do tick action subscriptions

// method to determine subscribers based on level:
// for pawn promotion, the player pawn moves every other turn, and the black player gets 5 moves
// pawn executes pawn method, which is attack, if cannot, move forward,
// opposing pieces exectute calculate path to target method, which is the player pawn, and chooses the best piece to move towards pawn
// then pawn goes again and restarts cycle