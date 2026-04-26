using System;
using TacticalPort.Core.Runtime;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core.Factories
{
    public static class BattleUnitRuntimeFactory
    {
        public static BattleUnitRuntime Create(BattleUnitId pId, BattleUnitSpawnDefinition pSpawn)
        {
            if (pSpawn == null)
                throw new ArgumentNullException(nameof(pSpawn));

            if (pSpawn.Unit == null)
                throw new InvalidOperationException("Spawn definition requires a unit definition.");

            return new BattleUnitRuntime(pId, pSpawn.Unit, pSpawn.StartCoordinate.ToRuntime());
        }
    }
}
