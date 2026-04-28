#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Core.Runtime;
using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TacticalPort.View
{
    public sealed class BoardView : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [Header("Display")]
        [SerializeField] private int _SortingStep = 10;
        [SerializeField] private int _PreviewSortingOffset = 8;
        [SerializeField] private Transform _SkillPreviewRoot;

        [Header("Cell State Prefabs")]
        [SerializeField] private GameObject _SpawnerPreviewPrefab;
        [SerializeField] private GameObject _PlayerOccupiedPreviewPrefab;
        [SerializeField] private GameObject _EnemyOccupiedPreviewPrefab;
        [SerializeField] private GameObject _ActiveOccupiedPreviewPrefab;
        [SerializeField] private GameObject _HoverPreviewPrefab;
        [SerializeField] private GameObject _MoveRangePreviewPrefab;
        [SerializeField] private GameObject _AreaPreviewPrefab;
        [SerializeField] private GameObject _GlyphPreviewPrefab;
        [SerializeField] private GameObject _TelegraphPreviewPrefab;
        [SerializeField] private GameObject _SkillRangePreviewPrefab;
        [SerializeField] private GameObject _SkillBlockedRangePreviewPrefab;

        private enum OccupiedCellVisualState
        {
            None,
            Player,
            Enemy,
            Active
        }

        private readonly Dictionary<GridCoord, CellDefinition> _CellsByCoord = new Dictionary<GridCoord, CellDefinition>();
        private readonly Dictionary<GridCoord, Vector3Int> _TilePositionsByCoord = new Dictionary<GridCoord, Vector3Int>();
        private readonly Dictionary<GridCoord, OccupiedCellVisualState> _OccupiedStatesByCoord = new Dictionary<GridCoord, OccupiedCellVisualState>();
        private readonly Dictionary<GridCoord, GameObject> _SpawnerMarkersByCoord = new Dictionary<GridCoord, GameObject>();
        private readonly Dictionary<GridCoord, GameObject> _PlayerOccupiedMarkersByCoord = new Dictionary<GridCoord, GameObject>();
        private readonly Dictionary<GridCoord, GameObject> _EnemyOccupiedMarkersByCoord = new Dictionary<GridCoord, GameObject>();
        private readonly Dictionary<GridCoord, GameObject> _ActiveOccupiedMarkersByCoord = new Dictionary<GridCoord, GameObject>();
        private readonly Dictionary<GridCoord, GameObject> _HoverMarkersByCoord = new Dictionary<GridCoord, GameObject>();
        private readonly Dictionary<GridCoord, GameObject> _MoveRangeMarkersByCoord = new Dictionary<GridCoord, GameObject>();
        private readonly Dictionary<GridCoord, GameObject> _AreaPreviewMarkersByCoord = new Dictionary<GridCoord, GameObject>();
        private readonly Dictionary<GridCoord, GameObject> _GlyphMarkersByCoord = new Dictionary<GridCoord, GameObject>();
        private readonly Dictionary<GridCoord, GameObject> _TelegraphMarkersByCoord = new Dictionary<GridCoord, GameObject>();
        private readonly Dictionary<GridCoord, GameObject> _SkillRangeMarkersByCoord = new Dictionary<GridCoord, GameObject>();
        private readonly Dictionary<GridCoord, GameObject> _BlockedSkillRangeMarkersByCoord = new Dictionary<GridCoord, GameObject>();
        private readonly HashSet<GridCoord> _SpawnerCells = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _ReachableCells = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _SkillReachableCells = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _BlockedSkillReachableCells = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _PreviewCells = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _GlyphCells = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _TelegraphCells = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _HoveredCells = new HashSet<GridCoord>();
        private BattleScenarioDefinition _Scenario;
        private BoardAuthoring _TilemapBoardAuthoring;
        private bool _ShowSpawnerCells = true;
        private Tilemap _Tilemap;
        private bool _HasHoveredCell;
        private GridCoord _HoveredCell;

        #endregion

        #region _____________________________/ ACCESSORS

        public BattleScenarioDefinition Scenario => _Scenario;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            CacheMissingReferences();
            RebuildBoard();
        }

        private void OnValidate()
        {
            CacheMissingReferences();
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorApplication.delayCall += () =>
                {
                    if (this == null)
                        return;

                    RebuildBoard();
                };
                return;
            }
