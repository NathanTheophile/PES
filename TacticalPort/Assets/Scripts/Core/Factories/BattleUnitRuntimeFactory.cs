using System;
using TacticalPort.Core.Runtime;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core.Factories
{
    public static class UnitRuntimeFactory
    {
        public static UnitRuntime Create(UnitId pId, UnitSpawnDefinition pSpawn)
        {
            if (pSpawn == null)
                throw new ArgumentNullException(nameof(pSpawn));

            if (pSpawn.Unit == null)
                throw new InvalidOperationException("Spawn definition requires a unit definition.");

            return new UnitRuntime(pId, pSpawn.Unit, pSpawn.StartCoordinate.ToRuntime());
        }
    }
}
