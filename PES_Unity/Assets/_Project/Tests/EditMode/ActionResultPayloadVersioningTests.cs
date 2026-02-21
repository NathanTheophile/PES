using NUnit.Framework;
using PES.Core.Simulation;

namespace PES.Tests.EditMode
{
    public class ActionResultPayloadVersioningTests
    {
        [Test]
        public void Constructor_WithoutSchemaVersion_UsesCurrentSchemaVersion()
        {
            var payload = new ActionResultPayload("SkillResolved", value1: 10, value2: 85);

            Assert.That(payload.SchemaVersion, Is.EqualTo(ActionResultPayload.CurrentSchemaVersion));
        }

        [Test]
        public void Constructor_WithCustomSchemaVersion_UsesProvidedVersion()
        {
            var payload = new ActionResultPayload("SkillResolved", value1: 10, value2: 85, schemaVersion: 2);

            Assert.That(payload.SchemaVersion, Is.EqualTo(2));
        }

        [Test]
        public void Constructor_WithInvalidSchemaVersion_FallsBackToCurrentVersion()
        {
            var payload = new ActionResultPayload("SkillResolved", schemaVersion: 0);

            Assert.That(payload.SchemaVersion, Is.EqualTo(ActionResultPayload.CurrentSchemaVersion));
        }
    }
}
