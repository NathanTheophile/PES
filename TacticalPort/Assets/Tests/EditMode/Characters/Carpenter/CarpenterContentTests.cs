using System.Linq;
using NUnit.Framework;
using TacticalPort.Core;
using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEditor;
using UnityEngine;

namespace TacticalPort.State.Tests.Characters.Carpenter
{
    public sealed class CarpenterContentTests
    {
        private const string Root = "Assets/AITests/Carpenter/Data";

        [Test]
        public void UnitPoolPassivesAndSharedPresentationAreComplete()
        {
            UnitDefinition carpenter = Load<UnitDefinition>("Units/Carpenter.asset");

            Assert.That(carpenter.Id, Is.EqualTo("carpenter"));
            Assert.That(carpenter.EnglishDisplayName, Is.EqualTo("Naval Carpenter"));
            Assert.That(carpenter.Team, Is.EqualTo(Team.TeamA));
            Assert.That(carpenter.MaxHealth, Is.EqualTo(470));
            Assert.That(carpenter.MobilityPerTurn, Is.EqualTo(3));
            Assert.That(carpenter.EnergyPerTurn, Is.EqualTo(6));
            Assert.That(carpenter.Velocity, Is.EqualTo(35));
            Assert.That(carpenter.MeleeDamagePercent, Is.EqualTo(105));
            Assert.That(carpenter.RangedDamagePercent, Is.EqualTo(100));
            Assert.That(carpenter.MeleeResistancePercent, Is.EqualTo(5));
            Assert.That(carpenter.RangedResistancePercent, Is.EqualTo(5));
            Assert.That(carpenter.StatPointBudget, Is.EqualTo(120));
            Assert.That(carpenter.PushDamageBonus, Is.EqualTo(10));
            Assert.That(carpenter.Skills, Has.Count.EqualTo(10));
            Assert.That(carpenter.Passives, Has.Count.EqualTo(2));
            Assert.That(carpenter.DefaultPassive.Id, Is.EqualTo("carpenter_workshop_network"));
            Assert.That(carpenter.CombatViewPrefabComponent, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(carpenter.CombatViewPrefabComponent), Is.EqualTo("Assets/Prefabs/Actors/P_ActorBase.prefab"));
            Assert.That(carpenter.ModelPrefab, Is.Not.Null);
            Assert.That(AssetDatabase.GetAssetPath(carpenter.ModelPrefab), Is.EqualTo("Assets/AITests/Carpenter/Prefabs/Carpenter_Blockout.prefab"));
            Assert.That(carpenter.ModelPrefab.GetComponentsInChildren<Renderer>(true), Is.Not.Empty);
            Assert.That(carpenter.ModelPrefab.GetComponentsInChildren<Renderer>(true).SelectMany(pRenderer => pRenderer.sharedMaterials), Has.All.Not.Null);

            SkillDefinition[] variants = carpenter.Skills.Where(pSkill => pSkill.Variant != null).Select(pSkill => pSkill.Variant).ToArray();
            Assert.That(variants, Has.Length.EqualTo(5));
            Assert.That(variants.Intersect(carpenter.Skills), Is.Empty);
        }

