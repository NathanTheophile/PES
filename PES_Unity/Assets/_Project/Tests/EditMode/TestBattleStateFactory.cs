using PES.Combat.Resolution;
using PES.Core.Random;
using PES.Core.Simulation;
using PES.Grid.Grid3D;

namespace PES.Tests.EditMode
{
    internal static class TestBattleStateFactory
    {
        public static BattleState CreateStateWithPosition(EntityId entity, Position3 position)
        {
            var state = new BattleState();
            state.SetEntityPosition(entity, position);
            return state;
        }

        public static ActionResolver CreateResolverWithSeed(int seed = 42)
        {
            return new ActionResolver(new SeededRngService(seed));
        }
    }

    internal sealed class SequenceRngService : IRngService
    {
        private readonly int[] _values;
        private int _index;

        public SequenceRngService(params int[] values)
        {
            _values = values;
            _index = 0;
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            var value = _values[_index < _values.Length ? _index : _values.Length - 1];
            _index++;
            return value;
        }
    }
}
