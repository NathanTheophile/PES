#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Bootstrap;
using TacticalPort.Core;
using TacticalPort.Shared;

namespace TacticalPort.Combat
{
    public sealed class LocalCombatCommandSink : ICombatCommandSink
    {
        #region _____________________________/ VALUES

        private readonly CombatBootstrap _Bootstrap;

        #endregion

        #region _____________________________| INIT

        public LocalCombatCommandSink(CombatBootstrap pBootstrap)
        {
            _Bootstrap = pBootstrap;
        }

        #endregion

        #region _____________________________/ ACCESSORS

        public bool UsesRemoteAuthority => false;
        public bool CanRunAuthoritativeSimulation => true;
        public bool CanRunEnemyAi => true;

        public bool CanLocallyControlTeam(Team pTeam) => GetLocalRelation(pTeam) == CombatTeamRelation.Own;

        public bool TryGetSlotForTeam(Team pTeam, out MatchPlayerSlot pSlot)
        {
            pSlot = CombatTeamUtility.ToSlot(pTeam);
            return pSlot != MatchPlayerSlot.None;
        }

        public bool TryGetLocalPlayerSlot(out MatchPlayerSlot pSlot)
        {
            pSlot = MatchPlayerSlot.TeamA;
            return true;
        }

        public CombatTeamRelation GetLocalRelation(Team pTeam) =>
            CombatTeamUtility.ResolveRelation(pTeam, MatchPlayerSlot.TeamA);

        #endregion

        #region _____________________________| SUBMIT

        public BattleActionResult Submit(BattleCommand pCommand) =>
            _Bootstrap != null
                ? _Bootstrap.ExecuteLocalCommand(pCommand)
                : BattleActionResult.Failed(pCommand.ActionType, "Combat command sink is not bound to a bootstrap.");

        #endregion
    }
}
