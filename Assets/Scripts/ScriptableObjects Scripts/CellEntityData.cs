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
    [SerializeField] private List<GameStats.CellContentTypes> cellTypesCanMoveOn = new List<GameStats.CellContentTypes>();
    [SerializeField] private int movementRange;
    [SerializeField] private bool canSharePosition;

    [SerializeField] private TickManager.TickTypes tickType;
    [SerializeField] private int tickOrder;
    [SerializeField] private TickActionModels.TickActionTypes tickActionType;
    [SerializeField] private TickActionModels.TickActionTargetTypes tickActionTargetType;
    [SerializeField] private Vector2Int tickActionManualTarget;

    public GameObject EntityObject => entityObject;
    public EntityTypes EntityType => entityType;
    public EntityGoupTypes EntityGoupType => entityGroupType;
    public List<GameStats.DirectionTypes> DirectionTypesCanMove => directionsCanMove;
    public List<GameStats.CellContentTypes> CellTypesCanMoveOn => cellTypesCanMoveOn;
    public int MovementRange => movementRange;
    public bool CanSharePosition => canSharePosition;

    public TickManager.TickTypes TickType => tickType;
    public int TickOrder => tickOrder;
    public TickActionModels.TickActionTypes TickActionType => tickActionType;
    public TickActionModels.TickActionTargetTypes TickActionTargetType => tickActionTargetType;
    public Vector2Int TickActionManualTarget => tickActionManualTarget;
}
