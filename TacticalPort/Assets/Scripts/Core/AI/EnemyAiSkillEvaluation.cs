#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core
{
    internal readonly struct EnemyAiSkillEvaluation
    {
        public EnemyAiSkillEvaluation(
            SkillDefinition pSkill,
            SkillTarget pTarget,
            UnitSkillAiOverride pRule,
            int pScore,
            string pReason)
        {
            Skill = pSkill;
            Target = pTarget;
            Rule = pRule;
            Score = pScore;
            Reason = pReason ?? string.Empty;
        }

        public SkillDefinition Skill { get; }
        public SkillTarget Target { get; }
        public UnitSkillAiOverride Rule { get; }
        public int Score { get; }
        public string Reason { get; }
        public bool IsValid => Skill != null && Target != null && Score > 0;

        public EnemyAiAction ToAction() =>
            EnemyAiAction.UseSkill(
                Skill,
                Target,
                string.IsNullOrWhiteSpace(Reason) ? $"AI used {Skill.DisplayName}." : Reason,
                Rule != null && Rule.KiteAfterUse);
    }

    internal static class EnemyAiSkillRuleUtility
    {
        public static List<UnitSkillAiOverride> BuildOrderedRules(UnitRuntime pActor, ISet<SkillDefinition> pRuledSkills)
        {
            List<UnitSkillAiOverride> lOrderedRules = new List<UnitSkillAiOverride>();
            IReadOnlyList<UnitSkillAiOverride> lRules = pActor?.Definition?.SkillAiOverrides;
            if (lRules == null)
                return lOrderedRules;

            for (int lIndex = 0; lIndex < lRules.Count; lIndex++)
            {
                UnitSkillAiOverride lRule = lRules[lIndex];
                if (lRule?.Skill == null || !ContainsSkill(pActor, lRule.Skill))
                    continue;

                lOrderedRules.Add(lRule);
                pRuledSkills?.Add(lRule.Skill);
            }

            lOrderedRules.Sort((pLeft, pRight) => pRight.Priority.CompareTo(pLeft.Priority));
            return lOrderedRules;
        }

        public static bool CanUseRule(UnitRuntime pActor, UnitSkillAiOverride pRule)
        {
            if (pRule == null || pRule.MaxCasterHealthPercent <= 0)
                return true;

            int lCasterHealthPercent = pActor.CurrentHealth * 100 / pActor.Definition.MaxHealth;
            return lCasterHealthPercent <= pRule.MaxCasterHealthPercent;
        }

        private static bool ContainsSkill(UnitRuntime pActor, SkillDefinition pSkill)
        {
            if (pActor?.Skills == null || pSkill == null)
                return false;

            for (int lIndex = 0; lIndex < pActor.Skills.Count; lIndex++)
            {
                if (pActor.Skills[lIndex] == pSkill)
                    return true;
            }

            return false;
        }
    }
}
