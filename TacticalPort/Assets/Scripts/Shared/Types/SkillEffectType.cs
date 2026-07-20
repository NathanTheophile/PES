#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
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
        CreateGlyph = 5,
        Pull = 6,
        AdvanceActivePassiveProgression = 7,
        RepairAndToggleStates = 8,
        ReduceStateDurations = 9
    }

    public enum SkillCategory
    {
        None = 0,
        MeleeAtk = 1,
        RangedAtk = 2,
        Utility = 3,
        Heal = 4
    }

    public enum DamageRangeType
    {
        None = 0,
        Melee = 1,
        Ranged = 2
    }

}
