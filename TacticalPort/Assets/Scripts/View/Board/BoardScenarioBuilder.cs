#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TacticalPort.View
{
    internal static class BoardScenarioBuilder
    {
        public static BattleScenarioDefinition Build(
            SceneScenarioDefinition pScenario,
            Tilemap pGroundTilemap,
            BoundsInt pBounds,
            IReadOnlyList<CellMetadata> pCellMetadata)
        {
            int lDefaultMovementCost = Mathf.Max(1, pScenario.DefaultMovementCost);
            List<CellDefinition> lCells = new List<CellDefinition>(pBounds.size.x * pBounds.size.y);
            List<UnitSpawnDefinition> lUnits = new List<UnitSpawnDefinition>();
            List<CellMetadata> lSpawnerMetadata = new List<CellMetadata>();
            List<GridCoord> lSpawnerCoords = new List<GridCoord>();

            for (int lY = pBounds.yMin; lY < pBounds.yMax; lY++)
            {
                for (int lX = pBounds.xMin; lX < pBounds.xMax; lX++)
                {
                    Vector3Int lAuthoredCell = new Vector3Int(lX, lY, 0);
                    GridCoord lRuntimeCoord = new GridCoord(lX - pBounds.xMin, lY - pBounds.yMin);
                    bool lHasTile = pGroundTilemap != null && pGroundTilemap.HasTile(lAuthoredCell);
                    CellMetadata lMetadata = GetCellMetadataOrDefault(pCellMetadata, lAuthoredCell, lDefaultMovementCost);

                    lCells.Add(new CellDefinition
                    {
                        Coordinate = new SerializableGridCoord(lRuntimeCoord.X, lRuntimeCoord.Y),
                        IsWalkable = lHasTile && lMetadata.IsWalkable,
                        BlocksLineOfSight = !lHasTile || lMetadata.BlocksLineOfSight,
                        MovementCost = lHasTile ? Mathf.Max(1, lMetadata.MovementCost) : lDefaultMovementCost
                    });

                    AppendAuthoredSpawn(lUnits, lSpawnerCoords, lSpawnerMetadata, lRuntimeCoord, lMetadata, lHasTile);
                }
            }

            AppendSelectedTeamSpawns(lUnits, lSpawnerCoords, lSpawnerMetadata);
            return BattleScenarioDefinition.CreateRuntime(
                pScenario.ScenarioId,
                pScenario.DisplayName,
                pBounds.size.x,
                pBounds.size.y,
                lDefaultMovementCost,
                lCells,
                lUnits);
        }

        private static void AppendAuthoredSpawn(
            ICollection<UnitSpawnDefinition> pUnits,
            ICollection<GridCoord> pSpawnerCoords,
            ICollection<CellMetadata> pSpawnerMetadata,
            GridCoord pRuntimeCoord,
            CellMetadata pMetadata,
            bool pHasTile)
        {
            if (!pHasTile || pMetadata == null)
                return;

            if (pMetadata.IsSpawner)
            {
                pSpawnerCoords.Add(pRuntimeCoord);
                pSpawnerMetadata.Add(pMetadata.Clone());
                return;
            }

            if (pMetadata.OccupantDefinition == null)
                return;

            pUnits.Add(new UnitSpawnDefinition
            {
                Unit = pMetadata.OccupantDefinition,
                StartCoordinate = new SerializableGridCoord(pRuntimeCoord.X, pRuntimeCoord.Y)
            });
        }

        private static CellMetadata GetCellMetadataOrDefault(
            IReadOnlyList<CellMetadata> pCellMetadata,
            Vector3Int pCellPosition,
            int pDefaultMovementCost)
        {
            if (pCellMetadata != null)
            {
                for (int lIndex = 0; lIndex < pCellMetadata.Count; lIndex++)
                {
                    CellMetadata lMetadata = pCellMetadata[lIndex];
                    if (lMetadata == null)
                        continue;

                    if (lMetadata.Coordinate.X != pCellPosition.x || lMetadata.Coordinate.Y != pCellPosition.y)
                        continue;

                    lMetadata.Sanitize(pDefaultMovementCost);
                    return lMetadata.Clone();
                }
            }

            return new CellMetadata
            {
                Coordinate = new SerializableGridCoord(pCellPosition.x, pCellPosition.y),
                IsWalkable = true,
                BlocksLineOfSight = false,
                MovementCost = pDefaultMovementCost,
                IsSpawner = false
            };
        }

        private static void AppendSelectedTeamSpawns(
            ICollection<UnitSpawnDefinition> pUnits,
            IReadOnlyList<GridCoord> pSpawnerCoords,
            IReadOnlyList<CellMetadata> pSpawnerMetadata)
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
                CellMetadata lMetadata = pSpawnerMetadata != null && lSpawnerIndex < pSpawnerMetadata.Count ? pSpawnerMetadata[lSpawnerIndex] : null;
                if (lMetadata?.OccupantDefinition == null)
                    continue;

                pUnits.Add(new UnitSpawnDefinition
                {
                    Unit = lMetadata.OccupantDefinition,
                    StartCoordinate = new SerializableGridCoord(pSpawnerCoords[lSpawnerIndex].X, pSpawnerCoords[lSpawnerIndex].Y)
                });
            }
        }
    }
}
