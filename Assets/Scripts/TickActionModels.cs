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

    private static void MoveToTarget(CellEntity cellEntity)
    {
        Debug.Log($"Entity at: {cellEntity.Position} Move to target");
        cellEntity.NumberOfTimesTickActionPerformed++;
        // determine target
        // calculate path
        // movement range

        // if reached target, unsubscribe
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

        playerPawn.CurrentPath = CalculatePathToTarget(LevelController.Instance.CurrentHole, playerPawn, new Vector2Int(playerPawn.Position.x, 6));

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

    public static List<Vector2Int> CalculatePathToTarget(Hole hole, CellEntity cellEntity, Vector2Int targetPosition, List<GameStats.CellContentTypes> validContentTypes = null, bool shouldAlternate = false)
    {
        Queue<(Vector2Int position, int indexTracker)> queue = new Queue<(Vector2Int position, int indexTracker)>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();

        queue.Enqueue((cellEntity.Position, 0));
        cameFrom[cellEntity.Position] = cellEntity.Position;

        Vector2Int farthestReachable = cellEntity.Position;

        while (queue.Count > 0)
        {
            var (current, indexTracker) = queue.Dequeue();

            if (current == targetPosition)
            {
                farthestReachable = current;
                break;
            }

            foreach (GameStats.DirectionTypes direction in cellEntity.DirectionsCanMove)
            {
                Vector2Int next = current + GetDirectionVector(direction);

                if (shouldAlternate && validContentTypes != null)
                {
                    GameStats.CellContentTypes nextContentType = validContentTypes[indexTracker];

                    if (IsPositionValid(hole, next, validContentTypes, nextContentType))
                    {
                        if (!cameFrom.ContainsKey(next))
                        {
                            queue.Enqueue((next, (indexTracker + 1) % validContentTypes.Count));
                            cameFrom[next] = current;
                            farthestReachable = next;
                        }
                    }
                }
                else
                {
                    if (IsPositionValid(hole, next, validContentTypes))
                    {
                        if (!cameFrom.ContainsKey(next))
                        {
                            queue.Enqueue((next, indexTracker));
                            cameFrom[next] = current;
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
        }

        while (step != cellEntity.Position)
        {
            path.Add(step);
            step = cameFrom[step];
        }

        path.Add(cellEntity.Position);
        path.Reverse();

        foreach (var pos in path)
        {
            Debug.Log(pos);
        }

        return path;
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

    //    while (queue.Count > 0)
    //    {
    //        var (current, indexTracker) = queue.Dequeue();

    //        if (current == targetPosition)
    //        {
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
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    List<Vector2Int> path = new List<Vector2Int>();
    //    Vector2Int step = targetPosition;

    //    if (!cameFrom.ContainsKey(step))
    //    {
    //        Debug.Log("Couldn't find a path to the target!");
    //        return path;
    //    }

    //    while (step != cellEntity.Position)
    //    {
    //        path.Add(step);
    //        step = cameFrom[step];
    //    }

    //    path.Reverse();
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