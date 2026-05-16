#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Shared
#endregion

using TacticalPort.Data;
using UnityEngine;

namespace TacticalPort.Shared
{
    public static class TeamSelectionState
    {
        #region _____________________________/ VALUES

        private const string SavedUnitIdsKey = "TacticalPort.TeamSelection.UnitIds.v1";

        public static bool HasSelection => CombatTeamCompositionState.HasLocalSelection;
        public static System.Collections.Generic.IReadOnlyList<UnitDefinition> SelectedUnits => CombatTeamCompositionState.LocalSelectedUnits;
        public static System.Collections.Generic.IReadOnlyList<string> SelectedUnitIds => CombatTeamCompositionState.LocalSelectedUnitIds;

        #endregion

        #region _____________________________| BUILD

        public static void Clear() => CombatTeamCompositionState.Clear();

        public static void SetSelectedUnits(System.Collections.Generic.IReadOnlyList<UnitDefinition> pUnits) =>
            CombatTeamCompositionState.SetLocalSelectedUnits(pUnits);

        public static void SaveSelectedUnits()
        {
            PlayerPrefs.SetString(SavedUnitIdsKey, string.Join("|", SelectedUnitIds));
            PlayerPrefs.Save();
        }

        public static bool LoadSavedUnitIds()
        {
            string lRawIds = PlayerPrefs.GetString(SavedUnitIdsKey, string.Empty);
            if (string.IsNullOrWhiteSpace(lRawIds))
                return false;

            string[] lIds = lRawIds.Split('|');
            CombatTeamCompositionState.SetLocalSelectedUnitIds(lIds);
            return SelectedUnitIds.Count > 0;
        }

        #endregion
    }
}
