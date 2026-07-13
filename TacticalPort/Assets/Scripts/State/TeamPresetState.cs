#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  State
#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TacticalPort.Data;
using UnityEngine;

namespace TacticalPort.State
{
    public static class TeamPresetState
    {
        public const string DefaultPresetId = "default-team";
        private const string DefaultPresetName = "Team 1";

        private static ITeamPresetRepository _Repository = new PlayerPrefsTeamPresetRepository();
        private static TeamPresetRepositorySnapshot _Snapshot = new TeamPresetRepositorySnapshot();
        private static Task _InitializationTask;
        private static Task _PendingSave = Task.CompletedTask;

        public static bool IsInitialized { get; private set; }
        public static string ActivePresetId => ActivePreset?.PresetId ?? string.Empty;
        public static TeamPreset ActivePreset => FindPreset(_Snapshot.ActivePresetId);
        public static IReadOnlyList<TeamPreset> Presets => _Snapshot.Presets;

        public static Task InitializeAsync(CancellationToken pCancellationToken = default)
        {
            if (IsInitialized)
                return Task.CompletedTask;

            return _InitializationTask ??= InitializeInternalAsync(_Repository, pCancellationToken);
        }

        public static void EnsureInitializedForLocalUse()
        {
            if (!IsInitialized)
                InitializeAsync().GetAwaiter().GetResult();
        }

        public static void ConfigureRepository(ITeamPresetRepository pRepository)
        {
            if (pRepository == null)
                throw new ArgumentNullException(nameof(pRepository));

            _Repository = pRepository;
            _Snapshot = new TeamPresetRepositorySnapshot();
            _InitializationTask = null;
            _PendingSave = Task.CompletedTask;
            IsInitialized = false;
        }

        public static TeamPreset GetOrCreateActivePreset()
        {
            EnsureInitializedForLocalUse();
            TeamPreset lPreset = ActivePreset;
            if (lPreset != null)
                return lPreset;

            lPreset = CreatePreset(DefaultPresetId, DefaultPresetName);
            _Snapshot.ActivePresetId = lPreset.PresetId;
            return lPreset;
        }

        public static TeamPreset CreatePreset(string pPresetId, string pDisplayName)
        {
            EnsureInitializedForLocalUse();
            EnsureSnapshotCollections();
            string lPresetId = string.IsNullOrWhiteSpace(pPresetId) ? Guid.NewGuid().ToString("N") : pPresetId.Trim();
            TeamPreset lExisting = FindPreset(lPresetId);
            if (lExisting != null)
                return lExisting;

            TeamPreset lPreset = new TeamPreset
            {
                PresetId = lPresetId,
                DisplayName = string.IsNullOrWhiteSpace(pDisplayName) ? DefaultPresetName : pDisplayName.Trim()
            };
            EnsureSlotCount(lPreset);
            _Snapshot.Presets.Add(lPreset);
            return lPreset;
        }

        public static bool SetActivePreset(string pPresetId)
        {
            EnsureInitializedForLocalUse();
            TeamPreset lPreset = FindPreset(pPresetId);
            if (lPreset == null)
                return false;

            _Snapshot.ActivePresetId = lPreset.PresetId;
            return true;
        }

        public static bool DeletePreset(string pPresetId)
        {
            EnsureInitializedForLocalUse();
            if (_Snapshot.Presets == null || _Snapshot.Presets.Count <= 1)
                return false;

            for (int lIndex = 0; lIndex < _Snapshot.Presets.Count; lIndex++)
            {
                if (!string.Equals(_Snapshot.Presets[lIndex]?.PresetId, pPresetId, StringComparison.Ordinal))
                    continue;

                _Snapshot.Presets.RemoveAt(lIndex);
                if (string.Equals(_Snapshot.ActivePresetId, pPresetId, StringComparison.Ordinal))
                    _Snapshot.ActivePresetId = _Snapshot.Presets[0].PresetId;
                return true;
            }

            return false;
        }

        public static void SetActiveUnits(IReadOnlyList<UnitDefinition> pUnits)
        {
            List<string> lUnitIds = new List<string>(TeamPreset.TeamSize);
            if (pUnits != null)
            {
                for (int lIndex = 0; lIndex < pUnits.Count && lUnitIds.Count < TeamPreset.TeamSize; lIndex++)
                {
                    string lId = pUnits[lIndex] != null ? pUnits[lIndex].Id : string.Empty;
                    if (!string.IsNullOrWhiteSpace(lId) && !lUnitIds.Contains(lId))
                        lUnitIds.Add(lId);
                }
            }

            SetActiveUnitIds(lUnitIds);
        }

