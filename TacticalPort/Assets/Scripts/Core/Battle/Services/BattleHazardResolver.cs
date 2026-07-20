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
    internal static class BattleHazardResolver
    {
        private const string GLYPH_PRESENCE_STATE_PREFIX = "glyph-presence::";

        public static bool TryScheduleHazard(
            IGridService pGridService,
            ICollection<TelegraphedHazardRuntime> pHazards,
            TelegraphedHazardRuntime pHazard)
        {
            if (pGridService == null || pHazards == null || pHazard == null || pHazard.Cells == null || pHazard.Cells.Count == 0)
                return false;

            bool lHasInsideCell = false;
            for (int lIndex = 0; lIndex < pHazard.Cells.Count; lIndex++)
            {
                if (pGridService.IsInside(pHazard.Cells[lIndex]))
                {
                    lHasInsideCell = true;
                    break;
                }
            }

            if (!lHasInsideCell)
                return false;

            pHazards.Add(pHazard);
            return true;
        }

        public static BattleActionResult ResolveTelegraphedHazards(
            IGridService pGridService,
            IReadOnlyDictionary<UnitId, UnitRuntime> pUnitsById,
            IList<TelegraphedHazardRuntime> pHazards,
            out bool pResolvedAny)
        {
            pResolvedAny = false;

            if (pHazards == null || pHazards.Count == 0)
                return BattleActionResult.Succeeded(BattleActionType.Hazard, "No telegraphed hazard resolved.");

            HashSet<UnitId> lAffectedUnitIds = new HashSet<UnitId>();
            int lResolvedHazards = 0;
            int lTotalDamage = 0;

            for (int lHazardIndex = pHazards.Count - 1; lHazardIndex >= 0; lHazardIndex--)
            {
                TelegraphedHazardRuntime lHazard = pHazards[lHazardIndex];
                if (lHazard == null)
                {
                    pHazards.RemoveAt(lHazardIndex);
                    continue;
                }

                lHazard.AdvanceTurn();
                if (!lHazard.IsReady)
                    continue;

                lResolvedHazards++;
                lTotalDamage += ApplyTelegraphedHazard(pGridService, pUnitsById, lHazard, lAffectedUnitIds);
                pHazards.RemoveAt(lHazardIndex);
            }

            string lMessage = lResolvedHazards > 0
                ? $"Resolved {lResolvedHazards} telegraphed hazard(s) for {lTotalDamage} damage."
                : "No telegraphed hazard resolved.";

            pResolvedAny = lResolvedHazards > 0;
            return BattleActionResult.Succeeded(BattleActionType.Hazard, lMessage, lAffectedUnitIds);
        }

        public static void ApplyStartTurnGlyphs(IGridService pGridService, UnitRuntime pActiveUnit)
            => ApplyTriggeredGlyphs(pGridService, pActiveUnit, GlyphTriggerTiming.TurnStart);

        public static void ApplyEndTurnGlyphs(IGridService pGridService, UnitRuntime pActiveUnit)
            => ApplyTriggeredGlyphs(pGridService, pActiveUnit, GlyphTriggerTiming.TurnEnd);

        public static void RefreshPersistentPresenceGlyphs(IGridService pGridService, IEnumerable<UnitRuntime> pUnits)
        {
            if (pGridService == null || pUnits == null)
                return;

            List<UnitRuntime> lUnits = new List<UnitRuntime>();
            foreach (UnitRuntime lUnit in pUnits)
            {
                if (lUnit != null)
                    lUnits.Add(lUnit);
            }

            lUnits.Sort((pLeft, pRight) => pLeft.Id.Value.CompareTo(pRight.Id.Value));
            for (int lUnitIndex = 0; lUnitIndex < lUnits.Count; lUnitIndex++)
                RefreshPersistentPresenceGlyphs(pGridService, lUnits[lUnitIndex]);
        }

        private static void ApplyTriggeredGlyphs(
            IGridService pGridService,
            UnitRuntime pActiveUnit,
            GlyphTriggerTiming pTiming)
        {
            if (pGridService == null || pActiveUnit == null || !pActiveUnit.IsAlive)
                return;

            HashSet<string> lProcessedGlyphGroups = new HashSet<string>();
            foreach (GridCoord lCell in pGridService.GetOccupiedCells(pActiveUnit.Id))
            {
                foreach (GridGlyphRuntime lGlyph in pGridService.GetGlyphsAt(lCell))
                {
                    if (lGlyph == null || lGlyph.TriggerTiming != pTiming || !lGlyph.CanAffect(pActiveUnit))
                        continue;

                    string lGlyphGroupId = ResolveGroupId(lGlyph);
                    if (!lProcessedGlyphGroups.Add(lGlyphGroupId))
                        continue;

                    ApplyGlyphEffects(lGlyph, pActiveUnit);
                }
            }
        }

        private static void RefreshPersistentPresenceGlyphs(IGridService pGridService, UnitRuntime pUnit)
        {
            HashSet<string> lDesiredStateKeys = new HashSet<string>();
            if (pUnit.IsAlive)
            {
                HashSet<string> lProcessedGroups = new HashSet<string>();
                foreach (GridCoord lCell in pGridService.GetOccupiedCells(pUnit.Id))
                {
                    foreach (GridGlyphRuntime lGlyph in pGridService.GetGlyphsAt(lCell))
                    {
                        if (lGlyph?.Definition == null
                            || lGlyph.TriggerTiming != GlyphTriggerTiming.PersistentPresence
                            || !lGlyph.CanAffect(pUnit))
                        {
                            continue;
                        }

                        string lGroupId = ResolveGroupId(lGlyph);
                        if (!lProcessedGroups.Add(lGroupId))
                            continue;

                        IReadOnlyList<GlyphEffectDefinition> lEffects = lGlyph.GetEffectsFor(pUnit);
                        for (int lEffectIndex = 0; lEffectIndex < lEffects.Count; lEffectIndex++)
                        {
                            GlyphEffectDefinition lEffect = lEffects[lEffectIndex];
                            if (lEffect?.Type != GlyphEffectType.ApplyState || lEffect.State == null)
                                continue;

                            string lStateKey = $"{GLYPH_PRESENCE_STATE_PREFIX}{lGroupId}::{lEffectIndex}";
                            lDesiredStateKeys.Add(lStateKey);
                            pUnit.SetPersistentState(
                                lStateKey,
                                lEffect.State,
                                lEffect.StateStacks,
                                lGlyph.SourceUnitId,
                                lGlyph.SourceTeam,
                                lGlyph.Definition.Id);
                        }
                    }
                }
            }

            List<string> lKeysToRemove = new List<string>();
            foreach (BattleStateRuntime lState in pUnit.ActiveStates)
            {
                if (lState != null
                    && lState.Key.StartsWith(GLYPH_PRESENCE_STATE_PREFIX, System.StringComparison.Ordinal)
                    && !lDesiredStateKeys.Contains(lState.Key))
                {
                    lKeysToRemove.Add(lState.Key);
                }
            }

            lKeysToRemove.Sort(System.StringComparer.Ordinal);
            for (int lIndex = 0; lIndex < lKeysToRemove.Count; lIndex++)
                pUnit.RemoveStateByKey(lKeysToRemove[lIndex]);
        }

        private static void ApplyGlyphEffects(GridGlyphRuntime pGlyph, UnitRuntime pTarget)
        {
            if (pGlyph.Definition == null)
            {
                pTarget.ApplyDamage(pGlyph.Power);
                if (pGlyph.AppliedState != null)
                    pTarget.TryApplyState(
                        pGlyph.AppliedState,
                        pGlyph.AppliedStateStacks,
                        pGlyph.AppliedStateDurationTurns,
                        pGlyph.SourceUnitId,
                        pGlyph.SourceTeam,
                        pGlyph.SourceSkillId);
                return;
            }

            IReadOnlyList<GlyphEffectDefinition> lEffects = pGlyph.GetEffectsFor(pTarget);
            for (int lIndex = 0; lIndex < lEffects.Count; lIndex++)
            {
                GlyphEffectDefinition lEffect = lEffects[lIndex];
                if (lEffect == null)
                    continue;

                switch (lEffect.Type)
                {
                    case GlyphEffectType.Damage:
                        pTarget.ApplyDamage(lEffect.Power);
                        break;

                    case GlyphEffectType.Heal:
                        pTarget.RestoreHealth(lEffect.Power);
                        break;

                    case GlyphEffectType.ApplyState when lEffect.State != null:
                        pTarget.TryApplyState(
                            lEffect.State,
                            lEffect.StateStacks,
                            lEffect.StateDurationTurns,
                            pGlyph.SourceUnitId,
                            pGlyph.SourceTeam,
                            pGlyph.Definition.Id);
                        break;
                }
            }
        }

        private static string ResolveGroupId(GridGlyphRuntime pGlyph) =>
            string.IsNullOrWhiteSpace(pGlyph.GlyphGroupId)
                ? $"{pGlyph.SourceSkillId}:{pGlyph.Cell.X}:{pGlyph.Cell.Y}"
                : pGlyph.GlyphGroupId;

        private static int ApplyTelegraphedHazard(
            IGridService pGridService,
            IReadOnlyDictionary<UnitId, UnitRuntime> pUnitsById,
            TelegraphedHazardRuntime pHazard,
            ISet<UnitId> pAffectedUnitIds)
        {
            if (pGridService == null || pUnitsById == null || pHazard == null || pHazard.Cells == null)
                return 0;

            int lTotalDamage = 0;
            HashSet<UnitId> lProcessedUnits = new HashSet<UnitId>();
            for (int lIndex = 0; lIndex < pHazard.Cells.Count; lIndex++)
            {
                GridCoord lCell = pHazard.Cells[lIndex];
                if (!pGridService.IsInside(lCell)
                    || !pGridService.TryGetOccupant(lCell, out UnitId lUnitId)
                    || !lProcessedUnits.Add(lUnitId)
                    || !pUnitsById.TryGetValue(lUnitId, out UnitRuntime lUnit)
                    || !pHazard.CanAffect(lUnit))
                {
                    continue;
                }

                int lDamage = lUnit.ApplyDamage(pHazard.Power);
                if (lDamage <= 0)
                    continue;

                lTotalDamage += lDamage;
                pAffectedUnitIds?.Add(lUnit.Id);
            }

            return lTotalDamage;
        }
    }
}
