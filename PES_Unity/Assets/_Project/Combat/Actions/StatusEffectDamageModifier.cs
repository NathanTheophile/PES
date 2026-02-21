using PES.Core.Simulation;

namespace PES.Combat.Actions
{
    public static class StatusEffectDamageModifier
    {
        public static int Apply(BattleState state, EntityId attackerId, EntityId defenderId, int baseDamage)
        {
            var safeBaseDamage = baseDamage < 0 ? 0 : baseDamage;
            if (safeBaseDamage == 0)
            {
                return 0;
            }

            var weakenedPenalty = state.GetStatusEffectPotency(attackerId, StatusEffectType.Weakened);
            var fortifiedReduction = state.GetStatusEffectPotency(defenderId, StatusEffectType.Fortified);
            var markedBonus = state.GetStatusEffectPotency(defenderId, StatusEffectType.Marked);

            var adjusted = safeBaseDamage - weakenedPenalty - fortifiedReduction + markedBonus;
            return adjusted < 0 ? 0 : adjusted;
        }
    }
}
