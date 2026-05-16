#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Matchmaking
#endregion

using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TacticalPort.Matchmaking
{
    public static class MatchCombatCompositionImporter
    {
        #region _____________________________| IMPORT

        public static void ImportLocalSelection(PlayerIdentity pLocalPlayer, MatchManifest pManifest, CombatTeamPresetCatalog pPresetCatalog = null)
        {
            ExportLocalSelection(pLocalPlayer, pManifest);
            CombatTeamCompositionState.ClearSlotCompositions();
            if (pManifest?.Players == null)
                return;

            for (int lIndex = 0; lIndex < pManifest.Players.Count; lIndex++)
                ImportAssignment(pManifest.Players[lIndex], pLocalPlayer, pPresetCatalog);
        }

        public static void ClearMatchCompositions()
        {
            CombatTeamCompositionState.ClearSlotCompositions();
        }

        private static void ImportAssignment(MatchPlayerAssignment pAssignment, PlayerIdentity pLocalPlayer, CombatTeamPresetCatalog pPresetCatalog)
        {
            if (pAssignment == null || pAssignment.Slot == MatchPlayerSlot.None)
                return;

            bool lIsLocalPlayer = pLocalPlayer.IsValid && pAssignment.PlayerId == pLocalPlayer.PlayerId;
            if (lIsLocalPlayer && CombatTeamCompositionState.HasLocalSelection)
            {
                CombatTeamCompositionState.AssignLocalSelectionToSlot(pAssignment.Slot);
                return;
            }

            if (TryResolveUnitIds(pAssignment.UnitIds, pPresetCatalog, out IReadOnlyList<UnitDefinition> lManifestUnits))
            {
                CombatTeamCompositionState.SetComposition(pAssignment.Slot, lManifestUnits);
                return;
            }

            if (pPresetCatalog != null && pPresetCatalog.TryGetUnits(pAssignment.TeamPresetId, out System.Collections.Generic.IReadOnlyList<UnitDefinition> lUnits))
                CombatTeamCompositionState.SetComposition(pAssignment.Slot, lUnits);
        }

        private static void ExportLocalSelection(PlayerIdentity pLocalPlayer, MatchManifest pManifest)
        {
            if (!pLocalPlayer.IsValid || pManifest == null)
                return;

            if (!CombatTeamCompositionState.HasLocalSelection)
                TeamSelectionState.LoadSavedUnitIds();

            if (CombatTeamCompositionState.LocalSelectedUnitIds.Count > 0)
                pManifest.SetUnitIds(pLocalPlayer.PlayerId, CombatTeamCompositionState.LocalSelectedUnitIds);
            else if (CombatTeamCompositionState.LocalSelectedUnits.Count > 0)
                pManifest.SetUnitIds(pLocalPlayer.PlayerId, CombatTeamCompositionState.LocalSelectedUnits);
        }

        private static bool TryResolveUnitIds(IReadOnlyList<string> pUnitIds, CombatTeamPresetCatalog pPresetCatalog, out IReadOnlyList<UnitDefinition> pUnits)
        {
            if (pUnitIds == null || pUnitIds.Count == 0)
            {
                pUnits = null;
                return false;
            }

            if (pPresetCatalog != null && pPresetCatalog.TryGetUnits(pUnitIds, out pUnits))
                return true;

#if UNITY_EDITOR
            List<UnitDefinition> lUnits = new List<UnitDefinition>();
            for (int lIndex = 0; lIndex < pUnitIds.Count; lIndex++)
            {
                UnitDefinition lUnit = FindEditorUnit(pUnitIds[lIndex]);
                if (lUnit != null)
                    lUnits.Add(lUnit);
            }

            pUnits = lUnits;
            return lUnits.Count > 0;
#else
            pUnits = null;
            return false;
#endif
        }

#if UNITY_EDITOR
        private static UnitDefinition FindEditorUnit(string pUnitId)
        {
            if (string.IsNullOrWhiteSpace(pUnitId))
                return null;

            string[] lGuids = AssetDatabase.FindAssets("t:UnitDefinition");
            for (int lIndex = 0; lIndex < lGuids.Length; lIndex++)
            {
                string lPath = AssetDatabase.GUIDToAssetPath(lGuids[lIndex]);
                UnitDefinition lUnit = AssetDatabase.LoadAssetAtPath<UnitDefinition>(lPath);
                if (lUnit != null && lUnit.Id == pUnitId)
                    return lUnit;
            }

            return null;
        }
#endif

        #endregion
    }
}
