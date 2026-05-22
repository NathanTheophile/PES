#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;
using TacticalPort.State;
using UnityEngine;

namespace TacticalPort.View
{
    internal static class BoardScenarioBuilder3D
    {
        private const int DefaultTeamSize = 3;

        public static BattleScenarioDefinition Build(
            SceneScenarioDefinition pScenario,
            IReadOnlyDictionary<Vector2Int, BoardTileAuthoring> pTilesByAuthoredCoordinate,
            RectInt pBounds)
        {
            pScenario ??= new SceneScenarioDefinition();

            int lDefaultMovementCost = Mathf.Max(1, pScenario.DefaultMovementCost);
            List<CellDefinition> lCells = new List<CellDefinition>(pBounds.width * pBounds.height);
            List<UnitSpawnDefinition> lUnits = new List<UnitSpawnDefinition>();
            List<UnitSpawnDefinition> lDirectUnits = new List<UnitSpawnDefinition>();
            List<SpawnCell> lSpawnCells = new List<SpawnCell>();
            List<UnitDefinition> lFallbackRoster = new List<UnitDefinition>();

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

                    AppendAuthoredSpawn(lDirectUnits, lSpawnCells, lFallbackRoster, lRuntimeCoord, lTile, lHasTile);
                }
            }

            if (lSpawnCells.Count > 0)
                AppendSelectedTeamSpawns(lUnits, lSpawnCells, lFallbackRoster, pScenario);
            else
                lUnits.AddRange(lDirectUnits);

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
            ICollection<UnitDefinition> pFallbackRoster,
            GridCoord pRuntimeCoord,
            BoardTileAuthoring pTile,
            bool pHasTile)
        {
            if (!pHasTile || pTile == null)
                return;

            if (pTile.OccupantDefinition != null)
                pFallbackRoster?.Add(pTile.OccupantDefinition);

            if (pTile.AssignedTeam != MatchPlayerSlot.None)
            {
                pSpawnCells.Add(new SpawnCell(pRuntimeCoord, pTile.AssignedTeam, pTile.OccupantDefinition));
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
            IReadOnlyList<SpawnCell> pSpawnCells,
            IReadOnlyList<UnitDefinition> pFallbackRoster,
            SceneScenarioDefinition pScenario)
        {
            if (pUnits == null || pSpawnCells == null || pSpawnCells.Count == 0)
                return;

            IReadOnlyList<UnitDefinition> lTeamAUnits = ResolveComposition(pScenario, MatchPlayerSlot.TeamA);
            IReadOnlyList<UnitDefinition> lTeamBUnits = ResolveComposition(pScenario, MatchPlayerSlot.TeamB);

            AppendSpawnedTeam(pUnits, pSpawnCells, pFallbackRoster, MatchPlayerSlot.TeamA, Team.TeamA, lTeamAUnits, ResolveTeamSize(lTeamAUnits));
            AppendSpawnedTeam(pUnits, pSpawnCells, pFallbackRoster, MatchPlayerSlot.TeamB, Team.TeamB, lTeamBUnits, ResolveTeamSize(lTeamBUnits));
        }

        private static IReadOnlyList<UnitDefinition> ResolveComposition(SceneScenarioDefinition pScenario, MatchPlayerSlot pSlot)
        {
            if (CombatTeamCompositionState.TryGetComposition(pSlot, out IReadOnlyList<UnitDefinition> lRuntimeUnits))
                return lRuntimeUnits;

            return pScenario != null && pScenario.TryGetComposition(pSlot, out IReadOnlyList<UnitDefinition> lSceneUnits)
                ? lSceneUnits
                : null;
        }

        private static int ResolveTeamSize(IReadOnlyList<UnitDefinition> pUnits) =>
            pUnits != null && pUnits.Count > 0 ? pUnits.Count : DefaultTeamSize;

        private static void AppendSpawnedTeam(
            ICollection<UnitSpawnDefinition> pUnits,
            IReadOnlyList<SpawnCell> pSpawnCells,
            IReadOnlyList<UnitDefinition> pFallbackRoster,
            MatchPlayerSlot pSlot,
            Team pTeam,
            IReadOnlyList<UnitDefinition> pSelectedUnits,
            int pMaxUnits)
        {
            int lAddedUnits = 0;
            int lSelectedIndex = 0;
            for (int lSpawnIndex = 0; lSpawnIndex < pSpawnCells.Count && lAddedUnits < pMaxUnits; lSpawnIndex++)
            {
                SpawnCell lSpawnCell = pSpawnCells[lSpawnIndex];
                if (lSpawnCell.Slot != pSlot)
                    continue;

                UnitDefinition lSource = ResolveSpawnUnit(lSpawnCell, pFallbackRoster, pSelectedUnits, ref lSelectedIndex);
                if (lSource == null)
                    continue;

                pUnits.Add(new UnitSpawnDefinition
                {
                    Unit = UnitDefinition.CreateRuntimeClone(lSource, pTeam),
                    StartCoordinate = new SerializableGridCoord(lSpawnCell.Coord.X, lSpawnCell.Coord.Y)
                });
                lAddedUnits++;
            }
        }

        private static UnitDefinition ResolveSpawnUnit(
            SpawnCell pSpawnCell,
            IReadOnlyList<UnitDefinition> pFallbackRoster,
            IReadOnlyList<UnitDefinition> pSelectedUnits,
            ref int pSelectedIndex)
        {
            while (pSelectedUnits != null && pSelectedIndex < pSelectedUnits.Count)
            {
                UnitDefinition lSelectedUnit = pSelectedUnits[pSelectedIndex++];
                if (lSelectedUnit != null)
                    return lSelectedUnit;
            }

            if (pSpawnCell.FallbackUnit != null)
                return pSpawnCell.FallbackUnit;

            if (pFallbackRoster == null)
                return null;

            for (int lIndex = 0; lIndex < pFallbackRoster.Count; lIndex++)
            {
                if (pFallbackRoster[lIndex] != null)
                    return pFallbackRoster[lIndex];
            }

            return null;
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
