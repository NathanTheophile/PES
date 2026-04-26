using System.Collections.Generic;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.Data
{
    [CreateAssetMenu(fileName = "BattleScenarioDefinition", menuName = "TacticalPort/Data/Battle Scenario Definition")]
    public sealed class BattleScenarioDefinition : ScriptableObject
    {
        #region _____________________________| VALUES

        [Header("Identity")]
        [SerializeField] private string _ScenarioId = string.Empty;
        [SerializeField] private string _DisplayName = string.Empty;

        [Header("Board")]
        [SerializeField, Min(1)] private int _Width = 8;
        [SerializeField, Min(1)] private int _Height = 8;
        [SerializeField, Min(1)] private int _DefaultMovementCost = 1;
        [SerializeField] private List<BattleGridCellDefinition> _Cells = new List<BattleGridCellDefinition>();

        [Header("Units")]
        [SerializeField] private List<BattleUnitSpawnDefinition> _Units = new List<BattleUnitSpawnDefinition>();

        #endregion

        #region _____________________________| ACCESSORS

        public string ScenarioId => _ScenarioId;
        public string DisplayName => string.IsNullOrWhiteSpace(_DisplayName) ? name : _DisplayName;
        public int Width => Mathf.Max(1, _Width);
        public int Height => Mathf.Max(1, _Height);
        public int DefaultMovementCost => Mathf.Max(1, _DefaultMovementCost);
        public IReadOnlyList<BattleGridCellDefinition> Cells => _Cells;
        public IReadOnlyList<BattleUnitSpawnDefinition> Units => _Units;

        #endregion

        #region _____________________________| FACTORIES

        public static BattleScenarioDefinition CreateRuntime(
            string pScenarioId,
            string pDisplayName,
            int pWidth,
            int pHeight,
            int pDefaultMovementCost,
            IReadOnlyList<BattleGridCellDefinition> pCells,
            IReadOnlyList<BattleUnitSpawnDefinition> pUnits)
        {
            BattleScenarioDefinition lScenario = CreateInstance<BattleScenarioDefinition>();
            lScenario.hideFlags = HideFlags.DontSave;
            lScenario.name = string.IsNullOrWhiteSpace(pDisplayName) ? "BattleScenarioRuntimeDefinition" : pDisplayName;
            lScenario._ScenarioId = string.IsNullOrWhiteSpace(pScenarioId) ? lScenario.name.ToLowerInvariant().Replace(" ", "_") : pScenarioId;
            lScenario._DisplayName = string.IsNullOrWhiteSpace(pDisplayName) ? lScenario.name : pDisplayName;
            lScenario._Width = Mathf.Max(1, pWidth);
            lScenario._Height = Mathf.Max(1, pHeight);
            lScenario._DefaultMovementCost = Mathf.Max(1, pDefaultMovementCost);
            lScenario._Cells = new List<BattleGridCellDefinition>();
            lScenario._Units = new List<BattleUnitSpawnDefinition>();

            if (pCells != null)
            {
                foreach (BattleGridCellDefinition lCell in pCells)
                {
                    if (lCell != null)
                        lScenario._Cells.Add(lCell.Clone());
                }
            }

            if (pUnits != null)
            {
                foreach (BattleUnitSpawnDefinition lUnit in pUnits)
                {
                    if (lUnit != null)
                        lScenario._Units.Add(lUnit.Clone());
                }
            }

            return lScenario;
        }

        #endregion

        #region _____________________________| BUILD

        public bool TryValidate(out string pFailureReason)
        {
            if (_Units.Count == 0)
            {
                pFailureReason = "Scenario requires at least one unit spawn.";
                return false;
            }

            for (int lIndex = 0; lIndex < _Units.Count; lIndex++)
            {
                BattleUnitSpawnDefinition lSpawn = _Units[lIndex];
                if (lSpawn == null || lSpawn.Unit == null)
                {
                    pFailureReason = $"Scenario spawn #{lIndex + 1} is missing a unit definition.";
                    return false;
                }
            }

            pFailureReason = string.Empty;
            return true;
        }

        public bool TryGetSpawn(BattleUnitId pUnitId, out BattleUnitSpawnDefinition pSpawn)
        {
            int lSpawnIndex = pUnitId.Value - 1;

            if (lSpawnIndex >= 0 && lSpawnIndex < _Units.Count)
            {
                pSpawn = _Units[lSpawnIndex];
                return pSpawn != null;
            }

            pSpawn = null;
            return false;
        }

        public IEnumerable<BattleGridCellDefinition> EnumerateCells()
        {
            Dictionary<GridCoord, BattleGridCellDefinition> lOverridesByCoord = new Dictionary<GridCoord, BattleGridCellDefinition>();

            foreach (BattleGridCellDefinition lCell in _Cells)
            {
                if (lCell == null)
                    continue;

                lOverridesByCoord[lCell.Coordinate.ToRuntime()] = lCell;
            }

            for (int lY = 0; lY < Height; lY++)
            {
                for (int lX = 0; lX < Width; lX++)
                {
                    GridCoord lCoord = new GridCoord(lX, lY);

                    if (lOverridesByCoord.TryGetValue(lCoord, out BattleGridCellDefinition lCell))
                    {
                        yield return lCell;
                        continue;
                    }

                    yield return new BattleGridCellDefinition
                    {
                        Coordinate = new SerializableGridCoord(lX, lY),
                        IsWalkable = true,
                        BlocksLineOfSight = false,
                        MovementCost = DefaultMovementCost
                    };
                }
            }
        }

        #endregion
    }
}
