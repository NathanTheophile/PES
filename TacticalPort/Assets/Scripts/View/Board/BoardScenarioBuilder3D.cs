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
                AppendSelectedTeamSpawns(lUnits, lSpawnCells, lFallbackRoster);
            else
                lUnits.AddRange(lDirectUnits);

            AssignMissingTeamSlotIndices(lUnits);

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
            IReadOnlyList<UnitDefinition> pFallbackRoster)
        {
            if (pUnits == null || pSpawnCells == null || pSpawnCells.Count == 0)
                return;

            IReadOnlyList<UnitDefinition> lTeamAUnits = ResolveComposition(MatchPlayerSlot.TeamA);
            IReadOnlyList<UnitDefinition> lTeamBUnits = ResolveComposition(MatchPlayerSlot.TeamB);

            AppendSpawnedTeam(pUnits, pSpawnCells, pFallbackRoster, MatchPlayerSlot.TeamA, Team.TeamA, lTeamAUnits, ResolveTeamSize(lTeamAUnits));
            AppendSpawnedTeam(pUnits, pSpawnCells, pFallbackRoster, MatchPlayerSlot.TeamB, Team.TeamB, lTeamBUnits, ResolveTeamSize(lTeamBUnits));
        }

        private static IReadOnlyList<UnitDefinition> ResolveComposition(MatchPlayerSlot pSlot) =>
            CombatTeamCompositionState.TryGetComposition(pSlot, out IReadOnlyList<UnitDefinition> lRuntimeUnits)
                ? lRuntimeUnits
                : null;

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

                UnitDefinition lSource = ResolveSpawnUnit(
                    lSpawnCell,
                    pFallbackRoster,
                    pSelectedUnits,
                    ref lSelectedIndex,
                    out int lSelectedUnitIndex);
                if (lSource == null)
                    continue;

                CombatTeamCompositionState.TryGetLoadout(pSlot, lSelectedUnitIndex, out UnitCombatLoadout lLoadout);

                pUnits.Add(new UnitSpawnDefinition
                {
                    Unit = UnitDefinition.CreateRuntimeClone(lSource, pTeam, lLoadout),
                    StartCoordinate = new SerializableGridCoord(lSpawnCell.Coord.X, lSpawnCell.Coord.Y),
                    TeamSlotIndex = lSelectedUnitIndex >= 0 ? lSelectedUnitIndex : lAddedUnits
                });
                lAddedUnits++;
            }
        }

        private static UnitDefinition ResolveSpawnUnit(
            SpawnCell pSpawnCell,
            IReadOnlyList<UnitDefinition> pFallbackRoster,
            IReadOnlyList<UnitDefinition> pSelectedUnits,
            ref int pSelectedIndex,
            out int pSelectedUnitIndex)
        {
            pSelectedUnitIndex = -1;
            while (pSelectedUnits != null && pSelectedIndex < pSelectedUnits.Count)
            {
                int lCurrentIndex = pSelectedIndex++;
                UnitDefinition lSelectedUnit = pSelectedUnits[lCurrentIndex];
                if (lSelectedUnit != null)
                {
                    pSelectedUnitIndex = lCurrentIndex;
                    return lSelectedUnit;
                }
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

        private static void AssignMissingTeamSlotIndices(IList<UnitSpawnDefinition> pUnits)
        {
            if (pUnits == null)
                return;

            int lNextTeamAIndex = FindNextTeamSlotIndex(pUnits, Team.TeamA);
            int lNextTeamBIndex = FindNextTeamSlotIndex(pUnits, Team.TeamB);
            for (int lIndex = 0; lIndex < pUnits.Count; lIndex++)
            {
                UnitSpawnDefinition lSpawn = pUnits[lIndex];
                if (lSpawn?.Unit == null || lSpawn.TeamSlotIndex >= 0)
                    continue;

                if (lSpawn.Unit.Team == Team.TeamA)
                    lSpawn.TeamSlotIndex = lNextTeamAIndex++;
                else if (lSpawn.Unit.Team == Team.TeamB)
                    lSpawn.TeamSlotIndex = lNextTeamBIndex++;
            }
        }

        private static int FindNextTeamSlotIndex(IList<UnitSpawnDefinition> pUnits, Team pTeam)
        {
            int lNextIndex = 0;
            for (int lIndex = 0; lIndex < pUnits.Count; lIndex++)
            {
                UnitSpawnDefinition lSpawn = pUnits[lIndex];
                if (lSpawn?.Unit != null && lSpawn.Unit.Team == pTeam && lSpawn.TeamSlotIndex >= lNextIndex)
                    lNextIndex = lSpawn.TeamSlotIndex + 1;
            }

            return lNextIndex;
        }
    }
}
