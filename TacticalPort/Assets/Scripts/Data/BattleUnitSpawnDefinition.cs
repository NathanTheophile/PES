using System;

namespace TacticalPort.Data
{
    [Serializable]
    public sealed class BattleUnitSpawnDefinition
    {
        #region _____________________________| VALUES

        public BattleUnitDefinition Unit;
        public SerializableGridCoord StartCoordinate = new SerializableGridCoord(0, 0);

        #endregion

        #region _____________________________| HELPERS

        public BattleUnitSpawnDefinition Clone(BattleUnitDefinition pUnitOverride = null) => new BattleUnitSpawnDefinition
        {
            Unit = pUnitOverride != null ? pUnitOverride : Unit,
            StartCoordinate = new SerializableGridCoord(StartCoordinate.X, StartCoordinate.Y)
        };

        #endregion
    }
}
