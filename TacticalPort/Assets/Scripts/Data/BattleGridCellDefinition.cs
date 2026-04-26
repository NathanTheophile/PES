using System;

namespace TacticalPort.Data
{
    [Serializable]
    public sealed class BattleGridCellDefinition
    {
        #region _____________________________| VALUES

        public SerializableGridCoord Coordinate = new SerializableGridCoord(0, 0);
        public bool IsWalkable = true;
        public int MovementCost = 1;

        #endregion

        #region _____________________________| HELPERS

        public BattleGridCellDefinition Clone() => new BattleGridCellDefinition
        {
            Coordinate = new SerializableGridCoord(Coordinate.X, Coordinate.Y),
            IsWalkable = IsWalkable,
            MovementCost = MovementCost
        };

        #endregion
    }
}
