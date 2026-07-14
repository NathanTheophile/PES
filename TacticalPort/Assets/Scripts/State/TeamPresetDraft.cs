#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : Pure mutable draft. Persistence is handled by TeamSelectionState.
//  State
#endregion

using System;
using System.Collections.Generic;
using TacticalPort.Data;

namespace TacticalPort.State
{
    public sealed class TeamPresetDraft
    {
        public sealed class UnitBuild
        {
            private readonly List<SkillDefinition> _Skills = new List<SkillDefinition>(TeamPreset.EquippedSkillCount);

            public UnitDefinition Unit { get; }
            public PassiveDefinition Passive { get; internal set; }
            public IReadOnlyList<SkillDefinition> Skills => _Skills;
            public UnitStatAllocationPreset StatAllocations { get; internal set; } = new UnitStatAllocationPreset();

            internal List<SkillDefinition> MutableSkills => _Skills;

            internal UnitBuild(UnitDefinition pUnit) => Unit = pUnit;
        }

        private readonly int _TeamSize;
        private readonly TeamPreset _PersistedPreset;
        private readonly List<UnitDefinition> _AvailableUnits = new List<UnitDefinition>();
        private readonly List<UnitDefinition> _SelectedUnits = new List<UnitDefinition>();
        private readonly Dictionary<UnitDefinition, UnitBuild> _Builds = new Dictionary<UnitDefinition, UnitBuild>();

        public IReadOnlyList<UnitDefinition> AvailableUnits => _AvailableUnits;
        public IReadOnlyList<UnitDefinition> SelectedUnits => _SelectedUnits;
        public bool HasPendingChanges { get; private set; }
        public bool IsValid => Validate(out _);

        public TeamPresetDraft(
            int pTeamSize,
            IReadOnlyList<UnitDefinition> pAvailableUnits,
            IReadOnlyList<UnitDefinition> pSelectedUnits,
            IReadOnlyList<string> pSelectedUnitIds,
            TeamPreset pPersistedPreset)
        {
            _TeamSize = Math.Max(1, pTeamSize);
            _PersistedPreset = pPersistedPreset?.Clone();
            AddAvailableUnits(pAvailableUnits);
            ResolveSelection(pSelectedUnits, pSelectedUnitIds);
            FillMissingSlots();

            for (int lIndex = 0; lIndex < _SelectedUnits.Count; lIndex++)
                GetOrCreateBuild(_SelectedUnits[lIndex]);

            HasPendingChanges = !MatchesPersistedPreset();
        }

        public UnitDefinition GetUnit(int pSlotIndex) =>
            pSlotIndex >= 0 && pSlotIndex < _SelectedUnits.Count ? _SelectedUnits[pSlotIndex] : null;

        public UnitBuild GetBuild(UnitDefinition pUnit) => GetOrCreateBuild(pUnit);

        public bool IsUnitSelectedElsewhere(UnitDefinition pUnit, int pSlotIndex)
        {
            if (pUnit == null)
                return false;

            for (int lIndex = 0; lIndex < _SelectedUnits.Count; lIndex++)
            {
                if (lIndex != pSlotIndex && _SelectedUnits[lIndex] == pUnit)
                    return true;
            }

            return false;
        }

        public bool TrySelectUnit(int pSlotIndex, UnitDefinition pUnit)
        {
            if (pUnit == null || !_AvailableUnits.Contains(pUnit) || IsUnitSelectedElsewhere(pUnit, pSlotIndex))
                return false;

            if (_SelectedUnits.Count == 0 && pSlotIndex == 0)
                _SelectedUnits.Add(pUnit);
            else if (pSlotIndex < 0 || pSlotIndex >= _SelectedUnits.Count)
                return false;
            else if (_SelectedUnits[pSlotIndex] == pUnit)
                return true;
            else
                _SelectedUnits[pSlotIndex] = pUnit;

            GetOrCreateBuild(pUnit);
            HasPendingChanges = true;
            return true;
        }

        public bool TrySelectPassive(UnitDefinition pUnit, PassiveDefinition pPassive)
        {
            UnitBuild lBuild = GetOrCreateBuild(pUnit);
            if (lBuild == null || pPassive == null || !Contains(pUnit.Passives, pPassive))
                return false;

            if (lBuild.Passive == pPassive)
                return true;

            lBuild.Passive = pPassive;
            HasPendingChanges = true;
            return true;
        }

        public bool IsSkillEquipped(UnitDefinition pUnit, SkillDefinition pSkill, int pExceptSlotIndex = -1)
        {
            UnitBuild lBuild = GetOrCreateBuild(pUnit);
            if (lBuild == null || pSkill == null)
                return false;

            for (int lIndex = 0; lIndex < lBuild.Skills.Count; lIndex++)
            {
                if (lIndex != pExceptSlotIndex && lBuild.Skills[lIndex] == pSkill)
                    return true;
            }

            return false;
        }

