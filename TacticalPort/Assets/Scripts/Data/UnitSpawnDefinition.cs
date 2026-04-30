#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Data
#endregion

using System;

namespace TacticalPort.Data
{
    [Serializable]
    public sealed class UnitSpawnDefinition
    {
        #region _____________________________/ VALUES

        public UnitDefinition Unit;
        public SerializableGridCoord StartCoordinate = new SerializableGridCoord(0, 0);

        #endregion

        #region _____________________________| HELPERS

        public UnitSpawnDefinition Clone(UnitDefinition pUnitOverride = null) => new UnitSpawnDefinition
        {
            Unit = pUnitOverride != null ? pUnitOverride : Unit,
            StartCoordinate = new SerializableGridCoord(StartCoordinate.X, StartCoordinate.Y)
        };

        #endregion
    }
}
