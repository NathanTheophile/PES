using PES.Core.Random;

namespace PES.Core.Simulation
{
    public interface IActionCommand
    {
        ActionResolution Resolve(BattleState state, IRngService rngService);
    }
}
