#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Core;
using TacticalPort.Shared;

namespace TacticalPort.Networking
{
    public static class CombatStateChecksum
    {
        #region _____________________________| COMPUTE

        public static int Compute(IBattleService pBattleService)
        {
            if (pBattleService == null)
                return 0;

            unchecked
            {
                int lHash = 17;
                lHash = Mix(lHash, (int)pBattleService.Phase);
                lHash = Mix(lHash, (int)pBattleService.Outcome);
                lHash = Mix(lHash, pBattleService.CurrentTurn != null ? pBattleService.CurrentTurn.RoundIndex : 0);
                lHash = Mix(lHash, pBattleService.CurrentTurn != null ? pBattleService.CurrentTurn.TurnIndex : 0);
                lHash = Mix(lHash, pBattleService.CurrentTurn != null ? pBattleService.CurrentTurn.UnitId.Value : 0);

                List<UnitRuntime> lUnits = new List<UnitRuntime>(pBattleService.Units);
                lUnits.Sort((pLeft, pRight) => GetUnitIdValue(pLeft).CompareTo(GetUnitIdValue(pRight)));

                foreach (UnitRuntime lUnit in lUnits)
                    MixUnit(ref lHash, lUnit);

                List<GridGlyphRuntime> lGlyphs = new List<GridGlyphRuntime>(pBattleService.GetActiveGlyphs());
                lGlyphs.Sort((pLeft, pRight) => CompareGlyphs(pLeft, pRight));
                foreach (GridGlyphRuntime lGlyph in lGlyphs)
                    MixGlyph(ref lHash, lGlyph);

                List<TelegraphedHazardRuntime> lHazards = new List<TelegraphedHazardRuntime>(pBattleService.GetTelegraphedHazards());
                lHazards.Sort((pLeft, pRight) => CompareHazards(pLeft, pRight));
                foreach (TelegraphedHazardRuntime lHazard in lHazards)
                    MixHazard(ref lHash, lHazard);

                return lHash;
            }
        }

        #endregion

        #region _____________________________| HELPERS

        private static void MixUnit(ref int pHash, UnitRuntime pUnit)
        {
            if (pUnit == null)
                return;

            pHash = Mix(pHash, pUnit.Id.Value);
            pHash = Mix(pHash, (int)pUnit.Team);
            pHash = Mix(pHash, pUnit.Position.X);
            pHash = Mix(pHash, pUnit.Position.Y);
            pHash = Mix(pHash, pUnit.FacingDirection.X);
            pHash = Mix(pHash, pUnit.FacingDirection.Y);
            pHash = Mix(pHash, pUnit.CurrentHealth);
            pHash = Mix(pHash, pUnit.RemainingMovement);
            pHash = Mix(pHash, pUnit.RemainingActionPoints);
            pHash = Mix(pHash, pUnit.IsAlive ? 1 : 0);

            foreach (BattleStateRuntime lState in pUnit.ActiveStates)
                MixState(ref pHash, lState);
        }

        private static void MixState(ref int pHash, BattleStateRuntime pState)
        {
            if (pState == null)
                return;

            pHash = Mix(pHash, StableStringHash(pState.Key));
            pHash = Mix(pHash, StableStringHash(pState.Definition != null ? pState.Definition.Id : string.Empty));
            pHash = Mix(pHash, pState.Stacks);
            pHash = Mix(pHash, pState.RemainingTurns);
            pHash = Mix(pHash, pState.IsPersistent ? 1 : 0);
        }

        private static void MixGlyph(ref int pHash, GridGlyphRuntime pGlyph)
        {
            if (pGlyph == null)
                return;

            pHash = Mix(pHash, pGlyph.SourceUnitId.Value);
            pHash = Mix(pHash, (int)pGlyph.SourceTeam);
            pHash = Mix(pHash, pGlyph.Cell.X);
            pHash = Mix(pHash, pGlyph.Cell.Y);
            pHash = Mix(pHash, pGlyph.Power);
            pHash = Mix(pHash, pGlyph.RemainingTurns);
            pHash = Mix(pHash, (int)pGlyph.TargetRule);
            pHash = Mix(pHash, StableStringHash(pGlyph.SourceSkillId));
            pHash = Mix(pHash, StableStringHash(pGlyph.GlyphGroupId));
        }

        private static void MixHazard(ref int pHash, TelegraphedHazardRuntime pHazard)
        {
            if (pHazard == null)
                return;

            pHash = Mix(pHash, StableStringHash(pHazard.Id));
            pHash = Mix(pHash, pHazard.SourceUnitId.Value);
            pHash = Mix(pHash, (int)pHazard.SourceTeam);
            pHash = Mix(pHash, pHazard.Power);
            pHash = Mix(pHash, pHazard.RemainingTurns);
            pHash = Mix(pHash, (int)pHazard.TargetRule);
            pHash = Mix(pHash, StableStringHash(pHazard.SourceSkillId));

            List<GridCoord> lCells = new List<GridCoord>(pHazard.Cells);
            lCells.Sort(CompareCells);
            foreach (GridCoord lCell in lCells)
            {
                pHash = Mix(pHash, lCell.X);
                pHash = Mix(pHash, lCell.Y);
            }
        }

        private static int Mix(int pHash, int pValue) => pHash * 31 + pValue;

        private static int GetUnitIdValue(UnitRuntime pUnit) => pUnit != null ? pUnit.Id.Value : 0;

        private static int CompareGlyphs(GridGlyphRuntime pLeft, GridGlyphRuntime pRight)
        {
            int lResult = CompareStrings(pLeft != null ? pLeft.GlyphGroupId : string.Empty, pRight != null ? pRight.GlyphGroupId : string.Empty);
            if (lResult != 0)
                return lResult;

            return CompareCells(pLeft != null ? pLeft.Cell : default, pRight != null ? pRight.Cell : default);
        }

        private static int CompareHazards(TelegraphedHazardRuntime pLeft, TelegraphedHazardRuntime pRight) =>
            CompareStrings(pLeft != null ? pLeft.Id : string.Empty, pRight != null ? pRight.Id : string.Empty);

        private static int CompareCells(GridCoord pLeft, GridCoord pRight)
        {
            int lResult = pLeft.X.CompareTo(pRight.X);
            return lResult != 0 ? lResult : pLeft.Y.CompareTo(pRight.Y);
        }

        private static int CompareStrings(string pLeft, string pRight) =>
            string.CompareOrdinal(pLeft ?? string.Empty, pRight ?? string.Empty);

        private static int StableStringHash(string pValue)
        {
            unchecked
            {
                int lHash = 23;
                if (string.IsNullOrEmpty(pValue))
                    return lHash;

                for (int lIndex = 0; lIndex < pValue.Length; lIndex++)
                    lHash = Mix(lHash, pValue[lIndex]);

                return lHash;
            }
        }

        #endregion
    }
}
