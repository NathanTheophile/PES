// Utilité : ce script résout de manière déterministe l'effet d'une compétence
// (jet de précision + dégâts finals) en consommant le RNG centralisé.
using PES.Core.Random;

namespace PES.Combat.Resolution
{
    public sealed class SkillResolutionService
    {
        public SkillResolutionResult Resolve(IRngService rngService, int baseDamage, int baseHitChance)
        {
            var clampedHitChance = Clamp(baseHitChance, 0, 100);
            var roll = rngService.NextInt(1, 101);
            var hit = roll <= clampedHitChance;

            if (!hit)
            {
                return new SkillResolutionResult(false, roll, clampedHitChance, 0);
            }

            var variance = rngService.NextInt(0, 4); // 0..3 déterministe selon seed
            var finalDamage = baseDamage + variance;
            return new SkillResolutionResult(true, roll, clampedHitChance, finalDamage);
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

    public readonly struct SkillResolutionResult
    {
        public SkillResolutionResult(bool hit, int roll, int hitChance, int finalDamage)
        {
            Hit = hit;
            Roll = roll;
            HitChance = hitChance;
            FinalDamage = finalDamage;
        }

        public bool Hit { get; }

        public int Roll { get; }

        public int HitChance { get; }

        public int FinalDamage { get; }
    }
}
