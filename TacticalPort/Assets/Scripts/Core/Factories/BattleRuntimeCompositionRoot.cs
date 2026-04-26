using TacticalPort.Core.Interfaces;
using TacticalPort.Core.Services;

namespace TacticalPort.Core.Factories
{
    public static class BattleRuntimeCompositionRoot
    {
        public static IBattleService CreateDefaultBattleService()
        {
            GridService lGridService = new GridService();
            TurnSystem lTurnSystem = new TurnSystem();
            SimpleSkillExecutor lSkillExecutor = new SimpleSkillExecutor();
            PathService lPathService = new PathService(lGridService);

            return new BattleService(lGridService, lPathService, lTurnSystem, lSkillExecutor);
        }
    }
}
