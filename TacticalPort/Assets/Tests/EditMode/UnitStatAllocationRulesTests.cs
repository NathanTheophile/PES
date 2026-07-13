using System.Reflection;
using NUnit.Framework;
using TacticalPort.Data;
using UnityEngine;

namespace TacticalPort.State.Tests
{
    public sealed class UnitStatAllocationRulesTests
    {
        [Test]
        public void CostsMatchDeckbuildingRules()
        {
            Assert.That(UnitStatAllocationRules.GetCostPerPoint(UnitStatType.Health), Is.EqualTo(1));
            Assert.That(UnitStatAllocationRules.GetCostPerPoint(UnitStatType.ActionPoints), Is.EqualTo(25));
            Assert.That(UnitStatAllocationRules.GetCostPerPoint(UnitStatType.Movement), Is.EqualTo(20));
            Assert.That(UnitStatAllocationRules.GetCostPerPoint(UnitStatType.MeleeDamage), Is.EqualTo(5));
            Assert.That(UnitStatAllocationRules.GetCostPerPoint(UnitStatType.RangedDamage), Is.EqualTo(5));
            Assert.That(UnitStatAllocationRules.GetCostPerPoint(UnitStatType.MeleeResistance), Is.EqualTo(5));
            Assert.That(UnitStatAllocationRules.GetCostPerPoint(UnitStatType.RangedResistance), Is.EqualTo(5));
            Assert.That(UnitStatAllocationRules.GetCostPerPoint(UnitStatType.Initiative), Is.EqualTo(1));
        }

        [Test]
        public void AllocationAtBudgetIsAcceptedAndAboveBudgetIsRejected()
        {
            UnitDefinition lUnit = ScriptableObject.CreateInstance<UnitDefinition>();
            try
            {
                SetPrivateField(lUnit, "_StatPointBudget", 120);
                UnitStatAllocationPreset lAllocation = new UnitStatAllocationPreset
                {
                    Power = 4,
                    MeleeDamage = 4
                };

                Assert.That(UnitStatAllocationRules.TryValidate(lUnit, lAllocation, out int lSpent, out string lFailure), Is.True, lFailure);
                Assert.That(lSpent, Is.EqualTo(120));

                lAllocation.MeleeDamage = 5;
                Assert.That(UnitStatAllocationRules.TryValidate(lUnit, lAllocation, out lSpent, out lFailure), Is.False);
                Assert.That(lSpent, Is.EqualTo(125));
            }
            finally
            {
                Object.DestroyImmediate(lUnit);
            }
        }

        [Test]
        public void RuntimeCloneAppliesValidatedModifiers()
        {
            UnitDefinition lUnit = ScriptableObject.CreateInstance<UnitDefinition>();
            UnitDefinition lClone = null;
            try
            {
                SetPrivateField(lUnit, "_MaxHealth", 100);
                SetPrivateField(lUnit, "_ActionPointsPerTurn", 6);
                SetPrivateField(lUnit, "_MoveRange", 3);
                lClone = UnitDefinition.CreateRuntimeClone(
                    lUnit,
                    null,
                    new UnitCombatLoadout(
                        null,
                        null,
                        new UnitStatModifiers(pHealth: 10, pActionPoints: 2, pMovement: 1)));

                Assert.That(lClone.MaxHealth, Is.EqualTo(110));
                Assert.That(lClone.ActionPointsPerTurn, Is.EqualTo(8));
                Assert.That(lClone.MoveRange, Is.EqualTo(4));
            }
            finally
            {
                if (lClone != null)
                    Object.DestroyImmediate(lClone);
                Object.DestroyImmediate(lUnit);
            }
        }

        private static void SetPrivateField<T>(UnitDefinition pUnit, string pFieldName, T pValue) =>
            typeof(UnitDefinition).GetField(pFieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(pUnit, pValue);
    }
}
