#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Data
#endregion

using System;
using System.Collections.Generic;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.Data
{
    [Serializable]
    public sealed class SceneScenarioDefinition
    {
        #region _____________________________/ VALUES

        [Header("Identity")]
        [SerializeField] private string _ScenarioId = "sample_scene";
        [SerializeField] private string _DisplayName = "Sample Combat";

        [Header("Board")]
        [SerializeField, Min(1)] private int _DefaultMovementCost = 1;

        [Header("Temporary Slot Compositions")]
        [SerializeField] private List<UnitDefinition> _TeamAComposition = new List<UnitDefinition>();
        [SerializeField] private List<UnitDefinition> _TeamBComposition = new List<UnitDefinition>();

        #endregion

        #region _____________________________/ ACCESSORS

        public string ScenarioId => string.IsNullOrWhiteSpace(_ScenarioId) ? "scene_board" : _ScenarioId;
        public string DisplayName => string.IsNullOrWhiteSpace(_DisplayName) ? "Scene Board" : _DisplayName;
        public int DefaultMovementCost => Mathf.Max(1, _DefaultMovementCost);

        #endregion

        #region _____________________________| COMPOSITIONS

        public bool TryGetComposition(MatchPlayerSlot pSlot, out IReadOnlyList<UnitDefinition> pUnits)
        {
            List<UnitDefinition> lUnits = pSlot == MatchPlayerSlot.TeamA
                ? _TeamAComposition
                : pSlot == MatchPlayerSlot.TeamB
                    ? _TeamBComposition
                    : null;

            if (lUnits != null && lUnits.Count > 0)
            {
                pUnits = lUnits;
                return true;
            }

            pUnits = null;
            return false;
        }

        #endregion
    }
}
