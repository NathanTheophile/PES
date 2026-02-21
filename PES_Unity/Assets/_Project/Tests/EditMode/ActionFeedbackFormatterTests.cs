using NUnit.Framework;
using PES.Combat.Actions;
using PES.Core.Simulation;
using PES.Presentation.Scene;

namespace PES.Tests.EditMode
{
    public class ActionFeedbackFormatterTests
    {
        [Test]
        public void FormatResolutionSummary_WithOutOfRange_UsesUnifiedFrenchLabel()
        {
            var resolution = new ActionResolution(
                success: false,
                code: ActionResolutionCode.Rejected,
                description: "BasicAttackRejected: target is outside range",
                failureReason: ActionFailureReason.OutOfRange);

            var label = ActionFeedbackFormatter.FormatResolutionSummary(resolution);

            Assert.That(label, Does.StartWith("❌ Hors de portée"));
            Assert.That(label, Does.Contain("BasicAttackRejected"));
        }

        [Test]
        public void FormatResolutionSummary_WithSucceeded_UsesSuccessPrefix()
        {
            var resolution = new ActionResolution(
                success: true,
                code: ActionResolutionCode.Succeeded,
                description: "MoveActionResolved: moved 2 tiles");

            var label = ActionFeedbackFormatter.FormatResolutionSummary(resolution);

            Assert.That(label, Is.EqualTo("✅ MoveActionResolved: moved 2 tiles"));
        }

        [Test]
        public void BuildSkillTooltip_ContainsRangeCostDamageAndState()
        {
            var policy = new SkillActionPolicy(
                skillId: 42,
                minRange: 1,
                maxRange: 4,
                baseDamage: 12,
                baseHitChance: 85,
                elevationPerRangeBonus: 2,
                rangeBonusPerElevationStep: 1,
                resourceCost: 3,
                cooldownTurns: 2);

            var tooltip = ActionFeedbackFormatter.BuildSkillTooltip(policy, currentCooldown: 0, currentResource: 5);

            Assert.That(tooltip, Does.Contain("Skill 42"));
            Assert.That(tooltip, Does.Contain("Portée 1-4"));
            Assert.That(tooltip, Does.Contain("Coût 3"));
            Assert.That(tooltip, Does.Contain("Dmg 12"));
            Assert.That(tooltip, Does.Contain("Hit 85%"));
            Assert.That(tooltip, Does.Contain("Prête"));
        }


        [Test]
        public void FormatEventRecordLine_IncludesTickAndMappedReason()
        {
            var record = new CombatEventRecord(
                tick: 7,
                code: ActionResolutionCode.Rejected,
                failureReason: ActionFailureReason.LineOfSightBlocked,
                description: "BasicAttackRejected: line of sight blocked");

            var line = ActionFeedbackFormatter.FormatEventRecordLine(record);

            Assert.That(line, Does.StartWith("[T7] ❌ Ligne de vue bloquée"));
            Assert.That(line, Does.Contain("BasicAttackRejected"));
        }


        [Test]
        public void BuildSkillSlotStatusLabel_WithCooldown_ReturnsCdTag()
        {
            var policy = new SkillActionPolicy(7, 1, 3, 9, 80, 2, 1, resourceCost: 2);

            var tag = ActionFeedbackFormatter.BuildSkillSlotStatusLabel(policy, currentCooldown: 3, currentResource: 10);

            Assert.That(tag, Is.EqualTo("CD:3"));
        }

        [Test]
        public void BuildSkillSlotStatusLabel_WithInsufficientResource_ReturnsNoResTag()
        {
            var policy = new SkillActionPolicy(7, 1, 3, 9, 80, 2, 1, resourceCost: 5);

            var tag = ActionFeedbackFormatter.BuildSkillSlotStatusLabel(policy, currentCooldown: 0, currentResource: 2);

            Assert.That(tag, Is.EqualTo("NO_RES:2/5"));
        }

        [Test]
        public void BuildSkillSlotStatusLabel_WhenReady_ReturnsReadyTag()
        {
            var policy = new SkillActionPolicy(7, 1, 3, 9, 80, 2, 1, resourceCost: 3);

            var tag = ActionFeedbackFormatter.BuildSkillSlotStatusLabel(policy, currentCooldown: 0, currentResource: 4);

            Assert.That(tag, Is.EqualTo("READY"));
        }

    }
}
