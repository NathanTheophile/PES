namespace PES.Core.Random
{
    public interface IRngService
    {
        int NextInt(int minInclusive, int maxExclusive);
    }
}