        public static void SetActiveUnitIds(IReadOnlyList<string> pUnitIds)
        {
            TeamPreset lPreset = GetOrCreateActivePreset();
            List<UnitBuildPreset> lPreviousSlots = new List<UnitBuildPreset>(lPreset.Slots);
            lPreset.Slots.Clear();

            if (pUnitIds != null)
            {
                for (int lIndex = 0; lIndex < pUnitIds.Count && lPreset.Slots.Count < TeamPreset.TeamSize; lIndex++)
                {
                    string lUnitId = pUnitIds[lIndex];
                    if (string.IsNullOrWhiteSpace(lUnitId) || ContainsUnit(lPreset.Slots, lUnitId))
                        continue;

                    UnitBuildPreset lPreviousBuild = FindBuild(lPreviousSlots, lUnitId);
                    lPreset.Slots.Add(lPreviousBuild?.Clone() ?? new UnitBuildPreset { UnitId = lUnitId });
                }
            }

            EnsureSlotCount(lPreset);
        }

        public static IReadOnlyList<string> GetActiveUnitIds()
        {
            TeamPreset lPreset = GetOrCreateActivePreset();
            List<string> lIds = new List<string>(TeamPreset.TeamSize);
            for (int lIndex = 0; lIndex < lPreset.Slots.Count; lIndex++)
            {
                string lId = lPreset.Slots[lIndex]?.UnitId;
                if (!string.IsNullOrWhiteSpace(lId))
                    lIds.Add(lId);
            }

            return lIds;
        }

        public static UnitBuildPreset GetOrCreateBuild(UnitDefinition pUnit)
        {
            if (pUnit == null)
                return null;

            TeamPreset lPreset = GetOrCreateActivePreset();
            UnitBuildPreset lBuild = FindBuild(lPreset.Slots, pUnit.Id);
            if (lBuild == null)
                return null;

            NormalizeBuild(lBuild, pUnit);
            return lBuild;
        }

        public static void SetPassive(UnitDefinition pUnit, PassiveDefinition pPassive)
        {
            UnitBuildPreset lBuild = GetOrCreateBuild(pUnit);
            if (lBuild != null && ContainsPassive(pUnit?.Passives, pPassive))
                lBuild.PassiveId = pPassive.Id;
        }

        public static void SetSkills(UnitDefinition pUnit, IReadOnlyList<SkillDefinition> pSkills)
        {
            UnitBuildPreset lBuild = GetOrCreateBuild(pUnit);
            if (lBuild == null)
                return;

            lBuild.SkillIds.Clear();
            if (pSkills != null)
            {
                for (int lIndex = 0; lIndex < pSkills.Count && lBuild.SkillIds.Count < TeamPreset.EquippedSkillCount; lIndex++)
                {
                    SkillDefinition lSkill = pSkills[lIndex];
                    if (lSkill != null && ContainsSkill(pUnit.Skills, lSkill) && !lBuild.SkillIds.Contains(lSkill.Id))
                        lBuild.SkillIds.Add(lSkill.Id);
                }
            }

            FillDefaultSkillIds(lBuild, pUnit);
        }

        public static bool TrySetStatAllocations(
            UnitDefinition pUnit,
            UnitStatAllocationPreset pAllocations,
            out string pFailure)
        {
            UnitBuildPreset lBuild = GetOrCreateBuild(pUnit);
            if (lBuild == null)
            {
                pFailure = "The unit is not part of the active preset.";
                return false;
            }

            if (!UnitStatAllocationRules.TryValidate(pUnit, pAllocations, out _, out pFailure))
                return false;

            lBuild.StatAllocations = pAllocations?.Clone() ?? new UnitStatAllocationPreset();
            return true;
        }

        public static UnitStatAllocationPreset ResolveStatAllocations(UnitDefinition pUnit)
        {
            UnitBuildPreset lBuild = GetOrCreateBuild(pUnit);
            return lBuild?.StatAllocations?.Clone() ?? new UnitStatAllocationPreset();
        }

        public static UnitStatModifiers ResolveStatModifiers(UnitDefinition pUnit) =>
            UnitStatAllocationRules.CreateRuntimeModifiers(pUnit, ResolveStatAllocations(pUnit));

        public static PassiveDefinition ResolvePassive(UnitDefinition pUnit)
        {
            UnitBuildPreset lBuild = GetOrCreateBuild(pUnit);
            if (lBuild == null)
                return pUnit?.DefaultPassive;

            PassiveDefinition lPassive = FindPassive(pUnit.Passives, lBuild.PassiveId);
            return lPassive != null ? lPassive : pUnit.DefaultPassive;
        }

