#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : Single source of truth for deckbuilding stat costs and validation.
//  State
#endregion

using TacticalPort.Data;

namespace TacticalPort.State
{
    public static class UnitStatAllocationRules
    {
        private static readonly UnitStatType[] AllocatedStats =
        {
            UnitStatType.Health,
            UnitStatType.ActionPoints,
            UnitStatType.Movement,
            UnitStatType.MeleeDamage,
            UnitStatType.RangedDamage,
            UnitStatType.Initiative,
            UnitStatType.MeleeResistance,
            UnitStatType.RangedResistance
        };

        public const int HealthCost = 1;
        public const int ActionPointCost = 25;
        public const int MovementCost = 20;
        public const int DamageCost = 5;
        public const int ResistanceCost = 5;
        public const int InitiativeCost = 1;

        public static int GetCostPerPoint(UnitStatType pStat) => pStat switch
        {
            UnitStatType.Health => HealthCost,
            UnitStatType.ActionPoints => ActionPointCost,
            UnitStatType.Movement => MovementCost,
            UnitStatType.MeleeDamage or UnitStatType.RangedDamage => DamageCost,
            UnitStatType.MeleeResistance or UnitStatType.RangedResistance => ResistanceCost,
            UnitStatType.Initiative => InitiativeCost,
            _ => 0
        };

        public static int GetValue(UnitStatAllocationPreset pAllocations, UnitStatType pStat)
        {
            if (pAllocations == null)
                return 0;

            return pStat switch
            {
                UnitStatType.Health => pAllocations.Health,
                UnitStatType.ActionPoints => pAllocations.Power,
                UnitStatType.Movement => pAllocations.Movement,
                UnitStatType.MeleeDamage => pAllocations.MeleeDamage,
                UnitStatType.RangedDamage => pAllocations.RangedDamage,
                UnitStatType.Initiative => pAllocations.Initiative,
                UnitStatType.MeleeResistance => pAllocations.MeleeResistance,
                UnitStatType.RangedResistance => pAllocations.RangedResistance,
                _ => 0
            };
        }

        public static void SetValue(UnitStatAllocationPreset pAllocations, UnitStatType pStat, int pValue)
        {
            if (pAllocations == null)
                return;

            switch (pStat)
            {
                case UnitStatType.Health: pAllocations.Health = pValue; break;
                case UnitStatType.ActionPoints: pAllocations.Power = pValue; break;
                case UnitStatType.Movement: pAllocations.Movement = pValue; break;
                case UnitStatType.MeleeDamage: pAllocations.MeleeDamage = pValue; break;
                case UnitStatType.RangedDamage: pAllocations.RangedDamage = pValue; break;
                case UnitStatType.Initiative: pAllocations.Initiative = pValue; break;
                case UnitStatType.MeleeResistance: pAllocations.MeleeResistance = pValue; break;
                case UnitStatType.RangedResistance: pAllocations.RangedResistance = pValue; break;
            }
        }

        public static int GetSpentPoints(UnitStatAllocationPreset pAllocations)
        {
            if (!TryCalculateSpentPoints(pAllocations, out int lSpentPoints))
                return int.MaxValue;

            return lSpentPoints;
        }

        public static int GetRemainingPoints(UnitDefinition pUnit, UnitStatAllocationPreset pAllocations) =>
            pUnit == null ? 0 : System.Math.Max(0, pUnit.StatPointBudget - GetSpentPoints(pAllocations));

        public static bool TryValidate(
            UnitDefinition pUnit,
            UnitStatAllocationPreset pAllocations,
            out int pSpentPoints,
            out string pFailure)
        {
            pSpentPoints = 0;
            pFailure = string.Empty;
            if (pUnit == null)
            {
                pFailure = "Missing unit definition.";
                return false;
            }

            if (!TryCalculateSpentPoints(pAllocations, out pSpentPoints))
            {
                pFailure = $"Stat allocations for '{pUnit.Id}' contain a negative value or overflow.";
                return false;
            }

            if (pSpentPoints > pUnit.StatPointBudget)
            {
                pFailure = $"Stat allocations for '{pUnit.Id}' cost {pSpentPoints}, above its budget of {pUnit.StatPointBudget}.";
                return false;
            }

            int lMeleeResistance = GetValue(pAllocations, UnitStatType.MeleeResistance);
            int lRangedResistance = GetValue(pAllocations, UnitStatType.RangedResistance);
            if (pUnit.MeleeResistancePercent + lMeleeResistance > 100
                || pUnit.RangedResistancePercent + lRangedResistance > 100)
            {
                pFailure = $"Resistance allocations for '{pUnit.Id}' cannot exceed 100%.";
                return false;
            }

            return true;
        }

        public static UnitStatModifiers CreateRuntimeModifiers(
            UnitDefinition pUnit,
            UnitStatAllocationPreset pAllocations)
        {
            if (!TryValidate(pUnit, pAllocations, out _, out _))
                return UnitStatModifiers.None;

            return new UnitStatModifiers(
                pAllocations?.Health ?? 0,
                pAllocations?.Power ?? 0,
                pAllocations?.Movement ?? 0,
                pAllocations?.MeleeDamage ?? 0,
                pAllocations?.MeleeResistance ?? 0,
                pAllocations?.RangedDamage ?? 0,
                pAllocations?.RangedResistance ?? 0,
                pAllocations?.Initiative ?? 0);
        }

        public static int GetEffectiveValue(
            UnitDefinition pUnit,
            UnitStatAllocationPreset pAllocations,
            UnitStatType pStat)
        {
            if (pUnit == null)
                return 0;

            int lBaseValue = pStat switch
            {
                UnitStatType.Health => pUnit.MaxHealth,
                UnitStatType.ActionPoints => pUnit.ActionPointsPerTurn,
                UnitStatType.Movement => pUnit.MoveRange,
                UnitStatType.MeleeDamage => pUnit.MeleeDamagePercent,
                UnitStatType.RangedDamage => pUnit.RangedDamagePercent,
                UnitStatType.Initiative => pUnit.Initiative,
                UnitStatType.MeleeResistance => pUnit.MeleeResistancePercent,
                UnitStatType.RangedResistance => pUnit.RangedResistancePercent,
                _ => 0
            };
            return lBaseValue + GetValue(pAllocations, pStat);
        }

        private static bool TryCalculateSpentPoints(UnitStatAllocationPreset pAllocations, out int pSpentPoints)
        {
            pSpentPoints = 0;
            if (pAllocations == null)
                return true;

            long lTotal = 0;
            for (int lIndex = 0; lIndex < AllocatedStats.Length; lIndex++)
            {
                UnitStatType lStat = AllocatedStats[lIndex];
                int lValue = GetValue(pAllocations, lStat);
                if (lValue < 0)
                    return false;

                lTotal += (long)lValue * GetCostPerPoint(lStat);
                if (lTotal > int.MaxValue)
                    return false;
            }

            pSpentPoints = (int)lTotal;
            return true;
        }
    }
}
