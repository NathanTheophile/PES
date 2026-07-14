#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion







using TacticalPort.Data;

namespace TacticalPort.Core
{
    public sealed class SimpleSkillExecutor : ISkillExecutor
    {
        #region _____________________________| SKILLS

        public BattleActionResult Validate(
            UnitRuntime pActor,
            SkillDefinition pBaseSkill,
            SkillDefinition pEffectiveSkill,
            SkillTarget pTarget,
            SkillExecutionContext pContext)
        {
            return SkillTargetResolver.TryResolveTarget(pActor, pBaseSkill, pEffectiveSkill, pTarget, pContext, out ResolvedSkillTarget lResolvedTarget, out BattleActionResult lValidation)
                ? BattleActionResult.Succeeded(BattleActionType.Skill, "Skill can be used.", SkillTargetResolver.ResolveAffectedUnitIds(pActor.Id, lResolvedTarget))
                : lValidation;
        }

        public BattleActionResult Execute(
            UnitRuntime pActor,
            SkillDefinition pBaseSkill,
            SkillDefinition pEffectiveSkill,
            SkillTarget pTarget,
            SkillExecutionContext pContext)
        {
            if (!SkillTargetResolver.TryResolveTarget(pActor, pBaseSkill, pEffectiveSkill, pTarget, pContext, out ResolvedSkillTarget lResolvedTarget, out BattleActionResult lValidation))
                return lValidation;

            BattleActionResult lResult = SkillEffectResolver.ApplyEffect(pActor, pEffectiveSkill, lResolvedTarget, pContext);
            if (lResult.IsSuccess)
                pActor.RegisterSkillUse(pBaseSkill, lResolvedTarget.UsageTargetKeys);

            return lResult;
        }

        #endregion
    }
}
