#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core
{
    public interface IBattleService
    {
        BattlePhase Phase { get; }
        BattleOutcome Outcome { get; }
        BattleTurnContext CurrentTurn { get; }
        UnitRuntime ActiveUnit { get; }
        bool HasActiveTurn { get; }
        IReadOnlyCollection<UnitRuntime> Units { get; }
        IReadOnlyList<UnitRuntime> TurnOrder { get; }
        event Action<BattleTurnContext> TurnStarted;
        event Action<UnitId> TurnEnded;
        event Action<BattleActionResult> SkillUsed;
        event Action<TelegraphedHazardRuntime> HazardScheduled;
        event Action<BattleActionResult> HazardsResolved;

        void Initialize(BattleScenarioDefinition pScenario, Team pPerfectVelocityTieStartingTeam = Team.TeamA);
        void EnterPlacementPhase();
        bool TryStartNextTurn(out BattleTurnContext turnContext);
        BattleActionResult RepositionUnitDuringPlacement(UnitId unitId, GridCoord destination);
        IReadOnlyCollection<GridCoord> GetReachableCells(UnitId unitId);
        BattleActionResult ValidateSkill(UnitId unitId, SkillId skillId, SkillTarget target);
        BattleActionResult MoveUnit(UnitId unitId, GridCoord destination);
        BattleActionResult UseSkill(UnitId unitId, SkillId skillId, SkillTarget target);
        BattleActionResult EndTurn(UnitId unitId);
        bool IsUnitActive(UnitId unitId);
        bool IsInside(GridCoord coordinate);
        bool BlocksLineOfSight(GridCoord coordinate);
        bool TryGetActiveUnit(out UnitRuntime unit);
        bool TryGetUnit(UnitId unitId, out UnitRuntime unit);
        IReadOnlyCollection<GridGlyphRuntime> GetActiveGlyphs();
        IReadOnlyCollection<TelegraphedHazardRuntime> GetTelegraphedHazards();
        bool TryScheduleHazard(TelegraphedHazardRuntime hazard);
        BattleActionResult ResolveTelegraphedHazards();
    }
}
