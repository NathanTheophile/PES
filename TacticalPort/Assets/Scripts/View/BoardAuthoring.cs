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
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TacticalPort.View
{
    public sealed class BoardAuthoring : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private SceneScenarioDefinition _Scenario = new SceneScenarioDefinition();
        [SerializeField] private Grid _Grid;
        [SerializeField] private Tilemap _GroundTilemap;
        [SerializeField] private List<CellMetadata> _CellMetadata = new List<CellMetadata>();

        private BattleScenarioDefinition _RuntimeScenario;

        #endregion

        #region _____________________________/ ACCESSORS

        public Tilemap FloorTilemap => _GroundTilemap;
        public int DefaultMovementCost => Mathf.Max(1, _Scenario.DefaultMovementCost);

        #endregion

        #region _____________________________| UNITY

        private void Awake() => CacheMissingReferences();

        private void OnValidate()
        {
            CacheMissingReferences();
            SanitizeMetadata();
            InvalidateRuntimeScenario();
        }

        #endregion

        #region _____________________________| BUILD

        public BattleScenarioDefinition BuildScenario()
        {
            if (_RuntimeScenario != null)
                return _RuntimeScenario;

            if (!TryGetPaintedBounds(out BoundsInt lBounds))
                return null;

            int lDefaultMovementCost = Mathf.Max(1, _Scenario.DefaultMovementCost);
            List<CellDefinition> lCells = new List<CellDefinition>(lBounds.size.x * lBounds.size.y);
            List<UnitSpawnDefinition> lUnits = new List<UnitSpawnDefinition>();
            List<CellMetadata> lSpawnerMetadata = new List<CellMetadata>();
            List<GridCoord> lSpawnerCoords = new List<GridCoord>();

            for (int lY = lBounds.yMin; lY < lBounds.yMax; lY++)
            {
                for (int lX = lBounds.xMin; lX < lBounds.xMax; lX++)
                {
                    Vector3Int lAuthoredCell = new Vector3Int(lX, lY, 0);
                    GridCoord lRuntimeCoord = new GridCoord(lX - lBounds.xMin, lY - lBounds.yMin);
                    bool lHasTile = _GroundTilemap != null && _GroundTilemap.HasTile(lAuthoredCell);
                    CellMetadata lMetadata = GetCellMetadataOrDefault(lAuthoredCell, lDefaultMovementCost);

                    lCells.Add(new CellDefinition
                    {
                        Coordinate = new SerializableGridCoord(lRuntimeCoord.X, lRuntimeCoord.Y),
                        IsWalkable = lHasTile && lMetadata.IsWalkable,
                        BlocksLineOfSight = !lHasTile || lMetadata.BlocksLineOfSight,
                        MovementCost = lHasTile ? Mathf.Max(1, lMetadata.MovementCost) : lDefaultMovementCost
                    });

                    if (lHasTile && lMetadata.OccupantDefinition != null)
                    {
                        if (lMetadata.IsSpawner)
                        {
                            lSpawnerCoords.Add(lRuntimeCoord);
                            lSpawnerMetadata.Add(lMetadata.Clone());
                        }
                        else
                        {
                            lUnits.Add(new UnitSpawnDefinition
                            {
                                Unit = lMetadata.OccupantDefinition,
                                StartCoordinate = new SerializableGridCoord(lRuntimeCoord.X, lRuntimeCoord.Y)
                            });
                        }
                    }
                    else if (lHasTile && lMetadata.IsSpawner)
                    {
                        lSpawnerCoords.Add(lRuntimeCoord);
                        lSpawnerMetadata.Add(lMetadata.Clone());
                    }
                }
            }

            AppendSelectedTeamSpawns(lUnits, lSpawnerCoords, lSpawnerMetadata);

            _RuntimeScenario = BattleScenarioDefinition.CreateRuntime(
                _Scenario.ScenarioId,
                _Scenario.DisplayName,
                lBounds.size.x,
                lBounds.size.y,
                lDefaultMovementCost,
                lCells,
                lUnits);
            return _RuntimeScenario;
        }

        #endregion

        #region _____________________________| METADATA

        public void InvalidateRuntimeScenario()
        {
            _RuntimeScenario = null;

            BoardView lBoardView = GetComponent<BoardView>();
            if (lBoardView != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    EditorApplication.delayCall += () =>
                    {
                        if (this == null || lBoardView == null)
                            return;

                        lBoardView.RebuildBoard();
                    };
                    return;
                }
#endif
                lBoardView.RebuildBoard();
            }
        }

        public bool TryGetPaintedBounds(out BoundsInt pBounds)
        {
            pBounds = default;
            if (_GroundTilemap == null)
                return false;

            BoundsInt lCellBounds = _GroundTilemap.cellBounds;
            bool lHasTile = false;
            int lMinX = 0;
            int lMinY = 0;
            int lMaxX = 0;
            int lMaxY = 0;

            foreach (Vector3Int lCellPosition in lCellBounds.allPositionsWithin)
            {
                if (!_GroundTilemap.HasTile(lCellPosition))
                    continue;

                if (!lHasTile)
                {
                    lMinX = lMaxX = lCellPosition.x;
                    lMinY = lMaxY = lCellPosition.y;
                    lHasTile = true;
                    continue;
                }

                lMinX = Mathf.Min(lMinX, lCellPosition.x);
                lMinY = Mathf.Min(lMinY, lCellPosition.y);
                lMaxX = Mathf.Max(lMaxX, lCellPosition.x);
                lMaxY = Mathf.Max(lMaxY, lCellPosition.y);
            }

            if (!lHasTile)
                return false;

            pBounds = new BoundsInt(
                new Vector3Int(lMinX, lMinY, 0),
                new Vector3Int(lMaxX - lMinX + 1, lMaxY - lMinY + 1, 1));
            return true;
        }

        public bool TryGetGridCoord(Vector3 pWorldPosition, out GridCoord pCoord)
        {
            pCoord = default;
            if (_GroundTilemap == null || !TryGetPaintedBounds(out BoundsInt lBounds))
                return false;

            Vector3Int lCell = _GroundTilemap.WorldToCell(pWorldPosition);
            if (!_GroundTilemap.HasTile(lCell))
                return false;

            pCoord = new GridCoord(lCell.x - lBounds.xMin, lCell.y - lBounds.yMin);
            return true;
        }

        public bool TryGetAuthoredCell(GridCoord pRuntimeCoord, out Vector3Int pCellPosition)
        {
            pCellPosition = default;
            if (!TryGetPaintedBounds(out BoundsInt lBounds))
                return false;

            if (pRuntimeCoord.X < 0 || pRuntimeCoord.Y < 0 || pRuntimeCoord.X >= lBounds.size.x || pRuntimeCoord.Y >= lBounds.size.y)
                return false;

            pCellPosition = new Vector3Int(lBounds.xMin + pRuntimeCoord.X, lBounds.yMin + pRuntimeCoord.Y, 0);
            return true;
        }

        public bool ContainsCell(GridCoord pRuntimeCoord)
        {
            return TryGetAuthoredCell(pRuntimeCoord, out Vector3Int lCellPosition)
                && _GroundTilemap != null
                && _GroundTilemap.HasTile(lCellPosition);
        }

        public Vector3 GetWorldPosition(GridCoord pRuntimeCoord)
        {
            return TryGetWorldPosition(pRuntimeCoord, out Vector3 lWorldPosition) ? lWorldPosition : Vector3.zero;
        }

        public bool TryGetWorldPosition(GridCoord pRuntimeCoord, out Vector3 pWorldPosition)
        {
            pWorldPosition = default;
            if (_GroundTilemap == null || !TryGetAuthoredCell(pRuntimeCoord, out Vector3Int lCell))
                return false;

            pWorldPosition = _GroundTilemap.GetCellCenterWorld(lCell);
            return true;
        }

        public IEnumerable<GridCoord> EnumeratePaintedCoordinates()
        {
            if (!TryGetPaintedBounds(out BoundsInt lBounds))
                yield break;

            for (int lY = lBounds.yMin; lY < lBounds.yMax; lY++)
            {
                for (int lX = lBounds.xMin; lX < lBounds.xMax; lX++)
                {
                    Vector3Int lCell = new Vector3Int(lX, lY, 0);
                    if (_GroundTilemap != null && _GroundTilemap.HasTile(lCell))
                        yield return new GridCoord(lX - lBounds.xMin, lY - lBounds.yMin);
                }
            }
        }

        public bool TryGetCellDefinition(GridCoord pRuntimeCoord, out CellDefinition pCellDefinition)
        {
            pCellDefinition = null;
            BattleScenarioDefinition lScenario = BuildScenario();
            if (lScenario == null)
                return false;

            foreach (CellDefinition lCell in lScenario.EnumerateCells())
            {
                if (lCell != null && lCell.Coordinate.ToRuntime() == pRuntimeCoord)
                {
                    pCellDefinition = lCell.Clone();
                    return true;
                }
            }

            return false;
        }

        public bool TryGetCellMetadata(GridCoord pRuntimeCoord, out CellMetadata pMetadata)
        {
            pMetadata = null;
            if (!TryGetAuthoredCell(pRuntimeCoord, out Vector3Int lCellPosition))
                return false;

            for (int lIndex = 0; lIndex < _CellMetadata.Count; lIndex++)
            {
                CellMetadata lMetadata = _CellMetadata[lIndex];
                if (lMetadata == null || lMetadata.Coordinate.X != lCellPosition.x || lMetadata.Coordinate.Y != lCellPosition.y)
                    continue;

                lMetadata.Sanitize(Mathf.Max(1, _Scenario.DefaultMovementCost));
                pMetadata = lMetadata.Clone();
                return true;
            }

            return false;
        }

        public void SetCellMetadata(CellMetadata pMetadata)
        {
            if (pMetadata == null)
                return;

            int lDefaultMovementCost = Mathf.Max(1, _Scenario.DefaultMovementCost);
            pMetadata.Sanitize(lDefaultMovementCost);
            Vector3Int lCell = pMetadata.ToCellPosition();

            for (int lIndex = 0; lIndex < _CellMetadata.Count; lIndex++)
            {
                CellMetadata lExisting = _CellMetadata[lIndex];
                if (lExisting == null || lExisting.Coordinate.X != lCell.x || lExisting.Coordinate.Y != lCell.y)
                    continue;

                if (pMetadata.IsDefault(lDefaultMovementCost))
                    _CellMetadata.RemoveAt(lIndex);
                else
                    _CellMetadata[lIndex] = pMetadata.Clone();

                InvalidateRuntimeScenario();
                return;
            }

            if (!pMetadata.IsDefault(lDefaultMovementCost))
                _CellMetadata.Add(pMetadata.Clone());

            InvalidateRuntimeScenario();
        }

        public void RemoveMetadata(Vector3Int pCellPosition)
        {
            for (int lIndex = _CellMetadata.Count - 1; lIndex >= 0; lIndex--)
            {
                CellMetadata lMetadata = _CellMetadata[lIndex];
                if (lMetadata != null && lMetadata.Coordinate.X == pCellPosition.x && lMetadata.Coordinate.Y == pCellPosition.y)
                    _CellMetadata.RemoveAt(lIndex);
            }

            InvalidateRuntimeScenario();
        }

        public void RemoveMetadataForMissingTiles()
        {
            if (_GroundTilemap == null)
                return;

            for (int lIndex = _CellMetadata.Count - 1; lIndex >= 0; lIndex--)
            {
                CellMetadata lMetadata = _CellMetadata[lIndex];
                if (lMetadata == null || !_GroundTilemap.HasTile(lMetadata.ToCellPosition()))
                    _CellMetadata.RemoveAt(lIndex);
            }

            InvalidateRuntimeScenario();
        }

        #endregion

        #region _____________________________| HELPERS

        private CellMetadata GetCellMetadataOrDefault(Vector3Int pCellPosition, int pDefaultMovementCost)
        {
            for (int lIndex = 0; lIndex < _CellMetadata.Count; lIndex++)
            {
                CellMetadata lMetadata = _CellMetadata[lIndex];
                if (lMetadata == null)
                    continue;

                if (lMetadata.Coordinate.X != pCellPosition.x || lMetadata.Coordinate.Y != pCellPosition.y)
                    continue;

                lMetadata.Sanitize(pDefaultMovementCost);
                return lMetadata.Clone();
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

        private void CacheMissingReferences()
        {
            _Grid ??= GetComponentInChildren<Grid>(true);
            if (_GroundTilemap == null && _Grid != null)
                _GroundTilemap = _Grid.GetComponentInChildren<Tilemap>(true);
        }

        private void SanitizeMetadata()
        {
            int lDefaultMovementCost = Mathf.Max(1, _Scenario.DefaultMovementCost);
            for (int lIndex = _CellMetadata.Count - 1; lIndex >= 0; lIndex--)
            {
                if (_CellMetadata[lIndex] == null)
                {
                    _CellMetadata.RemoveAt(lIndex);
                    continue;
                }

                _CellMetadata[lIndex].Sanitize(lDefaultMovementCost);
            }
        }

        #endregion
    }
}
