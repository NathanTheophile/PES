using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TacticalPort.Core;
using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.State.Tests
{
    public sealed class GlyphSystemTests
    {
        private readonly List<Object> _Objects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int lIndex = _Objects.Count - 1; lIndex >= 0; lIndex--)
                Object.DestroyImmediate(_Objects[lIndex]);
            _Objects.Clear();
        }

        [Test]
        public void LegacyGlyphRemainsTimedTurnStartDamageAndState()
        {
            StateDefinition lState = CreateState("legacy-state");
            UnitRuntime lUnit = new UnitRuntime(new UnitId(1), CreateUnit("target", Team.TeamB), new GridCoord(0, 0));
            GridGlyphRuntime lGlyph = new GridGlyphRuntime(
                new UnitId(2), Team.TeamA, new GridCoord(0, 0), 12, 2,
                SkillGlyphTargetRule.EnemiesOnly, "legacy", "legacy-group", lState, 1, 1);

            Assert.That(lGlyph.Definition, Is.Null);
            Assert.That(lGlyph.TriggerTiming, Is.EqualTo(GlyphTriggerTiming.TurnStart));
            Assert.That(lGlyph.CanAffect(lUnit), Is.True);
            lGlyph.AdvanceTurn();
            Assert.That(lGlyph.RemainingTurns, Is.EqualTo(1));
            Assert.That(lGlyph.IsExpired, Is.False);
        }

        [Test]
        public void TurnEndGlyphHealsWithRelationSpecificEffects()
        {
            GlyphDefinition lGlyph = CreateGlyph("end-heal", GlyphTriggerTiming.TurnEnd, GlyphTargetUnitType.CharactersOnly);
            SetField(lGlyph, "_AllyEffects", new List<GlyphEffectDefinition> { CreateEffect(GlyphEffectType.Heal, 17) });
            SkillDefinition lSkill = CreateGlyphSkill("healing-glyph", lGlyph);
            UnitDefinition lActorDefinition = CreateUnit("actor", Team.TeamA, lSkill);
            IBattleService lBattle = CreateBattle(Spawn(lActorDefinition, 2, 2, 0), Spawn(CreateUnit("enemy", Team.TeamB), 7, 7, 0));
            Assert.That(lBattle.TryGetUnit(new UnitId(1), out UnitRuntime lActor), Is.True);
            lActor.ApplyDirectHealthLoss(30);
            int lHealthBeforeGlyph = lActor.CurrentHealth;
            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);
            Assert.That(lBattle.UseSkill(lActor.Id, new SkillId(lSkill.Id), SkillTarget.ForCell(lActor.Position)).IsSuccess, Is.True);

            Assert.That(lBattle.EndTurn(lActor.Id).IsSuccess, Is.True);

            Assert.That(lActor.CurrentHealth, Is.EqualTo(Mathf.Min(lActor.CurrentMaxHealth, lHealthBeforeGlyph + 17)));
        }

        [Test]
        public void PersistentPresenceAppliesImmediatelyAndIsRemovedAfterLeaving()
        {
            StateDefinition lState = CreateState("presence-state");
            GlyphDefinition lGlyph = CreateGlyph("presence", GlyphTriggerTiming.PersistentPresence, GlyphTargetUnitType.CharactersOnly);
            SetField(lGlyph, "_AllyEffects", new List<GlyphEffectDefinition> { CreateEffect(GlyphEffectType.ApplyState, pState: lState) });
            SkillDefinition lSkill = CreateGlyphSkill("presence-glyph", lGlyph);
            UnitDefinition lActorDefinition = CreateUnit("actor", Team.TeamA, lSkill);
            IBattleService lBattle = CreateBattle(Spawn(lActorDefinition, 2, 2, 0), Spawn(CreateUnit("enemy", Team.TeamB), 7, 7, 0));
            Assert.That(lBattle.TryGetUnit(new UnitId(1), out UnitRuntime lActor), Is.True);
            Assert.That(lBattle.TryStartNextTurn(out _), Is.True);

            Assert.That(lBattle.UseSkill(lActor.Id, new SkillId(lSkill.Id), SkillTarget.ForCell(lActor.Position)).IsSuccess, Is.True);
            Assert.That(lActor.HasState(lState), Is.True);

            Assert.That(lBattle.MoveUnit(lActor.Id, new GridCoord(3, 2)).IsSuccess, Is.True);
            Assert.That(lActor.HasState(lState), Is.False);
        }

        [Test]
        public void SourceBoundGlyphDoesNotAgeAndCanBeRemovedByInvocationId()
        {
            BattleScenarioDefinition lScenario = Track(BattleScenarioDefinition.CreateRuntime(
                "grid", "Grid", 4, 4, 1, null, new List<UnitSpawnDefinition>()));
            GridService lGrid = new GridService();
            lGrid.Initialize(lScenario);
            GlyphDefinition lDefinition = CreateGlyph("attached", GlyphTriggerTiming.PersistentPresence, GlyphTargetUnitType.CharactersOnly);
            UnitId lInvocationId = new UnitId(8);
            GridGlyphRuntime lGlyph = new GridGlyphRuntime(
                lInvocationId, Team.TeamA, new GridCoord(1, 1), lDefinition, 1,
                "summon:attached:1:1", "summon-group", lInvocationId);
            lGrid.AddOrReplaceGlyph(lGlyph);

            lGrid.AdvancePersistentEffects();
            Assert.That(lGlyph.IsExpired, Is.False);
            Assert.That(lGrid.RemoveGlyphsByLifetimeSource(lInvocationId), Is.EqualTo(1));
            Assert.That(lGrid.GetAllGlyphs(), Is.Empty);
        }

        private GlyphDefinition CreateGlyph(string pId, GlyphTriggerTiming pTiming, GlyphTargetUnitType pTargetType)
        {
            GlyphDefinition lGlyph = Track(ScriptableObject.CreateInstance<GlyphDefinition>());
            SetField(lGlyph, "_Id", pId);
            SetField(lGlyph, "_Shape", SkillAoeShape.Single);
            SetField(lGlyph, "_TriggerTiming", pTiming);
            SetField(lGlyph, "_TargetUnitType", pTargetType);
            return lGlyph;
        }

        private static GlyphEffectDefinition CreateEffect(GlyphEffectType pType, int pPower = 0, StateDefinition pState = null)
        {
            GlyphEffectDefinition lEffect = new GlyphEffectDefinition();
            SetField(lEffect, "_Type", pType);
            SetField(lEffect, "_Power", pPower);
            SetField(lEffect, "_State", pState);
            return lEffect;
        }

        private SkillDefinition CreateGlyphSkill(string pId, GlyphDefinition pGlyph)
        {
            SkillDefinition lSkill = Track(ScriptableObject.CreateInstance<SkillDefinition>());
            SetField(lSkill, "_Id", pId);
            SetField(lSkill, "_TargetType", SkillTargetType.Self);
            SetField(lSkill, "_TargetRelation", SkillTargetRelation.Anyone);
            SetField(lSkill, "_CanAffectCaster", true);
            SetField(lSkill, "_PrimaryEffectType", SkillPrimaryEffectType.None);
            SetField(lSkill, "_AdditionalEffectType", SkillAdditionalEffectType.CreateGlyph);
            SetField(lSkill, "_GlyphDefinition", pGlyph);
            SetField(lSkill, "_GlyphDurationTurns", 3);
            return lSkill;
        }

        private UnitDefinition CreateUnit(string pId, Team pTeam, params SkillDefinition[] pSkills)
        {
            UnitDefinition lUnit = Track(ScriptableObject.CreateInstance<UnitDefinition>());
            SetField(lUnit, "_Id", pId);
            SetField(lUnit, "_Team", pTeam);
            SetField(lUnit, "_MaxHealth", 100);
            SetField(lUnit, "_MobilityPerTurn", 4);
            SetField(lUnit, "_EnergyPerTurn", 10);
            SetField(lUnit, "_Velocity", pTeam == Team.TeamA ? 100 : 1);
            SetField(lUnit, "_Skills", new List<SkillDefinition>(pSkills));
            return lUnit;
        }

        private StateDefinition CreateState(string pId)
        {
            StateDefinition lState = Track(ScriptableObject.CreateInstance<StateDefinition>());
            SetField(lState, "_Id", pId);
            return lState;
        }

        private IBattleService CreateBattle(params UnitSpawnDefinition[] pSpawns)
        {
            BattleScenarioDefinition lScenario = Track(BattleScenarioDefinition.CreateRuntime("glyph-tests", "Glyph Tests", 8, 8, 1, null, pSpawns));
            IBattleService lBattle = BattleRuntimeCompositionRoot.CreateDefaultBattleService();
            lBattle.Initialize(lScenario);
            return lBattle;
        }

        private static UnitSpawnDefinition Spawn(UnitDefinition pUnit, int pX, int pY, int pSlot) => new UnitSpawnDefinition
        {
            Unit = pUnit,
            StartCoordinate = new SerializableGridCoord(pX, pY),
            TeamSlotIndex = pSlot
        };

        private T Track<T>(T pObject) where T : Object
        {
            _Objects.Add(pObject);
            return pObject;
        }

        private static void SetField<TTarget, TValue>(TTarget pTarget, string pName, TValue pValue)
        {
            FieldInfo lField = typeof(TTarget).GetField(pName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(lField, Is.Not.Null, $"Missing field {typeof(TTarget).Name}.{pName}");
            lField.SetValue(pTarget, pValue);
        }
    }
}