#endif
            RebuildBoard();
        }

        #endregion

        #region _____________________________| CONFIGURE

        public void Configure(BattleScenarioDefinition pValue)
        {
            _Scenario = pValue;
            RebuildBoard();
        }

        public Vector3 GetWorldPosition(GridCoord pCoord)
        {
            return _TilemapBoardAuthoring != null
                ? _TilemapBoardAuthoring.GetWorldPosition(pCoord)
                : Vector3.zero;
        }

        public bool TryGetGridCoord(Vector3 pWorldPosition, out GridCoord pCoord)
        {
            pCoord = default;

            if (_TilemapBoardAuthoring != null && _TilemapBoardAuthoring.TryGetGridCoord(pWorldPosition, out pCoord))
                return true;

            return false;
        }

        public bool ContainsCell(GridCoord pCoord) => _CellsByCoord.ContainsKey(pCoord);

        public bool IsSpawnerCell(GridCoord pCoord) => _SpawnerCells.Contains(pCoord);

        public IReadOnlyCollection<GridCoord> GetSpawnerCells() => new List<GridCoord>(_SpawnerCells);

        public void SetSpawnerCellsVisible(bool pVisible)
        {
            if (_ShowSpawnerCells == pVisible)
                return;

            _ShowSpawnerCells = pVisible;
            RefreshCellStates();
        }

        public bool TryGetCellDefinition(GridCoord pCoord, out CellDefinition pCellDefinition)
        {
            if (_CellsByCoord.TryGetValue(pCoord, out CellDefinition lCell))
            {
                pCellDefinition = lCell;
                return true;
            }

            pCellDefinition = null;
            return false;
        }

        public void SetReachableCells(IReadOnlyCollection<GridCoord> pReachableCells)
        {
            _ReachableCells.Clear();
            if (pReachableCells != null)
            {
                foreach (GridCoord lCoord in pReachableCells)
                {
                    if (_CellsByCoord.ContainsKey(lCoord))
                        _ReachableCells.Add(lCoord);
                }
            }

            RefreshCellStates();
        }

        public void SetSkillRangeCells(IReadOnlyCollection<GridCoord> pReachableCells, IReadOnlyCollection<GridCoord> pBlockedReachableCells)
        {
            _SkillReachableCells.Clear();
            _BlockedSkillReachableCells.Clear();

            if (pReachableCells != null)
            {
                foreach (GridCoord lCoord in pReachableCells)
                {
                    if (_CellsByCoord.ContainsKey(lCoord))
                        _SkillReachableCells.Add(lCoord);
                }
            }

            if (pBlockedReachableCells != null)
            {
                foreach (GridCoord lCoord in pBlockedReachableCells)
                {
                    if (_CellsByCoord.ContainsKey(lCoord))
                        _BlockedSkillReachableCells.Add(lCoord);
                }
            }

            RefreshCellStates();
        }

        public void SetOccupiedCells(IReadOnlyCollection<UnitRuntime> pUnits, UnitId pActiveUnitId)
        {
            _OccupiedStatesByCoord.Clear();
            if (pUnits != null)
            {
                foreach (UnitRuntime lUnit in pUnits)
                {
                    if (lUnit == null || !lUnit.IsAlive)
                        continue;

                    OccupiedCellVisualState lState = ResolveOccupiedVisualState(lUnit, pActiveUnitId);
                    foreach (GridCoord lCell in lUnit.EnumerateOccupiedCells())
                        _OccupiedStatesByCoord[lCell] = lState;
                }
            }

            RefreshCellStates();
        }

        public void SetPreviewCells(IReadOnlyCollection<GridCoord> pPreviewCells)
        {
            _PreviewCells.Clear();
            if (pPreviewCells != null)
            {
                foreach (GridCoord lCoord in pPreviewCells)
                {
                    if (_CellsByCoord.ContainsKey(lCoord))
                        _PreviewCells.Add(lCoord);
                }
            }

            RefreshCellStates();
        }

        public void SetGlyphCells(IReadOnlyCollection<GridGlyphRuntime> pGlyphs)
        {
            _GlyphCells.Clear();
            if (pGlyphs != null)
            {
                foreach (GridGlyphRuntime lGlyph in pGlyphs)
                {
                    if (lGlyph != null && _CellsByCoord.ContainsKey(lGlyph.Cell))
                        _GlyphCells.Add(lGlyph.Cell);
                }
            }

            RefreshCellStates();
        }

        public void SetHazardCells(IReadOnlyCollection<TelegraphedHazardRuntime> pHazards)
        {
            _TelegraphCells.Clear();
            if (pHazards != null)
            {
                foreach (TelegraphedHazardRuntime lHazard in pHazards)
                {
                    if (lHazard?.Cells == null)
                        continue;

                    for (int lIndex = 0; lIndex < lHazard.Cells.Count; lIndex++)
                    {
                        GridCoord lCell = lHazard.Cells[lIndex];
                        if (_CellsByCoord.ContainsKey(lCell))
                            _TelegraphCells.Add(lCell);
                    }
                }
            }

            RefreshCellStates();
        }

        public void SetHoveredCell(GridCoord pCoord)
        {
            _HasHoveredCell = true;
            _HoveredCell = pCoord;
            RefreshCellStates();
        }

        public void ClearHoveredCell()
        {
            if (!_HasHoveredCell)
                return;

            _HasHoveredCell = false;
            RefreshCellStates();
        }

        public void RebuildBoard()
        {
            ClearBoardData();
            CacheMissingReferences();

            TryBuildTilemapBoard();
            RefreshCellStates();
        }

        #endregion

        #region _____________________________| HELPERS

        private void ClearBoardData()
        {
            _CellsByCoord.Clear();
            _TilePositionsByCoord.Clear();
            _OccupiedStatesByCoord.Clear();
            _SpawnerCells.Clear();
            _ReachableCells.Clear();
            _SkillReachableCells.Clear();
            _BlockedSkillReachableCells.Clear();
            _PreviewCells.Clear();
            _GlyphCells.Clear();
            _TelegraphCells.Clear();
            _HoveredCells.Clear();
            _HasHoveredCell = false;
            _Tilemap = null;
            ClearPreviewMarkers(_SpawnerMarkersByCoord);
            ClearPreviewMarkers(_PlayerOccupiedMarkersByCoord);
            ClearPreviewMarkers(_EnemyOccupiedMarkersByCoord);
            ClearPreviewMarkers(_ActiveOccupiedMarkersByCoord);
            ClearPreviewMarkers(_HoverMarkersByCoord);
            ClearPreviewMarkers(_MoveRangeMarkersByCoord);
            ClearPreviewMarkers(_AreaPreviewMarkersByCoord);
            ClearPreviewMarkers(_GlyphMarkersByCoord);
            ClearPreviewMarkers(_TelegraphMarkersByCoord);
            ClearPreviewMarkers(_SkillRangeMarkersByCoord);
            ClearPreviewMarkers(_BlockedSkillRangeMarkersByCoord);
        }

        private void CacheMissingReferences()
        {
            _SkillPreviewRoot ??= transform;
            _TilemapBoardAuthoring ??= GetComponent<BoardAuthoring>() ?? GetComponentInChildren<BoardAuthoring>(true);
        }

        private void RefreshCellStates()
        {
            RefreshSpawnerMarkers();
            RefreshOccupiedMarkers();
            RefreshHoverMarkers();
            RefreshMovementMarkers();
            RefreshAreaPreviewMarkers();
            RefreshGlyphMarkers();
            RefreshTelegraphMarkers();
            RefreshSkillRangeMarkers();
        }

        private OccupiedCellVisualState ResolveOccupiedVisualState(UnitRuntime pUnit, UnitId pActiveUnitId)
        {
            if (pUnit != null && pUnit.Id == pActiveUnitId)
                return OccupiedCellVisualState.Active;

            return pUnit != null && pUnit.Team == Team.Enemy
                ? OccupiedCellVisualState.Enemy
                : OccupiedCellVisualState.Player;
        }

        private bool TryBuildTilemapBoard()
        {
            if (_TilemapBoardAuthoring == null || _TilemapBoardAuthoring.FloorTilemap == null)
                return false;

            _Tilemap = _TilemapBoardAuthoring.FloorTilemap;
            foreach (GridCoord lCoord in _TilemapBoardAuthoring.EnumeratePaintedCoordinates())
            {
                if (!_TilemapBoardAuthoring.TryGetCellDefinition(lCoord, out CellDefinition lCellDefinition))
                    continue;
                if (!_TilemapBoardAuthoring.TryGetAuthoredCell(lCoord, out Vector3Int lTilePosition))
                    continue;

                _CellsByCoord[lCoord] = lCellDefinition;
                if (_TilemapBoardAuthoring.TryGetCellMetadata(lCoord, out CellMetadata lMetadata) && lMetadata != null && lMetadata.IsSpawner)
                    _SpawnerCells.Add(lCoord);
#if UNITY_EDITOR
                if (Application.isPlaying)
                    _Tilemap.SetTileFlags(lTilePosition, TileFlags.None);
#else
                _Tilemap.SetTileFlags(lTilePosition, TileFlags.None);
#endif
                _TilePositionsByCoord[lCoord] = lTilePosition;
            }

            return _CellsByCoord.Count > 0;
        }

        private void RefreshSpawnerMarkers()
        {
            if (!_ShowSpawnerCells)
            {
                ClearPreviewMarkers(_SpawnerMarkersByCoord);
                return;
            }

            SyncPreviewMarkers(_SpawnerMarkersByCoord, _SpawnerCells, _SpawnerPreviewPrefab);
        }

        private void RefreshOccupiedMarkers()
        {
            SyncPreviewMarkers(_PlayerOccupiedMarkersByCoord, GetOccupiedCells(OccupiedCellVisualState.Player), _PlayerOccupiedPreviewPrefab);
            SyncPreviewMarkers(_EnemyOccupiedMarkersByCoord, GetOccupiedCells(OccupiedCellVisualState.Enemy), _EnemyOccupiedPreviewPrefab);
            SyncPreviewMarkers(_ActiveOccupiedMarkersByCoord, GetOccupiedCells(OccupiedCellVisualState.Active), _ActiveOccupiedPreviewPrefab);
        }

        private void RefreshHoverMarkers()
        {
            _HoveredCells.Clear();
            if (_HasHoveredCell && _CellsByCoord.ContainsKey(_HoveredCell))
                _HoveredCells.Add(_HoveredCell);

            SyncPreviewMarkers(_HoverMarkersByCoord, _HoveredCells, _HoverPreviewPrefab);
        }

        private void RefreshMovementMarkers()
        {
            SyncPreviewMarkers(_MoveRangeMarkersByCoord, _ReachableCells, _MoveRangePreviewPrefab);
        }

        private void RefreshAreaPreviewMarkers()
        {
            SyncPreviewMarkers(_AreaPreviewMarkersByCoord, _PreviewCells, _AreaPreviewPrefab);
        }

        private void RefreshGlyphMarkers()
        {
            SyncPreviewMarkers(_GlyphMarkersByCoord, _GlyphCells, _GlyphPreviewPrefab);
        }

        private void RefreshTelegraphMarkers()
        {
            SyncPreviewMarkers(_TelegraphMarkersByCoord, _TelegraphCells, _TelegraphPreviewPrefab);
        }

        private void RefreshSkillRangeMarkers()
        {
            SyncPreviewMarkers(_SkillRangeMarkersByCoord, _SkillReachableCells, _SkillRangePreviewPrefab);
            SyncPreviewMarkers(_BlockedSkillRangeMarkersByCoord, _BlockedSkillReachableCells, _SkillBlockedRangePreviewPrefab);
        }

        private List<GridCoord> GetOccupiedCells(OccupiedCellVisualState pState)
        {
            List<GridCoord> lCells = new List<GridCoord>();
            foreach (KeyValuePair<GridCoord, OccupiedCellVisualState> lEntry in _OccupiedStatesByCoord)
            {
                if (lEntry.Value == pState)
                    lCells.Add(lEntry.Key);
            }

            return lCells;
        }

        private void SyncPreviewMarkers(
            IDictionary<GridCoord, GameObject> pRuntimeMarkers,
            IReadOnlyCollection<GridCoord> pCoords,
            GameObject pPrefab)
        {
            if (pRuntimeMarkers == null)
                return;

            if (pPrefab == null)
            {
                ClearPreviewMarkers(pRuntimeMarkers);
                return;
            }

            HashSet<GridCoord> lTargetCoords = new HashSet<GridCoord>();
            if (pCoords != null)
            {
                foreach (GridCoord lCoord in pCoords)
                    lTargetCoords.Add(lCoord);
            }

            List<GridCoord> lToRemove = new List<GridCoord>();
            foreach (KeyValuePair<GridCoord, GameObject> lEntry in pRuntimeMarkers)
            {
                if (!lTargetCoords.Contains(lEntry.Key))
                    lToRemove.Add(lEntry.Key);
            }

            for (int lIndex = 0; lIndex < lToRemove.Count; lIndex++)
            {
                GridCoord lCoord = lToRemove[lIndex];
                if (!pRuntimeMarkers.TryGetValue(lCoord, out GameObject lMarker))
                    continue;

                if (lMarker != null)
                {
                    if (Application.isPlaying)
                        Destroy(lMarker);
                    else
                        DestroyImmediate(lMarker);
                }

                pRuntimeMarkers.Remove(lCoord);
            }

            foreach (GridCoord lCoord in lTargetCoords)
            {
                if (!pRuntimeMarkers.TryGetValue(lCoord, out GameObject lMarker) || lMarker == null)
                {
                    lMarker = Instantiate(pPrefab, _SkillPreviewRoot != null ? _SkillPreviewRoot : transform);
                    pRuntimeMarkers[lCoord] = lMarker;
                }

                lMarker.transform.position = GetWorldPosition(lCoord);
                ApplyPreviewMarkerSorting(lMarker, lCoord);
                lMarker.SetActive(true);
            }
        }

        private void ClearPreviewMarkers(IDictionary<GridCoord, GameObject> pRuntimeMarkers)
        {
            if (pRuntimeMarkers == null)
                return;

            foreach (GameObject lMarker in pRuntimeMarkers.Values)
            {
                if (lMarker == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(lMarker);
                else
                    DestroyImmediate(lMarker);
            }

            pRuntimeMarkers.Clear();
        }

        private void ApplyPreviewMarkerSorting(GameObject pMarker, GridCoord pCoord)
        {
            if (pMarker == null)
                return;

            int lSortingOrder = -((pCoord.X + pCoord.Y) * Mathf.Max(1, _SortingStep)) + _PreviewSortingOffset;
            int lSortingLayerId = ResolveBoardSortingLayerId();

            SortingGroup[] lSortingGroups = pMarker.GetComponentsInChildren<SortingGroup>(true);
            for (int lIndex = 0; lIndex < lSortingGroups.Length; lIndex++)
            {
                lSortingGroups[lIndex].sortingLayerID = lSortingLayerId;
                lSortingGroups[lIndex].sortingOrder = lSortingOrder;
            }

            SpriteRenderer[] lRenderers = pMarker.GetComponentsInChildren<SpriteRenderer>(true);
            for (int lIndex = 0; lIndex < lRenderers.Length; lIndex++)
            {
                lRenderers[lIndex].sortingLayerID = lSortingLayerId;
                lRenderers[lIndex].sortingOrder = lSortingOrder;
            }
        }

        private int ResolveBoardSortingLayerId()
        {
            if (_Tilemap != null)
            {
                TilemapRenderer lTilemapRenderer = _Tilemap.GetComponent<TilemapRenderer>();
                if (lTilemapRenderer != null)
                    return lTilemapRenderer.sortingLayerID;
            }

            return 0;
        }

        #endregion
    }
}
