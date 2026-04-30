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

        public BattleActionResult Validate(UnitRuntime pActor, SkillDefinition pSkill, SkillTarget pTarget, SkillExecutionContext pContext)
        {
            return SkillTargetResolver.TryResolveTarget(pActor, pSkill, pTarget, pContext, out ResolvedSkillTarget lResolvedTarget, out BattleActionResult lValidation)
                ? BattleActionResult.Succeeded(BattleActionType.Skill, "Skill can be used.", SkillTargetResolver.ResolveAffectedUnitIds(pActor.Id, lResolvedTarget))
                : lValidation;
        }

        public BattleActionResult Execute(UnitRuntime pActor, SkillDefinition pSkill, SkillTarget pTarget, SkillExecutionContext pContext)
        {
            if (!SkillTargetResolver.TryResolveTarget(pActor, pSkill, pTarget, pContext, out ResolvedSkillTarget lResolvedTarget, out BattleActionResult lValidation))
                return lValidation;

            if (pActor.Position != lResolvedTarget.TargetCell)
                pActor.FaceTowards(lResolvedTarget.TargetCell);

            BattleActionResult lResult = SkillEffectResolver.ApplyEffect(pActor, pSkill, lResolvedTarget, pContext);
            if (lResult.IsSuccess)
                pActor.RegisterSkillUse(pSkill, lResolvedTarget.UsageTargetKeys);

            return lResult;
        }

        #endregion
    }
}
