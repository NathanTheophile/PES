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
        #region _____________________________| INIT

        public UnitRuntime(UnitId pId, UnitDefinition pDefinition, GridCoord pPosition, Team? pTeamOverride = null)
        {
            if (pDefinition == null)
                throw new ArgumentNullException(nameof(pDefinition));

            Id = pId;
            Definition = pDefinition;
            _TeamOverride = pTeamOverride;
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

        #region _____________________________/ VALUES

        private readonly List<SkillDefinition> _Skills;
        private readonly List<GridCoord> _OccupiedCellOffsets;
        private readonly BattleUnitStateCollection _States = new BattleUnitStateCollection();
        private readonly BattleUnitSkillUsageTracker _SkillUsage = new BattleUnitSkillUsageTracker();
        private readonly Team? _TeamOverride;

        #endregion

        #region _____________________________/ ACCESSORS

        public UnitId Id { get; }
        public UnitDefinition Definition { get; }
        public Team Team => _TeamOverride ?? Definition.Team;
        public GridCoord Position { get; private set; }
        public GridCoord FacingDirection { get; private set; }
        public int CurrentHealth { get; private set; }
        public int RemainingMovement { get; private set; }
        public int RemainingActionPoints { get; private set; }
        public bool IsAlive => CurrentHealth > 0;
        public IReadOnlyList<SkillDefinition> Skills => _Skills;
        public IReadOnlyList<GridCoord> OccupiedCellOffsets => _OccupiedCellOffsets;
        public IReadOnlyList<BattleStateRuntime> ActiveStates => _States.States;
        public event Action<int, bool> ValueChanged;

        #endregion

        #region _____________________________| TURN

        public void BeginTurn()
        {
            _SkillUsage.TickCooldowns();
            _SkillUsage.ClearTurnUses();
            _States.RemoveExpiredStates();

            if (!IsAlive)
            {
                RemainingMovement = 0;
                RemainingActionPoints = 0;
                return;
            }

            RemainingMovement = Math.Max(0, Definition.MoveRange + GetMovementModifier());
            RemainingActionPoints = Math.Max(0, Definition.ActionPointsPerTurn + GetActionPointModifier());
        }

        public void EndTurn()
        {
            _States.TickDurationsAtTurnEnd();
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

        public bool HasState(StateDefinition pState) => _States.HasState(pState);

        public bool HasPassiveMarker(StateDefinition pState) => _States.HasPassiveMarker(pState);

        public int GetStateStacks(StateDefinition pState) => _States.GetStateStacks(pState);

        public int GetDamageModifier() => _States.GetDamageModifier();

        public int GetDamageReduction() => _States.GetDamageReduction();

        public int GetRangeModifier() => _States.GetRangeModifier();

        public int GetActionPointModifier() => _States.GetActionPointModifier();

        public int GetMovementModifier() => _States.GetMovementModifier();

        public int GetSkillRangeMax(SkillDefinition pSkill) => pSkill != null ? Math.Max(0, pSkill.RangeMax + GetRangeModifier()) : 0;

        public int GetSkillRangeMin(SkillDefinition pSkill)
        {
            if (pSkill == null)
                return 0;

            int lRangeMax = GetSkillRangeMax(pSkill);
            return Math.Max(0, Math.Min(pSkill.RangeMin, lRangeMax));
        }

        public int ResolveOutgoingDamage(int pBaseDamage) => Math.Max(0, pBaseDamage + GetDamageModifier());

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
            _States.SetPersistentState(pStateKey, pState, pStacks);
            ClampTurnResourcesToCurrentMax();
        }

        public bool TryApplyState(StateDefinition pState, int pStacks = 1, int pDurationTurns = -1)
        {
            if (!_States.TryApplyState(pState, pStacks, pDurationTurns))
                return false;

            ClampTurnResourcesToCurrentMax();
            return true;
        }

        public bool RemoveTemporaryState(StateDefinition pState)
        {
            if (!_States.RemoveTemporaryState(pState))
                return false;

            ClampTurnResourcesToCurrentMax();
            return true;
        }

        public bool RemoveStateByKey(string pStateKey)
        {
            if (!_States.RemoveStateByKey(pStateKey))
                return false;

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
                RemainingMovement = 0;
                RemainingActionPoints = 0;
                return;
            }

            RemainingMovement = Math.Max(0, Math.Min(RemainingMovement, Definition.MoveRange + GetMovementModifier()));
            RemainingActionPoints = Math.Max(0, Math.Min(RemainingActionPoints, Definition.ActionPointsPerTurn + GetActionPointModifier()));
        }

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
                UnitStateEntry lEntry = Definition.BaseStates[lIndex];
                if (lEntry?.State == null)
                    continue;

                SetPersistentState($"base::{lIndex}", lEntry.State, lEntry.Stacks);
            }
        }

        #endregion
    }
}
