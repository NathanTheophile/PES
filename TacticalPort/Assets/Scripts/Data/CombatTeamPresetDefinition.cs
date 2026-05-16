#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Data
#endregion

using System.Collections.Generic;
using UnityEngine;

namespace TacticalPort.Data
{
    [CreateAssetMenu(fileName = "TeamPreset", menuName = "Project/Data/Combat Team Preset")]
    public sealed class CombatTeamPresetDefinition : ScriptableObject
    {
        #region _____________________________/ VALUES

        [SerializeField] private string _PresetId = string.Empty;
        [SerializeField] private List<UnitDefinition> _Units = new List<UnitDefinition>(3);

        #endregion

        #region _____________________________/ ACCESSORS

        public string PresetId => string.IsNullOrWhiteSpace(_PresetId) ? name : _PresetId;
        public IReadOnlyList<UnitDefinition> Units => _Units;

        #endregion

        #region _____________________________| VALIDATION

        public bool TryValidate(out string pFailure)
        {
            if (string.IsNullOrWhiteSpace(PresetId))
            {
                pFailure = "Team preset id is empty.";
                return false;
            }

            if (_Units == null || _Units.Count == 0)
            {
                pFailure = $"Team preset '{PresetId}' has no units.";
                return false;
            }

            for (int lIndex = 0; lIndex < _Units.Count; lIndex++)
            {
                if (_Units[lIndex] != null)
                    continue;

                pFailure = $"Team preset '{PresetId}' has an empty unit at index {lIndex}.";
                return false;
            }

            pFailure = string.Empty;
            return true;
        }

        #endregion
    }
}
