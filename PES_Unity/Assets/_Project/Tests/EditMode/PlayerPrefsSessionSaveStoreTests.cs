using NUnit.Framework;
using PES.Infrastructure.Serialization;

namespace PES.Tests.EditMode
{
    public sealed class PlayerPrefsSessionSaveStoreTests
    {
        [Test]
        public void SaveThenLoad_RoundTripsSessionData()
        {
            var key = "PES.Tests.SessionSaveStore.RoundTrip";
            var store = new PlayerPrefsSessionSaveStore(key);
            store.Clear();

            var initial = new SessionSaveData(ProductFlowSnapshot.Battle(4242));
            store.Save(initial);

            var loaded = store.TryLoad(out var restored);

            store.Clear();
            Assert.That(loaded, Is.True);
            Assert.That(restored.FlowSnapshot.HasBattleToResume, Is.True);
            Assert.That(restored.FlowSnapshot.LastBattleSeed, Is.EqualTo(4242));
            Assert.That(restored.ContractVersion, Is.EqualTo(SessionSaveData.CurrentContractVersion));
        }

        [Test]
        public void Clear_RemovesStoredSave()
        {
            var key = "PES.Tests.SessionSaveStore.Clear";
            var store = new PlayerPrefsSessionSaveStore(key);
            store.Save(new SessionSaveData(ProductFlowSnapshot.Battle(9)));

            store.Clear();

            Assert.That(store.TryLoad(out _), Is.False);
        }
    }
}
