#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Data
#endregion

using System;
using UnityEngine;

namespace TacticalPort.Data
{
    public enum EnemyAiTargetTeam
    {
        Auto = 0,
        Enemy = 1,
        Ally = 2,
        Self = 3
    }

    public enum EnemyAiTargetMode
    {
        BestScore = 0,
        WeakFirst = 1,
        StrongFirst = 2,
        CloseFirst = 3,
        MostMissingHealth = 4
    }

    [Serializable]
    public sealed class UnitSkillAiOverride
    {
        #region _____________________________/ VALUES

        [SerializeField] private SkillDefinition _Skill;
        [SerializeField] private int _Priority = 0;
        [SerializeField] private EnemyAiTargetTeam _TargetTeam = EnemyAiTargetTeam.Auto;
        [SerializeField] private EnemyAiTargetMode _TargetMode = EnemyAiTargetMode.BestScore;
        [SerializeField] private bool _AllowSelfCast;
        [SerializeField] private bool _PreferSelfCastWhenValid;
        [SerializeField, Range(0, 100)] private int _MaxCasterHealthPercent = 0;
        [SerializeField, Min(0)] private int _MinTargetMissingHealth = 0;
        [SerializeField] private bool _KiteAfterUse;

        #endregion

        #region _____________________________/ ACCESSORS

        public SkillDefinition Skill => _Skill;
        public int Priority => _Priority;
        public EnemyAiTargetTeam TargetTeam => _TargetTeam;
        public EnemyAiTargetMode TargetMode => _TargetMode;
        public bool AllowSelfCast => _AllowSelfCast;
        public bool PreferSelfCastWhenValid => _PreferSelfCastWhenValid;
        public int MaxCasterHealthPercent => Mathf.Clamp(_MaxCasterHealthPercent, 0, 100);
        public int MinTargetMissingHealth => Mathf.Max(0, _MinTargetMissingHealth);
        public bool KiteAfterUse => _KiteAfterUse;

        #endregion
    }
}
