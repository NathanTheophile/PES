using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using TacticalPort.Data;
using TacticalPort.UI;
using UnityEngine;

namespace TacticalPort.State.Tests
{
    public sealed class DeckbuildingDraftTests
    {
        private MemoryTeamPresetRepository _Repository;

        [SetUp]
        public void SetUp()
        {
            _Repository = new MemoryTeamPresetRepository();
            TeamPresetState.ConfigureRepository(_Repository);
            TeamPresetState.InitializeAsync().GetAwaiter().GetResult();
            TeamSelectionState.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            TeamSelectionState.Clear();
            TeamPresetState.ConfigureRepository(new PlayerPrefsTeamPresetRepository());
        }

        [Test]
        public void PassiveChangeIsSavedOnlyWhenDraftIsValidated()
        {
            UnitDefinition lUnit = ScriptableObject.CreateInstance<UnitDefinition>();
            PassiveDefinition lPassiveA = ScriptableObject.CreateInstance<PassiveDefinition>();
            PassiveDefinition lPassiveB = ScriptableObject.CreateInstance<PassiveDefinition>();
            SkillDefinition lSkill = ScriptableObject.CreateInstance<SkillDefinition>();
            GameObject lPanel = new GameObject("DeckbuildingDraftTest");
            lPanel.SetActive(false);

            try
            {
                lUnit.name = "unit-a";
                lPassiveA.name = "passive-a";
                lPassiveB.name = "passive-b";
                lSkill.name = "skill-a";
                SetUnitField(lUnit, "_Passives", new List<PassiveDefinition> { lPassiveA, lPassiveB });
                SetUnitField(lUnit, "_DefaultPassive", lPassiveA);
                SetUnitField(lUnit, "_Skills", new List<SkillDefinition> { lSkill });

                TeamSelectionState.SetSelectedUnits(new[] { lUnit });
                TeamPresetState.SetPassive(lUnit, lPassiveA);
                TeamPresetState.SetSkills(lUnit, new[] { lSkill });

                DeckbuildingPanelController lController = lPanel.AddComponent<DeckbuildingPanelController>();
                SetControllerField(lController, "_AvailableUnits", new List<UnitDefinition> { lUnit });
                SetControllerField(lController, "_TeamSize", 1);
                lController.ShowForTeamSlot(0);

                InvokeController(lController, "SelectPassive", 1);

                Assert.That(TeamPresetState.ResolvePassive(lUnit), Is.SameAs(lPassiveA));
                Assert.That(_Repository.SavedSnapshots, Is.Empty);

                InvokeController(lController, "ValidateChanges");

                Assert.That(TeamPresetState.ResolvePassive(lUnit), Is.SameAs(lPassiveB));
                Assert.That(_Repository.SavedSnapshots, Has.Count.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(lPanel);
                Object.DestroyImmediate(lUnit);
                Object.DestroyImmediate(lPassiveA);
                Object.DestroyImmediate(lPassiveB);
                Object.DestroyImmediate(lSkill);
            }
        }

        [Test]
        public void StatChangeIsSavedOnlyWhenDraftIsValidated()
        {
            UnitDefinition lUnit = ScriptableObject.CreateInstance<UnitDefinition>();
            SkillDefinition lSkill = ScriptableObject.CreateInstance<SkillDefinition>();
            GameObject lPanel = new GameObject("DeckbuildingStatDraftTest");
            lPanel.SetActive(false);

            try
            {
                lUnit.name = "unit-stats";
                lSkill.name = "skill-a";
                SetUnitField(lUnit, "_StatPointBudget", 120);
                SetUnitField(lUnit, "_Skills", new List<SkillDefinition> { lSkill });

                TeamSelectionState.SetSelectedUnits(new[] { lUnit });
                TeamPresetState.SetSkills(lUnit, new[] { lSkill });

                DeckbuildingPanelController lController = lPanel.AddComponent<DeckbuildingPanelController>();
                SetControllerField(lController, "_AvailableUnits", new List<UnitDefinition> { lUnit });
                SetControllerField(lController, "_TeamSize", 1);
                lController.ShowForTeamSlot(0);

                InvokeController(lController, "ChangeStat", UnitStatType.ActionPoints, 1);

                Assert.That(TeamPresetState.ResolveStatAllocations(lUnit).Power, Is.Zero);
                Assert.That(_Repository.SavedSnapshots, Is.Empty);

                InvokeController(lController, "ValidateChanges");

                Assert.That(TeamPresetState.ResolveStatAllocations(lUnit).Power, Is.EqualTo(1));
                Assert.That(_Repository.SavedSnapshots, Has.Count.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(lPanel);
                Object.DestroyImmediate(lUnit);
                Object.DestroyImmediate(lSkill);
            }
        }

        private static void SetUnitField<T>(UnitDefinition pUnit, string pName, T pValue) =>
            typeof(UnitDefinition).GetField(pName, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(pUnit, pValue);

        private static void SetControllerField<T>(DeckbuildingPanelController pController, string pName, T pValue) =>
            typeof(DeckbuildingPanelController).GetField(pName, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(pController, pValue);

        private static void InvokeController(DeckbuildingPanelController pController, string pName, params object[] pArguments) =>
            typeof(DeckbuildingPanelController).GetMethod(pName, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(pController, pArguments);

        private sealed class MemoryTeamPresetRepository : ITeamPresetRepository
        {
            public readonly List<TeamPresetRepositorySnapshot> SavedSnapshots = new List<TeamPresetRepositorySnapshot>();

            public Task<TeamPresetRepositorySnapshot> LoadAsync(CancellationToken pCancellationToken = default) =>
                Task.FromResult(new TeamPresetRepositorySnapshot());

            public Task SaveAsync(TeamPresetRepositorySnapshot pSnapshot, CancellationToken pCancellationToken = default)
            {
                SavedSnapshots.Add(pSnapshot.Clone());
                return Task.CompletedTask;
            }
        }
    }
}
