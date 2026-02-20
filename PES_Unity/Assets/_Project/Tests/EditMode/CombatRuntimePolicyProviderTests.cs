using NUnit.Framework;
using PES.Combat.Actions;
using PES.Core.Random;
using PES.Core.Simulation;
using PES.Grid.Grid3D;
using PES.Presentation.Adapters;
using PES.Presentation.Configuration;
using UnityEngine;

namespace PES.Tests.EditMode
{
    public sealed class CombatRuntimePolicyProviderTests
    {
        [Test]
        public void FromAsset_WhenNull_ReturnsNoOverrides()
        {
            var policies = CombatRuntimePolicyProvider.FromAsset(null);

            Assert.That(policies.MovePolicyOverride.HasValue, Is.False);
            Assert.That(policies.BasicAttackPolicyOverride.HasValue, Is.False);
        }

        [Test]
        public void FromAsset_WhenUsingDefaultAssetValues_MapsToCurrentDomainDefaults()
        {
            var asset = ScriptableObject.CreateInstance<CombatRuntimeConfigAsset>();

            var policies = CombatRuntimePolicyProvider.FromAsset(asset);

            Assert.That(policies.MovePolicyOverride.HasValue, Is.True);
            Assert.That(policies.MovePolicyOverride.Value.MaxMovementCostPerAction, Is.EqualTo(3));
            Assert.That(policies.MovePolicyOverride.Value.MaxVerticalStepPerTile, Is.EqualTo(1));

            Assert.That(policies.BasicAttackPolicyOverride.HasValue, Is.True);
            Assert.That(policies.BasicAttackPolicyOverride.Value.MinRange, Is.EqualTo(1));
            Assert.That(policies.BasicAttackPolicyOverride.Value.MaxRange, Is.EqualTo(2));
            Assert.That(policies.BasicAttackPolicyOverride.Value.MaxLineOfSightDelta, Is.EqualTo(2));
            Assert.That(policies.BasicAttackPolicyOverride.Value.ResolutionPolicy.BaseDamage, Is.EqualTo(12));
            Assert.That(policies.BasicAttackPolicyOverride.Value.ResolutionPolicy.BaseHitChance, Is.EqualTo(80));

            Object.DestroyImmediate(asset);
        }

        [Test]
        public void MoveAction_WhenProviderReturnsNullOverride_UsesDefaultFallbackPolicy()
        {
            var policies = CombatRuntimePolicyProvider.FromAsset(null);

            var state = new BattleState();
            var actor = new Core.Simulation.EntityId(500);
            state.SetEntityPosition(actor, new Position3(0, 0, 0));

            var resolver = new ActionResolver(new SeededRngService(1));
            var action = new MoveAction(actor, new GridCoord3(0, 0, 0), new GridCoord3(0, 0, 2), policies.MovePolicyOverride);

            var result = resolver.Resolve(state, action);

            Assert.That(result.Success, Is.False);
            Assert.That(result.Code, Is.EqualTo(ActionResolutionCode.Rejected));
            Assert.That(result.FailureReason, Is.EqualTo(ActionFailureReason.VerticalStepTooHigh));
        }
    }
}
