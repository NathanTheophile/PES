using System.Collections.Generic;
using TacticalPort.Data;

namespace TacticalPort.Shared
{
    public static class TeamSelectionState
    {
        private static readonly List<UnitDefinition> _SelectedUnits = new List<UnitDefinition>(3);

        public static bool HasSelection => _SelectedUnits.Count > 0;
        public static IReadOnlyList<UnitDefinition> SelectedUnits => _SelectedUnits;

        public static void Clear() => _SelectedUnits.Clear();

        public static void SetSelectedUnits(IReadOnlyList<UnitDefinition> pUnits)
        {
            _SelectedUnits.Clear();
            if (pUnits == null)
                return;

            for (int lIndex = 0; lIndex < pUnits.Count; lIndex++)
            {
                if (pUnits[lIndex] != null)
                    _SelectedUnits.Add(pUnits[lIndex]);
            }
        }
    }
}
