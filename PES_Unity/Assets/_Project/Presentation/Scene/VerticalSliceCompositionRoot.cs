namespace PES.Presentation.Scene
{
    public sealed class VerticalSliceCompositionRoot
    {
        public VerticalSliceComposition Compose(VerticalSliceBattleSetup setup)
        {
            var battleLoop = new VerticalSliceBattleLoop(
                movePolicyOverride: setup.EffectiveMovePolicy,
                basicAttackPolicyOverride: setup.BasicAttackPolicyOverride,
                actorDefinitions: setup.ActorDefinitions);

            setup.ApplyRuntimeResources(battleLoop.State);

            var planner = new VerticalSliceCommandPlanner(
                battleLoop.State,
                setup.EffectiveMovePolicy,
                setup.BasicAttackPolicyOverride,
                setup.SkillPolicyOverride,
                setup.SkillLoadoutMap);

            return new VerticalSliceComposition(battleLoop, planner);
        }
    }

    public readonly struct VerticalSliceComposition
    {
        public VerticalSliceComposition(VerticalSliceBattleLoop battleLoop, VerticalSliceCommandPlanner planner)
        {
            BattleLoop = battleLoop;
            Planner = planner;
        }

        public VerticalSliceBattleLoop BattleLoop { get; }

        public VerticalSliceCommandPlanner Planner { get; }
    }
}
