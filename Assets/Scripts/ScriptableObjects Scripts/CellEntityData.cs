using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New CellEntityData", menuName = "CellEntityData")]
[System.Serializable]
public class CellEntityData : ScriptableObject
{
    public enum EntityTypes
    {
        Stationary,
        Moving
    }

    public enum EntityGoupTypes
    {
        Neutral,
        PlayerGroupA,
        PlayerGroupB,
        RivalGroupA,
        RivalGroupB
    }

    [SerializeField] private GameObject entityObject;
    [SerializeField] private EntityTypes entityType;
    [SerializeField] private EntityGoupTypes entityGroupType;
    [SerializeField] private List<GameStats.DirectionTypes> directionsCanMove = new List<GameStats.DirectionTypes>();
    [SerializeField] private bool canChangeMovementDirection;
    [SerializeField] private List<GameStats.CellContentTypes> cellTypesCanMoveOn = new List<GameStats.CellContentTypes>();
    [SerializeField] private bool shouldAlternateCellTypesWhenMoving;
    [SerializeField] private int movementRange;
    [SerializeField] private bool canSharePosition;
    [SerializeField] private bool canBeRemovedByOtherEntities;
    [SerializeField] private int targetedValue;

    [SerializeField] private TickManager.TickTypes tickType;
    [SerializeField] private int tickOrder;
    [SerializeField] private TickActionModels.TickActionTypes tickActionType;
    [SerializeField] private TickActionModels.TickActionTargetTypes tickActionTargetType;
    [SerializeField] private Vector2Int tickActionManualTarget;
    [SerializeField] private List<GameStats.DirectionTypes> directionsCanAttack = new List<GameStats.DirectionTypes>();

    public GameObject EntityObject => entityObject;
    public EntityTypes EntityType => entityType;
    public EntityGoupTypes EntityGoupType => entityGroupType;
    public List<GameStats.DirectionTypes> DirectionsCanMove => directionsCanMove;
    public bool CanChangeMovementDirection => canChangeMovementDirection;
    public List<GameStats.CellContentTypes> CellTypesCanMoveOn => cellTypesCanMoveOn;
    public bool ShouldAlternateCellTypesWhenMoving => shouldAlternateCellTypesWhenMoving;
    public int MovementRange => movementRange;
    public bool CanSharePosition => canSharePosition;
    public bool CanBeRemovedByOtherEntities => canBeRemovedByOtherEntities;
    public int TargetedValue => targetedValue;

    public TickManager.TickTypes TickType => tickType;
    public int TickOrder => tickOrder;
    public TickActionModels.TickActionTypes TickActionType => tickActionType;
    public TickActionModels.TickActionTargetTypes TickActionTargetType => tickActionTargetType;
    public Vector2Int TickActionManualTarget => tickActionManualTarget;
    public List<GameStats.DirectionTypes> DirectionsCanAttack => directionsCanAttack;
}
