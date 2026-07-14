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
        [LabelText("Mobility / Turn")]
        [SerializeField, Min(0)] private int _MobilityPerTurn = 4;

        [TabGroup("Stats")]
        [LabelText("Energy / Turn")]
        [SerializeField, Min(0)] private int _EnergyPerTurn = 1;

        [TabGroup("Stats")]
        [LabelText("Velocity")]
        [SerializeField, Min(0)] private int _Velocity = 10;

        [TabGroup("Stats")]
        [LabelText("Melee Damage %")]
        [SerializeField, Min(0)] private int _MeleeDamagePercent = 100;

        [TabGroup("Stats")]
        [LabelText("Ranged Damage %")]
        [SerializeField, Min(0)] private int _RangedDamagePercent = 100;

        [TabGroup("Stats")]
        [LabelText("General Damage %")]
        [Tooltip("Added to the melee or ranged damage percentage. Zero is neutral.")]
        [SerializeField, Min(0)] private int _GeneralDamagePercent = 0;

        [TabGroup("Stats")]
        [LabelText("Melee Resistance %")]
        [SerializeField, Range(0, 100)] private int _MeleeResistancePercent = 0;

        [TabGroup("Stats")]
        [LabelText("Ranged Resistance %")]
        [SerializeField, Range(0, 100)] private int _RangedResistancePercent = 0;

        [TabGroup("Stats")]
        [LabelText("General Resistance %")]
        [Tooltip("Added to the matching melee or ranged resistance percentage. Zero is neutral.")]
        [SerializeField, Range(0, 100)] private int _GeneralResistancePercent = 0;

        [TabGroup("Stats")]
        [LabelText("Stat Point Budget")]
        [Tooltip("Capital available in deckbuilding for this unit's stat allocations.")]
        [SerializeField, Min(0)] private int _StatPointBudget = 120;

        [TabGroup("Stats")]
        [LabelText("Push Damage Bonus")]
        [SerializeField, Min(0)] private int _PushDamageBonus = 0;

        [TabGroup("Stats")]
        [LabelText("Footprint Width")]
        [SerializeField, Min(1)] private int _FootprintWidth = 1;

        [TabGroup("Stats")]
        [LabelText("Footprint Height")]
        [SerializeField, Min(1)] private int _FootprintHeight = 1;

        [TabGroup("Stats")]
        [LabelText("Participates In Turn Order")]
        [SerializeField] private bool _ParticipatesInTurnOrder = true;

        [TabGroup("Stats")]
        [LabelText("Counts For Victory")]
        [SerializeField] private bool _CountsForVictory = true;

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
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ShowIndexLabels = true)]
        [ValidateInput(nameof(HasValidSkillList), "Skills cannot contain null entries or duplicates.", InfoMessageType.Warning)]
        [SerializeField] private List<SkillDefinition> _Skills = new List<SkillDefinition>();

        [TabGroup("Passives")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ShowIndexLabels = true)]
        [ValidateInput(nameof(HasValidPassiveList), "Passives cannot contain null entries or duplicates.", InfoMessageType.Warning)]
        [SerializeField] private List<PassiveDefinition> _Passives = new List<PassiveDefinition>();

        [TabGroup("Passives")]
        [LabelText("Default Passive")]
        [SerializeField] private PassiveDefinition _DefaultPassive;

        [TabGroup("AI")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = false, ShowIndexLabels = true)]
        [SerializeField] private List<UnitSkillAiOverride> _SkillAiOverrides = new List<UnitSkillAiOverride>();

        [TabGroup("States")]
        [LabelText("Base States")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ShowIndexLabels = true)]
        [SerializeField] private List<UnitStateEntry> _BaseStates = new List<UnitStateEntry>();

        [TabGroup("States")]
        [LabelText("Phase States")]
        [ListDrawerSettings(ShowFoldout = true, DraggableItems = true, ShowIndexLabels = true)]
        [SerializeField] private List<UnitPhaseStateDefinition> _PhaseStates = new List<UnitPhaseStateDefinition>();

        [Tooltip("Combat view prefab used to instantiate this unit in the combat scene. Keep this as a component reference to avoid a Data -> View assembly dependency.")]
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
        public int MobilityPerTurn => Mathf.Max(0, _MobilityPerTurn);
        public int EnergyPerTurn => Mathf.Max(0, _EnergyPerTurn);
        public int Velocity => Mathf.Max(0, _Velocity);
        public int MeleeDamagePercent => Mathf.Max(0, _MeleeDamagePercent);
        public int RangedDamagePercent => Mathf.Max(0, _RangedDamagePercent);
        public int GeneralDamagePercent => Mathf.Max(0, _GeneralDamagePercent);
        public int MeleeResistancePercent => Mathf.Clamp(_MeleeResistancePercent, 0, 100);
        public int RangedResistancePercent => Mathf.Clamp(_RangedResistancePercent, 0, 100);
        public int GeneralResistancePercent => Mathf.Clamp(_GeneralResistancePercent, 0, 100);
        public int StatPointBudget => Mathf.Max(0, _StatPointBudget);
        public int PushDamageBonus => Mathf.Max(0, _PushDamageBonus);
        public int FootprintWidth => Mathf.Max(1, _FootprintWidth);
        public int FootprintHeight => Mathf.Max(1, _FootprintHeight);
        public bool ParticipatesInTurnOrder => _ParticipatesInTurnOrder;
        public bool CountsForVictory => _CountsForVictory;
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
        public IReadOnlyList<PassiveDefinition> Passives => _Passives;
        public PassiveDefinition DefaultPassive => _DefaultPassive != null ? _DefaultPassive : ResolveFirstPassive();
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

        private bool HasValidPassiveList()
        {
            if (_Passives == null)
                return true;

            for (int lIndex = 0; lIndex < _Passives.Count; lIndex++)
            {
                PassiveDefinition lPassive = _Passives[lIndex];
                if (lPassive == null)
                    return false;

                for (int lOtherIndex = lIndex + 1; lOtherIndex < _Passives.Count; lOtherIndex++)
                {
                    if (_Passives[lOtherIndex] == lPassive)
                        return false;
                }
            }

            return true;
        }

        private PassiveDefinition ResolveFirstPassive()
        {
            if (_Passives == null)
                return null;

            for (int lIndex = 0; lIndex < _Passives.Count; lIndex++)
            {
                if (_Passives[lIndex] != null)
                    return _Passives[lIndex];
            }

            return null;
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
            return CreateRuntimeClone(pSource, pTeamOverride, null);
        }

        public static UnitDefinition CreateRuntimeClone(
            UnitDefinition pSource,
            Team? pTeamOverride,
            UnitCombatLoadout pLoadout)
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
            UnitStatModifiers lStats = pLoadout?.StatModifiers ?? UnitStatModifiers.None;
            lDefinition._MaxHealth = pSource.MaxHealth + lStats.Health;
            lDefinition._MobilityPerTurn = pSource.MobilityPerTurn + lStats.Mobility;
            lDefinition._EnergyPerTurn = pSource.EnergyPerTurn + lStats.Energy;
            lDefinition._Velocity = pSource.Velocity + lStats.Velocity;
            lDefinition._MeleeDamagePercent = pSource.MeleeDamagePercent + lStats.MeleeDamage;
            lDefinition._RangedDamagePercent = pSource.RangedDamagePercent + lStats.RangedDamage;
            lDefinition._GeneralDamagePercent = pSource.GeneralDamagePercent;
            lDefinition._MeleeResistancePercent = pSource.MeleeResistancePercent + lStats.MeleeResistance;
            lDefinition._RangedResistancePercent = pSource.RangedResistancePercent + lStats.RangedResistance;
            lDefinition._GeneralResistancePercent = pSource.GeneralResistancePercent;
            lDefinition._StatPointBudget = pSource.StatPointBudget;
            lDefinition._PushDamageBonus = pSource.PushDamageBonus;
            lDefinition._FootprintWidth = pSource.FootprintWidth;
            lDefinition._FootprintHeight = pSource.FootprintHeight;
            lDefinition._ParticipatesInTurnOrder = pSource.ParticipatesInTurnOrder;
            lDefinition._CountsForVictory = pSource.CountsForVictory;
            lDefinition._EnemyAiTargetPriority = pSource.EnemyAiTargetPriority;
            lDefinition._EnemyAiMovementPolicy = pSource.EnemyAiMovementPolicy;
            lDefinition._EnemyAiPreferredDistance = pSource.EnemyAiPreferredDistance;
            lDefinition._EnemyAiThreatRadius = pSource.EnemyAiThreatRadius;
            lDefinition._EnemyAiProfile = pSource.EnemyAiProfile;
            lDefinition._Skills = pLoadout != null ? CopyList(pLoadout.Skills) : CopyList(pSource._Skills);
            lDefinition._Passives = CopyList(pSource._Passives);
            lDefinition._DefaultPassive = pLoadout?.Passive != null ? pLoadout.Passive : pSource.DefaultPassive;
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

        private static List<T> CopyList<T>(IReadOnlyList<T> pSource)
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
