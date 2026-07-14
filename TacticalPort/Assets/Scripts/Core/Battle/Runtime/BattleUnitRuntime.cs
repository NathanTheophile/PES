#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core
{
    public sealed class UnitRuntime
    {
        public const int BaseWearPercent = 5;
        public const int MaxWearPercent = 50;

        #region _____________________________| INIT

        public UnitRuntime(UnitId pId, UnitDefinition pDefinition, GridCoord pPosition, Team? pTeamOverride = null, int pTeamSlotIndex = -1)
        {
            if (pDefinition == null)
                throw new ArgumentNullException(nameof(pDefinition));

            Id = pId;
            Definition = pDefinition;
            _TeamOverride = pTeamOverride;
            TeamSlotIndex = pTeamSlotIndex;
            Position = pPosition;
            _Skills = BuildRuntimeSkills(pDefinition);
            _OccupiedCellOffsets = BuildOccupiedCellOffsets(pDefinition);
            CurrentHealth = Math.Max(1, pDefinition.MaxHealth);
            CurrentMaxHealth = CurrentHealth;
            RemainingMobility = 0;
            RemainingEnergy = 0;
            _ActivePassive = ResolveInitialPassive(pDefinition);
            InitializePersistentStates();
        }

        #endregion

        #region _____________________________/ VALUES

        private readonly List<SkillDefinition> _Skills;
        private readonly List<GridCoord> _OccupiedCellOffsets;
        private readonly BattleUnitStateCollection _States = new BattleUnitStateCollection();
        private readonly BattleUnitSkillUsageTracker _SkillUsage = new BattleUnitSkillUsageTracker();
        private readonly Team? _TeamOverride;
        private PassiveDefinition _ActivePassive;
        private int _WearRemainder;
        private bool _IsTurnActive;

        #endregion

        #region _____________________________/ ACCESSORS

        public UnitId Id { get; }
        public UnitDefinition Definition { get; }
        public Team Team => _TeamOverride ?? Definition.Team;
        public int TeamSlotIndex { get; }
        public GridCoord Position { get; private set; }
        public int CurrentHealth { get; private set; }
        public int CurrentMaxHealth { get; private set; }
        public int EffectiveWearPercent => Math.Min(MaxWearPercent, Math.Max(BaseWearPercent, BaseWearPercent + _States.GetWearPercent()));
        internal int WearRemainder => _WearRemainder;
        public int RemainingMobility { get; private set; }
        public int RemainingEnergy { get; private set; }
        public bool IsAlive => CurrentHealth > 0;
        public IReadOnlyList<SkillDefinition> Skills => _Skills;
        public IReadOnlyList<GridCoord> OccupiedCellOffsets => _OccupiedCellOffsets;
        public IReadOnlyList<BattleStateRuntime> ActiveStates => _States.States;
        public PassiveDefinition ActivePassive => _ActivePassive;
        public bool IsTurnActive => _IsTurnActive;
        public event Action<int, bool> ValueChanged;

        #endregion

        #region _____________________________| TURN

        public void BeginTurn()
        {
            _IsTurnActive = true;
            _SkillUsage.TickCooldowns();
            _SkillUsage.ClearTurnUses();
            _States.RemoveExpiredStates();
            if (!IsAlive)
            {
                _IsTurnActive = false;
                RemainingMobility = 0;
                RemainingEnergy = 0;
                return;
            }

            RemainingMobility = Math.Max(0, Definition.MobilityPerTurn + GetMobilityModifier());
            RemainingEnergy = Math.Max(0, Definition.EnergyPerTurn + GetEnergyModifier());
        }

        public void EndTurn()
        {
            _States.TickDurationsAtTurnEnd();
            _IsTurnActive = false;
            ClearTurnResources();
        }

        #endregion

        #region _____________________________| RESOURCES

        public bool CanSpendMobility(int pAmount) => pAmount >= 0 && RemainingMobility >= pAmount;
        public bool CanSpendEnergy(int pAmount) => pAmount >= 0 && RemainingEnergy >= pAmount;

        public bool TrySpendMobility(int pAmount)
        {
            if (!CanSpendMobility(pAmount))
                return false;

            RemainingMobility -= pAmount;
            return true;
        }

        public bool TrySpendEnergy(int pAmount)
        {
            if (!CanSpendEnergy(pAmount))
                return false;

            RemainingEnergy -= pAmount;
            return true;
        }

        private void ClearTurnResources()
        {
            RemainingMobility = 0;
            RemainingEnergy = 0;
        }

        #endregion

        #region _____________________________| STATE

        public void SetPosition(GridCoord pPosition) => Position = pPosition;

        public IEnumerable<GridCoord> EnumerateOccupiedCells()
        {
            for (int lIndex = 0; lIndex < _OccupiedCellOffsets.Count; lIndex++)
            {
                GridCoord lOffset = _OccupiedCellOffsets[lIndex];
                yield return new GridCoord(Position.X + lOffset.X, Position.Y + lOffset.Y);
            }
        }

        public bool HasState(StateDefinition pState) => _States.HasState(pState);

        public bool HasPassiveMarker(StateDefinition pState) => _States.HasPassiveMarker(pState);

        public int GetStateStacks(StateDefinition pState) => _States.GetStateStacks(pState);

        public int GetDamageModifier() => _States.GetDamageModifier();

        public int GetGeneralDamagePercent() => Definition.GeneralDamagePercent + GetDamageModifier();

        public int GetMeleeDamageModifier() => _States.GetMeleeDamageModifier();

        public int GetRangedDamageModifier() => _States.GetRangedDamageModifier();

        public int GetMeleeResistancePercent() => ClampResistancePercent(
            Definition.MeleeResistancePercent
            + _States.GetMeleeResistancePercent()
            + Definition.GeneralResistancePercent
            + _States.GetGeneralResistancePercent());

        public int GetRangedResistancePercent() => ClampResistancePercent(
            Definition.RangedResistancePercent
            + _States.GetRangedResistancePercent()
            + Definition.GeneralResistancePercent
            + _States.GetGeneralResistancePercent());

        public int GetRangeModifier() => _States.GetRangeModifier();

        public int GetEnergyModifier() => _States.GetEnergyModifier();

        public int GetMobilityModifier() => _States.GetMobilityModifier();

        public int GetSkillRangeMax(SkillDefinition pSkill) => pSkill != null ? Math.Max(0, pSkill.RangeMax + GetRangeModifier()) : 0;

        public int GetSkillRangeMin(SkillDefinition pSkill)
        {
            if (pSkill == null)
                return 0;

            int lRangeMax = GetSkillRangeMax(pSkill);
            return Math.Max(0, Math.Min(pSkill.RangeMin, lRangeMax));
        }

        public int ResolveOutgoingDamage(int pBaseDamage, UnitRuntime pTarget = null)
            => ResolveOutgoingDamage(pBaseDamage, ResolveDamageRangeTo(pTarget));

        public int ResolveOutgoingDamage(int pBaseDamage, DamageRangeType pDamageRange)
        {
            if (pBaseDamage <= 0)
                return 0;

            int lDamagePercent = ResolveDamagePercent(pDamageRange);
            return Math.Max(0, pBaseDamage * Math.Max(0, lDamagePercent) / 100);
        }

        public int ResolveIncomingDamage(int pAmount, DamageRangeType pDamageRange)
        {
            if (pAmount <= 0)
                return 0;

            int lResistancePercent = ResolveResistancePercent(pDamageRange);
            return Math.Max(0, pAmount * (100 - lResistancePercent) / 100);
        }

        public int ApplyDamage(int pAmount, DamageRangeType pDamageRange = DamageRangeType.None)
            => ApplyDamageInternal(pAmount, pDamageRange, false);

        public int ApplySkillDamage(int pAmount, DamageRangeType pDamageRange = DamageRangeType.None)
            => ApplyDamageInternal(pAmount, pDamageRange, true);

        private int ApplyDamageInternal(int pAmount, DamageRangeType pDamageRange, bool pApplyWear)
        {
            if (pAmount <= 0 || !IsAlive)
                return 0;

            int lResolvedAmount = ResolveIncomingDamage(pAmount, pDamageRange);
            if (lResolvedAmount <= 0)
                return 0;

            int lApplied = Math.Min(CurrentHealth, lResolvedAmount);
            CurrentHealth -= lApplied;
            if (pApplyWear)
                ApplyWear(lApplied);

            if (lApplied > 0)
                ValueChanged?.Invoke(lApplied, false);
            return lApplied;
        }

        public int RestoreHealth(int pAmount)
        {
            if (pAmount <= 0 || !IsAlive)
                return 0;

            int lMissingHealth = Math.Max(0, CurrentMaxHealth - CurrentHealth);
            int lRestored = Math.Min(lMissingHealth, pAmount);
            CurrentHealth += lRestored;
            if (lRestored > 0)
                ValueChanged?.Invoke(lRestored, true);
            return lRestored;
        }

        public void SetPersistentState(string pStateKey, StateDefinition pState, int pStacks = 1)
        {
            int lPreviousEnergyModifier = GetEnergyModifier();
            int lPreviousMobilityModifier = GetMobilityModifier();
            _States.SetPersistentState(pStateKey, pState, pStacks);
            RefreshTurnResourcesAfterModifierChange(lPreviousEnergyModifier, lPreviousMobilityModifier);
        }

        public bool TryApplyState(StateDefinition pState, int pStacks = 1, int pDurationTurns = -1)
        {
            int lPreviousEnergyModifier = GetEnergyModifier();
            int lPreviousMobilityModifier = GetMobilityModifier();
            if (!_States.TryApplyState(pState, pStacks, pDurationTurns))
                return false;

            RefreshTurnResourcesAfterModifierChange(lPreviousEnergyModifier, lPreviousMobilityModifier);
            return true;
        }

        public bool RemoveTemporaryState(StateDefinition pState)
        {
            int lPreviousEnergyModifier = GetEnergyModifier();
            int lPreviousMobilityModifier = GetMobilityModifier();
            if (!_States.RemoveTemporaryState(pState))
                return false;

            RefreshTurnResourcesAfterModifierChange(lPreviousEnergyModifier, lPreviousMobilityModifier);
            return true;
        }

        public bool RemoveStateByKey(string pStateKey)
        {
            int lPreviousEnergyModifier = GetEnergyModifier();
            int lPreviousMobilityModifier = GetMobilityModifier();
            if (!_States.RemoveStateByKey(pStateKey))
                return false;

            RefreshTurnResourcesAfterModifierChange(lPreviousEnergyModifier, lPreviousMobilityModifier);
            return true;
        }

        public SkillDefinition ResolveEffectiveSkill(SkillDefinition pBaseSkill) =>
            pBaseSkill?.Variant != null && HasSkillVariantEnabler() ? pBaseSkill.Variant : pBaseSkill;

        public int ConsumeSkillVariantEnablers()
        {
            List<BattleStateRuntime> lVariantStates = new List<BattleStateRuntime>();
            for (int lIndex = 0; lIndex < _States.States.Count; lIndex++)
            {
                BattleStateRuntime lState = _States.States[lIndex];
                if (lState?.Definition != null && lState.Definition.EnablesSkillVariant)
                    lVariantStates.Add(lState);
            }

            int lConsumedCount = 0;
            for (int lIndex = 0; lIndex < lVariantStates.Count; lIndex++)
            {
                BattleStateRuntime lState = lVariantStates[lIndex];
                int lPreviousEnergyModifier = GetEnergyModifier();
                int lPreviousMobilityModifier = GetMobilityModifier();
                if (!_States.ReplaceStateByKey(lState.Key, lState.Definition.VariantConsumedReplacementState))
                    continue;

                RefreshTurnResourcesAfterModifierChange(lPreviousEnergyModifier, lPreviousMobilityModifier);
                lConsumedCount++;
            }

            return lConsumedCount;
        }

        public int GetStateProgressionIndex(StateProgressionDefinition pProgression)
        {
            if (pProgression?.States == null)
                return -1;

            string lProgressionKey = ResolveProgressionKey(pProgression);
            for (int lStateIndex = 0; lStateIndex < _States.States.Count; lStateIndex++)
            {
                BattleStateRuntime lRuntimeState = _States.States[lStateIndex];
                if (lRuntimeState == null || lRuntimeState.Key != lProgressionKey)
                    continue;

                for (int lProgressionIndex = 0; lProgressionIndex < pProgression.States.Count; lProgressionIndex++)
                {
                    if (pProgression.States[lProgressionIndex] == lRuntimeState.Definition)
                        return lProgressionIndex;
                }
            }

            return -1;
        }

        public bool AdvanceStateProgression(StateProgressionDefinition pProgression, int pSteps = 1)
        {
            if (pProgression?.States == null || pProgression.States.Count == 0 || pSteps <= 0)
                return false;

            int lCurrentIndex = GetStateProgressionIndex(pProgression);
            int lNextIndex = Math.Min(pProgression.States.Count - 1, lCurrentIndex + pSteps);
            if (lNextIndex <= lCurrentIndex || pProgression.States[lNextIndex] == null)
                return false;

            SetPersistentState(ResolveProgressionKey(pProgression), pProgression.States[lNextIndex], 1);
            return true;
        }

        public bool TrySetActivePassive(PassiveDefinition pPassive)
        {
            if (pPassive != null && !ContainsPassive(Definition.Passives, pPassive))
                return false;

            _ActivePassive = pPassive;
            ClampTurnResourcesToCurrentMax();
            return true;
        }

        public bool TryGetSkill(SkillId pSkillId, out SkillDefinition pSkill)
        {
            if (pSkillId == SkillId.None)
            {
                pSkill = null;
                return false;
            }

            foreach (SkillDefinition lCandidate in _Skills)
            {
                if (new SkillId(lCandidate.Id) == pSkillId)
                {
                    pSkill = lCandidate;
                    return true;
                }

            }

            pSkill = null;
            return false;
        }

        public int GetRemainingCooldown(SkillDefinition pSkill) => _SkillUsage.GetRemainingCooldown(pSkill);

        public int GetSkillUsesThisTurn(SkillDefinition pSkill) => _SkillUsage.GetSkillUsesThisTurn(pSkill);

        public int GetSkillUsesOnTarget(SkillDefinition pSkill, string pTargetKey) => _SkillUsage.GetSkillUsesOnTarget(pSkill, pTargetKey);

        public bool TryValidateSkillUsage(SkillDefinition pSkill, IEnumerable<string> pTargetKeys, out string pFailureReason)
            => _SkillUsage.TryValidateUsage(pSkill, pTargetKeys, out pFailureReason);

        public void RegisterSkillUse(SkillDefinition pSkill, IEnumerable<string> pTargetKeys)
            => _SkillUsage.RegisterUse(pSkill, pTargetKeys);

        #endregion

        #region _____________________________| HELPERS

        private void ClampTurnResourcesToCurrentMax()
        {
            if (!IsAlive)
            {
                RemainingMobility = 0;
                RemainingEnergy = 0;
                return;
            }

            RemainingMobility = Math.Max(0, Math.Min(RemainingMobility, Definition.MobilityPerTurn + GetMobilityModifier()));
            RemainingEnergy = Math.Max(0, Math.Min(RemainingEnergy, Definition.EnergyPerTurn + GetEnergyModifier()));
        }

        private void RefreshTurnResourcesAfterModifierChange(int pPreviousEnergyModifier, int pPreviousMobilityModifier)
        {
            if (!_IsTurnActive || !IsAlive)
            {
                if (!_IsTurnActive)
                    ClearTurnResources();
                else
                    ClampTurnResourcesToCurrentMax();
                return;
            }

            int lEnergyDelta = GetEnergyModifier() - pPreviousEnergyModifier;
            int lMobilityDelta = GetMobilityModifier() - pPreviousMobilityModifier;
            RemainingEnergy = Math.Max(0, RemainingEnergy + lEnergyDelta);
            RemainingMobility = Math.Max(0, RemainingMobility + lMobilityDelta);
            ClampTurnResourcesToCurrentMax();
        }

        private bool HasSkillVariantEnabler()
        {
            for (int lIndex = 0; lIndex < _States.States.Count; lIndex++)
            {
                if (_States.States[lIndex]?.Definition?.EnablesSkillVariant == true)
                    return true;
            }

            return false;
        }

        private static string ResolveProgressionKey(StateProgressionDefinition pProgression) =>
            pProgression != null ? $"progression::{pProgression.Id}" : "progression::";

        private static List<SkillDefinition> BuildRuntimeSkills(UnitDefinition pDefinition)
        {
            List<SkillDefinition> lSkills = new List<SkillDefinition>();

            if (pDefinition.Skills != null)
            {
                foreach (SkillDefinition lSkill in pDefinition.Skills)
                {
                    if (lSkill != null)
                        lSkills.Add(lSkill);
                }
            }

            return lSkills;
        }

        private static List<GridCoord> BuildOccupiedCellOffsets(UnitDefinition pDefinition)
        {
            int lWidth = pDefinition != null ? pDefinition.FootprintWidth : 1;
            int lHeight = pDefinition != null ? pDefinition.FootprintHeight : 1;
            List<GridCoord> lOffsets = new List<GridCoord>(lWidth * lHeight);

            for (int lOffsetY = 0; lOffsetY < lHeight; lOffsetY++)
            {
                for (int lOffsetX = 0; lOffsetX < lWidth; lOffsetX++)
                    lOffsets.Add(new GridCoord(lOffsetX, lOffsetY));
            }

            return lOffsets;
        }

        private void InitializePersistentStates()
        {
            if (Definition?.BaseStates == null)
                return;

            for (int lIndex = 0; lIndex < Definition.BaseStates.Count; lIndex++)
            {
                UnitStateEntry lEntry = Definition.BaseStates[lIndex];
                if (lEntry?.State == null)
                    continue;

                SetPersistentState($"base::{lIndex}", lEntry.State, lEntry.Stacks);
            }
        }

        private static PassiveDefinition ResolveInitialPassive(UnitDefinition pDefinition) => pDefinition != null ? pDefinition.DefaultPassive : null;

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

        public DamageRangeType ResolveDamageRangeTo(UnitRuntime pTarget)
        {
            if (pTarget == null)
                return DamageRangeType.Ranged;

            return ResolveDistanceToUnit(pTarget) <= 1 ? DamageRangeType.Melee : DamageRangeType.Ranged;
        }

        private int ResolveDamagePercent(DamageRangeType pDamageRange)
        {
            bool lIsMelee = pDamageRange == DamageRangeType.Melee;
            int lBasePercent = lIsMelee ? Definition.MeleeDamagePercent : Definition.RangedDamagePercent;
            int lTypedModifier = lIsMelee ? GetMeleeDamageModifier() : GetRangedDamageModifier();
            return lBasePercent + lTypedModifier + GetGeneralDamagePercent();
        }

        private int ResolveResistancePercent(DamageRangeType pDamageRange)
        {
            switch (pDamageRange)
            {
                case DamageRangeType.Melee:
                    return GetMeleeResistancePercent();

                case DamageRangeType.Ranged:
                    return GetRangedResistancePercent();

                default:
                    return 0;
            }
        }

        private static int ClampResistancePercent(int pValue) => Math.Min(100, pValue);

        private void ApplyWear(int pAppliedDamage)
        {
            if (pAppliedDamage <= 0 || CurrentMaxHealth <= 1)
                return;

            long lWearNumerator = (long)pAppliedDamage * EffectiveWearPercent + _WearRemainder;
            int lMaxHealthLoss = (int)Math.Min(int.MaxValue, lWearNumerator / 100);
            _WearRemainder = (int)(lWearNumerator % 100);
            if (lMaxHealthLoss <= 0)
                return;

            CurrentMaxHealth = Math.Max(1, CurrentMaxHealth - lMaxHealthLoss);
            if (CurrentMaxHealth == 1)
                _WearRemainder = 0;
        }

        private int ResolveDistanceToUnit(UnitRuntime pTarget)
        {
            if (pTarget == null)
                return int.MaxValue;

            int lBestDistance = int.MaxValue;
            foreach (GridCoord lSourceCell in EnumerateOccupiedCells())
            {
                foreach (GridCoord lTargetCell in pTarget.EnumerateOccupiedCells())
                    lBestDistance = Math.Min(lBestDistance, lSourceCell.ManhattanDistanceTo(lTargetCell));
            }

            return lBestDistance;
        }

        #endregion
    }
}
