#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using System.Collections.Generic;
using TacticalPort.Data;

namespace TacticalPort.Core
{
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

        public void SetPersistentState(string pStateKey, StateDefinition pState, int pStacks)
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

            _States.Add(new BattleStateRuntime(pStateKey, pState, pStacks, 0, true));
        }

        public bool TryApplyState(StateDefinition pState, int pStacks, int pDurationTurns)
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

            _States.Add(new BattleStateRuntime(lStateKey, pState, pStacks, lResolvedDuration, false));
            return true;
        }

        public bool RemoveTemporaryState(StateDefinition pState) => pState != null && RemoveStateByKey(ResolveTemporaryStateKey(pState));

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

        public void TickDurationsAtTurnEnd()
        {
            for (int lIndex = _States.Count - 1; lIndex >= 0; lIndex--)
            {
                BattleStateRuntime lState = _States[lIndex];
                if (lState == null || lState.TickTurnEnd())
                    _States.RemoveAt(lIndex);
            }
        }

        public void RemoveExpiredStates()
        {
            for (int lIndex = _States.Count - 1; lIndex >= 0; lIndex--)
            {
                if (_States[lIndex] == null)
                    _States.RemoveAt(lIndex);
            }
        }

        private BattleStateRuntime FindStateByKey(string pStateKey)
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

        private static string ResolveTemporaryStateKey(StateDefinition pState) =>
            pState != null && !string.IsNullOrWhiteSpace(pState.Id) ? $"temporary::{pState.Id}" : "temporary::";
    }
}
