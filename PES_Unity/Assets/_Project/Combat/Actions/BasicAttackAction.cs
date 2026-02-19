using PES.Core.Random;
using PES.Core.Simulation;

namespace PES.Combat.Actions
{
    public readonly struct BasicAttackAction : IActionCommand
    {
        public BasicAttackAction(EntityId attackerId, EntityId targetId)
        {
            AttackerId = attackerId;
            TargetId = targetId;
        }

        public EntityId AttackerId { get; }
        public EntityId TargetId { get; }

        public ActionResolution Resolve(BattleState state, IRngService rngService)
        {
            var roll = rngService.NextInt(0, 100);
            var hit = roll >= 25;
            var summary = hit ? "hit" : "miss";
            return new ActionResolution(hit, $"BasicAttackAction: {AttackerId} -> {TargetId} [{summary}:{roll}]");
        }
    }
}