        [Test]
        public void ConstructionsExposeOwnershipDurabilityModesAndAutonomy()
        {
            StateDefinition construction = Load<StateDefinition>("States/CarpenterConstruction.asset");
            StateDefinition active = Load<StateDefinition>("States/CarpenterTurretActive.asset");
            StateDefinition inactive = Load<StateDefinition>("States/CarpenterTurretInactive.asset");
            UnitDefinition pulley = Load<UnitDefinition>("Units/CarpenterMooringPulley.asset");
            UnitDefinition harpoon = Load<UnitDefinition>("Units/CarpenterHarpoonTurret.asset");
            UnitDefinition ballista = Load<UnitDefinition>("Units/CarpenterDeckBallista.asset");

            foreach (UnitDefinition unit in new[] { pulley, harpoon, ballista })
            {
                Assert.That(unit.MaxHealth, Is.EqualTo(5));
                Assert.That(unit.DirectSkillDamage, Is.EqualTo(1));
                Assert.That(unit.CanReceiveStandardHealing, Is.False);
                Assert.That(unit.CountsForVictory, Is.False);
                Assert.That(unit.BlocksVisibility, Is.False);
                Assert.That(unit.VisibilityBlockingState, Is.SameAs(inactive));
                Assert.That(unit.BaseStates.Select(pEntry => pEntry.State), Does.Contain(construction));
                Assert.That(unit.BaseStates.Select(pEntry => pEntry.State), Does.Contain(active));
                Assert.That(unit.ModelPrefab, Is.Not.Null);
                Assert.That(unit.ModelPrefab.GetComponentsInChildren<Renderer>(true), Is.Not.Empty);
                Assert.That(unit.ModelPrefab.GetComponentsInChildren<Renderer>(true).SelectMany(pRenderer => pRenderer.sharedMaterials), Has.All.Not.Null);
                Assert.That(unit.ModelPrefab.GetComponentInChildren<Animator>(true), Is.Null);
            }

            Assert.That(AssetDatabase.GetAssetPath(pulley.ModelPrefab), Is.EqualTo("Assets/AITests/Carpenter/Prefabs/Carpenter_MooringPulley_Blockout.prefab"));
            Assert.That(AssetDatabase.GetAssetPath(harpoon.ModelPrefab), Is.EqualTo("Assets/AITests/Carpenter/Prefabs/Carpenter_HarpoonTurret_Blockout.prefab"));
            Assert.That(AssetDatabase.GetAssetPath(ballista.ModelPrefab), Is.EqualTo("Assets/AITests/Carpenter/Prefabs/Carpenter_DeckBallista_Blockout.prefab"));

            Assert.That(pulley.DirectSkillSwapRequiredState, Is.SameAs(active));
            Assert.That(harpoon.AutonomousBehavior, Is.EqualTo(AutonomousUnitBehavior.PullOrthogonalUnits));
            Assert.That(harpoon.AutonomousRange, Is.EqualTo(5));
            Assert.That(harpoon.AutonomousExcludedTargetState, Is.SameAs(construction));
            Assert.That(ballista.AutonomousBehavior, Is.EqualTo(AutonomousUnitBehavior.UseSkillOnNearestEnemy));
            Assert.That(ballista.AutonomousExecutionsPerTurn, Is.EqualTo(2));
            Assert.That(ballista.AutonomousSkill.Id, Is.EqualTo("carpenter_deck_ballista_shot"));
        }

        [Test]
        public void RelayVariantsRequireALivingOwnedConstructionAndKeepBaseUsageRules()
        {
            UnitDefinition carpenterDefinition = Load<UnitDefinition>("Units/Carpenter.asset");
            UnitDefinition turretDefinition = Load<UnitDefinition>("Units/CarpenterHarpoonTurret.asset");
            StateDefinition construction = Load<StateDefinition>("States/CarpenterConstruction.asset");
            UnitRuntime carpenter = new UnitRuntime(new UnitId(1), carpenterDefinition, new GridCoord(0, 0));
            UnitRuntime owned = new UnitRuntime(new UnitId(2), turretDefinition, new GridCoord(1, 0), pOwnerUnitId: carpenter.Id);
            UnitRuntime other = new UnitRuntime(new UnitId(3), turretDefinition, new GridCoord(2, 0), pOwnerUnitId: new UnitId(99));

            foreach (SkillDefinition skill in carpenter.Skills.Where(pSkill => pSkill.Variant != null))
            {
                Assert.That(skill.VariantTrigger, Is.EqualTo(SkillVariantTrigger.PrimaryTargetOwnedState));
                Assert.That(skill.VariantTargetRequiredState, Is.SameAs(construction));
                Assert.That(carpenter.ResolveEffectiveSkill(skill, owned), Is.SameAs(skill.Variant));
                Assert.That(carpenter.ResolveEffectiveSkill(skill, other), Is.SameAs(skill));
                Assert.That(skill.Variant.UsePerTurn, Is.EqualTo(skill.UsePerTurn));
                Assert.That(skill.Variant.UsePerTarget, Is.EqualTo(skill.UsePerTarget));
                Assert.That(skill.Variant.CooldownTurns, Is.EqualTo(skill.CooldownTurns));
                Assert.That(skill.Variant.TargetRelation, Is.EqualTo(SkillTargetRelation.AlliesOnly));
                Assert.That(skill.Variant.ExcludePrimaryTargetFromEffects, Is.True);
            }
        }

