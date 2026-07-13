using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace TacticalPort.State.Tests
{
    public sealed class TeamPresetStateTests
    {
        private MemoryTeamPresetRepository _Repository;

        [SetUp]
        public void SetUp()
        {
            _Repository = new MemoryTeamPresetRepository();
            TeamPresetState.ConfigureRepository(_Repository);
            TeamPresetState.InitializeAsync().GetAwaiter().GetResult();
        }

        [TearDown]
        public void TearDown() => TeamPresetState.ConfigureRepository(new PlayerPrefsTeamPresetRepository());

        [Test]
        public void ReorderingUnitsPreservesTheirBuilds()
        {
            TeamPresetState.SetActiveUnitIds(new[] { "unit-a", "unit-b", "unit-c" });
            UnitBuildPreset lBuild = FindBuild("unit-b");
            lBuild.PassiveId = "passive-b";
            lBuild.SkillIds.Add("skill-b");

            TeamPresetState.SetActiveUnitIds(new[] { "unit-c", "unit-b", "unit-a" });

            UnitBuildPreset lReorderedBuild = FindBuild("unit-b");
            Assert.That(lReorderedBuild.PassiveId, Is.EqualTo("passive-b"));
            Assert.That(lReorderedBuild.SkillIds, Is.EqualTo(new[] { "skill-b" }));
        }

        [Test]
        public async Task SaveCapturesAnIndependentSnapshot()
        {
            TeamPresetState.SetActiveUnitIds(new[] { "unit-a", "unit-b", "unit-c" });
            Task lSaveTask = TeamPresetState.SaveAsync();
            TeamPresetState.SetActiveUnitIds(new[] { "unit-c", "unit-b", "unit-a" });
            await lSaveTask;

            Assert.That(_Repository.SavedSnapshots, Has.Count.EqualTo(1));
            Assert.That(_Repository.SavedSnapshots[0].Presets[0].Slots[0].UnitId, Is.EqualTo("unit-a"));
        }

        [Test]
        public void LastPresetCannotBeDeleted()
        {
            Assert.That(TeamPresetState.DeletePreset(TeamPresetState.ActivePresetId), Is.False);
            Assert.That(TeamPresetState.Presets, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task CachedRepositoryMigratesLocalTeamWhenCloudIsEmpty()
        {
            MemoryTeamPresetRepository lCloud = new MemoryTeamPresetRepository();
            MemoryTeamPresetRepository lLocal = new MemoryTeamPresetRepository(CreateConfiguredSnapshot("local-unit"));
            CachedTeamPresetRepository lRepository = new CachedTeamPresetRepository(lCloud, lLocal);

            TeamPresetRepositorySnapshot lLoaded = await lRepository.LoadAsync();

            Assert.That(lLoaded.Presets[0].Slots[0].UnitId, Is.EqualTo("local-unit"));
            Assert.That(lCloud.SavedSnapshots, Has.Count.EqualTo(1));
        }

        [Test]
        public async Task CachedRepositoryKeepsNewerLocalSnapshot()
        {
            TeamPresetRepositorySnapshot lCloudSnapshot = CreateConfiguredSnapshot("cloud-unit");
            lCloudSnapshot.UpdatedAtUnixMilliseconds = 10;
            TeamPresetRepositorySnapshot lLocalSnapshot = CreateConfiguredSnapshot("local-unit");
            lLocalSnapshot.UpdatedAtUnixMilliseconds = 20;
            MemoryTeamPresetRepository lCloud = new MemoryTeamPresetRepository(lCloudSnapshot);
            CachedTeamPresetRepository lRepository = new CachedTeamPresetRepository(
                lCloud,
                new MemoryTeamPresetRepository(lLocalSnapshot));

            TeamPresetRepositorySnapshot lLoaded = await lRepository.LoadAsync();

            Assert.That(lLoaded.Presets[0].Slots[0].UnitId, Is.EqualTo("local-unit"));
            Assert.That(lCloud.SavedSnapshots, Has.Count.EqualTo(1));
        }

        private static UnitBuildPreset FindBuild(string pUnitId)
        {
            IReadOnlyList<UnitBuildPreset> lSlots = TeamPresetState.ActivePreset.Slots;
            for (int lIndex = 0; lIndex < lSlots.Count; lIndex++)
            {
                if (lSlots[lIndex]?.UnitId == pUnitId)
                    return lSlots[lIndex];
            }

            Assert.Fail($"Build '{pUnitId}' was not found.");
            return null;
        }

        private sealed class MemoryTeamPresetRepository : ITeamPresetRepository
        {
            public readonly List<TeamPresetRepositorySnapshot> SavedSnapshots = new List<TeamPresetRepositorySnapshot>();
            private readonly TeamPresetRepositorySnapshot _Snapshot;

            public MemoryTeamPresetRepository(TeamPresetRepositorySnapshot pSnapshot = null) =>
                _Snapshot = pSnapshot ?? new TeamPresetRepositorySnapshot();

            public Task<TeamPresetRepositorySnapshot> LoadAsync(CancellationToken pCancellationToken = default) =>
                Task.FromResult(_Snapshot.Clone());

            public Task SaveAsync(TeamPresetRepositorySnapshot pSnapshot, CancellationToken pCancellationToken = default)
            {
                SavedSnapshots.Add(pSnapshot.Clone());
                return Task.CompletedTask;
            }
        }

        private static TeamPresetRepositorySnapshot CreateConfiguredSnapshot(string pUnitId)
        {
            TeamPreset lPreset = new TeamPreset
            {
                PresetId = TeamPresetState.DefaultPresetId,
                DisplayName = "Team 1"
            };
            lPreset.Slots.Add(new UnitBuildPreset { UnitId = pUnitId });
            lPreset.Slots.Add(new UnitBuildPreset());
            lPreset.Slots.Add(new UnitBuildPreset());
            return new TeamPresetRepositorySnapshot
            {
                ActivePresetId = lPreset.PresetId,
                Presets = new List<TeamPreset> { lPreset }
            };
        }
    }
}
