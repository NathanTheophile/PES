using NUnit.Framework;
using PES.Combat.Actions;
using PES.Combat.Resolution;
using PES.Core.Random;
using PES.Core.Simulation;
using PES.Grid.Grid3D;


namespace PES.Tests.EditMode
{
    public class CastSkillPipelineTests
    {
        [Test]
                public void Resolve_CastSkillAction_CriticalChanceUsesBasePlusStat()
                {
                    var state = new BattleState();
                    var caster = new EntityId(890);
                    var target = new EntityId(891);
                    state.SetEntityPosition(caster, new Position3(0, 0, 0));
                    state.SetEntityPosition(target, new Position3(1, 0, 0));
                    state.SetEntityHitPoints(caster, 100);
                    state.SetEntityHitPoints(target, 100);
                    state.SetEntitySkillResource(caster, 5);
        
                    state.SetEntityRpgStats(caster, new CombatantRpgStats(
                        actionPoints: 6,
                        movementPoints: 6,
                        range: 1,
                        elevation: 1,
                        summonCapacity: 1,
                        hitPoints: 100,
                        assiduity: 0,
                        rapidity: 0,
                        criticalChance: 40,
                        criticalDamage: 0,
                        criticalResistance: 0,
                        attack: new DamageElementValues(0, 0, 0, 0, 15, 0),
                        power: new DamageElementValues(100, 100, 100, 100, 100, 100),
                        defense: default,
                        resistance: default));
        
                    state.SetEntityRpgStats(target, CombatantRpgStats.Empty);
        
                    var policy = new SkillActionPolicy(
                        skillId: 700,
                        minRange: 1,
                        maxRange: 3,
                        baseDamage: 10,
                        baseHitChance: 100,
                        elevationPerRangeBonus: 2,
                        rangeBonusPerElevationStep: 1,
                        resourceCost: 1,
                        damageElement: DamageElement.Elemental,
                        baseCriticalChance: 20);
        
                    var resolver = new ActionResolver(new SeededRngService(12));
                    var result = resolver.Resolve(state, new CastSkillAction(caster, target, policy));
        
                    Assert.That(result.Success, Is.True);
                    Assert.That(result.Description, Does.Contain("critChance:60"));
                }

    }
}