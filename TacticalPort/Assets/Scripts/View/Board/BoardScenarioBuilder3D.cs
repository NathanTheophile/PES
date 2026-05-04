#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.View
{
    internal static class BoardScenarioBuilder3D
    {
        public static BattleScenarioDefinition Build(
            SceneScenarioDefinition pScenario,
            IReadOnlyDictionary<Vector2Int, BoardTileAuthoring> pTilesByAuthoredCoordinate,
            RectInt pBounds)
        {
            pScenario ??= new SceneScenarioDefinition();

            int lDefaultMovementCost = Mathf.Max(1, pScenario.DefaultMovementCost);
            List<CellDefinition> lCells = new List<CellDefinition>(pBounds.width * pBounds.height);
            List<UnitSpawnDefinition> lUnits = new List<UnitSpawnDefinition>();
            List<SpawnCell> lSpawnCells = new List<SpawnCell>();

            for (int lY = pBounds.yMin; lY < pBounds.yMax; lY++)
            {
                for (int lX = pBounds.xMin; lX < pBounds.xMax; lX++)
                {
                    Vector2Int lAuthoredCoord = new Vector2Int(lX, lY);
                    GridCoord lRuntimeCoord = new GridCoord(lX - pBounds.xMin, lY - pBounds.yMin);
                    BoardTileAuthoring lTile = null;
                    bool lHasTile = pTilesByAuthoredCoordinate != null
                        && pTilesByAuthoredCoordinate.TryGetValue(lAuthoredCoord, out lTile)
                        && lTile != null;

                    lCells.Add(new CellDefinition
                    {
                        Coordinate = new SerializableGridCoord(lRuntimeCoord.X, lRuntimeCoord.Y),
                        IsWalkable = lHasTile && lTile.IsWalkable,
                        BlocksLineOfSight = !lHasTile || lTile.BlocksLineOfSight,
                        MovementCost = lHasTile ? lTile.MovementCost : lDefaultMovementCost
                    });

                    AppendAuthoredSpawn(lUnits, lSpawnCells, lRuntimeCoord, lTile, lHasTile);
                }
            }

            AppendSelectedTeamSpawns(lUnits, lSpawnCells);
            return BattleScenarioDefinition.CreateRuntime(
                pScenario.ScenarioId,
                pScenario.DisplayName,
                pBounds.width,
                pBounds.height,
                lDefaultMovementCost,
                lCells,
                lUnits);
        }

        private static void AppendAuthoredSpawn(
            ICollection<UnitSpawnDefinition> pUnits,
            ICollection<SpawnCell> pSpawnCells,
            GridCoord pRuntimeCoord,
            BoardTileAuthoring pTile,
            bool pHasTile)
        {
            if (!pHasTile || pTile == null)
                return;

            if (pTile.SpawnZone != MatchPlayerSlot.None)
            {
                pSpawnCells.Add(new SpawnCell(pRuntimeCoord, pTile.SpawnZone, pTile.OccupantDefinition));
                return;
            }

            if (pTile.OccupantDefinition == null)
                return;

            pUnits.Add(new UnitSpawnDefinition
            {
                Unit = pTile.OccupantDefinition,
                StartCoordinate = new SerializableGridCoord(pRuntimeCoord.X, pRuntimeCoord.Y)
            });
        }

        private static void AppendSelectedTeamSpawns(
            ICollection<UnitSpawnDefinition> pUnits,
            IReadOnlyList<SpawnCell> pSpawnCells)
        {
            if (pUnits == null || pSpawnCells == null || pSpawnCells.Count == 0)
                return;

            HashSet<int> lConsumedSpawnIndexes = new HashSet<int>();
            if (TeamSelectionState.HasSelection)
            {
                IReadOnlyList<UnitDefinition> lSelectedUnits = TeamSelectionState.SelectedUnits;
                int lSelectedIndex = 0;
                for (int lSpawnIndex = 0; lSpawnIndex < pSpawnCells.Count && lSelectedIndex < lSelectedUnits.Count; lSpawnIndex++)
                {
                    SpawnCell lSpawnCell = pSpawnCells[lSpawnIndex];
                    if (lSpawnCell.Slot != MatchPlayerSlot.TeamA)
                        continue;

                    UnitDefinition lSelectedUnit = lSelectedUnits[lSelectedIndex++];
                    if (lSelectedUnit == null)
                        continue;

                    pUnits.Add(new UnitSpawnDefinition
                    {
                        Unit = UnitDefinition.CreateRuntimeClone(lSelectedUnit, Team.Player),
                        StartCoordinate = new SerializableGridCoord(lSpawnCell.Coord.X, lSpawnCell.Coord.Y)
                    });
                    lConsumedSpawnIndexes.Add(lSpawnIndex);
                }
            }

            for (int lSpawnIndex = 0; lSpawnIndex < pSpawnCells.Count; lSpawnIndex++)
            {
                if (lConsumedSpawnIndexes.Contains(lSpawnIndex))
                    continue;

                SpawnCell lSpawnCell = pSpawnCells[lSpawnIndex];
                if (lSpawnCell.FallbackUnit == null)
                    continue;

                pUnits.Add(new UnitSpawnDefinition
                {
                    Unit = lSpawnCell.FallbackUnit,
                    StartCoordinate = new SerializableGridCoord(lSpawnCell.Coord.X, lSpawnCell.Coord.Y)
                });
            }
        }

        private readonly struct SpawnCell
        {
            public readonly GridCoord Coord;
            public readonly MatchPlayerSlot Slot;
            public readonly UnitDefinition FallbackUnit;

            public SpawnCell(GridCoord pCoord, MatchPlayerSlot pSlot, UnitDefinition pFallbackUnit)
            {
                Coord = pCoord;
                Slot = pSlot;
                FallbackUnit = pFallbackUnit;
            }
        }
    }
}
