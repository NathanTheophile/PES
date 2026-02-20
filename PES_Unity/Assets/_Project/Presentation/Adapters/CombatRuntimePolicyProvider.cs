using PES.Combat.Actions;
using PES.Presentation.Configuration;

namespace PES.Presentation.Adapters
{
    public readonly struct RuntimeCombatPolicies
    {
        public RuntimeCombatPolicies(MoveActionPolicy? movePolicyOverride, BasicAttackActionPolicy? basicAttackPolicyOverride)
        {
            MovePolicyOverride = movePolicyOverride;
            BasicAttackPolicyOverride = basicAttackPolicyOverride;
        }

        public MoveActionPolicy? MovePolicyOverride { get; }

        public BasicAttackActionPolicy? BasicAttackPolicyOverride { get; }
    }

    public static class CombatRuntimePolicyProvider
    {
        public static RuntimeCombatPolicies FromAsset(CombatRuntimeConfigAsset runtimeConfig)
        {
            if (runtimeConfig == null)
            {
                return new RuntimeCombatPolicies(null, null);
            }

            return new RuntimeCombatPolicies(runtimeConfig.ToMovePolicy(), runtimeConfig.ToBasicAttackPolicy());
        }
    }
}
