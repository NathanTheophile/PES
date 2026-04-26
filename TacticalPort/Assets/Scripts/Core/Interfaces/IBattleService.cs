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
        UnitRuntime ActiveUnit { get; }
        bool HasActiveTurn { get; }
        IReadOnlyCollection<UnitRuntime> Units { get; }

        void Initialize(BattleScenarioDefinition pScenario);
        bool TryStartNextTurn(out BattleTurnContext turnContext);
        IReadOnlyCollection<GridCoord> GetReachableCells(UnitId unitId);
        BattleActionResult ValidateSkill(UnitId unitId, SkillId skillId, SkillTarget target);
        BattleActionResult MoveUnit(UnitId unitId, GridCoord destination);
        BattleActionResult UseSkill(UnitId unitId, SkillId skillId, SkillTarget target);
        BattleActionResult EndTurn(UnitId unitId);
        bool IsUnitActive(UnitId unitId);
        bool TryGetActiveUnit(out UnitRuntime unit);
        bool TryGetUnit(UnitId unitId, out UnitRuntime unit);
    }
}
