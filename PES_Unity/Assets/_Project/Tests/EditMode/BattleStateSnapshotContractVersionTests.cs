using NUnit.Framework;
using PES.Core.Simulation;

namespace PES.Tests.EditMode
{
    public sealed class BattleStateSnapshotContractVersionTests
    {
        [Test]
        public void Constructor_WithoutContractVersion_UsesCurrentVersion()
        {
            var snapshot = new BattleStateSnapshot(
                tick: 0,
                entityPositions: new EntityPositionSnapshot[0],
                entityHitPoints: new EntityHitPointSnapshot[0]);

            Assert.That(snapshot.ContractVersion, Is.EqualTo(BattleStateSnapshot.CurrentContractVersion));
        }

        [Test]
        public void Constructor_WithInvalidContractVersion_FallsBackToCurrentVersion()
        {
            var snapshot = new BattleStateSnapshot(
                tick: 0,
                entityPositions: new EntityPositionSnapshot[0],
                entityHitPoints: new EntityHitPointSnapshot[0],
                contractVersion: 0);

            Assert.That(snapshot.ContractVersion, Is.EqualTo(BattleStateSnapshot.CurrentContractVersion));
        }

        [Test]
        public void Constructor_WithCustomContractVersion_UsesProvidedVersion()
        {
            var snapshot = new BattleStateSnapshot(
                tick: 0,
                entityPositions: new EntityPositionSnapshot[0],
                entityHitPoints: new EntityHitPointSnapshot[0],
                contractVersion: 2);

            Assert.That(snapshot.ContractVersion, Is.EqualTo(2));
        }
    }
}
