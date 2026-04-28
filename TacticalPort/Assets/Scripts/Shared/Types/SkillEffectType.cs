#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Shared
#endregion

namespace TacticalPort.Shared
{
    public enum SkillPrimaryEffectType
    {
        None = 0,
        Damage = 1,
        Heal = 2
    }

    public enum SkillAdditionalEffectType
    {
        None = 0,
        Push = 1,
        Teleport = 2,
        SwitchPositions = 3,
        Summon = 4,
        CreateGlyph = 5
    }

    public enum SkillEffectType
    {
        Damage = 0,
        Heal = 1,
        Push = 2,
        Teleport = 3,
        SwitchPositions = 4,
        Summon = 5,
        CreateGlyph = 6
    }
}
