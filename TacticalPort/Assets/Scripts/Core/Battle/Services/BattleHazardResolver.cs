#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Shared;

namespace TacticalPort.Core
{
    internal static class BattleHazardResolver
    {
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
        {
            if (pGridService == null || pActiveUnit == null || !pActiveUnit.IsAlive)
                return;

            HashSet<string> lProcessedGlyphGroups = new HashSet<string>();
            foreach (GridCoord lCell in pGridService.GetOccupiedCells(pActiveUnit.Id))
            {
                foreach (GridGlyphRuntime lGlyph in pGridService.GetGlyphsAt(lCell))
                {
                    if (lGlyph == null || !lGlyph.CanAffect(pActiveUnit))
                        continue;

                    string lGlyphGroupId = string.IsNullOrWhiteSpace(lGlyph.GlyphGroupId)
                        ? $"{lGlyph.SourceSkillId}:{lGlyph.Cell.X}:{lGlyph.Cell.Y}"
                        : lGlyph.GlyphGroupId;
                    if (!lProcessedGlyphGroups.Add(lGlyphGroupId))
                        continue;

                    pActiveUnit.ApplyDamage(lGlyph.Power);
                }
            }
        }

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
