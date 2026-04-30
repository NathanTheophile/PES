#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using System.Collections.Generic;
using TacticalPort.Data;

namespace TacticalPort.Core
{
    internal sealed class BattleUnitSkillUsageTracker
    {
        private readonly Dictionary<string, int> _Cooldowns = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _UsesThisTurn = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _UsesByTarget = new Dictionary<string, int>();

        public void ClearTurnUses()
        {
            _UsesThisTurn.Clear();
            _UsesByTarget.Clear();
        }

        public int GetRemainingCooldown(SkillDefinition pSkill) =>
            pSkill != null && _Cooldowns.TryGetValue(ResolveSkillKey(pSkill), out int lRemainingTurns)
                ? Math.Max(0, lRemainingTurns)
                : 0;

        public int GetSkillUsesThisTurn(SkillDefinition pSkill) =>
            pSkill != null && _UsesThisTurn.TryGetValue(ResolveSkillKey(pSkill), out int lCount)
                ? Math.Max(0, lCount)
                : 0;

        public int GetSkillUsesOnTarget(SkillDefinition pSkill, string pTargetKey) =>
            pSkill != null
            && !string.IsNullOrWhiteSpace(pTargetKey)
            && _UsesByTarget.TryGetValue(ResolveSkillTargetKey(pSkill, pTargetKey), out int lCount)
                ? Math.Max(0, lCount)
                : 0;

        public bool TryValidateUsage(SkillDefinition pSkill, IEnumerable<string> pTargetKeys, out string pFailureReason)
        {
            pFailureReason = string.Empty;

            if (pSkill == null)
            {
                pFailureReason = "Skill is missing.";
                return false;
            }

            int lRemainingCooldown = GetRemainingCooldown(pSkill);
            if (lRemainingCooldown > 0)
            {
                pFailureReason = $"{pSkill.DisplayName} is on cooldown for {lRemainingCooldown} more turn(s).";
                return false;
            }

            if (pSkill.UsePerTurn > 0 && GetSkillUsesThisTurn(pSkill) >= pSkill.UsePerTurn)
            {
                pFailureReason = $"{pSkill.DisplayName} has reached its turn usage limit.";
                return false;
            }

            if (pSkill.UsePerTarget <= 0 || pTargetKeys == null)
                return true;

            HashSet<string> lUniqueKeys = new HashSet<string>();
            foreach (string lTargetKey in pTargetKeys)
            {
                if (string.IsNullOrWhiteSpace(lTargetKey) || !lUniqueKeys.Add(lTargetKey))
                    continue;

                if (GetSkillUsesOnTarget(pSkill, lTargetKey) >= pSkill.UsePerTarget)
                {
                    pFailureReason = $"{pSkill.DisplayName} cannot affect this target anymore.";
                    return false;
                }
            }

            return true;
        }

        public void RegisterUse(SkillDefinition pSkill, IEnumerable<string> pTargetKeys)
        {
            if (pSkill == null)
                return;

            string lSkillKey = ResolveSkillKey(pSkill);
            _UsesThisTurn[lSkillKey] = GetSkillUsesThisTurn(pSkill) + 1;

            if (pSkill.CooldownTurns > 0)
                _Cooldowns[lSkillKey] = pSkill.CooldownTurns + 1;

            if (pTargetKeys == null || pSkill.UsePerTarget <= 0)
                return;

            HashSet<string> lUniqueKeys = new HashSet<string>();
            foreach (string lTargetKey in pTargetKeys)
            {
                if (string.IsNullOrWhiteSpace(lTargetKey) || !lUniqueKeys.Add(lTargetKey))
                    continue;

                string lCompositeKey = ResolveSkillTargetKey(pSkill, lTargetKey);
                _UsesByTarget[lCompositeKey] = GetSkillUsesOnTarget(pSkill, lTargetKey) + 1;
            }
        }

        public void TickCooldowns()
        {
            if (_Cooldowns.Count == 0)
                return;

            List<string> lKeys = new List<string>(_Cooldowns.Keys);
            foreach (string lKey in lKeys)
            {
                int lNextValue = Math.Max(0, _Cooldowns[lKey] - 1);
                if (lNextValue == 0)
                {
                    _Cooldowns.Remove(lKey);
                    continue;
                }

                _Cooldowns[lKey] = lNextValue;
            }
        }

        private static string ResolveSkillKey(SkillDefinition pSkill) =>
            pSkill != null && !string.IsNullOrWhiteSpace(pSkill.Id) ? pSkill.Id : string.Empty;

        private static string ResolveSkillTargetKey(SkillDefinition pSkill, string pTargetKey) => $"{ResolveSkillKey(pSkill)}::{pTargetKey}";
    }
}