        public bool TrySelectSkill(UnitDefinition pUnit, int pSlotIndex, SkillDefinition pSkill)
        {
            UnitBuild lBuild = GetOrCreateBuild(pUnit);
            if (lBuild == null
                || pSkill == null
                || pSlotIndex < 0
                || pSlotIndex >= TeamPreset.EquippedSkillCount
                || !Contains(pUnit.Skills, pSkill)
                || IsSkillEquipped(pUnit, pSkill, pSlotIndex))
                return false;

            if (lBuild.MutableSkills[pSlotIndex] == pSkill)
                return true;

            lBuild.MutableSkills[pSlotIndex] = pSkill;
            HasPendingChanges = true;
            return true;
        }

        public bool CanChangeStat(UnitDefinition pUnit, UnitStatType pStat, int pDelta)
        {
            UnitBuild lBuild = GetOrCreateBuild(pUnit);
            if (lBuild == null || pStat == UnitStatType.RemainingPoints)
                return false;

            UnitStatAllocationPreset lCandidate = lBuild.StatAllocations.Clone();
            int lValue = UnitStatAllocationRules.GetValue(lCandidate, pStat) + pDelta;
            if (lValue < 0)
                return false;

            UnitStatAllocationRules.SetValue(lCandidate, pStat, lValue);
            return UnitStatAllocationRules.TryValidate(pUnit, lCandidate, out _, out _);
        }

        public bool TryChangeStat(UnitDefinition pUnit, UnitStatType pStat, int pDelta)
        {
            UnitBuild lBuild = GetOrCreateBuild(pUnit);
            if (lBuild == null || !CanChangeStat(pUnit, pStat, pDelta))
                return false;

            int lValue = UnitStatAllocationRules.GetValue(lBuild.StatAllocations, pStat) + pDelta;
            UnitStatAllocationRules.SetValue(lBuild.StatAllocations, pStat, lValue);
            HasPendingChanges = true;
            return true;
        }

        public bool Validate(out string pFailure)
        {
            if (_SelectedUnits.Count != _TeamSize)
            {
                pFailure = $"The draft must contain exactly {_TeamSize} units.";
                return false;
            }

            HashSet<UnitDefinition> lUniqueUnits = new HashSet<UnitDefinition>();
            for (int lIndex = 0; lIndex < _SelectedUnits.Count; lIndex++)
            {
                UnitDefinition lUnit = _SelectedUnits[lIndex];
                UnitBuild lBuild = GetOrCreateBuild(lUnit);
                if (lUnit == null || !lUniqueUnits.Add(lUnit) || lBuild == null)
                {
                    pFailure = "The draft contains an empty or duplicated unit.";
                    return false;
                }

                if (lUnit.Passives != null && lUnit.Passives.Count > 0
                    && (lBuild.Passive == null || !Contains(lUnit.Passives, lBuild.Passive)))
                {
                    pFailure = $"Unit '{lUnit.Id}' requires a valid passive.";
                    return false;
                }

                if (!UnitStatAllocationRules.TryValidate(lUnit, lBuild.StatAllocations, out _, out pFailure))
                    return false;

                int lRequiredSkillCount = Math.Min(TeamPreset.EquippedSkillCount, CountAvailableSkills(lUnit.Skills));
                int lEquippedSkillCount = 0;
                HashSet<SkillDefinition> lUniqueSkills = new HashSet<SkillDefinition>();
                for (int lSkillIndex = 0; lSkillIndex < lBuild.Skills.Count; lSkillIndex++)
                {
                    SkillDefinition lSkill = lBuild.Skills[lSkillIndex];
                    if (lSkill == null)
                        continue;

                    if (!Contains(lUnit.Skills, lSkill) || !lUniqueSkills.Add(lSkill))
                    {
                        pFailure = $"Unit '{lUnit.Id}' contains an invalid or duplicated skill.";
                        return false;
                    }

                    lEquippedSkillCount++;
                }

                if (lEquippedSkillCount != lRequiredSkillCount)
                {
                    pFailure = $"Unit '{lUnit.Id}' requires {lRequiredSkillCount} equipped skills.";
                    return false;
                }
            }

            pFailure = string.Empty;
            return true;
        }

