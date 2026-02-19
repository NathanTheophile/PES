namespace PES.Core.Simulation
{
    public readonly struct EntityId
    {
        public EntityId(int value)
        {
            Value = value;
        }

        public int Value { get; }

        public override string ToString()
        {
            return $"Entity({Value})";
        }
    }
}