        public static List<SkillDefinition> ResolveSkills(UnitDefinition pUnit)
        {
            UnitBuildPreset lBuild = GetOrCreateBuild(pUnit);
            List<SkillDefinition> lSkills = new List<SkillDefinition>(TeamPreset.EquippedSkillCount);
            if (pUnit == null || lBuild == null)
                return lSkills;

            for (int lIndex = 0; lIndex < lBuild.SkillIds.Count && lSkills.Count < TeamPreset.EquippedSkillCount; lIndex++)
            {
                SkillDefinition lSkill = FindSkill(pUnit.Skills, lBuild.SkillIds[lIndex]);
                if (lSkill != null && !lSkills.Contains(lSkill))
                    lSkills.Add(lSkill);
            }

            while (lSkills.Count < TeamPreset.EquippedSkillCount)
                lSkills.Add(null);
            return lSkills;
        }

        public static Task SaveAsync(CancellationToken pCancellationToken = default)
        {
            EnsureInitializedForLocalUse();
            long lNow = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _Snapshot.UpdatedAtUnixMilliseconds = Math.Max(lNow, _Snapshot.UpdatedAtUnixMilliseconds + 1);
            TeamPresetRepositorySnapshot lSnapshot = _Snapshot.Clone();
            _PendingSave = SaveAfterPendingAsync(_PendingSave, _Repository, lSnapshot, pCancellationToken);
            return _PendingSave;
        }

        public static void SaveWithoutBlocking()
        {
            Task lSaveTask = SaveAsync();
            if (!lSaveTask.IsCompletedSuccessfully)
                _ = ObserveSaveFailureAsync(lSaveTask);
        }

        private static async Task InitializeInternalAsync(ITeamPresetRepository pRepository, CancellationToken pCancellationToken)
        {
            _Snapshot = await pRepository.LoadAsync(pCancellationToken) ?? new TeamPresetRepositorySnapshot();
            NormalizeSnapshot();
            IsInitialized = true;
        }

        private static async Task SaveAfterPendingAsync(
            Task pPreviousSave,
            ITeamPresetRepository pRepository,
            TeamPresetRepositorySnapshot pSnapshot,
            CancellationToken pCancellationToken)
        {
            try
            {
                await pPreviousSave;
            }
            catch (Exception lException)
            {
                Debug.LogError($"Previous team preset save failed: {lException.Message}");
            }

            await pRepository.SaveAsync(pSnapshot, pCancellationToken);
        }

        private static async Task ObserveSaveFailureAsync(Task pSaveTask)
        {
            try
            {
                await pSaveTask;
            }
            catch (Exception lException)
            {
                Debug.LogError($"Failed to save team presets: {lException.Message}");
            }
        }

        private static void NormalizeSnapshot()
        {
            EnsureSnapshotCollections();
            for (int lIndex = _Snapshot.Presets.Count - 1; lIndex >= 0; lIndex--)
            {
                TeamPreset lPreset = _Snapshot.Presets[lIndex];
                if (lPreset == null || string.IsNullOrWhiteSpace(lPreset.PresetId) || HasDuplicatePresetId(lIndex, lPreset.PresetId))
                {
                    _Snapshot.Presets.RemoveAt(lIndex);
                    continue;
                }

                lPreset.SchemaVersion = TeamPreset.CurrentSchemaVersion;
                EnsureSlotCount(lPreset);
            }

            if (_Snapshot.Presets.Count == 0)
                _Snapshot.Presets.Add(CreateEmptyDefaultPreset());

            if (FindPreset(_Snapshot.ActivePresetId) == null)
                _Snapshot.ActivePresetId = _Snapshot.Presets[0].PresetId;

            _Snapshot.SchemaVersion = TeamPreset.CurrentSchemaVersion;
        }

        private static void NormalizeBuild(UnitBuildPreset pBuild, UnitDefinition pUnit)
        {
            pBuild.SkillIds ??= new List<string>(TeamPreset.EquippedSkillCount);
            pBuild.StatAllocations ??= new UnitStatAllocationPreset();

            if (!UnitStatAllocationRules.TryValidate(pUnit, pBuild.StatAllocations, out _, out _))
                pBuild.StatAllocations = new UnitStatAllocationPreset();

            PassiveDefinition lPassive = FindPassive(pUnit.Passives, pBuild.PassiveId);
            pBuild.PassiveId = (lPassive != null ? lPassive : pUnit.DefaultPassive)?.Id ?? string.Empty;

            HashSet<string> lValidIds = new HashSet<string>(StringComparer.Ordinal);
            for (int lIndex = 0; lIndex < pBuild.SkillIds.Count;)
            {
                string lSkillId = pBuild.SkillIds[lIndex];
                if (FindSkill(pUnit.Skills, lSkillId) == null || !lValidIds.Add(lSkillId))
                {
                    pBuild.SkillIds.RemoveAt(lIndex);
                    continue;
                }

                lIndex++;
            }

            if (pBuild.SkillIds.Count > TeamPreset.EquippedSkillCount)
                pBuild.SkillIds.RemoveRange(TeamPreset.EquippedSkillCount, pBuild.SkillIds.Count - TeamPreset.EquippedSkillCount);
            FillDefaultSkillIds(pBuild, pUnit);
        }

