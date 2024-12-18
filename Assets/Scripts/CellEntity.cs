using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class CellEntity
{
    public CellEntityData.EntityTypes EntityType;
    public CellEntityData.EntityGoupTypes EntityGroupType;
    public List<GameStats.DirectionTypes> DirectionsCanMove = new List<GameStats.DirectionTypes>();
    public bool CanChangeMovementDirection;
    public List<GameStats.CellContentTypes> CellTypesCanMoveOn = new List<GameStats.CellContentTypes>();
    public bool ShouldAlternateCellTypesWhenMoving;
    public int MovementRange;
    public bool CanSharePosition;
    public bool CanBeRemovedByOtherEntities;
    public int TargetedValue;

    public TickManager.TickTypes TickType;
    public int TickOrder;
    public TickActionModels.TickActionTypes TickActionType;
    public TickActionModels.TickActionTargetTypes TickActionTargetType;
    public Vector2Int TickActionManualTarget;
    public List<GameStats.DirectionTypes> DirectionsCanAttack;

    public GameObject EntityObject;
    public System.Func<CellEntity, IEnumerator> TickAction;
    public bool CanPerformTickAction;
    public int NumberOfTickActionsPerformed;
    public int NumberOfTickActionsFailed;

    public Vector2Int Position;
    public List<Vector2Int> TargetPositions;
    public List<Vector2Int> CurrentPath = new List<Vector2Int>();

    public CellEntity(CellEntityData cellEntityData)
    {
        EntityType = cellEntityData.EntityType;
        EntityGroupType = cellEntityData.EntityGoupType;
        DirectionsCanMove = new List<GameStats.DirectionTypes>(cellEntityData.DirectionsCanMove);
        CanChangeMovementDirection = cellEntityData.CanChangeMovementDirection;
        CellTypesCanMoveOn = new List<GameStats.CellContentTypes>(cellEntityData.CellTypesCanMoveOn);
        ShouldAlternateCellTypesWhenMoving = cellEntityData.ShouldAlternateCellTypesWhenMoving;
        MovementRange = cellEntityData.MovementRange;
        CanSharePosition = cellEntityData.CanSharePosition;
        CanBeRemovedByOtherEntities = cellEntityData.CanBeRemovedByOtherEntities;
        TargetedValue = cellEntityData.TargetedValue;

        TickType = cellEntityData.TickType;
        TickOrder = cellEntityData.TickOrder;
        TickActionType = cellEntityData.TickActionType;
        TickActionTargetType = cellEntityData.TickActionTargetType;
        TickActionManualTarget = cellEntityData.TickActionManualTarget;
        DirectionsCanAttack = cellEntityData.DirectionsCanAttack;
    }
}
