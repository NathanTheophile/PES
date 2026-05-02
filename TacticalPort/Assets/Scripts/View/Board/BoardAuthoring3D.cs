#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TacticalPort.View
{
    public sealed class BoardAuthoring3D : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private SceneScenarioDefinition _Scenario = new SceneScenarioDefinition();
        [SerializeField] private Transform _TilesRoot;
        [SerializeField, Min(0.01f)] private float _CellSize = 1f;
        [Tooltip("Optional offset from a tile pivot to the gameplay surface. Keep zero when tile pivots are authored on the top surface.")]
        [SerializeField] private Vector3 _CellWorldOffset = Vector3.zero;
        [SerializeField, Min(0.01f)] private float _RaycastDistance = 1000f;
        [SerializeField] private LayerMask _TileRaycastMask = ~0;
        [SerializeField] private QueryTriggerInteraction _RaycastTriggerInteraction = QueryTriggerInteraction.Ignore;

        private readonly Dictionary<Vector2Int, BoardTileAuthoring> _TilesByAuthoredCoordinate = new Dictionary<Vector2Int, BoardTileAuthoring>();
        private readonly Dictionary<GridCoord, BoardTileAuthoring> _TilesByRuntimeCoordinate = new Dictionary<GridCoord, BoardTileAuthoring>();
        private readonly Dictionary<BoardTileAuthoring, GridCoord> _RuntimeCoordinatesByTile = new Dictionary<BoardTileAuthoring, GridCoord>();
        private readonly Dictionary<GridCoord, CellDefinition> _CellDefinitionsByCoordinate = new Dictionary<GridCoord, CellDefinition>();
        private BattleScenarioDefinition _RuntimeScenario;
        private RectInt _CachedBounds;
        private bool _HasCachedBoard;
#if UNITY_EDITOR
        private bool _HasScheduledBoardRebuild;
#endif

        #endregion

        #region _____________________________/ ACCESSORS

        public int DefaultMovementCost => Mathf.Max(1, _Scenario != null ? _Scenario.DefaultMovementCost : 1);
        public bool HasTiles
        {
            get
            {
                EnsureBoardCache();
                return _TilesByRuntimeCoordinate.Count > 0;
            }
        }

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            CacheMissingReferences();
            EnsureBoardCache();
        }

        private void OnValidate()
        {
            _CellSize = Mathf.Max(0.01f, _CellSize);
            _RaycastDistance = Mathf.Max(0.01f, _RaycastDistance);
            CacheMissingReferences();
            ClearBoardCache();

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

            EnsureBoardCache();
            if (!_HasCachedBoard)
                return null;

            _RuntimeScenario = BoardScenarioBuilder3D.Build(_Scenario, _TilesByAuthoredCoordinate, _CachedBounds);
            CacheCellDefinitions(_RuntimeScenario);
            return _RuntimeScenario;
        }

        public void InvalidateRuntimeScenario()
        {
            ClearBoardCache();

            BoardView lBoardView = GetComponent<BoardView>()
                ?? GetComponentInParent<BoardView>()
                ?? GetComponentInChildren<BoardView>(true);
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

        #endregion

        #region _____________________________| QUERY

        public IEnumerable<GridCoord> EnumerateAuthoredCoordinates()
        {
            EnsureBoardCache();
            foreach (GridCoord lCoord in _TilesByRuntimeCoordinate.Keys)
                yield return lCoord;
        }

        public bool ContainsCell(GridCoord pRuntimeCoord)
        {
            EnsureBoardCache();
            return _TilesByRuntimeCoordinate.ContainsKey(pRuntimeCoord);
        }

        public bool TryGetCellDefinition(GridCoord pRuntimeCoord, out CellDefinition pCellDefinition)
        {
            pCellDefinition = null;
            BuildScenario();
            if (_CellDefinitionsByCoordinate.TryGetValue(pRuntimeCoord, out CellDefinition lDefinition))
            {
                pCellDefinition = lDefinition.Clone();
                return true;
            }

            return false;
        }

        public bool TryGetWorldPosition(GridCoord pRuntimeCoord, out Vector3 pWorldPosition)
        {
            pWorldPosition = default;
            EnsureBoardCache();

            if (!_TilesByRuntimeCoordinate.TryGetValue(pRuntimeCoord, out BoardTileAuthoring lTile) || lTile == null)
                return false;

            pWorldPosition = lTile.transform.position + _CellWorldOffset;
            return true;
        }

        public Vector3 GetWorldPosition(GridCoord pRuntimeCoord) =>
            TryGetWorldPosition(pRuntimeCoord, out Vector3 lWorldPosition) ? lWorldPosition : Vector3.zero;

        public bool TryGetGridCoord(Vector3 pWorldPosition, out GridCoord pCoord)
        {
            pCoord = default;
            EnsureBoardCache();

            Vector3 lLocalPosition = transform.InverseTransformPoint(pWorldPosition);
            Vector2Int lAuthoredCoord = new Vector2Int(
                Mathf.RoundToInt(lLocalPosition.x / _CellSize),
                Mathf.RoundToInt(lLocalPosition.z / _CellSize));

            if (!_TilesByAuthoredCoordinate.TryGetValue(lAuthoredCoord, out BoardTileAuthoring lTile) || lTile == null)
                return false;

            return TryGetRuntimeCoord(lTile, out pCoord);
        }

        public bool TryGetGridCoord(Ray pRay, out GridCoord pCoord)
        {
            pCoord = default;

            if (Physics.Raycast(pRay, out RaycastHit lHit, _RaycastDistance, _TileRaycastMask, _RaycastTriggerInteraction))
            {
                BoardTileAuthoring lTile = lHit.collider.GetComponentInParent<BoardTileAuthoring>();
                if (lTile != null && lTile.transform.IsChildOf(ResolveTilesRoot()))
                    return TryGetRuntimeCoord(lTile, out pCoord);
            }

            Plane lBoardPlane = new Plane(transform.up, transform.position);
            if (!lBoardPlane.Raycast(pRay, out float lDistance))
                return false;

            return TryGetGridCoord(pRay.GetPoint(lDistance), out pCoord);
        }

        public bool TryGetRuntimeCoord(BoardTileAuthoring pTile, out GridCoord pCoord)
        {
            pCoord = default;
            EnsureBoardCache();
            return pTile != null && _RuntimeCoordinatesByTile.TryGetValue(pTile, out pCoord);
        }

        public bool TryGetTile(GridCoord pRuntimeCoord, out BoardTileAuthoring pTile)
        {
            EnsureBoardCache();
            return _TilesByRuntimeCoordinate.TryGetValue(pRuntimeCoord, out pTile);
        }

        #endregion

        #region _____________________________| HELPERS

        private void CacheMissingReferences()
        {
            _TilesRoot ??= transform;
        }

        private Transform ResolveTilesRoot() => _TilesRoot != null ? _TilesRoot : transform;

        private void EnsureBoardCache()
        {
            if (_HasCachedBoard)
                return;

            RebuildBoardCache();
        }

        private void ClearBoardCache()
        {
            _RuntimeScenario = null;
            _HasCachedBoard = false;
            _TilesByAuthoredCoordinate.Clear();
            _TilesByRuntimeCoordinate.Clear();
            _RuntimeCoordinatesByTile.Clear();
            _CellDefinitionsByCoordinate.Clear();
        }

        private void RebuildBoardCache()
        {
            _RuntimeScenario = null;
            _TilesByAuthoredCoordinate.Clear();
            _TilesByRuntimeCoordinate.Clear();
            _RuntimeCoordinatesByTile.Clear();
            _CellDefinitionsByCoordinate.Clear();

            BoardTileAuthoring[] lTiles = ResolveTilesRoot().GetComponentsInChildren<BoardTileAuthoring>(true);
            if (lTiles == null || lTiles.Length == 0)
            {
                _HasCachedBoard = false;
                return;
            }

            bool lHasTile = false;
            int lMinX = 0;
            int lMinY = 0;
            int lMaxX = 0;
            int lMaxY = 0;

            for (int lIndex = 0; lIndex < lTiles.Length; lIndex++)
            {
                BoardTileAuthoring lTile = lTiles[lIndex];
                if (lTile == null)
                    continue;

                Vector2Int lAuthoredCoord = ResolveAuthoredCoord(lTile.transform.position);
                if (_TilesByAuthoredCoordinate.ContainsKey(lAuthoredCoord))
                {
                    Debug.LogWarning($"Duplicate board tile at authored coordinate {lAuthoredCoord}. Tile '{lTile.name}' is ignored.", lTile);
                    continue;
                }

                _TilesByAuthoredCoordinate[lAuthoredCoord] = lTile;

                if (!lHasTile)
                {
                    lMinX = lMaxX = lAuthoredCoord.x;
                    lMinY = lMaxY = lAuthoredCoord.y;
                    lHasTile = true;
                    continue;
                }

                lMinX = Mathf.Min(lMinX, lAuthoredCoord.x);
                lMinY = Mathf.Min(lMinY, lAuthoredCoord.y);
                lMaxX = Mathf.Max(lMaxX, lAuthoredCoord.x);
                lMaxY = Mathf.Max(lMaxY, lAuthoredCoord.y);
            }

            if (!lHasTile)
            {
                _HasCachedBoard = false;
                return;
            }

            _CachedBounds = new RectInt(lMinX, lMinY, lMaxX - lMinX + 1, lMaxY - lMinY + 1);
            foreach (KeyValuePair<Vector2Int, BoardTileAuthoring> lEntry in _TilesByAuthoredCoordinate)
            {
                GridCoord lRuntimeCoord = new GridCoord(lEntry.Key.x - _CachedBounds.xMin, lEntry.Key.y - _CachedBounds.yMin);
                _TilesByRuntimeCoordinate[lRuntimeCoord] = lEntry.Value;
                _RuntimeCoordinatesByTile[lEntry.Value] = lRuntimeCoord;
            }

            _HasCachedBoard = true;
        }

        private Vector2Int ResolveAuthoredCoord(Vector3 pWorldPosition)
        {
            Vector3 lLocalPosition = transform.InverseTransformPoint(pWorldPosition);
            return new Vector2Int(
                Mathf.RoundToInt(lLocalPosition.x / _CellSize),
                Mathf.RoundToInt(lLocalPosition.z / _CellSize));
        }

        private void CacheCellDefinitions(BattleScenarioDefinition pScenario)
        {
            _CellDefinitionsByCoordinate.Clear();
            if (pScenario == null)
                return;

            foreach (CellDefinition lCell in pScenario.EnumerateCells())
            {
                if (lCell != null)
                    _CellDefinitionsByCoordinate[lCell.Coordinate.ToRuntime()] = lCell;
            }
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

        #endregion
    }
}
