using System;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.Data
{
    [Serializable]
    public sealed class TilemapBoardCellMetadata
    {
        public SerializableGridCoord Coordinate = new SerializableGridCoord(0, 0);
        public bool IsWalkable = true;
        public bool BlocksLineOfSight;
        public int MovementCost = 1;
        public BattleUnitDefinition OccupantDefinition;

        public Vector3Int ToCellPosition() => new Vector3Int(Coordinate.X, Coordinate.Y, 0);

        public void Sanitize(int pDefaultMovementCost)
        {
            MovementCost = Mathf.Max(1, MovementCost > 0 ? MovementCost : pDefaultMovementCost);
        }

        public bool IsDefault(int pDefaultMovementCost)
        {
            return IsWalkable
                && !BlocksLineOfSight
                && MovementCost == Mathf.Max(1, pDefaultMovementCost)
                && OccupantDefinition == null;
        }

        public TilemapBoardCellMetadata Clone()
        {
            return new TilemapBoardCellMetadata
            {
                Coordinate = new SerializableGridCoord(Coordinate.X, Coordinate.Y),
                IsWalkable = IsWalkable,
                BlocksLineOfSight = BlocksLineOfSight,
                MovementCost = MovementCost,
                OccupantDefinition = OccupantDefinition
            };
        }
    }
}