        public bool MatchesPersistedPreset()
        {
            if (_PersistedPreset?.Slots == null || _PersistedPreset.Slots.Count < _SelectedUnits.Count)
                return false;

            for (int lIndex = 0; lIndex < _SelectedUnits.Count; lIndex++)
            {
                UnitDefinition lUnit = _SelectedUnits[lIndex];
                UnitBuildPreset lPersistedBuild = _PersistedPreset.Slots[lIndex];
                UnitBuild lDraftBuild = GetOrCreateBuild(lUnit);
                if (lPersistedBuild?.UnitId != lUnit?.Id || lDraftBuild == null)
                    return false;

                string lPassiveId = lDraftBuild.Passive != null ? lDraftBuild.Passive.Id : string.Empty;
                if (!string.Equals(lPersistedBuild.PassiveId ?? string.Empty, lPassiveId, StringComparison.Ordinal)
                    || !HaveSameSkills(lPersistedBuild.SkillIds, lDraftBuild.Skills)
                    || !HaveSameStatAllocations(lPersistedBuild.StatAllocations, lDraftBuild.StatAllocations))
                    return false;
            }

            return true;
        }

        public List<UnitBuildPreset> CreateBuildPresets()
        {
            List<UnitBuildPreset> lPresets = new List<UnitBuildPreset>(_SelectedUnits.Count);
            for (int lIndex = 0; lIndex < _SelectedUnits.Count; lIndex++)
            {
                UnitDefinition lUnit = _SelectedUnits[lIndex];
                UnitBuild lBuild = GetOrCreateBuild(lUnit);
                UnitBuildPreset lPreset = new UnitBuildPreset
                {
                    UnitId = lUnit?.Id ?? string.Empty,
                    PassiveId = lBuild?.Passive != null ? lBuild.Passive.Id : string.Empty,
                    StatAllocations = lBuild?.StatAllocations?.Clone() ?? new UnitStatAllocationPreset()
                };

                if (lBuild != null)
                {
                    for (int lSkillIndex = 0; lSkillIndex < lBuild.Skills.Count; lSkillIndex++)
                    {
                        SkillDefinition lSkill = lBuild.Skills[lSkillIndex];
                        if (lSkill != null)
                            lPreset.SkillIds.Add(lSkill.Id);
                    }
                }

                lPresets.Add(lPreset);
            }

            return lPresets;
        }

        private void AddAvailableUnits(IReadOnlyList<UnitDefinition> pUnits)
        {
            if (pUnits == null)
                return;

            for (int lIndex = 0; lIndex < pUnits.Count; lIndex++)
            {
                UnitDefinition lUnit = pUnits[lIndex];
                if (lUnit != null && !_AvailableUnits.Contains(lUnit))
                    _AvailableUnits.Add(lUnit);
            }
        }

        private void ResolveSelection(IReadOnlyList<UnitDefinition> pUnits, IReadOnlyList<string> pUnitIds)
        {
            if (pUnits != null && pUnits.Count > 0)
            {
                for (int lIndex = 0; lIndex < pUnits.Count && _SelectedUnits.Count < _TeamSize; lIndex++)
                    AddSelectedUnit(FindAvailableUnit(pUnits[lIndex]?.Id));
                return;
            }

            if (pUnitIds == null)
                return;

            for (int lIndex = 0; lIndex < pUnitIds.Count && _SelectedUnits.Count < _TeamSize; lIndex++)
                AddSelectedUnit(FindAvailableUnit(pUnitIds[lIndex]));
        }

        private void FillMissingSlots()
        {
            for (int lIndex = 0; lIndex < _AvailableUnits.Count && _SelectedUnits.Count < _TeamSize; lIndex++)
                AddSelectedUnit(_AvailableUnits[lIndex]);
        }

        private void AddSelectedUnit(UnitDefinition pUnit)
        {
            if (pUnit != null && !_SelectedUnits.Contains(pUnit))
                _SelectedUnits.Add(pUnit);
        }

        private UnitDefinition FindAvailableUnit(string pUnitId)
        {
            if (string.IsNullOrWhiteSpace(pUnitId))
                return null;

            for (int lIndex = 0; lIndex < _AvailableUnits.Count; lIndex++)
            {
                UnitDefinition lUnit = _AvailableUnits[lIndex];
                if (string.Equals(lUnit.Id, pUnitId, StringComparison.Ordinal))
                    return lUnit;
            }

            return null;
        }

