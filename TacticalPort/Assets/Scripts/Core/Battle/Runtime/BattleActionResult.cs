#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core
{
    public readonly struct SkillHealthLoss
    {
        public SkillHealthLoss(UnitId pUnitId, int pAmount)
        {
            UnitId = pUnitId;
            Amount = Math.Max(0, pAmount);
        }

        public UnitId UnitId { get; }
        public int Amount { get; }
    }

    public enum HealthLossResolutionKind
    {
        Skill = 0,
        StateTick = 1,
        Glyph = 2,
        Hazard = 3
    }

    public sealed class HealthLossResolutionReport
    {
        public HealthLossResolutionReport(
            UnitId pSourceUnitId,
            Team pSourceTeam,
            HealthLossResolutionKind pKind,
            IReadOnlyList<SkillHealthLoss> pHealthLosses)
        {
            SourceUnitId = pSourceUnitId;
            SourceTeam = pSourceTeam;
            Kind = pKind;
            HealthLosses = pHealthLosses ?? Array.Empty<SkillHealthLoss>();
        }

        public UnitId SourceUnitId { get; }
        public Team SourceTeam { get; }
        public HealthLossResolutionKind Kind { get; }
        public IReadOnlyList<SkillHealthLoss> HealthLosses { get; }

        public int GetHealthLost(UnitId pUnitId)
        {
            int lTotal = 0;
            for (int lIndex = 0; lIndex < HealthLosses.Count; lIndex++)
            {
                if (HealthLosses[lIndex].UnitId == pUnitId)
                    lTotal += HealthLosses[lIndex].Amount;
            }

            return lTotal;
        }
    }

    public sealed class SkillResolutionReport
    {
        public SkillResolutionReport(
            UnitId pActorId,
            SkillDefinition pBaseSkill,
            SkillDefinition pEffectiveSkill,
            IReadOnlyList<SkillHealthLoss> pHealthLosses)
        {
            ActorId = pActorId;
            BaseSkill = pBaseSkill;
            EffectiveSkill = pEffectiveSkill;
            HealthLosses = pHealthLosses ?? Array.Empty<SkillHealthLoss>();
        }

        public UnitId ActorId { get; }
        public SkillDefinition BaseSkill { get; }
        public SkillDefinition EffectiveSkill { get; }
        public bool UsedVariant => BaseSkill != null && EffectiveSkill != null && BaseSkill != EffectiveSkill;
        public IReadOnlyList<SkillHealthLoss> HealthLosses { get; }

        public int GetHealthLost(UnitId pUnitId)
        {
            for (int lIndex = 0; lIndex < HealthLosses.Count; lIndex++)
            {
                if (HealthLosses[lIndex].UnitId == pUnitId)
                    return HealthLosses[lIndex].Amount;
            }

            return 0;
        }
    }

    [Flags]
    public enum BattleActionOutcomeFlags
    {
        None = 0,
        PrimaryDamage = 1 << 0,
        Heal = 1 << 1,
        CollisionDamage = 1 << 2,
        StateApplied = 1 << 3,
        Summon = 1 << 4,
        Glyph = 1 << 5,
        StateDurationReduced = 1 << 6
    }

    public sealed class BattleActionResult
    {
        #region _____________________________| FACTORIES

        public static BattleActionResult Failed(BattleActionType pActionType, string pMessage) =>
            new BattleActionResult(false, pActionType, pMessage, Array.Empty<UnitId>(), Array.Empty<GridCoord>(), BattleActionOutcomeFlags.None);

        public static BattleActionResult Succeeded(
            BattleActionType pActionType,
            string pMessage,
            IEnumerable<UnitId> pAffectedUnitIds = null,
            IEnumerable<GridCoord> pTraversedPath = null,
            BattleActionOutcomeFlags pOutcomeFlags = BattleActionOutcomeFlags.None) =>
            new BattleActionResult(
                true,
                pActionType,
                pMessage,
                pAffectedUnitIds?.ToArray() ?? Array.Empty<UnitId>(),
                pTraversedPath?.ToArray() ?? Array.Empty<GridCoord>(),
                pOutcomeFlags);

        private BattleActionResult(
            bool pIsSuccess,
            BattleActionType pActionType,
            string pMessage,
            IReadOnlyList<UnitId> pAffectedUnitIds,
            IReadOnlyList<GridCoord> pTraversedPath,
            BattleActionOutcomeFlags pOutcomeFlags)
        {
            IsSuccess = pIsSuccess;
            ActionType = pActionType;
            Message = pMessage ?? string.Empty;
            AffectedUnitIds = pAffectedUnitIds;
            TraversedPath = pTraversedPath;
            OutcomeFlags = pOutcomeFlags;
        }

        #endregion

        #region _____________________________/ ACCESSORS

        public bool IsSuccess { get; }
        public BattleActionType ActionType { get; }
        public string Message { get; }
        public IReadOnlyList<UnitId> AffectedUnitIds { get; }
        public IReadOnlyList<GridCoord> TraversedPath { get; }
        public BattleActionOutcomeFlags OutcomeFlags { get; }

        #endregion
    }
}
