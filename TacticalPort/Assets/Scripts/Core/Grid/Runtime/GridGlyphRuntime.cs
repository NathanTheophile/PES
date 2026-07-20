#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core
{
    public sealed class GridGlyphRuntime
    {
        public GridGlyphRuntime(
            UnitId pSourceUnitId,
            Team pSourceTeam,
            GridCoord pCell,
            int pPower,
            int pRemainingTurns,
            SkillGlyphTargetRule pTargetRule,
            string pSourceSkillId,
            string pGlyphGroupId = null,
            StateDefinition pAppliedState = null,
            int pAppliedStateStacks = 1,
            int pAppliedStateDurationTurns = -1)
        {
            SourceUnitId = pSourceUnitId;
            SourceTeam = pSourceTeam;
            Cell = pCell;
            Power = pPower < 0 ? 0 : pPower;
            RemainingTurns = pRemainingTurns <= 0 ? 1 : pRemainingTurns;
            TargetRule = pTargetRule;
            SourceSkillId = pSourceSkillId ?? string.Empty;
            GlyphGroupId = string.IsNullOrWhiteSpace(pGlyphGroupId) ? SourceSkillId : pGlyphGroupId;
            AppliedState = pAppliedState;
            AppliedStateStacks = pAppliedStateStacks < 1 ? 1 : pAppliedStateStacks;
            AppliedStateDurationTurns = pAppliedStateDurationTurns;
        }

        public GridGlyphRuntime(
            UnitId pSourceUnitId,
            Team pSourceTeam,
            GridCoord pCell,
            GlyphDefinition pDefinition,
            int pRemainingTurns,
            string pSourceSkillId,
            string pGlyphGroupId,
            UnitId pLifetimeSourceUnitId = default)
        {
            SourceUnitId = pSourceUnitId;
            SourceTeam = pSourceTeam;
            Cell = pCell;
            Definition = pDefinition;
            RemainingTurns = pLifetimeSourceUnitId.IsValid ? 0 : (pRemainingTurns <= 0 ? 1 : pRemainingTurns);
            SourceSkillId = pSourceSkillId ?? string.Empty;
            GlyphGroupId = string.IsNullOrWhiteSpace(pGlyphGroupId) ? SourceSkillId : pGlyphGroupId;
            LifetimeSourceUnitId = pLifetimeSourceUnitId;
            AppliedStateStacks = 1;
            AppliedStateDurationTurns = -1;
        }

        public UnitId SourceUnitId { get; }
        public Team SourceTeam { get; }
        public GridCoord Cell { get; }
        public int Power { get; }
        public int RemainingTurns { get; private set; }
        public SkillGlyphTargetRule TargetRule { get; }
        public string SourceSkillId { get; }
        public string GlyphGroupId { get; }
        public StateDefinition AppliedState { get; }
        public int AppliedStateStacks { get; }
        public int AppliedStateDurationTurns { get; }
        public GlyphDefinition Definition { get; }
        public UnitId LifetimeSourceUnitId { get; }
        public bool IsSourceBound => LifetimeSourceUnitId.IsValid;
        public GlyphTriggerTiming TriggerTiming => Definition != null ? Definition.TriggerTiming : GlyphTriggerTiming.TurnStart;
        public bool IsExpired => !IsSourceBound && RemainingTurns <= 0;

        public bool CanAffect(UnitRuntime pUnit)
        {
            if (pUnit == null || !pUnit.IsAlive)
                return false;

            if (Definition != null)
            {
                bool lIsSummon = pUnit.OwnerUnitId.IsValid;
                return Definition.TargetUnitType switch
                {
                    GlyphTargetUnitType.CharactersOnly => !lIsSummon,
                    GlyphTargetUnitType.SummonsOnly => lIsSummon,
                    _ => true
                };
            }

            switch (TargetRule)
            {
                case SkillGlyphTargetRule.AlliesOnly:
                    return pUnit.Team == SourceTeam;

                case SkillGlyphTargetRule.EnemiesOnly:
                    return pUnit.Team != SourceTeam;

                default:
                    return true;
            }
        }

        public void AdvanceTurn()
        {
            if (!IsSourceBound && RemainingTurns > 0)
                RemainingTurns--;
        }

        public IReadOnlyList<GlyphEffectDefinition> GetEffectsFor(UnitRuntime pUnit)
        {
            if (Definition == null || pUnit == null)
                return System.Array.Empty<GlyphEffectDefinition>();

            if (pUnit.Team == SourceTeam)
                return Definition.AllyEffects;

            bool lAreOpponents = (SourceTeam == Team.TeamA && pUnit.Team == Team.TeamB)
                || (SourceTeam == Team.TeamB && pUnit.Team == Team.TeamA);
            return lAreOpponents ? Definition.EnemyEffects : Definition.NeutralEffects;
        }
    }

    public sealed class TelegraphedHazardRuntime
    {
        #region _____________________________| INIT

        public TelegraphedHazardRuntime(
            string pId,
            UnitId pSourceUnitId,
            Team pSourceTeam,
            IReadOnlyCollection<GridCoord> pCells,
            int pPower,
            int pRemainingTurns,
            SkillGlyphTargetRule pTargetRule,
            string pSourceSkillId = null)
        {
            Id = string.IsNullOrWhiteSpace(pId) ? System.Guid.NewGuid().ToString("N") : pId;
            SourceUnitId = pSourceUnitId;
            SourceTeam = pSourceTeam;
            Cells = pCells != null ? new List<GridCoord>(pCells).AsReadOnly() : new List<GridCoord>().AsReadOnly();
            Power = pPower < 0 ? 0 : pPower;
            RemainingTurns = pRemainingTurns <= 0 ? 1 : pRemainingTurns;
            TargetRule = pTargetRule;
            SourceSkillId = pSourceSkillId ?? string.Empty;
        }

        #endregion

        #region _____________________________/ ACCESSORS

        public string Id { get; }
        public UnitId SourceUnitId { get; }
        public Team SourceTeam { get; }
        public IReadOnlyList<GridCoord> Cells { get; }
        public int Power { get; }
        public int RemainingTurns { get; private set; }
        public SkillGlyphTargetRule TargetRule { get; }
        public string SourceSkillId { get; }
        public bool IsReady => RemainingTurns <= 0;

        #endregion

        #region _____________________________| HELPERS

        public bool CanAffect(UnitRuntime pUnit)
        {
            if (pUnit == null || !pUnit.IsAlive)
                return false;

            switch (TargetRule)
            {
                case SkillGlyphTargetRule.AlliesOnly:
                    return pUnit.Team == SourceTeam;

                case SkillGlyphTargetRule.EnemiesOnly:
                    return pUnit.Team != SourceTeam;

                default:
                    return true;
            }
        }

        public void AdvanceTurn()
        {
            if (RemainingTurns > 0)
                RemainingTurns--;
        }

        #endregion
    }
}
