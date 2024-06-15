using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

public static class TickActionModels
{
    public enum TickActionTypes
    {
        None,
        MoveToTarget,
        MoveToTargetAndAttackAlongPath,
        PrioritizeAttackOverMove
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
            case TickActionTypes.MoveToTargetAndAttackAlongPath:
                cellEntity.TickAction = MoveToTargetAndAttackAlongPath;
                break;
            case TickActionTypes.PrioritizeAttackOverMove:
                cellEntity.TickAction = PrioritizeAttackOverMove;
                break;
        }
    }

    // Tick Actions
    private static IEnumerator MoveToTarget(CellEntity cellEntity)
    {
        cellEntity.NumberOfTickActionsPerformed++;

        Vector2Int currentPosition = cellEntity.Position;
        Vector2Int startingPositon = currentPosition;

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
                        (List<Vector2Int>, bool) pathToEntity = CalculatePathToTarget(LevelController.Instance.CurrentHole, cellEntity, entity.Position, cellEntity.CellTypesCanMoveOn, cellEntity.ShouldAlternateCellTypesWhenMoving);
                        entityPaths.Add(entity, pathToEntity);
                    }
                }
                break;
            case TickActionTargetTypes.SameGroupEntity:
                foreach (CellEntity entity in LevelController.Instance.CellEntities)
                {
                    if (entity.EntityGroupType == cellEntity.EntityGroupType && entity != cellEntity)
                    {
                        (List<Vector2Int>, bool) pathToEntity = CalculatePathToTarget(LevelController.Instance.CurrentHole, cellEntity, entity.Position, cellEntity.CellTypesCanMoveOn, cellEntity.ShouldAlternateCellTypesWhenMoving);
                        entityPaths.Add(entity, pathToEntity);
                    }
                }
                break;
            case TickActionTargetTypes.Manual:
                (List<Vector2Int>, bool) pathToTarget = CalculatePathToTarget(LevelController.Instance.CurrentHole, cellEntity, targetPosition, cellEntity.CellTypesCanMoveOn, cellEntity.ShouldAlternateCellTypesWhenMoving);

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
                if (selectedTarget != null && shortestPath.Count > 0)
                {
                    Debug.Log($"Selected target: {selectedTarget.Position} with path length: {shortestPath.Count}");

                    Vector2Int previousPosition = currentPosition;

                    for (int i = 0; i < cellEntity.MovementRange; i++)
                    {
                        if (i > shortestPath.Count - 1)
                        {
                            break;
                        }

                        Vector2Int nextPosition = shortestPath[i];
                        Vector2Int currentDirection = nextPosition - currentPosition;
                        Vector2Int previousDirection = currentPosition - previousPosition;

                        if (i > 0 && !cellEntity.CanChangeMovementDirection && currentDirection != previousDirection)
                        {
                            break;
                        }

                        yield return AnimationModels.MoveSnap(cellEntity.EntityObject.transform, new Vector3(nextPosition.x, nextPosition.y, cellEntity.EntityObject.transform.position.z));

                        int cellIndex = LevelController.Instance.CurrentHole.HoleLayout.FindIndex(holeCell => holeCell.Position == cellEntity.Position);
                        LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Remove(cellEntity);

                        previousPosition = currentPosition;
                        currentPosition = nextPosition;

                        cellEntity.Position = currentPosition;

                        cellIndex = LevelController.Instance.CurrentHole.HoleLayout.FindIndex(holeCell => holeCell.Position == cellEntity.Position);
                        LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Add(cellEntity);

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
                if (shortestPath != null && shortestPath.Count > 0)
                {
                    Debug.Log($"Target position: {targetPosition} with path length: {shortestPath.Count}");

                    Vector2Int previousPosition = currentPosition;

                    for (int i = 0; i < cellEntity.MovementRange; i++)
                    {
                        if (i > shortestPath.Count - 1)
                        {
                            break;
                        }

                        Vector2Int nextPosition = shortestPath[i];
                        Vector2Int currentDirection = shortestPath[i] - currentPosition;
                        Vector2Int previousDirection = currentPosition - previousPosition;

                        if (i > 0 && !cellEntity.CanChangeMovementDirection && currentDirection != previousDirection)
                        {
                            break;
                        }

                        yield return AnimationModels.MoveSnap(cellEntity.EntityObject.transform, new Vector3(nextPosition.x, nextPosition.y, cellEntity.EntityObject.transform.position.z));

                        int cellIndex = LevelController.Instance.CurrentHole.HoleLayout.FindIndex(holeCell => holeCell.Position == cellEntity.Position);
                        LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Remove(cellEntity);

                        previousPosition = currentPosition;
                        currentPosition = nextPosition;

                        cellEntity.Position = currentPosition;

                        cellIndex = LevelController.Instance.CurrentHole.HoleLayout.FindIndex(holeCell => holeCell.Position == cellEntity.Position);
                        LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Add(cellEntity);

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
        if (cellEntity.Position == startingPositon)
        {
            cellEntity.NumberOfTickActionsFailed++;

            if (cellEntity.NumberOfTickActionsFailed > 1)
            {
                Debug.Log("The CellEntity failed to reach their target position.");
                cellEntity.NumberOfTickActionsFailed = 0;
                TickManager.Instance.Unsubscribe(cellEntity, cellEntity.TickAction, cellEntity.TickType);
                Debug.Log($"Unsubscribed Entity at: {cellEntity.Position}");
            }
        }

        foreach (var cell in LevelController.Instance.CurrentHole.HoleLayout)
        {
            if (cell.CellEntities.Count > 0)
            {
                foreach (var entity in cell.CellEntities)
                {
                    Debug.Log($"position with entity {entity.EntityObject.name}: {cell.Position}");
                }
            }
        }
    }

    private static IEnumerator MoveToTargetAndAttackAlongPath(CellEntity cellEntity)
    {
        cellEntity.NumberOfTickActionsPerformed++;

        Vector2Int currentPosition = cellEntity.Position;
        Vector2Int startingPositon = currentPosition;

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
                        (List<Vector2Int>, bool) pathToEntity = CalculatePathToTarget(LevelController.Instance.CurrentHole, cellEntity, entity.Position, cellEntity.CellTypesCanMoveOn, cellEntity.ShouldAlternateCellTypesWhenMoving);
                        entityPaths.Add(entity, pathToEntity);
                    }
                }
                break;
            case TickActionTargetTypes.SameGroupEntity:
                foreach (CellEntity entity in LevelController.Instance.CellEntities)
                {
                    if (entity.EntityGroupType == cellEntity.EntityGroupType && entity != cellEntity)
                    {
                        (List<Vector2Int>, bool) pathToEntity = CalculatePathToTarget(LevelController.Instance.CurrentHole, cellEntity, entity.Position, cellEntity.CellTypesCanMoveOn, cellEntity.ShouldAlternateCellTypesWhenMoving);
                        entityPaths.Add(entity, pathToEntity);
                    }
                }
                break;
            case TickActionTargetTypes.Manual:
                (List<Vector2Int>, bool) pathToTarget = CalculatePathToTarget(LevelController.Instance.CurrentHole, cellEntity, targetPosition, cellEntity.CellTypesCanMoveOn, cellEntity.ShouldAlternateCellTypesWhenMoving);

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
                if (selectedTarget != null && shortestPath.Count > 0)
                {
                    Debug.Log($"Selected target: {selectedTarget.Position} with path length: {shortestPath.Count}");

                    Vector2Int previousPosition = currentPosition;

                    for (int i = 0; i < cellEntity.MovementRange; i++)
                    {
                        if (i > shortestPath.Count - 1)
                        {
                            break;
                        }

                        Vector2Int nextPosition = shortestPath[i];
                        Vector2Int currentDirection = nextPosition - currentPosition;
                        Vector2Int previousDirection = currentPosition - previousPosition;

                        if (i > 0 && !cellEntity.CanChangeMovementDirection && currentDirection != previousDirection)
                        {
                            break;
                        }

                        yield return AnimationModels.MoveSnap(cellEntity.EntityObject.transform, new Vector3(nextPosition.x, nextPosition.y, cellEntity.EntityObject.transform.position.z));

                        int cellIndex = LevelController.Instance.CurrentHole.HoleLayout.FindIndex(holeCell => holeCell.Position == cellEntity.Position);
                        LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Remove(cellEntity);

                        previousPosition = currentPosition;
                        currentPosition = nextPosition;

                        cellEntity.Position = currentPosition;

                        cellIndex = LevelController.Instance.CurrentHole.HoleLayout.FindIndex(holeCell => holeCell.Position == cellEntity.Position);
                        LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Add(cellEntity);

                        for (int k = LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Count - 1; k >= 0; k--)
                        {
                            if (LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities[k] != cellEntity && LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities[k].CanBeRemovedByOtherEntities)
                            {
                                LevelController.Instance.StartCoroutine(AnimationModels.RotateAndFlyAway(LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities[k].EntityObject, GetDirection(previousPosition, cellEntity.Position)));
                                LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Remove(LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities[k]);
                            }
                        }

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
                if (shortestPath != null && shortestPath.Count > 0)
                {
                    Debug.Log($"Target position: {targetPosition} with path length: {shortestPath.Count}");

                    Vector2Int previousPosition = currentPosition;

                    for (int i = 0; i < cellEntity.MovementRange; i++)
                    {
                        if (i > shortestPath.Count - 1)
                        {
                            break;
                        }

                        Vector2Int nextPosition = shortestPath[i];
                        Vector2Int currentDirection = shortestPath[i] - currentPosition;
                        Vector2Int previousDirection = currentPosition - previousPosition;

                        if (i > 0 && !cellEntity.CanChangeMovementDirection && currentDirection != previousDirection)
                        {
                            break;
                        }

                        yield return AnimationModels.MoveSnap(cellEntity.EntityObject.transform, new Vector3(nextPosition.x, nextPosition.y, cellEntity.EntityObject.transform.position.z));

                        int cellIndex = LevelController.Instance.CurrentHole.HoleLayout.FindIndex(holeCell => holeCell.Position == cellEntity.Position);
                        LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Remove(cellEntity);

                        previousPosition = currentPosition;
                        currentPosition = nextPosition;

                        cellEntity.Position = currentPosition;

                        cellIndex = LevelController.Instance.CurrentHole.HoleLayout.FindIndex(holeCell => holeCell.Position == cellEntity.Position);
                        LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Add(cellEntity);

                        for (int k = LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Count - 1; k >= 0; k--)
                        {
                            if (LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities[k] != cellEntity && LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities[k].CanBeRemovedByOtherEntities)
                            {
                                LevelController.Instance.StartCoroutine(AnimationModels.RotateAndFlyAway(LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities[k].EntityObject, GetDirection(previousPosition, cellEntity.Position)));
                                LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Remove(LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities[k]);
                            }
                        }

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
        if (cellEntity.Position == startingPositon)
        {
            cellEntity.NumberOfTickActionsFailed++;

            if (cellEntity.NumberOfTickActionsFailed > 1)
            {
                Debug.Log("The CellEntity failed to reach their target position.");
                cellEntity.NumberOfTickActionsFailed = 0;
                TickManager.Instance.Unsubscribe(cellEntity, cellEntity.TickAction, cellEntity.TickType);
                Debug.Log($"Unsubscribed Entity at: {cellEntity.Position}");
            }
        }

        foreach (var cell in LevelController.Instance.CurrentHole.HoleLayout)
        {
            if (cell.CellEntities.Count > 0)
            {
                foreach (var entity in cell.CellEntities)
                {
                    Debug.Log($"position with entity {entity.EntityObject.name}: {cell.Position}");
                }
            }
        }
    }

    private static IEnumerator PrioritizeAttackOverMove(CellEntity cellEntity)
    {
        Vector2Int currentPosition = cellEntity.Position;
        Vector2Int startingPositon = currentPosition;

        // Selected target to move towards and attack
        (CellEntity selectedTarget, List<Vector2Int> shortestPath) = CalculatePossibleEntitiesToAttack(LevelController.Instance.CurrentHole, cellEntity, cellEntity.CellTypesCanMoveOn, cellEntity.ShouldAlternateCellTypesWhenMoving);

        if (selectedTarget != null && shortestPath.Count > 0)
        {
            Debug.Log($"Selected target: {selectedTarget.Position} with path length: {shortestPath.Count}");

            Vector2Int previousPosition = currentPosition;

            for (int i = 0; i < cellEntity.MovementRange; i++)
            {
                if (i > shortestPath.Count - 1)
                {
                    break;
                }

                Vector2Int nextPosition = shortestPath[i];
                Vector2Int currentDirection = nextPosition - currentPosition;
                Vector2Int previousDirection = currentPosition - previousPosition;

                if (i > 0 && !cellEntity.CanChangeMovementDirection && currentDirection != previousDirection)
                {
                    break;
                }

                yield return AnimationModels.MoveSnap(cellEntity.EntityObject.transform, new Vector3(nextPosition.x, nextPosition.y, cellEntity.EntityObject.transform.position.z));

                int cellIndex = LevelController.Instance.CurrentHole.HoleLayout.FindIndex(holeCell => holeCell.Position == cellEntity.Position);
                LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Remove(cellEntity);

                previousPosition = currentPosition;
                currentPosition = nextPosition;

                cellEntity.Position = currentPosition;

                cellIndex = LevelController.Instance.CurrentHole.HoleLayout.FindIndex(holeCell => holeCell.Position == cellEntity.Position);
                LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Add(cellEntity);

                for (int k = LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Count - 1; k >= 0; k--)
                {
                    if (LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities[k] != cellEntity && LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities[k].CanBeRemovedByOtherEntities)
                    {
                        LevelController.Instance.StartCoroutine(AnimationModels.RotateAndFlyAway(LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities[k].EntityObject, GetDirection(previousPosition, cellEntity.Position)));
                        LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Remove(LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities[k]);
                    }
                }

                cellEntity.NumberOfTickActionsFailed = 0;
            }
        }
        else
        {
            Debug.Log("No valid attack. Will attempt to move to target.");

            if (selectedTarget != null && cellEntity.Position == selectedTarget.Position)
            {
                Debug.Log("The CellEntity has reached its target position.");
                cellEntity.NumberOfTickActionsFailed = 0;
                TickManager.Instance.Unsubscribe(cellEntity, cellEntity.TickAction, cellEntity.TickType);
                Debug.Log($"Unsubscribed Entity at: {cellEntity.Position}");
            }
        }
    }


    public static (CellEntity, List<Vector2Int>) CalculatePossibleEntitiesToAttack(Hole hole, CellEntity cellEntity, List<GameStats.CellContentTypes> validContentTypes = null, bool shouldAlternate = false)
    {
        if (cellEntity.CanChangeMovementDirection)
        {
            return CalculatePossibleAttacksWithBFS(hole, cellEntity, validContentTypes, shouldAlternate);
        }

        return CalculatePossibleAttacksWithMinimalDirectionChanges(hole, cellEntity, validContentTypes, shouldAlternate);
    }

    public static (List<Vector2Int> path, bool isUnblocked) CalculatePathToTarget(Hole hole, CellEntity cellEntity, Vector2Int targetPosition, List<GameStats.CellContentTypes> validContentTypes = null, bool shouldAlternate = false)
    {
        if (cellEntity.CanChangeMovementDirection)
        {
            return CalculatePathWithBFS(hole, cellEntity, targetPosition, validContentTypes, shouldAlternate);
        }

        // New logic for entities that cannot change direction mid movement
        return CalculatePathWithMinimalDirectionChanges(hole, cellEntity, targetPosition, validContentTypes, shouldAlternate);
    }

    public static List<Vector2Int> FindAdjacentCellsWithEntities(Hole hole, CellEntity cellEntity)
    {
        List<Vector2Int> adjacentPositions = GetAdjacentPositions(cellEntity.Position);
        List<Vector2Int> positionsWithEntities = new List<Vector2Int>();

        foreach (Vector2Int position in adjacentPositions)
        {
            HoleCell cell = hole.HoleLayout.FirstOrDefault(c => c.Position == position);

            if (cell != null && cell.CellEntities.Count > 0)
            {
                positionsWithEntities.Add(position);
            }
        }

        return positionsWithEntities;
    }

    private static (CellEntity, List<Vector2Int>) CalculatePossibleAttacksWithBFS(Hole hole, CellEntity cellEntity, List<GameStats.CellContentTypes> validContentTypes, bool shouldAlternate)
    {
        List<CellEntity> entitiesAtPositions = new List<CellEntity>();
        Dictionary<CellEntity, List<Vector2Int>> entityPaths = new Dictionary<CellEntity, List<Vector2Int>>();
        int indexTracker = 0;
        int movementRange = cellEntity.MovementRange;

        Queue<(Vector2Int position, int distance, List<Vector2Int> path)> queue = new Queue<(Vector2Int, int, List<Vector2Int>)>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        queue.Enqueue((cellEntity.Position, 0, new List<Vector2Int>()));
        visited.Add(cellEntity.Position);

        while (queue.Count > 0)
        {
            var (currentPosition, currentDistance, currentPath) = queue.Dequeue();

            foreach (GameStats.DirectionTypes direction in cellEntity.DirectionsCanAttack)
            {
                Vector2Int directionVector = GetDirectionVector(direction);
                Vector2Int nextPosition = currentPosition + directionVector;
                int nextDistance = currentDistance + 1;

                if (nextDistance > movementRange)
                {
                    continue;
                }

                if (shouldAlternate && validContentTypes != null)
                {
                    if (!IsPositionValid(cellEntity, hole, nextPosition, validContentTypes, validContentTypes[indexTracker]))
                    {
                        continue;
                    }

                    indexTracker = (indexTracker + 1) % validContentTypes.Count;
                }
                else
                {
                    if (!IsPositionValid(cellEntity, hole, nextPosition, validContentTypes))
                    {
                        continue;
                    }
                }

                if (!visited.Contains(nextPosition))
                {
                    visited.Add(nextPosition);
                    List<Vector2Int> newPath = new List<Vector2Int>(currentPath) { nextPosition };

                    int cellIndex = LevelController.Instance.CurrentHole.HoleLayout.FindIndex(holeCell => holeCell.Position == nextPosition);

                    if (cellIndex != -1)
                    {
                        // Check if the cell contains entities that can be removed by other entities
                        var removableEntities = LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Where(x => x.CanBeRemovedByOtherEntities).ToList();
                        if (removableEntities.Any())
                        {
                            foreach (var entity in removableEntities)
                            {
                                entitiesAtPositions.Add(entity);
                                entityPaths[entity] = newPath;
                            }
                            continue; // Stop exploring this path
                        }

                        // Check if there is an obstacle that cannot be passed through
                        bool isBlockedByEntity = LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Any(x => !x.CanBeRemovedByOtherEntities && !x.CanSharePosition);

                        if (isBlockedByEntity)
                        {
                            continue; // Stop exploring this path
                        }
                    }

                    queue.Enqueue((nextPosition, nextDistance, newPath));
                }
            }
        }

        // Filter entities to attack based on TickActionTargetType
        switch (cellEntity.TickActionTargetType)
        {
            case TickActionTargetTypes.Manual:
            case TickActionTargetTypes.NotSameGroupEntity:
                entitiesAtPositions = entitiesAtPositions.Where(entity => entity.EntityGroupType != cellEntity.EntityGroupType).ToList();
                break;
            case TickActionTargetTypes.SameGroupEntity:
                entitiesAtPositions = entitiesAtPositions.Where(entity => entity.EntityGroupType == cellEntity.EntityGroupType && entity != cellEntity).ToList();
                break;
        }

        // Select target entity to attack
        CellEntity selectedTarget = null;
        if (entitiesAtPositions.Count > 0)
        {
            selectedTarget = entitiesAtPositions.OrderBy(cell => cell.TargetedValue).FirstOrDefault();
        }

        // Return the selected target and the path to that target
        if (selectedTarget != null && entityPaths.ContainsKey(selectedTarget))
        {
            return (selectedTarget, entityPaths[selectedTarget]);
        }

        return (null, null);
    }

    private static (CellEntity, List<Vector2Int>) CalculatePossibleAttacksWithMinimalDirectionChanges(Hole hole, CellEntity cellEntity, List<GameStats.CellContentTypes> validContentTypes, bool shouldAlternate)
    {
        List<CellEntity> entitiesAtPositions = new List<CellEntity>();
        Dictionary<CellEntity, List<Vector2Int>> entityPaths = new Dictionary<CellEntity, List<Vector2Int>>();
        int indexTracker = 0;

        foreach (GameStats.DirectionTypes direction in cellEntity.DirectionsCanAttack)
        {
            Vector2Int currentPosition = cellEntity.Position;
            Vector2Int directionVector = GetDirectionVector(direction);
            int distanceTraveled = 0;
            List<Vector2Int> currentPath = new List<Vector2Int>();

            while (distanceTraveled < cellEntity.MovementRange)
            {
                currentPosition += directionVector;
                distanceTraveled++;
                currentPath.Add(currentPosition);

                if (shouldAlternate && validContentTypes != null)
                {
                    if (!IsPositionValid(cellEntity, hole, currentPosition, validContentTypes, validContentTypes[indexTracker]))
                    {
                        break;
                    }

                    indexTracker = (indexTracker + 1) % validContentTypes.Count;
                }
                else
                {
                    if (!IsPositionValid(cellEntity, hole, currentPosition, validContentTypes))
                    {
                        break;
                    }
                }

                int cellIndex = LevelController.Instance.CurrentHole.HoleLayout.FindIndex(holeCell => holeCell.Position == currentPosition);

                if (cellIndex != -1)
                {
                    // Check if the cell contains entities that can be removed by other entities
                    var removableEntities = LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Where(x => x.CanBeRemovedByOtherEntities).ToList();
                    if (removableEntities.Any())
                    {
                        foreach (var entity in removableEntities)
                        {
                            entitiesAtPositions.Add(entity);
                            entityPaths[entity] = new List<Vector2Int>(currentPath);
                        }
                        break; // Stop exploring this path
                    }

                    // Check if there is an obstacle that cannot be passed through
                    bool isBlockedByEntity = LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Any(x => !x.CanBeRemovedByOtherEntities && !x.CanSharePosition);

                    if (isBlockedByEntity)
                    {
                        break; // Stop exploring this path
                    }
                }
            }
        }

        // Filter entities to attack based on TickActionTargetType
        switch (cellEntity.TickActionTargetType)
        {
            case TickActionTargetTypes.Manual:
            case TickActionTargetTypes.NotSameGroupEntity:
                entitiesAtPositions = entitiesAtPositions.Where(entity => entity.EntityGroupType != cellEntity.EntityGroupType).ToList();
                break;
            case TickActionTargetTypes.SameGroupEntity:
                entitiesAtPositions = entitiesAtPositions.Where(entity => entity.EntityGroupType == cellEntity.EntityGroupType && entity != cellEntity).ToList();
                break;
        }

        // Select target entity to attack
        CellEntity selectedTarget = null;
        if (entitiesAtPositions.Count > 0)
        {
            selectedTarget = entitiesAtPositions.OrderBy(cell => cell.TargetedValue).FirstOrDefault();
        }

        // Return the selected target and the path to that target
        if (selectedTarget != null && entityPaths.ContainsKey(selectedTarget))
        {
            return (selectedTarget, entityPaths[selectedTarget]);
        }

        return (null, null);
    }


    //private static List<CellEntity> CalculatePossibleAttacksWithBFS(Hole hole, CellEntity cellEntity, List<GameStats.CellContentTypes> validContentTypes, bool shouldAlternate)
    //{
    //    List<CellEntity> entitiesAtPositions = new List<CellEntity>();
    //    int indexTracker = 0;
    //    int movementRange = cellEntity.MovementRange;

    //    Queue<(Vector2Int position, int distance)> queue = new Queue<(Vector2Int, int)>();
    //    HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

    //    queue.Enqueue((cellEntity.Position, 0));
    //    visited.Add(cellEntity.Position);

    //    while (queue.Count > 0)
    //    {
    //        var (currentPosition, currentDistance) = queue.Dequeue();

    //        foreach (GameStats.DirectionTypes direction in cellEntity.DirectionsCanAttack)
    //        {
    //            Vector2Int directionVector = GetDirectionVector(direction);
    //            Vector2Int nextPosition = currentPosition + directionVector;
    //            int nextDistance = currentDistance + 1;

    //            if (nextDistance > movementRange)
    //            {
    //                continue;
    //            }

    //            if (shouldAlternate && validContentTypes != null)
    //            {
    //                if (!IsPositionValid(cellEntity, hole, nextPosition, validContentTypes, validContentTypes[indexTracker]))
    //                {
    //                    continue;
    //                }

    //                indexTracker = (indexTracker + 1) % validContentTypes.Count;
    //            }
    //            else
    //            {
    //                if (!IsPositionValid(cellEntity, hole, nextPosition, validContentTypes))
    //                {
    //                    continue;
    //                }
    //            }

    //            if (!visited.Contains(nextPosition))
    //            {
    //                visited.Add(nextPosition);

    //                int cellIndex = LevelController.Instance.CurrentHole.HoleLayout.FindIndex(holeCell => holeCell.Position == nextPosition);

    //                if (cellIndex != -1)
    //                {
    //                    // Check if the cell contains entities that can be removed by other entities
    //                    var removableEntities = LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Where(x => x.CanBeRemovedByOtherEntities).ToList();
    //                    if (removableEntities.Any())
    //                    {
    //                        entitiesAtPositions.AddRange(removableEntities);
    //                        continue; // Stop exploring this path
    //                    }

    //                    // Check if there is an obstacle that cannot be passed through
    //                    bool isBlockedByEntity = LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Any(x => !x.CanBeRemovedByOtherEntities && !x.CanSharePosition);

    //                    if (isBlockedByEntity)
    //                    {
    //                        continue; // Stop exploring this path
    //                    }
    //                }

    //                queue.Enqueue((nextPosition, nextDistance));
    //            }
    //        }
    //    }

    //    return entitiesAtPositions;
    //}

    //private static List<CellEntity> CalculatePossibleAttacksWithMinimalDirectionChanges(Hole hole, CellEntity cellEntity, List<GameStats.CellContentTypes> validContentTypes, bool shouldAlternate)
    //{
    //    List<CellEntity> entitiesAtPositions = new List<CellEntity>();
    //    int indexTracker = 0;

    //    foreach (GameStats.DirectionTypes direction in cellEntity.DirectionsCanAttack)
    //    {
    //        Vector2Int currentPosition = cellEntity.Position;
    //        Vector2Int directionVector = GetDirectionVector(direction);
    //        int distanceTraveled = 0;

    //        while (distanceTraveled < cellEntity.MovementRange)
    //        {
    //            currentPosition += directionVector;
    //            distanceTraveled++;

    //            if (shouldAlternate && validContentTypes != null)
    //            {
    //                if (!IsPositionValid(cellEntity, hole, currentPosition, validContentTypes, validContentTypes[indexTracker]))
    //                {
    //                    break;
    //                }

    //                indexTracker = (indexTracker + 1) % validContentTypes.Count;
    //            }
    //            else
    //            {
    //                if (!IsPositionValid(cellEntity, hole, currentPosition, validContentTypes))
    //                {
    //                    break;
    //                }
    //            }

    //            int cellIndex = LevelController.Instance.CurrentHole.HoleLayout.FindIndex(holeCell => holeCell.Position == currentPosition);

    //            if (cellIndex != -1)
    //            {
    //                // Check if the cell contains entities that can be removed by other entities
    //                var removableEntities = LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Where(x => x.CanBeRemovedByOtherEntities).ToList();
    //                if (removableEntities.Any())
    //                {
    //                    entitiesAtPositions.AddRange(removableEntities);
    //                    break; // Stop exploring this path
    //                }

    //                // Check if there is an obstacle that cannot be passed through
    //                bool isBlockedByEntity = LevelController.Instance.CurrentHole.HoleLayout[cellIndex].CellEntities.Any(x => !x.CanBeRemovedByOtherEntities && !x.CanSharePosition);

    //                if (isBlockedByEntity)
    //                {
    //                    break; // Stop exploring this path
    //                }
    //            }
    //        }
    //    }

    //    return entitiesAtPositions;
    //}

    private static (List<Vector2Int> path, bool isUnblocked) CalculatePathWithBFS(Hole hole, CellEntity cellEntity, Vector2Int targetPosition, List<GameStats.CellContentTypes> validContentTypes, bool shouldAlternate)
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

                        if (IsPositionValid(cellEntity, hole, next, validContentTypes, nextContentType))
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
                        if (IsPositionValid(cellEntity, hole, next, validContentTypes))
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

    private static (List<Vector2Int> path, bool isUnblocked) CalculatePathWithMinimalDirectionChanges(Hole hole, CellEntity cellEntity, Vector2Int targetPosition, List<GameStats.CellContentTypes> validContentTypes, bool shouldAlternate)
    {
        var priorityQueue = new SortedSet<(int directionChanges, float distance, Vector2Int position, Vector2Int previousPosition)>(new PathComparer());
        Dictionary<Vector2Int, (Vector2Int previousPosition, int directionChanges)> cameFrom = new Dictionary<Vector2Int, (Vector2Int, int)>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        Vector2Int startPosition = cellEntity.Position;
        priorityQueue.Add((0, Vector2Int.Distance(startPosition, targetPosition), startPosition, startPosition));
        cameFrom[startPosition] = (startPosition, 0);
        visited.Add(startPosition);

        Vector2Int farthestReachable = startPosition;
        float closestDistance = Vector2Int.Distance(startPosition, targetPosition);
        bool isUnblocked = false;

        while (priorityQueue.Count > 0)
        {
            var (currentDirectionChanges, currentDistance, currentPosition, previousPosition) = priorityQueue.Min;
            priorityQueue.Remove(priorityQueue.Min);

            if (currentPosition == targetPosition)
            {
                farthestReachable = currentPosition;
                isUnblocked = true;
                break;
            }

            foreach (GameStats.DirectionTypes direction in cellEntity.DirectionsCanMove)
            {
                Vector2Int directionVector = GetDirectionVector(direction);
                Vector2Int nextPosition = currentPosition + directionVector;
                int newDirectionChanges = cameFrom[currentPosition].directionChanges;

                if (currentPosition != previousPosition && directionVector != (currentPosition - previousPosition))
                {
                    newDirectionChanges++;
                }

                while (IsPositionValid(cellEntity, hole, nextPosition, validContentTypes))
                {
                    if (!visited.Contains(nextPosition))
                    {
                        bool isValid = shouldAlternate && validContentTypes != null
                            ? IsPositionValid(cellEntity, hole, nextPosition, validContentTypes, validContentTypes[newDirectionChanges % validContentTypes.Count])
                            : IsPositionValid(cellEntity, hole, nextPosition, validContentTypes);

                        if (isValid)
                        {
                            priorityQueue.Add((newDirectionChanges, Vector2Int.Distance(nextPosition, targetPosition), nextPosition, currentPosition));
                            cameFrom[nextPosition] = (currentPosition, newDirectionChanges);
                            visited.Add(nextPosition);

                            float distanceToTarget = Vector2Int.Distance(nextPosition, targetPosition);
                            if (distanceToTarget < closestDistance)
                            {
                                farthestReachable = nextPosition;
                                closestDistance = distanceToTarget;
                            }
                        }
                    }

                    nextPosition += directionVector;
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

        while (step != startPosition)
        {
            path.Add(step);
            step = cameFrom[step].previousPosition;
        }

        path.Reverse();

        // Add intermediate positions
        List<Vector2Int> fullPath = new List<Vector2Int>();

        // Adding intermediate positions from start position to the first point in the path
        if (path.Count > 0)
        {
            Vector2Int firstPathPoint = path[0];
            Vector2Int direction = firstPathPoint - startPosition;
            direction = new Vector2Int(Mathf.Clamp(direction.x, -1, 1), Mathf.Clamp(direction.y, -1, 1));

            Vector2Int current = startPosition + direction;
            while (current != firstPathPoint)
            {
                fullPath.Add(current);
                current += direction;
            }
        }

        for (int i = 0; i < path.Count - 1; i++)
        {
            Vector2Int start = path[i];
            Vector2Int end = path[i + 1];
            fullPath.Add(start);

            Vector2Int direction = end - start;
            direction = new Vector2Int(Mathf.Clamp(direction.x, -1, 1), Mathf.Clamp(direction.y, -1, 1));

            Vector2Int current = start + direction;
            while (current != end)
            {
                fullPath.Add(current);
                current += direction;
            }
        }

        // Check to add the last point if path has elements
        if (path.Count > 0)
        {
            fullPath.Add(path[path.Count - 1]);
        }

        return (fullPath, isUnblocked);
    }

    private class PathComparer : IComparer<(int directionChanges, float distance, Vector2Int position, Vector2Int previousPosition)>
    {
        public int Compare((int directionChanges, float distance, Vector2Int position, Vector2Int previousPosition) x, (int directionChanges, float distance, Vector2Int position, Vector2Int previousPosition) y)
        {
            int result = x.directionChanges.CompareTo(y.directionChanges);
            if (result == 0)
            {
                result = x.distance.CompareTo(y.distance);
                if (result == 0)
                {
                    result = x.position.x.CompareTo(y.position.x);
                    if (result == 0)
                    {
                        result = x.position.y.CompareTo(y.position.y);
                    }
                }
            }
            return result;
        }
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

    private static bool IsPositionValid(CellEntity cellEntity, Hole hole, Vector2Int position, List<GameStats.CellContentTypes> validContentTypes, GameStats.CellContentTypes specificContentType = GameStats.CellContentTypes.Empty)
    {
        HoleCell cell = hole.HoleLayout.FirstOrDefault(c => c.Position == position);

        if (cell == null)
        {
            return false;
        }

        // Check if all CellEntities in the cell can share position
        bool allCanSharePosition = cell.CellEntities.All(x => x.CanSharePosition);

        // Check if at least one CellEntity in the cell can be removed by other entities
        bool canBeRemovedByOtherEntities = cell.CellEntities.Any(x => x.CanBeRemovedByOtherEntities);

        if (validContentTypes != null && specificContentType != GameStats.CellContentTypes.Empty)
        {
            return (cell.CellEntities.Count == 0 || allCanSharePosition || (canBeRemovedByOtherEntities && (cellEntity.TickActionType == TickActionTypes.MoveToTargetAndAttackAlongPath || cellEntity.TickActionType == TickActionTypes.PrioritizeAttackOverMove)))
                && validContentTypes.Contains(cell.CellContentType) && cell.CellContentType == specificContentType;
        }
        else if (validContentTypes != null && specificContentType == GameStats.CellContentTypes.Empty)
        {
            return (cell.CellEntities.Count == 0 || allCanSharePosition || (canBeRemovedByOtherEntities && (cellEntity.TickActionType == TickActionTypes.MoveToTargetAndAttackAlongPath || cellEntity.TickActionType == TickActionTypes.PrioritizeAttackOverMove)))
                && validContentTypes.Contains(cell.CellContentType);
        }

        return cell.CellEntities.Count == 0 || allCanSharePosition || (canBeRemovedByOtherEntities && (cellEntity.TickActionType == TickActionTypes.MoveToTargetAndAttackAlongPath || cellEntity.TickActionType == TickActionTypes.PrioritizeAttackOverMove));
    }

    private static Vector2 GetDirection(Vector2Int startPos, Vector2Int endPos)
    {
        Vector2Int difference = endPos - startPos;

        if (Mathf.Abs(difference.x) > Mathf.Abs(difference.y))
        {
            return new Vector2(Mathf.Sign(difference.x), 0); // Horizontal direction
        }

        return new Vector2(0, Mathf.Sign(difference.y)); // Vertical direction
    }
}