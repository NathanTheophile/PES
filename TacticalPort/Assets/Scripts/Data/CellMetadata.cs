#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Data
#endregion

using System;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.Data
{
    [Serializable]
    public sealed class CellMetadata
    {
        #region _____________________________/ VALUES

        public SerializableGridCoord Coordinate = new SerializableGridCoord(0, 0);
        public bool IsWalkable = true;
        public bool BlocksLineOfSight;
        public int MovementCost = 1;
        public bool IsSpawner;
        public UnitDefinition OccupantDefinition;

        #endregion

        #region _____________________________| CONVERT

        public Vector3Int ToCellPosition() => new Vector3Int(Coordinate.X, Coordinate.Y, 0);

        #endregion

        #region _____________________________| BUILD

        public void Sanitize(int pDefaultMovementCost)
        {
            MovementCost = Mathf.Max(1, MovementCost > 0 ? MovementCost : pDefaultMovementCost);
        }

        public bool IsDefault(int pDefaultMovementCost) =>
            IsWalkable
            && !BlocksLineOfSight
            && MovementCost == Mathf.Max(1, pDefaultMovementCost)
            && !IsSpawner
            && OccupantDefinition == null;

        #endregion

        #region _____________________________| HELPERS

        public CellMetadata Clone() => new CellMetadata
        {
            Coordinate = new SerializableGridCoord(Coordinate.X, Coordinate.Y),
            IsWalkable = IsWalkable,
            BlocksLineOfSight = BlocksLineOfSight,
            MovementCost = MovementCost,
            IsSpawner = IsSpawner,
            OccupantDefinition = OccupantDefinition
        };

        #endregion
    }
}
