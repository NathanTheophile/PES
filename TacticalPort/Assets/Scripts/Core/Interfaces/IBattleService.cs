using System.Collections.Generic;
using TacticalPort.Core.Runtime;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core.Interfaces
{
    public interface IBattleService
    {
        BattlePhase Phase { get; }
        BattleOutcome Outcome { get; }
        BattleTurnContext CurrentTurn { get; }
        BattleUnitRuntime ActiveUnit { get; }
        bool HasActiveTurn { get; }
        IReadOnlyCollection<BattleUnitRuntime> Units { get; }

        void Initialize(BattleScenarioDefinition pScenario);
        bool TryStartNextTurn(out BattleTurnContext turnContext);
        IReadOnlyCollection<GridCoord> GetReachableCells(BattleUnitId unitId);
        BattleActionResult ValidateSkill(BattleUnitId unitId, SkillId skillId, SkillTarget target);
        BattleActionResult MoveUnit(BattleUnitId unitId, GridCoord destination);
        BattleActionResult UseSkill(BattleUnitId unitId, SkillId skillId, SkillTarget target);
        BattleActionResult EndTurn(BattleUnitId unitId);
        bool IsUnitActive(BattleUnitId unitId);
        bool TryGetActiveUnit(out BattleUnitRuntime unit);
        bool TryGetUnit(BattleUnitId unitId, out BattleUnitRuntime unit);
    }
}
