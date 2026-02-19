using PES.Core.Random;

namespace PES.Core.Simulation
{
    public sealed class ActionResolver
    {
        private readonly IRngService _rngService;

        public ActionResolver(IRngService rngService)
        {
            _rngService = rngService;
        }

        public ActionResolution Resolve(BattleState state, IActionCommand command)
        {
            var result = command.Resolve(state, _rngService);
            state.AddEvent(result.Description);
            state.AdvanceTick();
            return result;
        }
    }

    public readonly struct ActionResolution
    {
        public ActionResolution(bool success, string description)
        {
            Success = success;
            Description = description;
        }

        public bool Success { get; }

        public string Description { get; }
    }
}
