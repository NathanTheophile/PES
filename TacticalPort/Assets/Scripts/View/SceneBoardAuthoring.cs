using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.View
{
    public sealed class SceneBoardAuthoring : MonoBehaviour
    {
        #region _____________________________| VALUES

        [SerializeField] private SceneBattleScenarioDefinition _Scenario = new SceneBattleScenarioDefinition();

        private BattleScenarioDefinition _RuntimeScenario;

        #endregion

        #region _____________________________| ACCESSORS

        public SceneBattleScenarioDefinition Scenario => _Scenario;
        public bool HasCells => GetComponentsInChildren<SceneBoardCell>(true).Length > 0;

        #endregion

        private void OnValidate() => _RuntimeScenario = null;

        public BattleScenarioDefinition BuildScenario()
        {
            if (_RuntimeScenario != null)
                return _RuntimeScenario;

            if (!TryBuildScenario(out _RuntimeScenario, out string lFailureReason) && !string.IsNullOrWhiteSpace(lFailureReason))
                Debug.LogError(lFailureReason, this);

            return _RuntimeScenario;
        }

        public bool TryBuildScenario(out BattleScenarioDefinition pScenario, out string pFailureReason)
        {
            pScenario = null;

            if (!TryCollectCells(out List<SceneBoardCell> lCells, out pFailureReason))
                return false;

            if (lCells.Count == 0)
            {
                pFailureReason = $"Scene board '{name}' does not contain any {nameof(SceneBoardCell)} children.";
                return false;
            }

            pScenario = CreateRuntimeScenario(lCells);
            pFailureReason = string.Empty;
            return true;
        }

        private bool TryCollectCells(out List<SceneBoardCell> pCells, out string pFailureReason)
        {
            pCells = new List<SceneBoardCell>();
            Dictionary<GridCoord, SceneBoardCell> lCellsByCoord = new Dictionary<GridCoord, SceneBoardCell>();

            foreach (SceneBoardCell lCell in GetComponentsInChildren<SceneBoardCell>(true))
            {
                if (lCell == null)
                    continue;

                GridCoord lCoord = lCell.GridCoord;
                if (lCoord.X < 0 || lCoord.Y < 0)
                {
                    pFailureReason = $"Scene board '{name}' contains cell '{lCell.name}' with invalid coordinate {lCoord}.";
                    return false;
                }

                if (lCellsByCoord.TryGetValue(lCoord, out SceneBoardCell lExistingCell))
                {
                    pFailureReason = $"Scene board '{name}' has duplicate cells at {lCoord}: '{lExistingCell.name}' and '{lCell.name}'.";
                    return false;
                }

                lCellsByCoord.Add(lCoord, lCell);
                pCells.Add(lCell);
            }

            pCells.Sort(CompareCells);
            pFailureReason = string.Empty;
            return true;
        }

        private BattleScenarioDefinition CreateRuntimeScenario(IReadOnlyList<SceneBoardCell> pCells)
        {
            List<BattleGridCellDefinition> lCells = new List<BattleGridCellDefinition>(pCells.Count);
            List<BattleUnitSpawnDefinition> lUnits = new List<BattleUnitSpawnDefinition>();
            int lWidth = 1;
            int lHeight = 1;
            SceneBattleScenarioDefinition lScenarioMetadata = _Scenario ?? new SceneBattleScenarioDefinition();
            int lDefaultMovementCost = lScenarioMetadata.DefaultMovementCost;

            for (int lIndex = 0; lIndex < pCells.Count; lIndex++)
            {
                SceneBoardCell lCell = pCells[lIndex];
                GridCoord lCoord = lCell.GridCoord;

                lWidth = Mathf.Max(lWidth, lCoord.X + 1);
                lHeight = Mathf.Max(lHeight, lCoord.Y + 1);

                lCells.Add(new BattleGridCellDefinition
                {
                    Coordinate = new SerializableGridCoord(lCoord.X, lCoord.Y),
                    IsWalkable = lCell.IsWalkable,
                    MovementCost = Mathf.Max(1, lCell.MovementCost)
                });

                if (lCell.OccupantDefinition != null)
                {
                    lUnits.Add(new BattleUnitSpawnDefinition
                    {
                        Unit = lCell.OccupantDefinition,
                        StartCoordinate = new SerializableGridCoord(lCoord.X, lCoord.Y)
                    });
                }
            }

            return BattleScenarioDefinition.CreateRuntime(
                lScenarioMetadata.ScenarioId,
                lScenarioMetadata.DisplayName,
                lWidth,
                lHeight,
                lDefaultMovementCost,
                lCells,
                lUnits);
        }

        private static int CompareCells(SceneBoardCell pLeft, SceneBoardCell pRight)
        {
            GridCoord lLeft = pLeft.GridCoord;
            GridCoord lRight = pRight.GridCoord;
            int lYCompare = lLeft.Y.CompareTo(lRight.Y);
            return lYCompare != 0 ? lYCompare : lLeft.X.CompareTo(lRight.X);
        }
    }
}
