using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TacticalPort.Data;
using TacticalPort.Matchmaking;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.State.Tests
{
    public sealed class MatchUnitBuildTests
    {
        [Test]
        public void LegacyManifestWithoutBuildsRemainsValid()
        {
            MatchManifest lManifest = CreateManifest();
            lManifest.SetAssignment(MatchPlayerSlot.TeamA, "player-a", "preset-a", new[] { "unit-a" });
            lManifest.SetAssignment(MatchPlayerSlot.TeamB, "player-b", "preset-b", new[] { "unit-b" });

            Assert.That(lManifest.TryValidatePlayerAssignments(out string lFailure), Is.True, lFailure);
            Assert.That(lManifest.HasAllPlayerCompositions, Is.True);
        }

        [Test]
        public void ManifestJsonRoundTripPreservesUnitBuild()
        {
            MatchManifest lManifest = CreateManifest();
            MatchUnitBuild lBuild = new MatchUnitBuild
            {
                UnitId = "unit-a",
                PassiveId = "passive-a",
                SkillIds = new List<string> { "skill-1", "skill-2" },
                StatAllocations = new MatchUnitStatAllocations { Health = 2, Initiative = 1 }
            };
            lManifest.SetAssignment(MatchPlayerSlot.TeamA, "player-a", "preset-a", new[] { "unit-a" }, new[] { lBuild });
            lManifest.SetAssignment(MatchPlayerSlot.TeamB, "player-b", "preset-b", new[] { "unit-b" });

            MatchManifest lRoundTrip = JsonUtility.FromJson<MatchManifest>(JsonUtility.ToJson(lManifest));

            Assert.That(lRoundTrip.TryValidatePlayerAssignments(out string lFailure), Is.True, lFailure);
            Assert.That(lRoundTrip.Players[0].UnitBuilds[0].PassiveId, Is.EqualTo("passive-a"));
            Assert.That(lRoundTrip.Players[0].UnitBuilds[0].SkillIds, Is.EqualTo(new[] { "skill-1", "skill-2" }));
            Assert.That(lRoundTrip.Players[0].UnitBuilds[0].StatAllocations.Health, Is.EqualTo(2));
        }

        [Test]
        public void ManifestRejectsNegativeStatAllocation()
        {
            MatchManifest lManifest = CreateManifest();
            MatchUnitBuild lBuild = new MatchUnitBuild
            {
                UnitId = "unit-a",
                StatAllocations = new MatchUnitStatAllocations { Health = -1 }
            };
            lManifest.SetAssignment(MatchPlayerSlot.TeamA, "player-a", "preset-a", new[] { "unit-a" }, new[] { lBuild });
            lManifest.SetAssignment(MatchPlayerSlot.TeamB, "player-b", "preset-b", new[] { "unit-b" });

            Assert.That(lManifest.TryValidatePlayerAssignments(out _), Is.False);
        }

        [Test]
        public void RuntimeCloneUsesSelectedSkillsPassiveAndStats()
        {
            UnitDefinition lUnit = ScriptableObject.CreateInstance<UnitDefinition>();
            SkillDefinition lSkillA = ScriptableObject.CreateInstance<SkillDefinition>();
            SkillDefinition lSkillB = ScriptableObject.CreateInstance<SkillDefinition>();
            PassiveDefinition lPassiveA = ScriptableObject.CreateInstance<PassiveDefinition>();
            PassiveDefinition lPassiveB = ScriptableObject.CreateInstance<PassiveDefinition>();
            lSkillA.name = "skill-a";
            lSkillB.name = "skill-b";
            lPassiveA.name = "passive-a";
            lPassiveB.name = "passive-b";
            SetPrivateField(lUnit, "_Skills", new List<SkillDefinition> { lSkillA, lSkillB });
            SetPrivateField(lUnit, "_Passives", new List<PassiveDefinition> { lPassiveA, lPassiveB });
            SetPrivateField(lUnit, "_DefaultPassive", lPassiveA);

            UnitDefinition lClone = UnitDefinition.CreateRuntimeClone(
                lUnit,
                Team.TeamB,
                new UnitCombatLoadout(
                    lPassiveB,
                    new[] { lSkillB },
                    new UnitStatModifiers(pHealth: 12, pInitiative: 3)));

            Assert.That(lClone.Skills, Is.EqualTo(new[] { lSkillB }));
            Assert.That(lClone.DefaultPassive, Is.SameAs(lPassiveB));
            Assert.That(lClone.Team, Is.EqualTo(Team.TeamB));
            Assert.That(lClone.MaxHealth, Is.EqualTo(lUnit.MaxHealth + 12));
            Assert.That(lClone.Initiative, Is.EqualTo(lUnit.Initiative + 3));

            Object.DestroyImmediate(lClone);
            Object.DestroyImmediate(lUnit);
            Object.DestroyImmediate(lSkillA);
            Object.DestroyImmediate(lSkillB);
            Object.DestroyImmediate(lPassiveA);
            Object.DestroyImmediate(lPassiveB);
        }

        private static MatchManifest CreateManifest() => new MatchManifest { MatchId = "match-test" };

        private static void SetPrivateField<T>(UnitDefinition pUnit, string pFieldName, T pValue) =>
            typeof(UnitDefinition).GetField(pFieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(pUnit, pValue);
    }
}
