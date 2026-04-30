#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Data
#endregion

using System;

namespace TacticalPort.Data
{
    [Serializable]
    public sealed class CellDefinition
    {
        #region _____________________________/ VALUES

        public SerializableGridCoord Coordinate = new SerializableGridCoord(0, 0);
        public bool IsWalkable = true;
        public bool BlocksLineOfSight;
        public int MovementCost = 1;

        #endregion

        #region _____________________________| HELPERS

        public CellDefinition Clone() => new CellDefinition
        {
            Coordinate = new SerializableGridCoord(Coordinate.X, Coordinate.Y),
            IsWalkable = IsWalkable,
            BlocksLineOfSight = BlocksLineOfSight,
            MovementCost = MovementCost
        };

        #endregion
    }
}
