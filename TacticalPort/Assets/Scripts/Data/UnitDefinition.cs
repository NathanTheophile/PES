#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Data
#endregion

using System.Collections.Generic;
using Sirenix.OdinInspector;
using TacticalPort.Shared;
using UnityEngine;
using UnityEngine.Serialization;

namespace TacticalPort.Data
{
    [CreateAssetMenu(fileName = "UnitDefinition", menuName = "Project/Data/Unit Definition")]
    public sealed class UnitDefinition : ScriptableObject
    {
        #region _____________________________/ VALUES

        [TabGroup("Metadata")]
        [LabelText("Id")]
        [SerializeField] private string _Id = string.Empty;

        [TabGroup("Metadata")]
        [LabelText("Display Name")]
        [SerializeField] private string _DisplayName = string.Empty;

        [TabGroup("Metadata")]
        [LabelText("Description")]
        [SerializeField, TextArea] private string _Description = string.Empty;

        [TabGroup("Stats")]
        [LabelText("Team")]
        [SerializeField] private Team _Team = Team.Neutral;

        [TabGroup("Stats")]
        [LabelText("Max Health")]
        [SerializeField, Min(1)] private int _MaxHealth = 10;

        [TabGroup("Stats")]
        [LabelText("Move Range")]
        [SerializeField, Min(0)] private int _MoveRange = 4;

        [TabGroup("Stats")]
        [LabelText("AP/Turn")]
        [SerializeField, Min(0)] private int _ActionPointsPerTurn = 1;

        [TabGroup("Stats")]
        [LabelText("Initiative")]
        [SerializeField, Min(0)] private int _Initiative = 10;

        [TabGroup("Stats")]
        [LabelText("Melee Damage %")]
        [SerializeField, Min(0)] private int _MeleeDamagePercent = 100;

        [TabGroup("Stats")]
        [LabelText("Ranged Damage %")]
        [SerializeField, Min(0)] private int _RangedDamagePercent = 100;

        [TabGroup("Stats")]
        [LabelText("Push Damage Bonus")]
        [SerializeField, Min(0)] private int _PushDamageBonus = 0;

        [TabGroup("Stats")]
        [LabelText("Footprint Width")]
        [SerializeField, Min(1)] private int _FootprintWidth = 1;

        [TabGroup("Stats")]
        [LabelText("Footprint Height")]
        [SerializeField, Min(1)] private int _FootprintHeight = 1;

        [TabGroup("AI")]
        [LabelText("Target Priority")]
        [HideIf(nameof(UsesEnemyAiProfile))]
        [SerializeField] private EnemyAiTargetPriority _EnemyAiTargetPriority = EnemyAiTargetPriority.WeakFirst;

        [TabGroup("AI")]
        [LabelText("Movement Policy")]
        [HideIf(nameof(UsesEnemyAiProfile))]
        [SerializeField] private EnemyAiMovementPolicy _EnemyAiMovementPolicy = EnemyAiMovementPolicy.Auto;

        [TabGroup("AI")]
        [LabelText("Preferred Distance")]
        [HideIf(nameof(UsesEnemyAiProfile))]
        [SerializeField, Min(0)] private int _EnemyAiPreferredDistance = 0;

        [TabGroup("AI")]
        [LabelText("Threat Radius")]
        [HideIf(nameof(UsesEnemyAiProfile))]
        [SerializeField, Min(0)] private int _EnemyAiThreatRadius = 0;