        private static void FillDefaultSkillIds(UnitBuildPreset pBuild, UnitDefinition pUnit)
        {
            if (pUnit?.Skills == null)
                return;

            for (int lIndex = 0; lIndex < pUnit.Skills.Count && pBuild.SkillIds.Count < TeamPreset.EquippedSkillCount; lIndex++)
            {
                string lId = pUnit.Skills[lIndex] != null ? pUnit.Skills[lIndex].Id : string.Empty;
                if (!string.IsNullOrWhiteSpace(lId) && !pBuild.SkillIds.Contains(lId))
                    pBuild.SkillIds.Add(lId);
            }
        }

        private static TeamPreset CreateEmptyDefaultPreset()
        {
            TeamPreset lPreset = new TeamPreset { PresetId = DefaultPresetId, DisplayName = DefaultPresetName };
            EnsureSlotCount(lPreset);
            return lPreset;
        }

        private static void EnsureSnapshotCollections()
        {
            _Snapshot ??= new TeamPresetRepositorySnapshot();
            _Snapshot.Presets ??= new List<TeamPreset>();
        }

        private static void EnsureSlotCount(TeamPreset pPreset)
        {
            pPreset.Slots ??= new List<UnitBuildPreset>(TeamPreset.TeamSize);
            while (pPreset.Slots.Count < TeamPreset.TeamSize)
                pPreset.Slots.Add(new UnitBuildPreset());
            if (pPreset.Slots.Count > TeamPreset.TeamSize)
                pPreset.Slots.RemoveRange(TeamPreset.TeamSize, pPreset.Slots.Count - TeamPreset.TeamSize);
        }

        private static bool HasDuplicatePresetId(int pCurrentIndex, string pPresetId)
        {
            for (int lIndex = 0; lIndex < pCurrentIndex; lIndex++)
            {
                if (string.Equals(_Snapshot.Presets[lIndex]?.PresetId, pPresetId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        private static TeamPreset FindPreset(string pPresetId)
        {
            if (_Snapshot?.Presets == null || string.IsNullOrWhiteSpace(pPresetId))
                return null;

            for (int lIndex = 0; lIndex < _Snapshot.Presets.Count; lIndex++)
            {
                TeamPreset lPreset = _Snapshot.Presets[lIndex];
                if (lPreset != null && string.Equals(lPreset.PresetId, pPresetId, StringComparison.Ordinal))
                    return lPreset;
            }

            return null;
        }

        private static UnitBuildPreset FindBuild(IReadOnlyList<UnitBuildPreset> pBuilds, string pUnitId)
        {
            if (pBuilds == null || string.IsNullOrWhiteSpace(pUnitId))
                return null;

            for (int lIndex = 0; lIndex < pBuilds.Count; lIndex++)
            {
                UnitBuildPreset lBuild = pBuilds[lIndex];
                if (lBuild != null && string.Equals(lBuild.UnitId, pUnitId, StringComparison.Ordinal))
                    return lBuild;
            }

            return null;
        }

        private static bool ContainsUnit(IReadOnlyList<UnitBuildPreset> pBuilds, string pUnitId) => FindBuild(pBuilds, pUnitId) != null;

        private static PassiveDefinition FindPassive(IReadOnlyList<PassiveDefinition> pPassives, string pPassiveId)
        {
            if (pPassives == null || string.IsNullOrWhiteSpace(pPassiveId))
                return null;

            for (int lIndex = 0; lIndex < pPassives.Count; lIndex++)
            {
                PassiveDefinition lPassive = pPassives[lIndex];
                if (lPassive != null && string.Equals(lPassive.Id, pPassiveId, StringComparison.Ordinal))
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
                if (lSkill != null && string.Equals(lSkill.Id, pSkillId, StringComparison.Ordinal))
                    return lSkill;
            }

            return null;
        }

        private static bool ContainsPassive(IReadOnlyList<PassiveDefinition> pPassives, PassiveDefinition pPassive)
        {
            if (pPassives == null || pPassive == null)
                return false;

            for (int lIndex = 0; lIndex < pPassives.Count; lIndex++)
            {
                if (pPassives[lIndex] == pPassive)
                    return true;
            }

            return false;
        }

        private static bool ContainsSkill(IReadOnlyList<SkillDefinition> pSkills, SkillDefinition pSkill)
        {
            if (pSkills == null || pSkill == null)
                return false;

            for (int lIndex = 0; lIndex < pSkills.Count; lIndex++)
            {
                if (pSkills[lIndex] == pSkill)
                    return true;
            }

            return false;
        }
    }
}
