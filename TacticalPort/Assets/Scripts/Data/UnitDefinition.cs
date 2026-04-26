using System.Collections.Generic;
using TacticalPort.Shared;
using TacticalPort.View;
using UnityEngine;

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
        [SerializeField, Min(0)] private int _PushDamageBonus = 0;
        [SerializeField, Min(1)] private int _FootprintWidth = 1;
        [SerializeField, Min(1)] private int _FootprintHeight = 1;
        [SerializeField] private List<SkillDefinition> _Skills = new List<SkillDefinition>();
        [SerializeField] private List<UnitStateEntry> _BaseStates = new List<UnitStateEntry>();
        [SerializeField] private List<UnitPhaseStateDefinition> _PhaseStates = new List<UnitPhaseStateDefinition>();

        [SerializeField] private UnitView _UnitViewPrefab;
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
        public int PushDamageBonus => Mathf.Max(0, _PushDamageBonus);
        public int FootprintWidth => Mathf.Max(1, _FootprintWidth);
        public int FootprintHeight => Mathf.Max(1, _FootprintHeight);
        public bool IsBig => FootprintWidth > 1 || FootprintHeight > 1;
        public IReadOnlyList<SkillDefinition> Skills => _Skills;
        public IReadOnlyList<UnitStateEntry> BaseStates => _BaseStates;
        public IReadOnlyList<UnitPhaseStateDefinition> PhaseStates => _PhaseStates;
        public UnitView UnitViewPrefab => _UnitViewPrefab;
        public Sprite Portrait => _Portrait;
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
            lDefinition._PushDamageBonus = pSource.PushDamageBonus;
            lDefinition._FootprintWidth = pSource.FootprintWidth;
            lDefinition._FootprintHeight = pSource.FootprintHeight;
            lDefinition._Skills = CopyList(pSource._Skills);
            lDefinition._BaseStates = CopyList(pSource._BaseStates);
            lDefinition._PhaseStates = CopyList(pSource._PhaseStates);
            lDefinition._UnitViewPrefab = pSource.UnitViewPrefab;
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
