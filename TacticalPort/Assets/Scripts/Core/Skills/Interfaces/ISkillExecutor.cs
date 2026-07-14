#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion




using TacticalPort.Data;

namespace TacticalPort.Core
{
    public interface ISkillExecutor
    {
        BattleActionResult Validate(UnitRuntime actor, SkillDefinition baseSkill, SkillDefinition effectiveSkill, SkillTarget target, SkillExecutionContext context);
        BattleActionResult Execute(UnitRuntime actor, SkillDefinition baseSkill, SkillDefinition effectiveSkill, SkillTarget target, SkillExecutionContext context);
    }
}
