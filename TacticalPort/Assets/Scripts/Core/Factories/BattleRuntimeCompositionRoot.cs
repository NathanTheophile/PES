#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

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
