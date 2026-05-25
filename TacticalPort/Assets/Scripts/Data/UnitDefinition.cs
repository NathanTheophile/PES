#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Data
#endregion

using System.Collections.Generic;
using TacticalPort.Shared;
using UnityEngine;
using UnityEngine.Serialization;

namespace TacticalPort.Data
{
    [CreateAssetMenu(fileName = "UnitDefinition", menuName = "Project/Data/Unit Definition")]
    public sealed class UnitDefinition : ScriptableObject
    {
        #region _____________________________/ VALUES

        [SerializeField] private string _Id = string.Empty;
        [SerializeField] private string _DisplayName = string.Empty;
        [SerializeField, TextArea] private string _Description = string.Empty;

        [SerializeField] private Team _Team = Team.Neutral;
        [SerializeField, Min(1)] private int _MaxHealth = 10;
        [SerializeField, Min(0)] private int _MoveRange = 4;
        [SerializeField, Min(0)] private int _ActionPointsPerTurn = 1;
        [SerializeField, Min(0)] private int _Initiative = 10;
        [SerializeField, Min(0), InspectorName("Melee Damage %")] private int _MeleeDamagePercent = 100;
        [SerializeField, Min(0), InspectorName("Ranged Damage %")] private int _RangedDamagePercent = 100;
        [SerializeField, Min(0)] private int _PushDamageBonus = 0;
        [SerializeField, Min(1)] private int _FootprintWidth = 1;
        [SerializeField, Min(1)] private int _FootprintHeight = 1;
        [SerializeField] private EnemyAiTargetPriority _EnemyAiTargetPriority = EnemyAiTargetPriority.WeakFirst;
        [SerializeField] private EnemyAiMovementPolicy _EnemyAiMovementPolicy = EnemyAiMovementPolicy.Auto;
        [SerializeField, Min(0)] private int _EnemyAiPreferredDistance = 0;
        [SerializeField, Min(0)] private int _EnemyAiThreatRadius = 0;
        [SerializeField] private EnemyAiProfileDefinition _EnemyAiProfile;
        [SerializeField] private List<SkillDefinition> _Skills = new List<SkillDefinition>();
        [SerializeField] private List<UnitSkillAiOverride> _SkillAiOverrides = new List<UnitSkillAiOverride>();
        [SerializeField] private List<UnitStateEntry> _BaseStates = new List<UnitStateEntry>();
        [SerializeField] private List<UnitPhaseStateDefinition> _PhaseStates = new List<UnitPhaseStateDefinition>();

        [Tooltip("Combat view prefab used to instantiate this unit in the combat scene. Keep this as a component reference to avoid a Data -> View assembly dependency.")]
        [FormerlySerializedAs("_UnitViewPrefab")]
        [SerializeField] private MonoBehaviour _CombatViewPrefab;
        [Tooltip("Optional 3D model prefab spawned by the combat view. This keeps the character asset centralized without making Data depend on presentation scripts.")]
        [SerializeField] private GameObject _ModelPrefab;
        [Tooltip("2D image used by menus, team selection and compact combat UI.")]
        [SerializeField] private Sprite _PreviewSprite;
        [Tooltip("Larger portrait image used by character details and timeline UI when available.")]
        [SerializeField] private Sprite _Portrait;
        [SerializeField] private Color _Tint = Color.white;

        #endregion

        #region _____________________________/ ACCESSORS

