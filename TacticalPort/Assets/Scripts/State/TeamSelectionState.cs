#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Shared
#endregion

using TacticalPort.Data;

namespace TacticalPort.State
{
    public static class TeamSelectionState
    {
        #region _____________________________/ VALUES

        public static bool HasSelection => CombatTeamCompositionState.HasLocalSelection;
        public static System.Collections.Generic.IReadOnlyList<UnitDefinition> SelectedUnits => CombatTeamCompositionState.LocalSelectedUnits;
        public static System.Collections.Generic.IReadOnlyList<string> SelectedUnitIds => CombatTeamCompositionState.LocalSelectedUnitIds;

        #endregion

        #region _____________________________| BUILD

        public static void Clear() => CombatTeamCompositionState.Clear();

        public static void SetSelectedUnits(System.Collections.Generic.IReadOnlyList<UnitDefinition> pUnits)
        {
            CombatTeamCompositionState.SetLocalSelectedUnits(pUnits);
            TeamPresetState.SetActiveUnits(pUnits);
            RefreshSelectedUnitLoadouts();
        }

        public static void SaveSelectedUnits()
        {
            RefreshSelectedUnitLoadouts();
            TeamPresetState.SaveWithoutBlocking();
        }

        public static bool TryApplyDraftAndSave(TeamPresetDraft pDraft, out string pFailure)
        {
            if (!TeamPresetState.TryApplyDraft(pDraft, out pFailure))
                return false;

            CombatTeamCompositionState.SetLocalSelectedUnits(pDraft.SelectedUnits);
            RefreshSelectedUnitLoadouts();
            TeamPresetState.SaveWithoutBlocking();
            return true;
        }

        public static void RefreshSelectedUnitLoadouts()
        {
            System.Collections.Generic.IReadOnlyList<UnitDefinition> lUnits = CombatTeamCompositionState.LocalSelectedUnits;
            System.Collections.Generic.List<UnitCombatLoadout> lLoadouts = new System.Collections.Generic.List<UnitCombatLoadout>(lUnits.Count);
            for (int lIndex = 0; lIndex < lUnits.Count; lIndex++)
            {
                UnitDefinition lUnit = lUnits[lIndex];
                lLoadouts.Add(new UnitCombatLoadout(
                    TeamPresetState.ResolvePassive(lUnit),
                    TeamPresetState.ResolveSkills(lUnit),
                    TeamPresetState.ResolveStatModifiers(lUnit)));
            }

            CombatTeamCompositionState.SetLocalLoadouts(lLoadouts);
        }

        public static bool LoadSavedUnitIds()
        {
            TeamPresetState.EnsureInitializedForLocalUse();
            System.Collections.Generic.IReadOnlyList<string> lPresetIds = TeamPresetState.GetActiveUnitIds();
            if (lPresetIds.Count > 0)
            {
                CombatTeamCompositionState.SetLocalSelectedUnitIds(lPresetIds);
                return true;
            }

            return false;
        }

        #endregion
    }
}