        private UnitBuild GetOrCreateBuild(UnitDefinition pUnit)
        {
            if (pUnit == null)
                return null;

            if (_Builds.TryGetValue(pUnit, out UnitBuild lExisting))
                return lExisting;

            UnitBuild lBuild = new UnitBuild(pUnit);
            UnitBuildPreset lPersistedBuild = FindPersistedBuild(pUnit.Id);
            lBuild.Passive = FindPassive(pUnit.Passives, lPersistedBuild?.PassiveId) ?? pUnit.DefaultPassive;
            UnitStatAllocationPreset lPersistedStats = lPersistedBuild?.StatAllocations;
            lBuild.StatAllocations = UnitStatAllocationRules.TryValidate(pUnit, lPersistedStats, out _, out _)
                ? lPersistedStats?.Clone() ?? new UnitStatAllocationPreset()
                : new UnitStatAllocationPreset();

            IReadOnlyList<string> lSkillIds = lPersistedBuild?.SkillIds;
            if (lSkillIds != null)
            {
                for (int lIndex = 0; lIndex < lSkillIds.Count && lBuild.MutableSkills.Count < TeamPreset.EquippedSkillCount; lIndex++)
                {
                    SkillDefinition lSkill = FindSkill(pUnit.Skills, lSkillIds[lIndex]);
                    if (lSkill != null && !lBuild.MutableSkills.Contains(lSkill))
                        lBuild.MutableSkills.Add(lSkill);
                }
            }

            FillDefaultSkills(pUnit, lBuild.MutableSkills);
            while (lBuild.MutableSkills.Count < TeamPreset.EquippedSkillCount)
                lBuild.MutableSkills.Add(null);

            _Builds.Add(pUnit, lBuild);
            return lBuild;
        }

        private UnitBuildPreset FindPersistedBuild(string pUnitId)
        {
            IReadOnlyList<UnitBuildPreset> lSlots = _PersistedPreset?.Slots;
            if (lSlots == null || string.IsNullOrWhiteSpace(pUnitId))
                return null;

            for (int lIndex = 0; lIndex < lSlots.Count; lIndex++)
            {
                UnitBuildPreset lBuild = lSlots[lIndex];
                if (lBuild != null && string.Equals(lBuild.UnitId, pUnitId, StringComparison.Ordinal))
                    return lBuild;
            }

            return null;
        }

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

        private static void FillDefaultSkills(UnitDefinition pUnit, ICollection<SkillDefinition> pTarget)
        {
            if (pUnit?.Skills == null || pTarget == null)
                return;

            for (int lIndex = 0; lIndex < pUnit.Skills.Count && pTarget.Count < TeamPreset.EquippedSkillCount; lIndex++)
            {
                SkillDefinition lSkill = pUnit.Skills[lIndex];
                if (lSkill != null && !pTarget.Contains(lSkill))
                    pTarget.Add(lSkill);
            }
        }

        private static int CountAvailableSkills(IReadOnlyList<SkillDefinition> pSkills)
        {
            if (pSkills == null)
                return 0;

            HashSet<SkillDefinition> lSkills = new HashSet<SkillDefinition>();
            for (int lIndex = 0; lIndex < pSkills.Count; lIndex++)
            {
                if (pSkills[lIndex] != null)
                    lSkills.Add(pSkills[lIndex]);
            }

            return lSkills.Count;
        }

        private static bool Contains<T>(IReadOnlyList<T> pValues, T pValue) where T : class
        {
            if (pValues == null || pValue == null)
                return false;

            for (int lIndex = 0; lIndex < pValues.Count; lIndex++)
            {
                if (pValues[lIndex] == pValue)
                    return true;
            }

            return false;
        }

        private static bool HaveSameSkills(IReadOnlyList<string> pPersistedIds, IReadOnlyList<SkillDefinition> pSkills)
        {
            int lPersistedCount = pPersistedIds?.Count ?? 0;
            int lSkillCount = 0;
            if (pSkills != null)
            {
                for (int lIndex = 0; lIndex < pSkills.Count; lIndex++)
                {
                    if (pSkills[lIndex] != null)
                        lSkillCount++;
                }
            }

            if (lPersistedCount != lSkillCount)
                return false;

            int lPersistedIndex = 0;
            for (int lIndex = 0; pSkills != null && lIndex < pSkills.Count; lIndex++)
            {
                SkillDefinition lSkill = pSkills[lIndex];
                if (lSkill == null)
                    continue;

                if (!string.Equals(pPersistedIds[lPersistedIndex], lSkill.Id, StringComparison.Ordinal))
                    return false;
                lPersistedIndex++;
            }

            return true;
        }

        private static bool HaveSameStatAllocations(UnitStatAllocationPreset pLeft, UnitStatAllocationPreset pRight)
        {
            pLeft ??= new UnitStatAllocationPreset();
            pRight ??= new UnitStatAllocationPreset();
            return pLeft.Health == pRight.Health
                && pLeft.Energy == pRight.Energy
                && pLeft.Mobility == pRight.Mobility
                && pLeft.MeleeDamage == pRight.MeleeDamage
                && pLeft.MeleeResistance == pRight.MeleeResistance
                && pLeft.RangedDamage == pRight.RangedDamage
                && pLeft.RangedResistance == pRight.RangedResistance
                && pLeft.Velocity == pRight.Velocity;
        }
    }
}
