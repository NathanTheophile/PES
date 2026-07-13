#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : Resolved runtime references for one unit build.
//  Data
#endregion

using System.Collections.Generic;

namespace TacticalPort.Data
{
    public sealed class UnitCombatLoadout
    {
        private readonly List<SkillDefinition> _Skills = new List<SkillDefinition>();

        public PassiveDefinition Passive { get; }
        public IReadOnlyList<SkillDefinition> Skills => _Skills;
        public UnitStatModifiers StatModifiers { get; }

        public UnitCombatLoadout(PassiveDefinition pPassive, IReadOnlyList<SkillDefinition> pSkills)
            : this(pPassive, pSkills, null)
        {
        }

        public UnitCombatLoadout(
            PassiveDefinition pPassive,
            IReadOnlyList<SkillDefinition> pSkills,
            UnitStatModifiers pStatModifiers)
        {
            Passive = pPassive;
            StatModifiers = pStatModifiers ?? UnitStatModifiers.None;
            if (pSkills == null)
                return;

            for (int lIndex = 0; lIndex < pSkills.Count; lIndex++)
            {
                SkillDefinition lSkill = pSkills[lIndex];
                if (lSkill != null && !_Skills.Contains(lSkill))
                    _Skills.Add(lSkill);
            }
        }
    }
}
