#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Shared
#endregion

using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.State
{
    public static class CombatTeamCompositionState
    {
        #region _____________________________/ VALUES

        private static readonly List<UnitDefinition> _LocalSelectedUnits = new List<UnitDefinition>(3);
        private static readonly List<string> _LocalSelectedUnitIds = new List<string>(3);
        private static readonly List<UnitDefinition> _TeamAUnits = new List<UnitDefinition>(3);
        private static readonly List<UnitDefinition> _TeamBUnits = new List<UnitDefinition>(3);

        #endregion

        #region _____________________________/ ACCESSORS

        public static bool HasLocalSelection => _LocalSelectedUnits.Count > 0 || _LocalSelectedUnitIds.Count > 0;
        public static IReadOnlyList<UnitDefinition> LocalSelectedUnits => _LocalSelectedUnits;
        public static IReadOnlyList<string> LocalSelectedUnitIds => _LocalSelectedUnitIds;

        #endregion

        #region _____________________________| LOCAL SELECTION

        public static void SetLocalSelectedUnits(IReadOnlyList<UnitDefinition> pUnits)
        {
            CopyUnits(pUnits, _LocalSelectedUnits);
            CopyUnitIds(_LocalSelectedUnits, _LocalSelectedUnitIds);

            // Offline/debug compatibility: a local selection still feeds TeamA until a match assigns a slot.
            SetComposition(MatchPlayerSlot.TeamA, _LocalSelectedUnits);
        }

        public static void SetLocalSelectedUnitIds(IReadOnlyList<string> pUnitIds)
        {
            CopyStrings(pUnitIds, _LocalSelectedUnitIds);
        }

        public static void Clear()
        {
            _LocalSelectedUnits.Clear();
            _LocalSelectedUnitIds.Clear();
            ClearSlotCompositions();
        }

        #endregion

        #region _____________________________| SLOT COMPOSITIONS

        public static void ClearSlotCompositions()
        {
            _TeamAUnits.Clear();
            _TeamBUnits.Clear();
        }

        public static void AssignLocalSelectionToSlot(MatchPlayerSlot pSlot)
        {
            if (!HasLocalSelection)
                return;

            SetComposition(pSlot, _LocalSelectedUnits);
        }

        public static void SetComposition(MatchPlayerSlot pSlot, IReadOnlyList<UnitDefinition> pUnits)
        {
            List<UnitDefinition> lTarget = ResolveMutableList(pSlot);
            if (lTarget == null)
                return;

            CopyUnits(pUnits, lTarget);
        }

        public static bool TryGetComposition(MatchPlayerSlot pSlot, out IReadOnlyList<UnitDefinition> pUnits)
        {
            List<UnitDefinition> lUnits = ResolveMutableList(pSlot);
            if (lUnits != null && lUnits.Count > 0)
            {
                pUnits = lUnits;
                return true;
            }

            pUnits = null;
            return false;
        }

        private static List<UnitDefinition> ResolveMutableList(MatchPlayerSlot pSlot) =>
            pSlot == MatchPlayerSlot.TeamA
                ? _TeamAUnits
                : pSlot == MatchPlayerSlot.TeamB
                    ? _TeamBUnits
                    : null;

        private static void CopyUnits(IReadOnlyList<UnitDefinition> pSource, ICollection<UnitDefinition> pTarget)
        {
            pTarget.Clear();
            if (pSource == null)
                return;

            for (int lIndex = 0; lIndex < pSource.Count; lIndex++)
            {
                if (pSource[lIndex] != null)
                    pTarget.Add(pSource[lIndex]);
            }
        }

        private static void CopyUnitIds(IReadOnlyList<UnitDefinition> pSource, ICollection<string> pTarget)
        {
            pTarget.Clear();
            if (pSource == null)
                return;

            for (int lIndex = 0; lIndex < pSource.Count; lIndex++)
            {
                string lId = pSource[lIndex] != null ? pSource[lIndex].Id : string.Empty;
                if (!string.IsNullOrWhiteSpace(lId))
                    pTarget.Add(lId);
            }
        }

        private static void CopyStrings(IReadOnlyList<string> pSource, ICollection<string> pTarget)
        {
            pTarget.Clear();
            if (pSource == null)
                return;

            for (int lIndex = 0; lIndex < pSource.Count; lIndex++)
            {
                if (!string.IsNullOrWhiteSpace(pSource[lIndex]))
                    pTarget.Add(pSource[lIndex]);
            }
        }

        #endregion
    }
}
