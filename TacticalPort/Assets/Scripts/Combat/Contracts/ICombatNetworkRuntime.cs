#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : Network-facing combat runtime contract
#endregion

using TacticalPort.Core;
using TacticalPort.Shared;

namespace TacticalPort.Combat
{
    public interface ICombatNetworkRuntime
    {
        bool IsPlacementPhaseActive { get; }
        BattleActionResult ExecuteLocalCommand(BattleCommand pCommand);
        void ApplyRemoteCommandResult(BattleActionResult pResult);
        bool TryValidatePlacementReadyForSlot(MatchPlayerSlot pSlot, out BattleActionResult pFailure);
        bool TryGetUnitTeam(UnitId pUnitId, out Team pTeam);
        int ComputeStateChecksum();
    }
}
