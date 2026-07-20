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
    internal readonly struct StateDamageResolution
    {
        public StateDamageResolution(UnitId pSourceUnitId, Team pSourceTeam, int pDamage)
        {
            SourceUnitId = pSourceUnitId;
            SourceTeam = pSourceTeam;
            Damage = Math.Max(0, pDamage);
        }

        public UnitId SourceUnitId { get; }
        public Team SourceTeam { get; }
        public int Damage { get; }
    }

    internal sealed class BattleUnitStateCollection
    {
        private enum StateModifierType
        {
            Damage,
            MeleeDamage,
            RangedDamage,
            MeleeResistance,
            RangedResistance,
            GeneralResistance,
            Wear,
            Range,
            Energy,
            Mobility
        }

        private readonly List<BattleStateRuntime> _States = new List<BattleStateRuntime>();

        public IReadOnlyList<BattleStateRuntime> States => _States;

        public bool HasState(StateDefinition pState) => pState != null && GetStateStacks(pState) > 0;

        public bool HasPassiveMarker(StateDefinition pState) => pState != null && pState.IsPassiveMarker && HasState(pState);

        public bool BlocksHealing
        {
            get
            {
                foreach (BattleStateRuntime lState in _States)
                {
                    if (lState?.Definition?.BlocksHealing == true)
                        return true;
                }

                return false;
            }
        }

        public int GetStateStacks(StateDefinition pState)
        {
            if (pState == null)
                return 0;

            int lTotalStacks = 0;
            foreach (BattleStateRuntime lState in _States)
            {
                if (lState?.Definition == pState)
                    lTotalStacks += lState.Stacks;
            }

            return Math.Max(0, lTotalStacks);
        }

        public int GetDamageModifier() => ResolveModifier(StateModifierType.Damage);

        public int GetMeleeDamageModifier() => ResolveModifier(StateModifierType.MeleeDamage);

        public int GetRangedDamageModifier() => ResolveModifier(StateModifierType.RangedDamage);

        public int GetMeleeResistancePercent() => ResolveModifier(StateModifierType.MeleeResistance);

        public int GetRangedResistancePercent() => ResolveModifier(StateModifierType.RangedResistance);

        public int GetGeneralResistancePercent() => ResolveModifier(StateModifierType.GeneralResistance);

        public int GetWearPercent() => ResolveModifier(StateModifierType.Wear);

        public int GetRangeModifier() => ResolveModifier(StateModifierType.Range);

        public int GetEnergyModifier() => ResolveModifier(StateModifierType.Energy);

        public int GetMobilityModifier() => ResolveModifier(StateModifierType.Mobility);

        public int GetBeginTurnDamage()
        {
            int lDamage = 0;
            foreach (BattleStateRuntime lState in _States)
            {
                if (lState?.Definition != null)
                    lDamage += Math.Max(0, lState.Definition.BeginTurnDamagePerStack) * lState.Stacks;
            }

            return lDamage;
        }

        public IReadOnlyList<StateDamageResolution> GetBeginTurnDamageResolutions()
        {
            List<StateDamageResolution> lResolutions = new List<StateDamageResolution>();
            foreach (BattleStateRuntime lState in _States)
            {
                int lDamage = lState?.Definition != null
                    ? Math.Max(0, lState.Definition.BeginTurnDamagePerStack) * lState.Stacks
                    : 0;
                if (lDamage <= 0)
                    continue;

                int lExistingIndex = -1;
                for (int lIndex = 0; lIndex < lResolutions.Count; lIndex++)
                {
                    if (lResolutions[lIndex].SourceUnitId == lState.SourceUnitId
                        && lResolutions[lIndex].SourceTeam == lState.SourceTeam)
                    {
                        lExistingIndex = lIndex;
                        break;
                    }
                }

                if (lExistingIndex >= 0)
                {
                    StateDamageResolution lExisting = lResolutions[lExistingIndex];
                    lResolutions[lExistingIndex] = new StateDamageResolution(
                        lExisting.SourceUnitId,
                        lExisting.SourceTeam,
                        lExisting.Damage + lDamage);
                }
                else
                {
                    lResolutions.Add(new StateDamageResolution(lState.SourceUnitId, lState.SourceTeam, lDamage));
                }
            }

            lResolutions.Sort((pLeft, pRight) =>
            {
                int lSource = pLeft.SourceUnitId.Value.CompareTo(pRight.SourceUnitId.Value);
                return lSource != 0 ? lSource : ((int)pLeft.SourceTeam).CompareTo((int)pRight.SourceTeam);
            });
            return lResolutions;
        }

        public void SetPersistentState(
            string pStateKey,
            StateDefinition pState,
            int pStacks,
            UnitId pSourceUnitId = default,
            Team pSourceTeam = Team.Neutral,
            string pSourceContextId = null)
        {
            if (string.IsNullOrWhiteSpace(pStateKey))
                return;

            if (pState == null || pStacks <= 0)
            {
                RemoveStateByKey(pStateKey);
                return;
            }

            RemoveExclusiveGroupStates(pState, pStateKey);

            BattleStateRuntime lExistingState = FindStateByKey(pStateKey);
            if (lExistingState != null)
            {
                lExistingState.Reconfigure(pState, pStacks, 0, true);
                lExistingState.SetSource(pSourceUnitId, pSourceTeam, pSourceContextId);
                return;
            }

            _States.Add(new BattleStateRuntime(
                pStateKey, pState, pStacks, 0, true, pSourceUnitId, pSourceTeam, pSourceContextId));
        }

        public bool TryApplyState(
            StateDefinition pState,
            int pStacks,
            int pDurationTurns,
            UnitId pSourceUnitId = default,
            Team pSourceTeam = Team.Neutral,
            string pSourceContextId = null)
        {
            if (pState == null || pStacks <= 0)
                return false;

            string lStateKey = ResolveTemporaryStateKey(pState, pSourceUnitId, pSourceContextId);
            RemoveExclusiveGroupStates(pState, lStateKey);
            int lResolvedDuration = pDurationTurns >= 0 ? pDurationTurns : pState.DurationTurns;
            BattleStateRuntime lExistingState = FindStateByKey(lStateKey);
            if (lExistingState != null)
            {
                lExistingState.AddStacks(pStacks);
                lExistingState.SetRemainingTurns(lResolvedDuration);
                lExistingState.SetSource(pSourceUnitId, pSourceTeam, pSourceContextId);
                return true;
            }

            _States.Add(new BattleStateRuntime(
                lStateKey, pState, pStacks, lResolvedDuration, false, pSourceUnitId, pSourceTeam, pSourceContextId));
            return true;
        }

        public bool RemoveTemporaryState(StateDefinition pState)
        {
            if (pState == null)
                return false;

            bool lRemoved = false;
            for (int lIndex = _States.Count - 1; lIndex >= 0; lIndex--)
            {
                BattleStateRuntime lState = _States[lIndex];
                if (lState != null && !lState.IsPersistent && lState.Definition == pState)
                {
                    _States.RemoveAt(lIndex);
                    lRemoved = true;
                }
            }

            return lRemoved;
        }

        public BattleStateRuntime FindStateByKey(string pStateKey)
        {
            if (string.IsNullOrWhiteSpace(pStateKey))
                return null;

            foreach (BattleStateRuntime lState in _States)
            {
                if (lState != null && lState.Key == pStateKey)
                    return lState;
            }

            return null;
        }

        public bool RemoveStateByKey(string pStateKey)
        {
            if (string.IsNullOrWhiteSpace(pStateKey))
                return false;

            for (int lIndex = _States.Count - 1; lIndex >= 0; lIndex--)
            {
                if (_States[lIndex] == null || _States[lIndex].Key != pStateKey)
                    continue;

                _States.RemoveAt(lIndex);
                return true;
            }

            return false;
        }

        public int RemoveStatesBySource(
            UnitId pSourceUnitId,
            string pSourceContextId = null,
            StateDefinition pState = null)
        {
            if (!pSourceUnitId.IsValid)
                return 0;

            int lRemovedCount = 0;
            bool lFilterContext = pSourceContextId != null;
            for (int lIndex = _States.Count - 1; lIndex >= 0; lIndex--)
            {
                BattleStateRuntime lState = _States[lIndex];
                if (lState == null
                    || lState.SourceUnitId != pSourceUnitId
                    || (lFilterContext && lState.SourceContextId != pSourceContextId)
                    || (pState != null && lState.Definition != pState))
                {
                    continue;
                }

                _States.RemoveAt(lIndex);
                lRemovedCount++;
            }

            return lRemovedCount;
        }

        public int ReduceTemporaryStateDurations(int pTurns)
        {
            if (pTurns <= 0)
                return 0;

            List<BattleStateRuntime> lCandidates = new List<BattleStateRuntime>();
            foreach (BattleStateRuntime lState in _States)
            {
                if (lState == null
                    || lState.IsPersistent
                    || !lState.HasDuration
                    || lState.Definition == null
                    || lState.Definition.IsPassiveMarker
                    || lState.Definition.ProtectsDurationReduction)
                {
                    continue;
                }

                lCandidates.Add(lState);
            }

            lCandidates.Sort((pLeft, pRight) => string.CompareOrdinal(pLeft.Key, pRight.Key));
            for (int lIndex = 0; lIndex < lCandidates.Count; lIndex++)
            {
                BattleStateRuntime lState = lCandidates[lIndex];
                lState.SetRemainingTurns(lState.RemainingTurns - pTurns);
                if (!lState.HasDuration)
                    RemoveStateByKey(lState.Key);
            }

            return lCandidates.Count;
        }

        public bool ReplaceStateByKey(string pStateKey, StateDefinition pReplacementState, int pStacks = 1)
        {
            BattleStateRuntime lExistingState = FindStateByKey(pStateKey);
            if (lExistingState == null)
                return false;

            if (pReplacementState == null)
                return RemoveStateByKey(pStateKey);

            lExistingState.Reconfigure(
                pReplacementState,
                pStacks,
                lExistingState.RemainingTurns,
                lExistingState.IsPersistent);
            return true;
        }

        public int TickTemporaryDurationsForSource(UnitId pSourceUnitId, bool pIncludeSourceLess)
        {
            if (!pSourceUnitId.IsValid)
                return 0;

            List<BattleStateRuntime> lCandidates = new List<BattleStateRuntime>();
            foreach (BattleStateRuntime lState in _States)
            {
                if (lState == null
                    || lState.IsPersistent
                    || !lState.HasDuration
                    || lState.Definition?.IsPassiveMarker == true
                    || (lState.SourceUnitId != pSourceUnitId
                        && !(pIncludeSourceLess && !lState.SourceUnitId.IsValid)))
                {
                    continue;
                }

                lCandidates.Add(lState);
            }

            lCandidates.Sort((pLeft, pRight) => string.CompareOrdinal(pLeft.Key, pRight.Key));
            for (int lIndex = 0; lIndex < lCandidates.Count; lIndex++)
            {
                BattleStateRuntime lState = lCandidates[lIndex];
                if (lState.TickDuration())
                    RemoveStateByKey(lState.Key);
            }

            return lCandidates.Count;
        }

        public void RemoveExpiredStates()
        {
            for (int lIndex = _States.Count - 1; lIndex >= 0; lIndex--)
            {
                if (_States[lIndex] == null)
                    _States.RemoveAt(lIndex);
            }
        }

        private int ResolveModifier(StateModifierType pType)
        {
            int lModifier = 0;
            foreach (BattleStateRuntime lState in _States)
            {
                if (lState?.Definition == null)
                    continue;

                lModifier += ResolveModifierPerStack(lState.Definition, pType) * lState.Stacks;
            }

            return lModifier;
        }

        private static int ResolveModifierPerStack(StateDefinition pState, StateModifierType pType)
        {
            switch (pType)
            {
                case StateModifierType.Damage:
                    return pState.DamageModifierPerStack;

                case StateModifierType.MeleeDamage:
                    return pState.MeleeDamageModifierPerStack;

                case StateModifierType.RangedDamage:
                    return pState.RangedDamageModifierPerStack;

                case StateModifierType.MeleeResistance:
                    return pState.MeleeResistancePercentPerStack;

                case StateModifierType.RangedResistance:
                    return pState.RangedResistancePercentPerStack;

                case StateModifierType.GeneralResistance:
                    return pState.GeneralResistancePercentPerStack;

                case StateModifierType.Wear:
                    return pState.WearPercentPerStack;

                case StateModifierType.Range:
                    return pState.RangeModifierPerStack;

                case StateModifierType.Energy:
                    return pState.EnergyModifierPerStack;

                case StateModifierType.Mobility:
                    return pState.MobilityModifierPerStack;

                default:
                    return 0;
            }
        }

        private void RemoveExclusiveGroupStates(StateDefinition pState, string pExceptKey)
        {
            if (pState == null || string.IsNullOrWhiteSpace(pState.ExclusiveGroupId))
                return;

            for (int lIndex = _States.Count - 1; lIndex >= 0; lIndex--)
            {
                BattleStateRuntime lRuntime = _States[lIndex];
                if (lRuntime == null
                    || lRuntime.Key == pExceptKey
                    || lRuntime.Definition == null
                    || lRuntime.Definition.ExclusiveGroupId != pState.ExclusiveGroupId)
                {
                    continue;
                }

                _States.RemoveAt(lIndex);
            }
        }

        private static string ResolveTemporaryStateKey(StateDefinition pState, UnitId pSourceUnitId, string pSourceContextId)
        {
            string lStateId = pState != null && !string.IsNullOrWhiteSpace(pState.Id) ? pState.Id : string.Empty;
            if (pState == null
                || !pState.UsesSourceScopedInstances
                || (!pSourceUnitId.IsValid && string.IsNullOrWhiteSpace(pSourceContextId)))
                return $"temporary::{lStateId}";

            return $"temporary::{lStateId}::source:{pSourceUnitId.Value}::context:{pSourceContextId ?? string.Empty}";
        }
    }
}
