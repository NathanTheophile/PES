using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TacticalPort.Core;
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
                StatAllocations = new MatchUnitStatAllocations { Health = 2, Velocity = 1 }
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
        public void ManifestJsonRoundTripPreservesPerfectVelocityTieStartingSlot()
        {
            MatchManifest lManifest = CreateManifest();
            lManifest.PerfectVelocityTieStartingSlot = MatchPlayerSlot.TeamB;

            MatchManifest lRoundTrip = JsonUtility.FromJson<MatchManifest>(JsonUtility.ToJson(lManifest));

            Assert.That(lRoundTrip.PerfectVelocityTieStartingSlot, Is.EqualTo(MatchPlayerSlot.TeamB));
            Assert.That(lRoundTrip.ResolvePerfectVelocityTieStartingSlot(), Is.EqualTo(MatchPlayerSlot.TeamB));
        }

        [Test]
        public void LegacyManifestTieBreakerIsDeterministicFromMatchId()
        {
            MatchManifest lFirst = new MatchManifest { MatchId = "authoritative-match-id" };
            MatchManifest lSecond = new MatchManifest { MatchId = "authoritative-match-id" };

            MatchPlayerSlot lResolvedSlot = lFirst.ResolvePerfectVelocityTieStartingSlot();
            Assert.That(lResolvedSlot, Is.EqualTo(lSecond.ResolvePerfectVelocityTieStartingSlot()));
            Assert.That(lResolvedSlot == MatchPlayerSlot.TeamA || lResolvedSlot == MatchPlayerSlot.TeamB, Is.True);
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
                    new UnitStatModifiers(pHealth: 12, pVelocity: 3)));

            Assert.That(lClone.Skills, Is.EqualTo(new[] { lSkillB }));
            Assert.That(lClone.DefaultPassive, Is.SameAs(lPassiveB));
            Assert.That(lClone.Team, Is.EqualTo(Team.TeamB));
            Assert.That(lClone.MaxHealth, Is.EqualTo(lUnit.MaxHealth + 12));
            Assert.That(lClone.Velocity, Is.EqualTo(lUnit.Velocity + 3));

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

    public sealed class MatchMapSceneCatalogTests
    {
        [TestCase("alpha-1")]
        [TestCase("poutch")]
        [TestCase("custom-direct")]
        [TestCase("custom-relay")]
        public void DefaultCatalogResolvesCanonicalIdAndAliases(string pMapId)
        {
            MatchMapSceneCatalog lCatalog = new MatchMapSceneCatalog();

            Assert.That(lCatalog.TryResolveScene(pMapId, out string lSceneName, out string lFailure), Is.True, lFailure);
            Assert.That(lSceneName, Is.EqualTo("S_Map_Alpha_1"));
        }

        [Test]
        public void CatalogRejectsUnknownAndDuplicateMapIds()
        {
            MatchMapSceneCatalog lCatalog = new MatchMapSceneCatalog();
            Assert.That(lCatalog.TryResolveScene("missing", out _, out _), Is.False);

            MatchMapSceneCatalog lDuplicateCatalog = new MatchMapSceneCatalog(new[]
            {
                new MatchMapSceneCatalog.Entry("alpha-1", "SceneA"),
                new MatchMapSceneCatalog.Entry("ALPHA-1", "SceneB")
            });

            Assert.That(lDuplicateCatalog.TryValidate(out _), Is.False);
        }
    }

    public sealed class UnitRuntimeResourceTests
    {
        [Test]
        public void EnergyAndMobilityCanBeSpentAndResetEachTurn()
        {
            UnitDefinition lDefinition = ScriptableObject.CreateInstance<UnitDefinition>();
            try
            {
                SetPrivateField(lDefinition, "_EnergyPerTurn", 6);
                SetPrivateField(lDefinition, "_MobilityPerTurn", 4);
                UnitRuntime lUnit = new UnitRuntime(new UnitId(1), lDefinition, new GridCoord(0, 0));

                lUnit.BeginTurn();
                Assert.That(lUnit.TrySpendEnergy(2), Is.True);
                Assert.That(lUnit.TrySpendMobility(3), Is.True);
                Assert.That(lUnit.RemainingEnergy, Is.EqualTo(4));
                Assert.That(lUnit.RemainingMobility, Is.EqualTo(1));
                Assert.That(lUnit.TrySpendMobility(2), Is.False);

                lUnit.EndTurn();
                Assert.That(lUnit.RemainingEnergy, Is.Zero);
                Assert.That(lUnit.RemainingMobility, Is.Zero);

                lUnit.BeginTurn();
                Assert.That(lUnit.RemainingEnergy, Is.EqualTo(6));
                Assert.That(lUnit.RemainingMobility, Is.EqualTo(4));
            }
            finally
            {
                Object.DestroyImmediate(lDefinition);
            }
        }

        private static void SetPrivateField<T>(UnitDefinition pUnit, string pFieldName, T pValue) =>
            typeof(UnitDefinition).GetField(pFieldName, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(pUnit, pValue);
    }

    public sealed class TurnSystemOrderTests
    {
        [Test]
        public void TurnOrderUsesStartingTeamThenExplicitSlots()
        {
            List<UnitDefinition> lDefinitions = new List<UnitDefinition>();
            try
            {
                UnitRuntime lTeamASlot2 = CreateUnit(lDefinitions, 1, Team.TeamA, 10, 1);
                UnitRuntime lTeamBSlot2 = CreateUnit(lDefinitions, 2, Team.TeamB, 5, 1);
                UnitRuntime lTeamASlot1 = CreateUnit(lDefinitions, 3, Team.TeamA, 10, 0);
                UnitRuntime lTeamBSlot1 = CreateUnit(lDefinitions, 4, Team.TeamB, 5, 0);
                TurnSystem lTurnSystem = new TurnSystem();

                lTurnSystem.Initialize(new[] { lTeamASlot2, lTeamBSlot2, lTeamASlot1, lTeamBSlot1 }, Team.TeamB);

                Assert.That(lTurnSystem.TurnOrder, Is.EqualTo(new[] { lTeamASlot1, lTeamBSlot1, lTeamASlot2, lTeamBSlot2 }));
            }
            finally
            {
                DestroyDefinitions(lDefinitions);
            }
        }

        [Test]
        public void EqualTotalsUseHighestIndividualVelocity()
        {
            List<UnitDefinition> lDefinitions = new List<UnitDefinition>();
            try
            {
                UnitRuntime lTeamA1 = CreateUnit(lDefinitions, 1, Team.TeamA, 8, 0);
                UnitRuntime lTeamA2 = CreateUnit(lDefinitions, 2, Team.TeamA, 2, 1);
                UnitRuntime lTeamB1 = CreateUnit(lDefinitions, 3, Team.TeamB, 6, 0);
                UnitRuntime lTeamB2 = CreateUnit(lDefinitions, 4, Team.TeamB, 4, 1);
                TurnSystem lTurnSystem = new TurnSystem();

                lTurnSystem.Initialize(new[] { lTeamA1, lTeamA2, lTeamB1, lTeamB2 }, Team.TeamB);

                Assert.That(lTurnSystem.TurnOrder[0].Team, Is.EqualTo(Team.TeamA));
            }
            finally
            {
                DestroyDefinitions(lDefinitions);
            }
        }

        [Test]
        public void StartingTeamUsesRuntimeVelocityAllocations()
        {
            List<UnitDefinition> lDefinitions = new List<UnitDefinition>();
            try
            {
                UnitDefinition lTeamASource = CreateDefinition(lDefinitions, 5);
                UnitDefinition lTeamBSource = CreateDefinition(lDefinitions, 8);
                UnitDefinition lTeamARuntime = UnitDefinition.CreateRuntimeClone(
                    lTeamASource,
                    Team.TeamA,
                    new UnitCombatLoadout(null, null, new UnitStatModifiers(pVelocity: 4)));
                UnitDefinition lTeamBRuntime = UnitDefinition.CreateRuntimeClone(lTeamBSource, Team.TeamB);
                lDefinitions.Add(lTeamARuntime);
                lDefinitions.Add(lTeamBRuntime);
                UnitRuntime lTeamA = new UnitRuntime(new UnitId(1), lTeamARuntime, new GridCoord(0, 0), Team.TeamA, 0);
                UnitRuntime lTeamB = new UnitRuntime(new UnitId(2), lTeamBRuntime, new GridCoord(0, 0), Team.TeamB, 0);
                TurnSystem lTurnSystem = new TurnSystem();

                lTurnSystem.Initialize(new[] { lTeamB, lTeamA }, Team.TeamB);

                Assert.That(lTurnSystem.TurnOrder[0], Is.SameAs(lTeamA));
            }
            finally
            {
                DestroyDefinitions(lDefinitions);
            }
        }

        [Test]
        public void PerfectTieUsesAuthoritativeFallback()
        {
            List<UnitDefinition> lDefinitions = new List<UnitDefinition>();
            try
            {
                UnitRuntime lTeamA1 = CreateUnit(lDefinitions, 1, Team.TeamA, 5, 0);
                UnitRuntime lTeamA2 = CreateUnit(lDefinitions, 2, Team.TeamA, 5, 1);
                UnitRuntime lTeamB1 = CreateUnit(lDefinitions, 3, Team.TeamB, 5, 0);
                UnitRuntime lTeamB2 = CreateUnit(lDefinitions, 4, Team.TeamB, 5, 1);
                TurnSystem lTurnSystem = new TurnSystem();

                lTurnSystem.Initialize(new[] { lTeamA1, lTeamA2, lTeamB1, lTeamB2 }, Team.TeamB);

                Assert.That(lTurnSystem.TurnOrder, Is.EqualTo(new[] { lTeamB1, lTeamA1, lTeamB2, lTeamA2 }));
            }
            finally
            {
                DestroyDefinitions(lDefinitions);
            }
        }

        [Test]
        public void IncompleteTeamsStillAlternateInSlotOrder()
        {
            List<UnitDefinition> lDefinitions = new List<UnitDefinition>();
            try
            {
                UnitRuntime lTeamA1 = CreateUnit(lDefinitions, 1, Team.TeamA, 5, 0);
                UnitRuntime lTeamA2 = CreateUnit(lDefinitions, 2, Team.TeamA, 4, 1);
                UnitRuntime lTeamB1 = CreateUnit(lDefinitions, 3, Team.TeamB, 10, 0);
                TurnSystem lTurnSystem = new TurnSystem();

                lTurnSystem.Initialize(new[] { lTeamA2, lTeamB1, lTeamA1 }, Team.TeamA);

                Assert.That(lTurnSystem.TurnOrder, Is.EqualTo(new[] { lTeamB1, lTeamA1, lTeamA2 }));
            }
            finally
            {
                DestroyDefinitions(lDefinitions);
            }
        }

        private static UnitRuntime CreateUnit(
            ICollection<UnitDefinition> pDefinitions,
            int pId,
            Team pTeam,
            int pVelocity,
            int pSlot)
        {
            UnitDefinition lDefinition = CreateDefinition(pDefinitions, pVelocity);
            return new UnitRuntime(new UnitId(pId), lDefinition, new GridCoord(0, 0), pTeam, pSlot);
        }

        private static UnitDefinition CreateDefinition(ICollection<UnitDefinition> pDefinitions, int pVelocity)
        {
            UnitDefinition lDefinition = ScriptableObject.CreateInstance<UnitDefinition>();
            typeof(UnitDefinition).GetField("_Velocity", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(lDefinition, pVelocity);
            pDefinitions.Add(lDefinition);
            return lDefinition;
        }

        private static void DestroyDefinitions(IEnumerable<UnitDefinition> pDefinitions)
        {
            foreach (UnitDefinition lDefinition in pDefinitions)
                Object.DestroyImmediate(lDefinition);
        }
    }
}
