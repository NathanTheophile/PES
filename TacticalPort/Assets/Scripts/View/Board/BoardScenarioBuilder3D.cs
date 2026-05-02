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
            List<UnitDefinition> lSpawnerFallbackUnits = new List<UnitDefinition>();
            List<GridCoord> lSpawnerCoords = new List<GridCoord>();

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

                    AppendAuthoredSpawn(lUnits, lSpawnerCoords, lSpawnerFallbackUnits, lRuntimeCoord, lTile, lHasTile);
                }
            }

            AppendSelectedTeamSpawns(lUnits, lSpawnerCoords, lSpawnerFallbackUnits);
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
            ICollection<GridCoord> pSpawnerCoords,
            ICollection<UnitDefinition> pSpawnerFallbackUnits,
            GridCoord pRuntimeCoord,
            BoardTileAuthoring pTile,
            bool pHasTile)
        {
            if (!pHasTile || pTile == null)
                return;

            if (pTile.IsSpawner)
            {
                pSpawnerCoords.Add(pRuntimeCoord);
                pSpawnerFallbackUnits.Add(pTile.OccupantDefinition);
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
            IReadOnlyList<GridCoord> pSpawnerCoords,
            IReadOnlyList<UnitDefinition> pSpawnerFallbackUnits)
        {
            if (pUnits == null || pSpawnerCoords == null || pSpawnerCoords.Count == 0)
                return;

            int lSpawnerIndex = 0;
            if (TeamSelectionState.HasSelection)
            {
                IReadOnlyList<UnitDefinition> lSelectedUnits = TeamSelectionState.SelectedUnits;
                for (int lIndex = 0; lIndex < lSelectedUnits.Count && lSpawnerIndex < pSpawnerCoords.Count; lIndex++)
                {
                    UnitDefinition lSelectedUnit = lSelectedUnits[lIndex];
                    if (lSelectedUnit == null)
                        continue;

                    pUnits.Add(new UnitSpawnDefinition
                    {
                        Unit = UnitDefinition.CreateRuntimeClone(lSelectedUnit, Team.Player),
                        StartCoordinate = new SerializableGridCoord(pSpawnerCoords[lSpawnerIndex].X, pSpawnerCoords[lSpawnerIndex].Y)
                    });
                    lSpawnerIndex++;
                }
            }

            for (; lSpawnerIndex < pSpawnerCoords.Count; lSpawnerIndex++)
            {
                UnitDefinition lFallbackUnit = pSpawnerFallbackUnits != null && lSpawnerIndex < pSpawnerFallbackUnits.Count
                    ? pSpawnerFallbackUnits[lSpawnerIndex]
                    : null;
                if (lFallbackUnit == null)
                    continue;

                pUnits.Add(new UnitSpawnDefinition
                {
                    Unit = lFallbackUnit,
                    StartCoordinate = new SerializableGridCoord(pSpawnerCoords[lSpawnerIndex].X, pSpawnerCoords[lSpawnerIndex].Y)
                });
            }
        }
    }
}
