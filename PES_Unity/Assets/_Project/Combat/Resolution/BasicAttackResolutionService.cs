using PES.Core.Random;

namespace PES.Combat.Resolution
{
    public readonly struct BasicAttackResolutionPolicy
    {
        public BasicAttackResolutionPolicy(int baseDamage, int baseHitChance)
        {
            BaseDamage = baseDamage;
            BaseHitChance = baseHitChance;
        }

        public int BaseDamage { get; }

        public int BaseHitChance { get; }
    }

    public readonly struct BasicAttackResolutionResult
    {
        public BasicAttackResolutionResult(bool hit, int roll, int hitChance, int finalDamage, int heightDamageBonus)
        {
            Hit = hit;
            Roll = roll;
            HitChance = hitChance;
            FinalDamage = finalDamage;
            HeightDamageBonus = heightDamageBonus;
        }

        public bool Hit { get; }
        public int Roll { get; }
        public int HitChance { get; }
        public int FinalDamage { get; }
        public int HeightDamageBonus { get; }
    }

    public sealed class BasicAttackResolutionService
    {
        public BasicAttackResolutionResult Resolve(IRngService rngService, int verticalDelta, BasicAttackResolutionPolicy policy)
        {
            var cappedDelta = Clamp(verticalDelta, -2, 2);
            var hitChance = policy.BaseHitChance + (cappedDelta * 5);
            hitChance = Clamp(hitChance, 5, 95);

            var roll = rngService.NextInt(0, 100);
            var hit = roll < hitChance;
            if (!hit)
            {
                return new BasicAttackResolutionResult(false, roll, hitChance, 0, 0);
            }

            var heightDamageBonus = cappedDelta * 2;
            var variance = rngService.NextInt(0, 4);
            var finalDamage = policy.BaseDamage + heightDamageBonus + variance;
            finalDamage = finalDamage < 0 ? 0 : finalDamage;

            return new BasicAttackResolutionResult(true, roll, hitChance, finalDamage, heightDamageBonus);
        }

        private static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            return value > max ? max : value;
        }
    }
}
