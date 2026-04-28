#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

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