        [Test]
        public void StatesAndPerpendicularAreaMatchTheKit()
        {
            Assert.That(Load<StateDefinition>("States/CarpenterPersistentDamage.asset").BeginTurnDamagePerStack, Is.EqualTo(8));
            Assert.That(Load<StateDefinition>("States/CarpenterRangedResistanceStolen5.asset").MaxStacks, Is.EqualTo(4));
            Assert.That(Load<StateDefinition>("States/CarpenterRangedResistanceStolen5.asset").RangedResistancePercentPerStack, Is.EqualTo(5));
            Assert.That(Load<StateDefinition>("States/CarpenterEnergyMinus2.asset").EnergyModifierPerStack, Is.EqualTo(-2));

            Assert.That(GridVisibilityUtility.IsInsideAreaShape(SkillAoeShape.PerpendicularLine, 0, 1, 1, new GridCoord(1, 0)), Is.True);
            Assert.That(GridVisibilityUtility.IsInsideAreaShape(SkillAoeShape.PerpendicularLine, 0, -1, 1, new GridCoord(1, 0)), Is.True);
            Assert.That(GridVisibilityUtility.IsInsideAreaShape(SkillAoeShape.PerpendicularLine, 1, 0, 1, new GridCoord(1, 0)), Is.False);
        }

        [Test]
        public void ConstructionDurabilityHealingAndModeToggleUseTheGenericRuntimeRules()
        {
            UnitDefinition definition = Load<UnitDefinition>("Units/CarpenterHarpoonTurret.asset");
            StateDefinition active = Load<StateDefinition>("States/CarpenterTurretActive.asset");
            StateDefinition inactive = Load<StateDefinition>("States/CarpenterTurretInactive.asset");
            UnitRuntime unit = new UnitRuntime(new UnitId(12), definition, new GridCoord(2, 2), pOwnerUnitId: new UnitId(1));

            Assert.That(unit.ApplyDirectHealthLoss(1), Is.EqualTo(1));
            Assert.That(unit.CurrentHealth, Is.EqualTo(4));
            Assert.That(unit.RestoreHealth(10), Is.Zero);
            Assert.That(unit.CurrentHealth, Is.EqualTo(4));
            Assert.That(unit.RestoreDirectHealth(1), Is.EqualTo(1));
            Assert.That(unit.TryTogglePersistentStates(active, inactive), Is.True);
            Assert.That(unit.HasState(active), Is.False);
            Assert.That(unit.HasState(inactive), Is.True);
            Assert.That(unit.BlocksVisibility, Is.True);
            Assert.That(unit.TryTogglePersistentStates(active, inactive), Is.True);
            Assert.That(unit.BlocksVisibility, Is.False);
        }

        [Test]
        public void PersistentDamageTicksExactlyTwiceAtBeginTurn()
        {
            UnitDefinition definition = Load<UnitDefinition>("Units/Carpenter.asset");
            StateDefinition damage = Load<StateDefinition>("States/CarpenterPersistentDamage.asset");
            UnitRuntime unit = new UnitRuntime(new UnitId(1), definition, new GridCoord(0, 0));
            Assert.That(unit.TryApplyState(damage, 1, 2), Is.True);

            unit.BeginTurn();
            Assert.That(unit.ApplyBeginTurnStateDamage(), Is.EqualTo(8));
            unit.TickTemporaryStateDurationsForSource(unit.Id, true);
            unit.EndTurn();
            unit.BeginTurn();
            Assert.That(unit.ApplyBeginTurnStateDamage(), Is.EqualTo(8));
            unit.TickTemporaryStateDurationsForSource(unit.Id, true);
            unit.EndTurn();
            unit.BeginTurn();
            Assert.That(unit.ApplyBeginTurnStateDamage(), Is.Zero);
        }

        private static T Load<T>(string relativePath) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>($"{Root}/{relativePath}");
            Assert.That(asset, Is.Not.Null, relativePath);
            return asset;
        }
    }
}
