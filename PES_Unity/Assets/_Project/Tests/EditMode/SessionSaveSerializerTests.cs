using NUnit.Framework;
using PES.Infrastructure.Serialization;

namespace PES.Tests.EditMode
{
    public sealed class SessionSaveSerializerTests
    {
        [Test]
        public void DeserializeOrDefault_WithInvalidVersion_FallsBackToCurrentVersion()
        {
            const string invalidVersionJson = "{\"ContractVersion\":0,\"LastKnownState\":\"Battle\",\"LastBattleSeed\":17,\"HasBattleToResume\":true}";

            var save = SessionSaveSerializer.DeserializeOrDefault(invalidVersionJson);

            Assert.That(save.ContractVersion, Is.EqualTo(SessionSaveData.CurrentContractVersion));
            Assert.That(save.FlowSnapshot.HasBattleToResume, Is.True);
            Assert.That(save.FlowSnapshot.LastBattleSeed, Is.EqualTo(17));
        }

        [Test]
        public void DeserializeOrDefault_WithMalformedJson_ReturnsDefaultMenuSave()
        {
            var save = SessionSaveSerializer.DeserializeOrDefault("{not-json");

            Assert.That(save.ContractVersion, Is.EqualTo(SessionSaveData.CurrentContractVersion));
            Assert.That(save.FlowSnapshot.LastKnownState, Is.EqualTo("Menu"));
            Assert.That(save.FlowSnapshot.HasBattleToResume, Is.False);
        }

        [Test]
        public void Serialize_ThenDeserialize_RoundTripsBattleSeed()
        {
            var initial = new SessionSaveData(ProductFlowSnapshot.Battle(909));

            var json = SessionSaveSerializer.Serialize(initial);
            var restored = SessionSaveSerializer.DeserializeOrDefault(json);

            Assert.That(restored.FlowSnapshot.HasBattleToResume, Is.True);
            Assert.That(restored.FlowSnapshot.LastBattleSeed, Is.EqualTo(909));
        }
    }
}
