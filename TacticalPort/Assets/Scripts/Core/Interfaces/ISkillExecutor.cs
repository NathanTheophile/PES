using TacticalPort.Core.Runtime;
using TacticalPort.Data;

namespace TacticalPort.Core.Interfaces
{
    public interface ISkillExecutor
    {
        BattleActionResult Validate(BattleUnitRuntime actor, SkillDefinition skill, SkillTarget target, SkillExecutionContext context);
        BattleActionResult Execute(BattleUnitRuntime actor, SkillDefinition skill, SkillTarget target, SkillExecutionContext context);
    }
}
