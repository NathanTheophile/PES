using TacticalPort.Core.Runtime;
using TacticalPort.Data;

namespace TacticalPort.Core.Interfaces
{
    public interface ISkillExecutor
    {
        BattleActionResult Validate(UnitRuntime actor, SkillDefinition skill, SkillTarget target, SkillExecutionContext context);
        BattleActionResult Execute(UnitRuntime actor, SkillDefinition skill, SkillTarget target, SkillExecutionContext context);
    }
}
