#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Core;
using TacticalPort.Shared;

namespace TacticalPort.Combat
{
    public interface ICombatCommandSink
    {
        bool UsesRemoteAuthority { get; }
        bool CanRunAuthoritativeSimulation { get; }
        bool CanRunEnemyAi { get; }
        bool CanLocallyControlTeam(Team pTeam);
        bool TryGetSlotForTeam(Team pTeam, out MatchPlayerSlot pSlot);
        bool TryGetLocalPlayerSlot(out MatchPlayerSlot pSlot);
        CombatTeamRelation GetLocalRelation(Team pTeam);
        BattleActionResult Submit(BattleCommand pCommand);
    }
}