        [TabGroup("AI")]
        [LabelText("Enemy AI Profile")]
        [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        [SerializeField] private EnemyAiProfileDefinition _EnemyAiProfile;

        [TabGroup("Skills")]
        [ListDrawerSettings(Expanded = true, DraggableItems = true, ShowIndexLabels = true)]
        [ValidateInput(nameof(HasValidSkillList), "Skills cannot contain null entries or duplicates.", InfoMessageType.Warning)]
        [SerializeField] private List<SkillDefinition> _Skills = new List<SkillDefinition>();

        [TabGroup("AI")]
        [ListDrawerSettings(Expanded = true, DraggableItems = false, ShowIndexLabels = true)]
        [SerializeField] private List<UnitSkillAiOverride> _SkillAiOverrides = new List<UnitSkillAiOverride>();

        [TabGroup("States")]
        [LabelText("Base States")]
        [ListDrawerSettings(Expanded = true, DraggableItems = true, ShowIndexLabels = true)]
        [SerializeField] private List<UnitStateEntry> _BaseStates = new List<UnitStateEntry>();

        [TabGroup("States")]
        [LabelText("Phase States")]
        [ListDrawerSettings(Expanded = true, DraggableItems = true, ShowIndexLabels = true)]
        [SerializeField] private List<UnitPhaseStateDefinition> _PhaseStates = new List<UnitPhaseStateDefinition>();

        [Tooltip("Combat view prefab used to instantiate this unit in the combat scene. Keep this as a component reference to avoid a Data -> View assembly dependency.")]
        [FormerlySerializedAs("_UnitViewPrefab")]
        [TabGroup("Metadata")]
        [LabelText("Combat View Prefab")]
        [SerializeField] private MonoBehaviour _CombatViewPrefab;
        [Tooltip("Optional 3D model prefab spawned by the combat view. This keeps the character asset centralized without making Data depend on presentation scripts.")]
        [TabGroup("Metadata")]
        [LabelText("Model Prefab")]
        [AssetsOnly]
        [SerializeField] private GameObject _ModelPrefab;
        [Tooltip("2D image used by menus, team selection and compact combat UI.")]
        [TabGroup("Metadata")]
        [LabelText("Preview Sprite")]
        [PreviewField(64)]
        [AssetsOnly]
        [SerializeField] private Sprite _PreviewSprite;
        [Tooltip("Larger portrait image used by character details and timeline UI when available.")]
        [TabGroup("Metadata")]
        [LabelText("Portrait")]
        [PreviewField(64)]
        [AssetsOnly]
        [SerializeField] private Sprite _Portrait;
        [TabGroup("Metadata")]
        [LabelText("Tint")]
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

        #region _____________________________| ODIN

        [TabGroup("AI")]
        [Button("Sync AI Overrides From Skills")]
        [InfoBox("Overrides are synced from the Skills tab, so the skill kit is not entered twice.", InfoMessageType.Info)]
        private void SyncAiOverridesFromSkills()
        {
            if (_SkillAiOverrides == null)
                _SkillAiOverrides = new List<UnitSkillAiOverride>();

            for (int lIndex = _SkillAiOverrides.Count - 1; lIndex >= 0; lIndex--)
            {
                UnitSkillAiOverride lOverride = _SkillAiOverrides[lIndex];
                if (lOverride == null || lOverride.Skill == null || !ContainsSkill(_Skills, lOverride.Skill))
                    _SkillAiOverrides.RemoveAt(lIndex);
            }

            if (_Skills == null)
                return;

            for (int lIndex = 0; lIndex < _Skills.Count; lIndex++)
            {
                SkillDefinition lSkill = _Skills[lIndex];
                if (lSkill != null && !ContainsOverride(lSkill))
                    _SkillAiOverrides.Add(new UnitSkillAiOverride(lSkill));
            }
        }

        private bool UsesEnemyAiProfile() => _EnemyAiProfile != null;

        private bool HasValidSkillList()
        {
            if (_Skills == null)
                return true;

            for (int lIndex = 0; lIndex < _Skills.Count; lIndex++)
            {
                SkillDefinition lSkill = _Skills[lIndex];
                if (lSkill == null)
                    return false;

                for (int lOtherIndex = lIndex + 1; lOtherIndex < _Skills.Count; lOtherIndex++)
                {
                    if (_Skills[lOtherIndex] == lSkill)
                        return false;
                }
            }

            return true;
        }

        private bool ContainsOverride(SkillDefinition pSkill)
        {
            if (_SkillAiOverrides == null || pSkill == null)
                return false;

            for (int lIndex = 0; lIndex < _SkillAiOverrides.Count; lIndex++)
            {
                if (_SkillAiOverrides[lIndex]?.Skill == pSkill)
                    return true;
            }

            return false;
        }

        private static bool ContainsSkill(IReadOnlyList<SkillDefinition> pSkills, SkillDefinition pSkill)
        {
            if (pSkills == null || pSkill == null)
                return false;

            for (int lIndex = 0; lIndex < pSkills.Count; lIndex++)
            {
                if (pSkills[lIndex] == pSkill)
                    return true;
            }

            return false;
        }

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
