using System.Reflection;
using NUnit.Framework;
using PES.Presentation.Adapters;
using PES.Presentation.Configuration;
using UnityEngine;

namespace PES.Tests.EditMode
{
    public sealed class SkillDefinitionAdapterTests
    {
        [Test]
        public void ToPolicy_WhenAssetIsConfigured_MapsStableDeterministicPolicy()
        {
            var asset = ScriptableObject.CreateInstance<SkillDefinitionAsset>();
            SetPrivateField(asset, "_skillId", 42);
            SetPrivateField(asset, "_minRange", 2);
            SetPrivateField(asset, "_maxRange", 5);
            SetPrivateField(asset, "_baseDamage", 15);
            SetPrivateField(asset, "_baseHitChance", 90);
            SetPrivateField(asset, "_elevationPerRangeBonus", 3);
            SetPrivateField(asset, "_rangeBonusPerElevationStep", 2);
            SetPrivateField(asset, "_resourceCost", 4);
            SetPrivateField(asset, "_cooldownTurns", 1);

            var first = SkillDefinitionAdapter.ToPolicy(asset);
            var second = SkillDefinitionAdapter.ToPolicy(asset);

            Assert.That(first.HasValue, Is.True);
            Assert.That(second.HasValue, Is.True);
            Assert.That(first.Value.SkillId, Is.EqualTo(42));
            Assert.That(first.Value.MinRange, Is.EqualTo(2));
            Assert.That(first.Value.MaxRange, Is.EqualTo(5));
            Assert.That(first.Value.BaseDamage, Is.EqualTo(15));
            Assert.That(first.Value.BaseHitChance, Is.EqualTo(90));
            Assert.That(first.Value.ElevationPerRangeBonus, Is.EqualTo(3));
            Assert.That(first.Value.RangeBonusPerElevationStep, Is.EqualTo(2));
            Assert.That(first.Value.ResourceCost, Is.EqualTo(4));
            Assert.That(first.Value.CooldownTurns, Is.EqualTo(1));
            Assert.That(second.Value, Is.EqualTo(first.Value));

            Object.DestroyImmediate(asset);
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Missing field '{fieldName}'");
            field.SetValue(target, value);
        }
    }
}
