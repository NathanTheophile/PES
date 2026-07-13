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
        private static readonly List<UnitCombatLoadout> _LocalLoadouts = new List<UnitCombatLoadout>(3);
        private static readonly List<UnitDefinition> _TeamAUnits = new List<UnitDefinition>(3);
        private static readonly List<UnitDefinition> _TeamBUnits = new List<UnitDefinition>(3);
        private static readonly List<UnitCombatLoadout> _TeamALoadouts = new List<UnitCombatLoadout>(3);
        private static readonly List<UnitCombatLoadout> _TeamBLoadouts = new List<UnitCombatLoadout>(3);

        #endregion

        #region _____________________________/ ACCESSORS

        public static bool HasLocalSelection => _LocalSelectedUnits.Count > 0 || _LocalSelectedUnitIds.Count > 0;
        public static IReadOnlyList<UnitDefinition> LocalSelectedUnits => _LocalSelectedUnits;
        public static IReadOnlyList<string> LocalSelectedUnitIds => _LocalSelectedUnitIds;
        public static IReadOnlyList<UnitCombatLoadout> LocalLoadouts => _LocalLoadouts;

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

        public static void SetLocalLoadouts(IReadOnlyList<UnitCombatLoadout> pLoadouts)
        {
            CopyLoadouts(pLoadouts, _LocalLoadouts);
            SetComposition(MatchPlayerSlot.TeamA, _LocalSelectedUnits, _LocalLoadouts);
        }

        public static void Clear()
        {
            _LocalSelectedUnits.Clear();
            _LocalSelectedUnitIds.Clear();
            _LocalLoadouts.Clear();
            ClearSlotCompositions();
        }

        #endregion

        #region _____________________________| SLOT COMPOSITIONS

        public static void ClearSlotCompositions()
        {
            _TeamAUnits.Clear();
            _TeamBUnits.Clear();
            _TeamALoadouts.Clear();
            _TeamBLoadouts.Clear();
        }

        public static void AssignLocalSelectionToSlot(MatchPlayerSlot pSlot)
        {
            if (!HasLocalSelection)
                return;

            SetComposition(pSlot, _LocalSelectedUnits, _LocalLoadouts);
        }

        public static void SetComposition(MatchPlayerSlot pSlot, IReadOnlyList<UnitDefinition> pUnits)
        {
            SetComposition(pSlot, pUnits, null);
        }

        public static void SetComposition(
            MatchPlayerSlot pSlot,
            IReadOnlyList<UnitDefinition> pUnits,
            IReadOnlyList<UnitCombatLoadout> pLoadouts)
        {
            List<UnitDefinition> lTarget = ResolveMutableList(pSlot);
            List<UnitCombatLoadout> lLoadoutTarget = ResolveMutableLoadoutList(pSlot);
            if (lTarget == null || lLoadoutTarget == null)
                return;

            CopyUnits(pUnits, lTarget);
            CopyLoadouts(pLoadouts, lLoadoutTarget);
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

        public static bool TryGetLoadout(MatchPlayerSlot pSlot, int pUnitIndex, out UnitCombatLoadout pLoadout)
        {
            List<UnitCombatLoadout> lLoadouts = ResolveMutableLoadoutList(pSlot);
            if (lLoadouts != null && pUnitIndex >= 0 && pUnitIndex < lLoadouts.Count)
            {
                pLoadout = lLoadouts[pUnitIndex];
                return pLoadout != null;
            }

            pLoadout = null;
            return false;
        }

        private static List<UnitDefinition> ResolveMutableList(MatchPlayerSlot pSlot) =>
            pSlot == MatchPlayerSlot.TeamA
                ? _TeamAUnits
                : pSlot == MatchPlayerSlot.TeamB
                    ? _TeamBUnits
                    : null;

        private static List<UnitCombatLoadout> ResolveMutableLoadoutList(MatchPlayerSlot pSlot) =>
            pSlot == MatchPlayerSlot.TeamA
                ? _TeamALoadouts
                : pSlot == MatchPlayerSlot.TeamB
                    ? _TeamBLoadouts
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

        private static void CopyLoadouts(IReadOnlyList<UnitCombatLoadout> pSource, ICollection<UnitCombatLoadout> pTarget)
        {
            pTarget.Clear();
            if (pSource == null)
                return;

            for (int lIndex = 0; lIndex < pSource.Count; lIndex++)
                pTarget.Add(pSource[lIndex]);
        }

        #endregion
    }
}
