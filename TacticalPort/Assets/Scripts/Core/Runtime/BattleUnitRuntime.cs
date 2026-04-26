using System;
using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core.Runtime
{
    public sealed class BattleUnitRuntime
    {
        #region _____________________________| INIT

        public BattleUnitRuntime(BattleUnitId pId, BattleUnitDefinition pDefinition, GridCoord pPosition)
        {
            if (pDefinition == null)
                throw new ArgumentNullException(nameof(pDefinition));

            Id = pId;
            Definition = pDefinition;
            Position = pPosition;
            _Skills = BuildRuntimeSkills(pDefinition);
            _OccupiedCellOffsets = BuildOccupiedCellOffsets(pDefinition);
            FacingDirection = new GridCoord(0, -1);
            CurrentHealth = Math.Max(1, pDefinition.MaxHealth);
            RemainingMovement = 0;
            RemainingActionPoints = 0;
            InitializePersistentStates();
        }

        #endregion

        #region _____________________________| VALUES

        private readonly List<SkillDefinition> _Skills;
        private readonly List<GridCoord> _OccupiedCellOffsets;
        private readonly List<BattleStateRuntime> _ActiveStates = new List<BattleStateRuntime>();
        private readonly Dictionary<string, int> _SkillCooldowns = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _SkillUsesThisTurn = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _SkillUsesByTarget = new Dictionary<string, int>();

        #endregion

        #region _____________________________| ACCESSORS

        public BattleUnitId Id { get; }
        public BattleUnitDefinition Definition { get; }
        public BattleTeam Team => Definition.Team;
        public GridCoord Position { get; private set; }
        public GridCoord FacingDirection { get; private set; }
        public int CurrentHealth { get; private set; }
        public int RemainingMovement { get; private set; }
        public int RemainingActionPoints { get; private set; }
        public bool IsAlive => CurrentHealth > 0;
        public IReadOnlyList<SkillDefinition> Skills => _Skills;
        public IReadOnlyList<GridCoord> OccupiedCellOffsets => _OccupiedCellOffsets;
        public IReadOnlyList<BattleStateRuntime> ActiveStates => _ActiveStates;
        public event Action<int, bool> ValueChanged;

        #endregion

        #region _____________________________| TURN

        public void BeginTurn()
        {
            TickCooldowns();
            _SkillUsesThisTurn.Clear();
            RemoveExpiredStates();

            if (!IsAlive)
            {
                RemainingMovement = 0;
                RemainingActionPoints = 0;
                return;
            }

            RemainingMovement = Math.Max(0, Definition.MoveRange);
            RemainingActionPoints = Math.Max(0, Definition.ActionPointsPerTurn);
        }

        public void EndTurn()
        {
            TickStateDurationsAtTurnEnd();
            ClearTurnResources();
        }

        #endregion

        #region _____________________________| RESOURCES

        public bool CanSpendMovement(int pAmount) => pAmount >= 0 && RemainingMovement >= pAmount;
        public bool CanSpendActionPoints(int pAmount) => pAmount >= 0 && RemainingActionPoints >= pAmount;

        public bool TrySpendMovement(int pAmount)
        {
            if (!CanSpendMovement(pAmount))
                return false;

            RemainingMovement -= pAmount;
            return true;
        }

        public bool TrySpendActionPoints(int pAmount)
        {
            if (!CanSpendActionPoints(pAmount))
                return false;

            RemainingActionPoints -= pAmount;
            return true;
        }

        private void ClearTurnResources()
        {
            RemainingMovement = 0;
            RemainingActionPoints = 0;
        }

        #endregion

        #region _____________________________| STATE

        public void SetPosition(GridCoord pPosition) => Position = pPosition;

        public void FaceTowards(GridCoord pTargetCell)
        {
            GridCoord lDirection = NormalizeCardinalDirection(new GridCoord(pTargetCell.X - Position.X, pTargetCell.Y - Position.Y));
            if (lDirection.X == 0 && lDirection.Y == 0)
                return;

            FacingDirection = lDirection;
        }

        public void FaceDirection(GridCoord pDirection)
        {
            GridCoord lDirection = NormalizeCardinalDirection(pDirection);
            if (lDirection.X == 0 && lDirection.Y == 0)
                return;

            FacingDirection = lDirection;
        }

        public IEnumerable<GridCoord> EnumerateOccupiedCells()
        {
            for (int lIndex = 0; lIndex < _OccupiedCellOffsets.Count; lIndex++)
            {
                GridCoord lOffset = _OccupiedCellOffsets[lIndex];
                yield return new GridCoord(Position.X + lOffset.X, Position.Y + lOffset.Y);
            }
        }

        public bool HasState(StateDefinition pState) => pState != null && GetStateStacks(pState) > 0;

        public bool HasPassiveMarker(StateDefinition pState)
        {
            return pState != null && pState.IsPassiveMarker && HasState(pState);
        }

        public int GetStateStacks(StateDefinition pState)
        {
            if (pState == null)
                return 0;

            int lTotalStacks = 0;
            foreach (BattleStateRuntime lState in _ActiveStates)
            {
                if (lState?.Definition == pState)
                    lTotalStacks += lState.Stacks;
            }

            return Math.Max(0, lTotalStacks);
        }

        public int GetDamageModifier()
        {
            int lModifier = 0;

            foreach (BattleStateRuntime lState in _ActiveStates)
            {
                if (lState?.Definition == null)
                    continue;

                lModifier += lState.Definition.DamageModifierPerStack * lState.Stacks;
            }

            return lModifier;
        }

        public int GetDamageReduction()
        {
            int lReduction = 0;

            foreach (BattleStateRuntime lState in _ActiveStates)
            {
                if (lState?.Definition == null)
                    continue;

                lReduction += lState.Definition.DamageReductionPerStack * lState.Stacks;
            }

            return Math.Max(0, lReduction);
        }

        public int GetRangeModifier()
        {
            int lModifier = 0;

            foreach (BattleStateRuntime lState in _ActiveStates)
            {
                if (lState?.Definition == null)
                    continue;

                lModifier += lState.Definition.RangeModifierPerStack * lState.Stacks;
            }

            return lModifier;
        }

        public int GetSkillRangeMax(SkillDefinition pSkill)
        {
            if (pSkill == null)
                return 0;

            return Math.Max(0, pSkill.RangeMax + GetRangeModifier());
        }

        public int GetSkillRangeMin(SkillDefinition pSkill)
        {
            if (pSkill == null)
                return 0;

            int lRangeMax = GetSkillRangeMax(pSkill);
            return Math.Max(0, Math.Min(pSkill.RangeMin, lRangeMax));
        }

        public int ResolveOutgoingDamage(int pBaseDamage)
        {
            return Math.Max(0, pBaseDamage + GetDamageModifier());
        }

        public int ApplyDamage(int pAmount)
        {
            if (pAmount <= 0 || !IsAlive)
                return 0;

            int lResolvedAmount = Math.Max(0, pAmount - GetDamageReduction());
            if (lResolvedAmount <= 0)
                return 0;

            int lApplied = Math.Min(CurrentHealth, lResolvedAmount);
            CurrentHealth -= lApplied;
            if (lApplied > 0)
                ValueChanged?.Invoke(lApplied, false);
            return lApplied;
        }

        public int RestoreHealth(int pAmount)
        {
            if (pAmount <= 0 || !IsAlive)
                return 0;

            int lMissingHealth = Math.Max(0, Definition.MaxHealth - CurrentHealth);
            int lRestored = Math.Min(lMissingHealth, pAmount);
            CurrentHealth += lRestored;
            if (lRestored > 0)
                ValueChanged?.Invoke(lRestored, true);
            return lRestored;
        }

        public void SetPersistentState(string pStateKey, StateDefinition pState, int pStacks = 1)
        {
            if (string.IsNullOrWhiteSpace(pStateKey))
                return;

            if (pState == null || pStacks <= 0)
            {
                RemoveStateByKey(pStateKey);
                return;
            }

            BattleStateRuntime lExistingState = FindStateByKey(pStateKey);
            if (lExistingState != null)
            {
                lExistingState.Reconfigure(pState, pStacks, 0, true);
                return;
            }

            _ActiveStates.Add(new BattleStateRuntime(pStateKey, pState, pStacks, 0, true));
        }

        public bool TryApplyState(StateDefinition pState, int pStacks = 1, int pDurationTurns = -1)
        {
            if (pState == null || pStacks <= 0)
                return false;

            string lStateKey = ResolveTemporaryStateKey(pState);
            int lResolvedDuration = pDurationTurns >= 0 ? pDurationTurns : pState.DurationTurns;
            BattleStateRuntime lExistingState = FindStateByKey(lStateKey);
            if (lExistingState != null)
            {
                lExistingState.AddStacks(pStacks);
                lExistingState.SetRemainingTurns(lResolvedDuration);
                return true;
            }

            _ActiveStates.Add(new BattleStateRuntime(lStateKey, pState, pStacks, lResolvedDuration, false));
            return true;
        }

        public bool RemoveTemporaryState(StateDefinition pState)
        {
            if (pState == null)
                return false;

            return RemoveStateByKey(ResolveTemporaryStateKey(pState));
        }

        public bool RemoveStateByKey(string pStateKey)
        {
            if (string.IsNullOrWhiteSpace(pStateKey))
                return false;

            for (int lIndex = _ActiveStates.Count - 1; lIndex >= 0; lIndex--)
            {
                if (_ActiveStates[lIndex] == null || _ActiveStates[lIndex].Key != pStateKey)
                    continue;

                _ActiveStates.RemoveAt(lIndex);
                return true;
            }

            return false;
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

        public int GetRemainingCooldown(SkillDefinition pSkill)
        {
            if (pSkill == null)
                return 0;

            return _SkillCooldowns.TryGetValue(ResolveSkillKey(pSkill), out int lRemainingTurns)
                ? Math.Max(0, lRemainingTurns)
                : 0;
        }

        public int GetSkillUsesThisTurn(SkillDefinition pSkill)
        {
            if (pSkill == null)
                return 0;

            return _SkillUsesThisTurn.TryGetValue(ResolveSkillKey(pSkill), out int lCount)
                ? Math.Max(0, lCount)
                : 0;
        }

        public int GetSkillUsesOnTarget(SkillDefinition pSkill, string pTargetKey)
        {
            if (pSkill == null || string.IsNullOrWhiteSpace(pTargetKey))
                return 0;

            return _SkillUsesByTarget.TryGetValue(ResolveSkillTargetKey(pSkill, pTargetKey), out int lCount)
                ? Math.Max(0, lCount)
                : 0;
        }

        public bool TryValidateSkillUsage(SkillDefinition pSkill, IEnumerable<string> pTargetKeys, out string pFailureReason)
        {
            pFailureReason = string.Empty;

            if (pSkill == null)
            {
                pFailureReason = "Skill is missing.";
                return false;
            }

            int lRemainingCooldown = GetRemainingCooldown(pSkill);
            if (lRemainingCooldown > 0)
            {
                pFailureReason = $"{pSkill.DisplayName} is on cooldown for {lRemainingCooldown} more turn(s).";
                return false;
            }

            if (pSkill.UsePerTurn > 0 && GetSkillUsesThisTurn(pSkill) >= pSkill.UsePerTurn)
            {
                pFailureReason = $"{pSkill.DisplayName} has reached its turn usage limit.";
                return false;
            }

            if (pSkill.UsePerTarget <= 0 || pTargetKeys == null)
                return true;

            HashSet<string> lUniqueKeys = new HashSet<string>();
            foreach (string lTargetKey in pTargetKeys)
            {
                if (string.IsNullOrWhiteSpace(lTargetKey) || !lUniqueKeys.Add(lTargetKey))
                    continue;

                if (GetSkillUsesOnTarget(pSkill, lTargetKey) >= pSkill.UsePerTarget)
                {
                    pFailureReason = $"{pSkill.DisplayName} cannot affect this target anymore.";
                    return false;
                }
            }

            return true;
        }

        public void RegisterSkillUse(SkillDefinition pSkill, IEnumerable<string> pTargetKeys)
        {
            if (pSkill == null)
                return;

            string lSkillKey = ResolveSkillKey(pSkill);
            _SkillUsesThisTurn[lSkillKey] = GetSkillUsesThisTurn(pSkill) + 1;

            if (pSkill.CooldownTurns > 0)
                _SkillCooldowns[lSkillKey] = pSkill.CooldownTurns + 1;

            if (pTargetKeys == null || pSkill.UsePerTarget <= 0)
                return;

            HashSet<string> lUniqueKeys = new HashSet<string>();
            foreach (string lTargetKey in pTargetKeys)
            {
                if (string.IsNullOrWhiteSpace(lTargetKey) || !lUniqueKeys.Add(lTargetKey))
                    continue;

                string lCompositeKey = ResolveSkillTargetKey(pSkill, lTargetKey);
                _SkillUsesByTarget[lCompositeKey] = GetSkillUsesOnTarget(pSkill, lTargetKey) + 1;
            }
        }

        #endregion

        #region _____________________________| HELPERS

        private BattleStateRuntime FindStateByKey(string pStateKey)
        {
            if (string.IsNullOrWhiteSpace(pStateKey))
                return null;

            foreach (BattleStateRuntime lState in _ActiveStates)
            {
                if (lState != null && lState.Key == pStateKey)
                    return lState;
            }

            return null;
        }

        private void TickStateDurationsAtTurnEnd()
        {
            if (_ActiveStates.Count == 0)
                return;

            for (int lIndex = _ActiveStates.Count - 1; lIndex >= 0; lIndex--)
            {
                BattleStateRuntime lState = _ActiveStates[lIndex];
                if (lState == null)
                {
                    _ActiveStates.RemoveAt(lIndex);
                    continue;
                }

                if (lState.TickTurnEnd())
                    _ActiveStates.RemoveAt(lIndex);
            }
        }

        private void RemoveExpiredStates()
        {
            for (int lIndex = _ActiveStates.Count - 1; lIndex >= 0; lIndex--)
            {
                BattleStateRuntime lState = _ActiveStates[lIndex];
                if (lState == null)
                {
                    _ActiveStates.RemoveAt(lIndex);
                    continue;
                }
            }
        }

        private void TickCooldowns()
        {
            if (_SkillCooldowns.Count == 0)
                return;

            List<string> lKeys = new List<string>(_SkillCooldowns.Keys);
            foreach (string lKey in lKeys)
            {
                int lNextValue = Math.Max(0, _SkillCooldowns[lKey] - 1);
                if (lNextValue == 0)
                {
                    _SkillCooldowns.Remove(lKey);
                    continue;
                }

                _SkillCooldowns[lKey] = lNextValue;
            }
        }

        private static string ResolveTemporaryStateKey(StateDefinition pState)
        {
            return pState != null && !string.IsNullOrWhiteSpace(pState.Id)
                ? $"temporary::{pState.Id}"
                : "temporary::";
        }

        private static string ResolveSkillKey(SkillDefinition pSkill)
        {
            return pSkill != null && !string.IsNullOrWhiteSpace(pSkill.Id) ? pSkill.Id : string.Empty;
        }

        private static string ResolveSkillTargetKey(SkillDefinition pSkill, string pTargetKey)
        {
            return $"{ResolveSkillKey(pSkill)}::{pTargetKey}";
        }

        private static List<SkillDefinition> BuildRuntimeSkills(BattleUnitDefinition pDefinition)
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

            if (lSkills.Count > 0)
                return lSkills;

            lSkills.Add(SkillDefinition.CreateRuntime(
                "basic_attack",
                "Basic Attack",
                SkillTargetType.Unit,
                SkillPrimaryEffectType.Damage,
                SkillAdditionalEffectType.None,
                1,
                3,
                1,
                "Fallback attack used when a unit has no explicit skills."));

            return lSkills;
        }

        private static List<GridCoord> BuildOccupiedCellOffsets(BattleUnitDefinition pDefinition)
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

        private static GridCoord NormalizeCardinalDirection(GridCoord pDirection)
        {
            int lAbsX = Math.Abs(pDirection.X);
            int lAbsY = Math.Abs(pDirection.Y);

            if (lAbsX == 0 && lAbsY == 0)
                return new GridCoord(0, 0);

            if (lAbsX >= lAbsY)
                return new GridCoord(Math.Sign(pDirection.X), 0);

            return new GridCoord(0, Math.Sign(pDirection.Y));
        }

        private void InitializePersistentStates()
        {
            if (Definition?.BaseStates == null)
                return;

            for (int lIndex = 0; lIndex < Definition.BaseStates.Count; lIndex++)
            {
                BattleUnitStateEntry lEntry = Definition.BaseStates[lIndex];
                if (lEntry?.State == null)
                    continue;

                SetPersistentState($"base::{lIndex}", lEntry.State, lEntry.Stacks);
            }
        }

        #endregion
    }
}
