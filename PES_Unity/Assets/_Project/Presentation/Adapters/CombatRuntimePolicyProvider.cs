using PES.Combat.Actions;
using PES.Presentation.Configuration;

namespace PES.Presentation.Adapters
{
    public readonly struct RuntimeCombatPolicies
    {
        public RuntimeCombatPolicies(MoveActionPolicy? movePolicyOverride, BasicAttackActionPolicy? basicAttackPolicyOverride, SkillActionPolicy? skillPolicyOverride)
        {
            MovePolicyOverride = movePolicyOverride;
            BasicAttackPolicyOverride = basicAttackPolicyOverride;
            SkillPolicyOverride = skillPolicyOverride;
        }

        public MoveActionPolicy? MovePolicyOverride { get; }

        public BasicAttackActionPolicy? BasicAttackPolicyOverride { get; }

        public SkillActionPolicy? SkillPolicyOverride { get; }
    }

    public static class CombatRuntimePolicyProvider
    {
        public static RuntimeCombatPolicies FromAsset(CombatRuntimeConfigAsset runtimeConfig)
        {
            if (runtimeConfig == null)
            {
                return new RuntimeCombatPolicies(null, null, null);
            }

            return new RuntimeCombatPolicies(runtimeConfig.ToMovePolicy(), runtimeConfig.ToBasicAttackPolicy(), runtimeConfig.ToSkillPolicy());
        }
    }
}