        public string Id => string.IsNullOrWhiteSpace(_Id) ? name : _Id;
        public string DisplayName => string.IsNullOrWhiteSpace(_DisplayName) ? name : _DisplayName;
        public string Description => _Description;
        public Team Team => _Team;
        public int MaxHealth => Mathf.Max(1, _MaxHealth);
        public int MoveRange => Mathf.Max(0, _MoveRange);
        public int ActionPointsPerTurn => Mathf.Max(0, _ActionPointsPerTurn);
        public int Initiative => Mathf.Max(0, _Initiative);
        public int MeleeDamagePercent => Mathf.Max(0, _MeleeDamagePercent);
        public int RangedDamagePercent => Mathf.Max(0, _RangedDamagePercent);
        public int PushDamageBonus => Mathf.Max(0, _PushDamageBonus);
        public int FootprintWidth => Mathf.Max(1, _FootprintWidth);
        public int FootprintHeight => Mathf.Max(1, _FootprintHeight);
        public bool IsBig => FootprintWidth > 1 || FootprintHeight > 1;
        public EnemyAiTargetPriority EnemyAiTargetPriority => _EnemyAiProfile != null ? _EnemyAiProfile.TargetPriority : _EnemyAiTargetPriority;
        public EnemyAiMovementPolicy EnemyAiMovementPolicy => _EnemyAiProfile != null ? _EnemyAiProfile.MovementPolicy : _EnemyAiMovementPolicy;
        public int EnemyAiPreferredDistance => _EnemyAiProfile != null ? _EnemyAiProfile.PreferredDistance : Mathf.Max(0, _EnemyAiPreferredDistance);
        public int EnemyAiThreatRadius => _EnemyAiProfile != null ? _EnemyAiProfile.ThreatRadius : Mathf.Max(0, _EnemyAiThreatRadius);
        public bool EnemyAiKiteAfterSuccessfulAction => _EnemyAiProfile != null && _EnemyAiProfile.KiteAfterSuccessfulAction;
        public int EnemyAiDamageWeight => _EnemyAiProfile != null ? _EnemyAiProfile.DamageWeight : 100;
        public int EnemyAiHealWeight => _EnemyAiProfile != null ? _EnemyAiProfile.HealWeight : 100;
        public int EnemyAiKillConfirmWeight => _EnemyAiProfile != null ? _EnemyAiProfile.KillConfirmWeight : 100;
        public int EnemyAiAoeWeight => _EnemyAiProfile != null ? _EnemyAiProfile.AoeWeight : 100;
        public int EnemyAiSafetyWeight => _EnemyAiProfile != null ? _EnemyAiProfile.SafetyWeight : 100;
        public bool EnemyAiDebugDecisions => _EnemyAiProfile != null && _EnemyAiProfile.DebugDecisions;
        public EnemyAiProfileDefinition EnemyAiProfile => _EnemyAiProfile;
        public IReadOnlyList<SkillDefinition> Skills => _Skills;
        public IReadOnlyList<UnitSkillAiOverride> SkillAiOverrides => _SkillAiOverrides;
        public IReadOnlyList<UnitStateEntry> BaseStates => _BaseStates;
        public IReadOnlyList<UnitPhaseStateDefinition> PhaseStates => _PhaseStates;
        public MonoBehaviour CombatViewPrefabComponent => _CombatViewPrefab;
        public MonoBehaviour UnitViewPrefabComponent => _CombatViewPrefab;
        public GameObject ModelPrefab => _ModelPrefab;
        public Sprite PreviewSprite => _PreviewSprite;
        public Sprite Portrait => _Portrait;
        public Sprite DisplaySprite => _PreviewSprite != null ? _PreviewSprite : _Portrait;
        public Color Tint => _Tint;

        #endregion

        #region _____________________________| FACTORIES

        public static UnitDefinition CreateRuntimeClone(UnitDefinition pSource, Team? pTeamOverride = null)
        {
            if (pSource == null)
                return null;

            UnitDefinition lDefinition = CreateInstance<UnitDefinition>();
            lDefinition.hideFlags = HideFlags.DontSave;
            lDefinition.name = $"{pSource.DisplayName}_Runtime";
            lDefinition._Id = pSource.Id;
            lDefinition._DisplayName = pSource.DisplayName;
            lDefinition._Description = pSource.Description;
            lDefinition._Team = pTeamOverride ?? pSource.Team;
            lDefinition._MaxHealth = pSource.MaxHealth;
            lDefinition._MoveRange = pSource.MoveRange;
            lDefinition._ActionPointsPerTurn = pSource.ActionPointsPerTurn;
            lDefinition._Initiative = pSource.Initiative;
            lDefinition._MeleeDamagePercent = pSource.MeleeDamagePercent;
            lDefinition._RangedDamagePercent = pSource.RangedDamagePercent;
            lDefinition._PushDamageBonus = pSource.PushDamageBonus;
            lDefinition._FootprintWidth = pSource.FootprintWidth;
            lDefinition._FootprintHeight = pSource.FootprintHeight;
            lDefinition._EnemyAiTargetPriority = pSource.EnemyAiTargetPriority;
            lDefinition._EnemyAiMovementPolicy = pSource.EnemyAiMovementPolicy;
            lDefinition._EnemyAiPreferredDistance = pSource.EnemyAiPreferredDistance;
            lDefinition._EnemyAiThreatRadius = pSource.EnemyAiThreatRadius;
            lDefinition._EnemyAiProfile = pSource.EnemyAiProfile;
            lDefinition._Skills = CopyList(pSource._Skills);
            lDefinition._SkillAiOverrides = CopyList(pSource._SkillAiOverrides);
            lDefinition._BaseStates = CopyList(pSource._BaseStates);
            lDefinition._PhaseStates = CopyList(pSource._PhaseStates);
            lDefinition._CombatViewPrefab = pSource.CombatViewPrefabComponent;
            lDefinition._ModelPrefab = pSource.ModelPrefab;
            lDefinition._PreviewSprite = pSource.PreviewSprite;
            lDefinition._Portrait = pSource.Portrait;
            lDefinition._Tint = pSource.Tint;
            return lDefinition;
        }

        private static List<T> CopyList<T>(List<T> pSource)
        {
            List<T> lResult = new List<T>();
            if (pSource == null)
                return lResult;

            for (int lIndex = 0; lIndex < pSource.Count; lIndex++)
                lResult.Add(pSource[lIndex]);

            return lResult;
        }

        #endregion
    }
}
