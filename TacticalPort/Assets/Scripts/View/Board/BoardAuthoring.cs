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
        private BoundsInt _CachedPaintedBounds;
        private bool _HasCachedPaintedBounds;
#if UNITY_EDITOR
        private bool _HasScheduledBoardRebuild;
#endif

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
            ClearRuntimeScenarioCache();

#if UNITY_EDITOR
            if (!Application.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
                ScheduleBoardRebuild();
#endif
        }

        #endregion

        #region _____________________________| BUILD

        public BattleScenarioDefinition BuildScenario()
        {
            if (_RuntimeScenario != null)
                return _RuntimeScenario;

            if (!TryGetPaintedBounds(out BoundsInt lBounds))
                return null;

            _RuntimeScenario = BoardScenarioBuilder.Build(_Scenario, _GroundTilemap, lBounds, _CellMetadata);
            return _RuntimeScenario;
        }

        #endregion

        #region _____________________________| METADATA

        public void InvalidateRuntimeScenario()
        {
            ClearRuntimeScenarioCache();

            BoardView lBoardView = GetComponent<BoardView>();
            if (lBoardView != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    ScheduleBoardRebuild();
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

            if (Application.isPlaying && _HasCachedPaintedBounds)
            {
                pBounds = _CachedPaintedBounds;
                return true;
            }

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
            if (Application.isPlaying)
            {
                _CachedPaintedBounds = pBounds;
                _HasCachedPaintedBounds = true;
            }

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

        private void CacheMissingReferences()
        {
            _Grid ??= GetComponentInChildren<Grid>(true);
            if (_GroundTilemap == null && _Grid != null)
                _GroundTilemap = _Grid.GetComponentInChildren<Tilemap>(true);
        }

        private void ClearRuntimeScenarioCache()
        {
            _RuntimeScenario = null;
            _HasCachedPaintedBounds = false;
        }

#if UNITY_EDITOR
        private void ScheduleBoardRebuild()
        {
            if (_HasScheduledBoardRebuild)
                return;

            _HasScheduledBoardRebuild = true;
            EditorApplication.delayCall += () =>
            {
                _HasScheduledBoardRebuild = false;

                if (this == null || Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                    return;

                BoardView lBoardView = GetComponent<BoardView>();
                if (lBoardView != null)
                    lBoardView.RebuildBoard();
            };
        }
#endif

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
