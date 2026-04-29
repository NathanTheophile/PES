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
    internal static class BattlePhaseStateResolver
    {
        public static void RefreshAll(IEnumerable<UnitRuntime> pUnits)
        {
            if (pUnits == null)
                return;

            foreach (UnitRuntime lUnit in pUnits)
                Refresh(lUnit);
        }

        public static void Refresh(IReadOnlyDictionary<UnitId, UnitRuntime> pUnitsById, IEnumerable<UnitId> pUnitIds)
        {
            if (pUnitsById == null || pUnitIds == null)
                return;

            HashSet<UnitId> lUniqueIds = new HashSet<UnitId>();
            foreach (UnitId lUnitId in pUnitIds)
            {
                if (!lUniqueIds.Add(lUnitId))
                    continue;

                if (pUnitsById.TryGetValue(lUnitId, out UnitRuntime lUnit))
                    Refresh(lUnit);
            }
        }

        public static void Refresh(UnitRuntime pUnit)
        {
            if (pUnit?.Definition?.PhaseStates == null)
                return;

            for (int lIndex = 0; lIndex < pUnit.Definition.PhaseStates.Count; lIndex++)
            {
                UnitPhaseStateDefinition lPhaseState = pUnit.Definition.PhaseStates[lIndex];
                string lPhaseKey = $"phase::{lIndex}";

                if (ShouldBeActive(pUnit, lPhaseState))
                {
                    pUnit.SetPersistentState(lPhaseKey, lPhaseState.State, lPhaseState.Stacks);
                    continue;
                }

                pUnit.RemoveStateByKey(lPhaseKey);
            }
        }

        private static bool ShouldBeActive(UnitRuntime pUnit, UnitPhaseStateDefinition pPhaseState)
        {
            if (pUnit == null || pPhaseState?.State == null || !pUnit.IsAlive)
                return false;

            int lThresholdHealth = (int)Math.Ceiling(pUnit.Definition.MaxHealth * pPhaseState.HealthThresholdNormalized);
            return pUnit.CurrentHealth <= Math.Max(0, lThresholdHealth);
        }
    }
}
