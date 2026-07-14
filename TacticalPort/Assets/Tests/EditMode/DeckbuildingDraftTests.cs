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

                InvokeController(lController, "ChangeStat", UnitStatType.Energy, 1);

                Assert.That(TeamPresetState.ResolveStatAllocations(lUnit).Energy, Is.Zero);
                Assert.That(_Repository.SavedSnapshots, Is.Empty);

                InvokeController(lController, "ValidateChanges");

                Assert.That(TeamPresetState.ResolveStatAllocations(lUnit).Energy, Is.EqualTo(1));
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

    public sealed class TeamPresetDraftModelTests
    {
        private readonly List<Object> _Assets = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int lIndex = _Assets.Count - 1; lIndex >= 0; lIndex--)
                Object.DestroyImmediate(_Assets[lIndex]);
            _Assets.Clear();
        }

        [Test]
        public void SelectionRejectsAUnitAlreadyUsedByAnotherSlot()
        {
            UnitDefinition lUnitA = CreateUnit("unit-a");
            UnitDefinition lUnitB = CreateUnit("unit-b");
            TeamPresetDraft lDraft = CreateDraft(2, new[] { lUnitA, lUnitB });

            Assert.That(lDraft.TrySelectUnit(0, lUnitB), Is.False);
            Assert.That(lDraft.SelectedUnits, Is.EqualTo(new[] { lUnitA, lUnitB }));
            Assert.That(lDraft.IsValid, Is.True);
        }

        [Test]
        public void PassiveAndSkillSelectionsEnforceTheUnitPoolAndUniqueSkills()
        {
            PassiveDefinition lPassiveA = CreateAsset<PassiveDefinition>("passive-a");
            PassiveDefinition lPassiveB = CreateAsset<PassiveDefinition>("passive-b");
            List<SkillDefinition> lSkills = new List<SkillDefinition>();
            for (int lIndex = 0; lIndex < TeamPreset.EquippedSkillCount + 1; lIndex++)
                lSkills.Add(CreateAsset<SkillDefinition>($"skill-{lIndex}"));
            UnitDefinition lUnit = CreateUnit("unit-a", lSkills, new[] { lPassiveA, lPassiveB }, lPassiveA);
            TeamPresetDraft lDraft = CreateDraft(1, new[] { lUnit });

            Assert.That(lDraft.TrySelectPassive(lUnit, lPassiveB), Is.True);
            Assert.That(lDraft.TrySelectSkill(lUnit, 0, lSkills[6]), Is.True);
            Assert.That(lDraft.TrySelectSkill(lUnit, 1, lSkills[6]), Is.False);
            Assert.That(lDraft.GetBuild(lUnit).Passive, Is.SameAs(lPassiveB));
            Assert.That(lDraft.GetBuild(lUnit).Skills[0], Is.SameAs(lSkills[6]));
            Assert.That(lDraft.IsValid, Is.True);
        }

        [Test]
        public void StatChangesRespectBudgetAndRemainInsideTheDraft()
        {
            UnitDefinition lUnit = CreateUnit("unit-a", pStatBudget: UnitStatAllocationRules.EnergyCost);
            TeamPresetDraft lDraft = CreateDraft(1, new[] { lUnit });

            Assert.That(lDraft.TryChangeStat(lUnit, UnitStatType.Energy, 1), Is.True);
            Assert.That(lDraft.TryChangeStat(lUnit, UnitStatType.Energy, 1), Is.False);
            Assert.That(lDraft.GetBuild(lUnit).StatAllocations.Energy, Is.EqualTo(1));
            Assert.That(lDraft.IsValid, Is.True);
        }

        [Test]
        public void PersistedPresetComparisonIncludesOrderBuildAndStats()
        {
            UnitDefinition lUnitA = CreateUnit("unit-a", pStatBudget: UnitStatAllocationRules.EnergyCost);
            UnitDefinition lUnitB = CreateUnit("unit-b");
            TeamPresetDraft lInitial = CreateDraft(2, new[] { lUnitA, lUnitB });
            TeamPreset lPersisted = new TeamPreset { Slots = lInitial.CreateBuildPresets() };
            TeamPresetDraft lLoaded = CreateDraft(2, new[] { lUnitA, lUnitB }, lPersisted);

            Assert.That(lLoaded.MatchesPersistedPreset(), Is.True);
            Assert.That(lLoaded.HasPendingChanges, Is.False);

            Assert.That(lLoaded.TryChangeStat(lUnitA, UnitStatType.Energy, 1), Is.True);
            Assert.That(lLoaded.MatchesPersistedPreset(), Is.False);
            Assert.That(lLoaded.HasPendingChanges, Is.True);
        }

        [Test]
        public void DraftIsInvalidWhenTheAvailableRosterCannotFillEverySlot()
        {
            UnitDefinition lUnit = CreateUnit("unit-a");
            TeamPresetDraft lDraft = CreateDraft(2, new[] { lUnit });

            Assert.That(lDraft.Validate(out string lFailure), Is.False);
            Assert.That(lFailure, Does.Contain("exactly 2 units"));
        }

        private TeamPresetDraft CreateDraft(int pTeamSize, IReadOnlyList<UnitDefinition> pUnits, TeamPreset pPersisted = null) =>
            new TeamPresetDraft(pTeamSize, pUnits, pUnits, null, pPersisted);

        private UnitDefinition CreateUnit(
            string pName,
            IReadOnlyList<SkillDefinition> pSkills = null,
            IReadOnlyList<PassiveDefinition> pPassives = null,
            PassiveDefinition pDefaultPassive = null,
            int pStatBudget = 0)
        {
            UnitDefinition lUnit = CreateAsset<UnitDefinition>(pName);
            SetUnitField(lUnit, "_Skills", pSkills != null ? new List<SkillDefinition>(pSkills) : new List<SkillDefinition>());
            SetUnitField(lUnit, "_Passives", pPassives != null ? new List<PassiveDefinition>(pPassives) : new List<PassiveDefinition>());
            SetUnitField(lUnit, "_DefaultPassive", pDefaultPassive);
            SetUnitField(lUnit, "_StatPointBudget", pStatBudget);
            return lUnit;
        }

        private T CreateAsset<T>(string pName) where T : ScriptableObject
        {
            T lAsset = ScriptableObject.CreateInstance<T>();
            lAsset.name = pName;
            _Assets.Add(lAsset);
            return lAsset;
        }

        private static void SetUnitField<T>(UnitDefinition pUnit, string pName, T pValue) =>
            typeof(UnitDefinition).GetField(pName, BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(pUnit, pValue);
    }
}
