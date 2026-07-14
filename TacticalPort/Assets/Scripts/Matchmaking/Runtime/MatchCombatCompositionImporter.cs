#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Matchmaking
#endregion

using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;
using TacticalPort.State;
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
            ExportLocalSelection(pLocalPlayer, pManifest, pPresetCatalog);
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
                CombatTeamCompositionState.SetComposition(
                    pAssignment.Slot,
                    lManifestUnits,
                    ResolveLoadouts(lManifestUnits, pAssignment.UnitBuilds));
                return;
            }

            if (pPresetCatalog != null && pPresetCatalog.TryGetUnits(pAssignment.TeamPresetId, out System.Collections.Generic.IReadOnlyList<UnitDefinition> lUnits))
                CombatTeamCompositionState.SetComposition(pAssignment.Slot, lUnits);
        }

        public static bool ExportLocalSelection(
            PlayerIdentity pLocalPlayer,
            MatchManifest pManifest,
            CombatTeamPresetCatalog pPresetCatalog = null)
        {
            if (!pLocalPlayer.IsValid || pManifest == null)
                return false;

            if (!CombatTeamCompositionState.HasLocalSelection)
                TeamSelectionState.LoadSavedUnitIds();

            IReadOnlyList<UnitDefinition> lUnits = CombatTeamCompositionState.LocalSelectedUnits;
            if ((lUnits == null || lUnits.Count == 0)
                && TryResolveUnitIds(CombatTeamCompositionState.LocalSelectedUnitIds, pPresetCatalog, out IReadOnlyList<UnitDefinition> lResolvedUnits))
            {
                lUnits = lResolvedUnits;
                CombatTeamCompositionState.SetLocalSelectedUnits(lUnits);
            }

            if (lUnits != null && lUnits.Count > 0)
            {
                List<MatchUnitBuild> lBuilds = BuildLocalUnitBuilds(lUnits, out List<UnitCombatLoadout> lLoadouts);
                CombatTeamCompositionState.SetLocalLoadouts(lLoadouts);
                pManifest.SetUnitIds(pLocalPlayer.PlayerId, lUnits);
                pManifest.SetUnitBuilds(pLocalPlayer.PlayerId, lBuilds);
                return true;
            }

            if (CombatTeamCompositionState.LocalSelectedUnitIds.Count <= 0)
                return false;

            pManifest.SetUnitIds(pLocalPlayer.PlayerId, CombatTeamCompositionState.LocalSelectedUnitIds);
            return true;
        }

        private static List<MatchUnitBuild> BuildLocalUnitBuilds(
            IReadOnlyList<UnitDefinition> pUnits,
            out List<UnitCombatLoadout> pLoadouts)
        {
            List<MatchUnitBuild> lBuilds = new List<MatchUnitBuild>(pUnits.Count);
            pLoadouts = new List<UnitCombatLoadout>(pUnits.Count);
            TeamPreset lPreset = TeamPresetState.GetOrCreateActivePreset();

            for (int lIndex = 0; lIndex < pUnits.Count; lIndex++)
            {
                UnitDefinition lUnit = pUnits[lIndex];
                PassiveDefinition lPassive = TeamPresetState.ResolvePassive(lUnit);
                List<SkillDefinition> lSkills = TeamPresetState.ResolveSkills(lUnit);
                UnitBuildPreset lPresetBuild = FindPresetBuild(lPreset, lUnit.Id);
                MatchUnitBuild lBuild = new MatchUnitBuild
                {
                    UnitId = lUnit.Id,
                    PassiveId = lPassive != null ? lPassive.Id : string.Empty,
                    StatAllocations = CopyStatAllocations(lPresetBuild?.StatAllocations)
                };

                for (int lSkillIndex = 0; lSkillIndex < lSkills.Count; lSkillIndex++)
                {
                    SkillDefinition lSkill = lSkills[lSkillIndex];
                    if (lSkill != null)
                        lBuild.SkillIds.Add(lSkill.Id);
                }

                lBuilds.Add(lBuild);
                pLoadouts.Add(new UnitCombatLoadout(
                    lPassive,
                    lSkills,
                    TeamPresetState.ResolveStatModifiers(lUnit)));
            }

            return lBuilds;
        }

        private static IReadOnlyList<UnitCombatLoadout> ResolveLoadouts(
            IReadOnlyList<UnitDefinition> pUnits,
            IReadOnlyList<MatchUnitBuild> pBuilds)
        {
            if (pUnits == null || pBuilds == null || pBuilds.Count != pUnits.Count)
                return null;

            List<UnitCombatLoadout> lLoadouts = new List<UnitCombatLoadout>(pUnits.Count);
            for (int lIndex = 0; lIndex < pUnits.Count; lIndex++)
            {
                UnitDefinition lUnit = pUnits[lIndex];
                MatchUnitBuild lBuild = pBuilds[lIndex];
                if (!TryResolveLoadout(lUnit, lBuild, out UnitCombatLoadout lLoadout))
                    lLoadout = CreateDefaultLoadout(lUnit);

                lLoadouts.Add(lLoadout);
            }

            return lLoadouts;
        }

        private static bool TryResolveLoadout(UnitDefinition pUnit, MatchUnitBuild pBuild, out UnitCombatLoadout pLoadout)
        {
            pLoadout = null;
            if (pUnit == null || pBuild == null || pBuild.UnitId != pUnit.Id || pBuild.SkillIds == null)
                return false;

            PassiveDefinition lPassive = FindPassive(pUnit.Passives, pBuild.PassiveId);
            if (!string.IsNullOrWhiteSpace(pBuild.PassiveId) && lPassive == null)
                return false;

            List<SkillDefinition> lSkills = new List<SkillDefinition>(pBuild.SkillIds.Count);
            for (int lIndex = 0; lIndex < pBuild.SkillIds.Count; lIndex++)
            {
                SkillDefinition lSkill = FindSkill(pUnit.Skills, pBuild.SkillIds[lIndex]);
                if (lSkill == null || lSkills.Contains(lSkill))
                    return false;

                lSkills.Add(lSkill);
            }

            UnitStatAllocationPreset lStatAllocations = CopyStatAllocations(pBuild.StatAllocations);
            if (!UnitStatAllocationRules.TryValidate(pUnit, lStatAllocations, out _, out _))
                return false;

            pLoadout = new UnitCombatLoadout(
                lPassive,
                lSkills,
                UnitStatAllocationRules.CreateRuntimeModifiers(pUnit, lStatAllocations));
            return true;
        }

        private static UnitCombatLoadout CreateDefaultLoadout(UnitDefinition pUnit)
        {
            List<SkillDefinition> lSkills = new List<SkillDefinition>(TeamPreset.EquippedSkillCount);
            IReadOnlyList<SkillDefinition> lAvailableSkills = pUnit?.Skills;
            if (lAvailableSkills != null)
            {
                for (int lIndex = 0; lIndex < lAvailableSkills.Count && lSkills.Count < TeamPreset.EquippedSkillCount; lIndex++)
                {
                    SkillDefinition lSkill = lAvailableSkills[lIndex];
                    if (lSkill != null && !lSkills.Contains(lSkill))
                        lSkills.Add(lSkill);
                }
            }

            return new UnitCombatLoadout(pUnit?.DefaultPassive, lSkills);
        }

        private static UnitBuildPreset FindPresetBuild(TeamPreset pPreset, string pUnitId)
        {
            if (pPreset?.Slots == null)
                return null;

            for (int lIndex = 0; lIndex < pPreset.Slots.Count; lIndex++)
            {
                UnitBuildPreset lBuild = pPreset.Slots[lIndex];
                if (lBuild != null && lBuild.UnitId == pUnitId)
                    return lBuild;
            }

            return null;
        }

        private static MatchUnitStatAllocations CopyStatAllocations(UnitStatAllocationPreset pSource) => new MatchUnitStatAllocations
        {
            Health = pSource?.Health ?? 0,
            Energy = pSource?.Energy ?? 0,
            Mobility = pSource?.Mobility ?? 0,
            MeleeDamage = pSource?.MeleeDamage ?? 0,
            MeleeResistance = pSource?.MeleeResistance ?? 0,
            RangedDamage = pSource?.RangedDamage ?? 0,
            RangedResistance = pSource?.RangedResistance ?? 0,
            Velocity = pSource?.Velocity ?? 0
        };

        private static UnitStatAllocationPreset CopyStatAllocations(MatchUnitStatAllocations pSource) => new UnitStatAllocationPreset
        {
            Health = pSource?.Health ?? 0,
            Energy = pSource?.Energy ?? 0,
            Mobility = pSource?.Mobility ?? 0,
            MeleeDamage = pSource?.MeleeDamage ?? 0,
            MeleeResistance = pSource?.MeleeResistance ?? 0,
            RangedDamage = pSource?.RangedDamage ?? 0,
            RangedResistance = pSource?.RangedResistance ?? 0,
            Velocity = pSource?.Velocity ?? 0
        };

        private static PassiveDefinition FindPassive(IReadOnlyList<PassiveDefinition> pPassives, string pPassiveId)
        {
            if (pPassives == null || string.IsNullOrWhiteSpace(pPassiveId))
                return null;

            for (int lIndex = 0; lIndex < pPassives.Count; lIndex++)
            {
                PassiveDefinition lPassive = pPassives[lIndex];
                if (lPassive != null && lPassive.Id == pPassiveId)
                    return lPassive;
            }

            return null;
        }

        private static SkillDefinition FindSkill(IReadOnlyList<SkillDefinition> pSkills, string pSkillId)
        {
            if (pSkills == null || string.IsNullOrWhiteSpace(pSkillId))
                return null;

            for (int lIndex = 0; lIndex < pSkills.Count; lIndex++)
            {
                SkillDefinition lSkill = pSkills[lIndex];
                if (lSkill != null && lSkill.Id == pSkillId)
                    return lSkill;
            }

            return null;
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
