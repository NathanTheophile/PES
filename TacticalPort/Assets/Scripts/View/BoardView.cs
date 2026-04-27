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
        [Header("Layout")]
        [SerializeField] private Vector3 _Origin;
        [SerializeField] private Vector2 _CellWorldSize = new Vector2(1f, 0.5f);
        [SerializeField] private int _SortingStep = 10;
        [SerializeField] private int _PreviewSortingOffset = 8;

        [Header("Display")]
        [SerializeField] private Transform _CellRoot;
        [SerializeField] private Transform _SkillPreviewRoot;
        [SerializeField] private Color _WalkableColor = Color.white;
        [SerializeField] private Color _BlockedColor = Color.gray;
        [SerializeField] private Color _SpawnerColor = new Color(0.26f, 0.74f, 0.34f, 1f);
        [SerializeField] private Color _PlayerOccupiedColor = new Color(0.27f, 0.47f, 0.85f, 1f);
        [SerializeField] private Color _EnemyOccupiedColor = new Color(0.86f, 0.27f, 0.27f, 1f);
        [SerializeField] private Color _ActiveOccupiedColor = new Color(1f, 0.62f, 0.12f, 1f);
        [SerializeField] private Color _ReachableColor = new Color(0.21f, 0.57f, 0.92f, 1f);
        [SerializeField] private Color _BlockedReachableColor = new Color(0.21f, 0.57f, 0.92f, 0.35f);
        [SerializeField] private Color _PreviewColor = new Color(0.94f, 0.58f, 0.2f, 1f);
        [SerializeField] private Color _GlyphColor = new Color(0.86f, 0.23f, 0.23f, 0.85f);
        [SerializeField] private Color _HoveredColor = new Color(0.98f, 0.86f, 0.34f, 1f);
        [SerializeField] private Color _HoveredBlockedColor = new Color(0.78f, 0.44f, 0.28f, 1f);
        [SerializeField] private Color _HoveredReachableColor = new Color(0.37f, 0.9f, 1f, 1f);
        [SerializeField] private Color _HoveredBlockedReachableColor = new Color(0.37f, 0.9f, 1f, 0.6f);
        [SerializeField] private Color _HoveredPreviewColor = new Color(1f, 0.78f, 0.37f, 1f);
        [SerializeField] private Color _HoveredGlyphColor = new Color(0.96f, 0.37f, 0.37f, 1f);
        [SerializeField] private GameObject _MoveRangePreviewPrefab;
        [SerializeField] private GameObject _AreaPreviewPrefab;
        [SerializeField] private GameObject _GlyphPreviewPrefab;
        [SerializeField] private GameObject _SkillRangePreviewPrefab;
        [SerializeField] private GameObject _SkillBlockedRangePreviewPrefab;

        private readonly Dictionary<GridCoord, CellDefinition> _CellsByCoord = new Dictionary<GridCoord, CellDefinition>();
        private readonly Dictionary<GridCoord, SpriteRenderer> _RenderersByCoord = new Dictionary<GridCoord, SpriteRenderer>();
        private readonly Dictionary<GridCoord, Vector3Int> _TilePositionsByCoord = new Dictionary<GridCoord, Vector3Int>();
        private readonly Dictionary<GridCoord, Color> _OccupiedColorsByCoord = new Dictionary<GridCoord, Color>();
        private readonly Dictionary<GridCoord, GameObject> _MoveRangeMarkersByCoord = new Dictionary<GridCoord, GameObject>();
        private readonly Dictionary<GridCoord, GameObject> _AreaPreviewMarkersByCoord = new Dictionary<GridCoord, GameObject>();
        private readonly Dictionary<GridCoord, GameObject> _GlyphMarkersByCoord = new Dictionary<GridCoord, GameObject>();
        private readonly Dictionary<GridCoord, GameObject> _SkillRangeMarkersByCoord = new Dictionary<GridCoord, GameObject>();
        private readonly Dictionary<GridCoord, GameObject> _BlockedSkillRangeMarkersByCoord = new Dictionary<GridCoord, GameObject>();
        private readonly HashSet<GridCoord> _SpawnerCells = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _ReachableCells = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _SkillReachableCells = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _BlockedSkillReachableCells = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _PreviewCells = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _GlyphCells = new HashSet<GridCoord>();
        private BattleScenarioDefinition _Scenario;
        private BoardAuthoring _TilemapBoardAuthoring;
        private Tilemap _Tilemap;
        private bool _HasHoveredCell;
        private GridCoord _HoveredCell;

        public BattleScenarioDefinition Scenario => _Scenario;

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

        public void Configure(BattleScenarioDefinition pValue)
        {
            _Scenario = pValue;
            RebuildBoard();
        }

        public Vector3 GetWorldPosition(GridCoord pCoord)
        {
            if (_TilemapBoardAuthoring != null && _TilemapBoardAuthoring.ContainsCell(pCoord))
                return _TilemapBoardAuthoring.GetWorldPosition(pCoord);

            float lHalfWidth = _CellWorldSize.x * 0.5f;
            float lHalfHeight = _CellWorldSize.y * 0.5f;
            return _Origin + new Vector3((pCoord.X - pCoord.Y) * lHalfWidth, (pCoord.X + pCoord.Y) * lHalfHeight, 0f);
        }

        public bool TryGetGridCoord(Vector3 pWorldPosition, out GridCoord pCoord)
        {
            pCoord = default;

            if (_TilemapBoardAuthoring != null && _TilemapBoardAuthoring.TryGetGridCoord(pWorldPosition, out pCoord))
                return true;

            if (_Scenario == null)
                return false;

            float lHalfWidth = _CellWorldSize.x * 0.5f;
            float lHalfHeight = _CellWorldSize.y * 0.5f;
            if (Mathf.Approximately(lHalfWidth, 0f) || Mathf.Approximately(lHalfHeight, 0f))
                return false;

            Vector3 lLocalPosition = pWorldPosition - _Origin;
            float lNormalizedX = lLocalPosition.x / lHalfWidth;
            float lNormalizedY = lLocalPosition.y / lHalfHeight;
            GridCoord lCandidateCoord = new GridCoord(
                Mathf.RoundToInt((lNormalizedX + lNormalizedY) * 0.5f),
                Mathf.RoundToInt((lNormalizedY - lNormalizedX) * 0.5f));

            if (!_CellsByCoord.ContainsKey(lCandidateCoord))
                return false;

            Vector3 lCellCenter = GetWorldPosition(lCandidateCoord);
            Vector3 lCellOffset = pWorldPosition - lCellCenter;
            float lDiamondDistance = Mathf.Abs(lCellOffset.x) / lHalfWidth + Mathf.Abs(lCellOffset.y) / lHalfHeight;
            if (lDiamondDistance > 1.05f)
                return false;

            pCoord = lCandidateCoord;
            return true;
        }

        public bool ContainsCell(GridCoord pCoord) => _CellsByCoord.ContainsKey(pCoord);

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
            _OccupiedColorsByCoord.Clear();
            if (pUnits != null)
            {
                foreach (UnitRuntime lUnit in pUnits)
                {
                    if (lUnit == null || !lUnit.IsAlive)
                        continue;

                    Color lColor = ResolveOccupiedColor(lUnit, pActiveUnitId);
                    foreach (GridCoord lCell in lUnit.EnumerateOccupiedCells())
                        _OccupiedColorsByCoord[lCell] = lColor;
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

            if (TryBuildTilemapBoard())
            {
                RefreshCellStates();
                return;
            }

            foreach (SceneBoardCell lCell in EnumerateSceneCells())
            {
                if (lCell == null)
                    continue;

                GridCoord lCoord = lCell.GridCoord;
                lCell.transform.position = GetWorldPosition(lCoord);
                _CellsByCoord[lCoord] = new CellDefinition
                {
                    Coordinate = new SerializableGridCoord(lCoord.X, lCoord.Y),
                    IsWalkable = lCell.IsWalkable,
                    BlocksLineOfSight = lCell.BlocksLineOfSight,
                    MovementCost = lCell.MovementCost
                };

                SpriteRenderer lRenderer = lCell.GetComponentInChildren<SpriteRenderer>(true);
                if (lRenderer == null)
                    continue;

                lRenderer.sortingOrder = -((lCoord.X + lCoord.Y) * Mathf.Max(1, _SortingStep));
                _RenderersByCoord[lCoord] = lRenderer;
            }

            RefreshCellStates();
        }

        private void ClearBoardData()
        {
            if (_Tilemap != null)
            {
                foreach (Vector3Int lCell in _TilePositionsByCoord.Values)
                    _Tilemap.SetColor(lCell, Color.white);
            }

            _CellsByCoord.Clear();
            _RenderersByCoord.Clear();
            _TilePositionsByCoord.Clear();
            _OccupiedColorsByCoord.Clear();
            _SpawnerCells.Clear();
            _ReachableCells.Clear();
            _SkillReachableCells.Clear();
            _BlockedSkillReachableCells.Clear();
            _PreviewCells.Clear();
            _GlyphCells.Clear();
            _HasHoveredCell = false;
            _Tilemap = null;
            ClearPreviewMarkers(_MoveRangeMarkersByCoord);
            ClearPreviewMarkers(_AreaPreviewMarkersByCoord);
            ClearPreviewMarkers(_GlyphMarkersByCoord);
            ClearPreviewMarkers(_SkillRangeMarkersByCoord);
            ClearPreviewMarkers(_BlockedSkillRangeMarkersByCoord);
        }

        private void CacheMissingReferences()
        {
            _CellRoot ??= transform;
            _SkillPreviewRoot ??= transform;
            _TilemapBoardAuthoring ??= GetComponent<BoardAuthoring>() ?? GetComponentInChildren<BoardAuthoring>(true);
        }

        private IEnumerable<SceneBoardCell> EnumerateSceneCells()
        {
            Transform lRoot = _CellRoot != null ? _CellRoot : transform;
            return lRoot.GetComponentsInChildren<SceneBoardCell>(true);
        }

        private void RefreshCellStates()
        {
            RefreshMovementMarkers();
            RefreshAreaPreviewMarkers();
            RefreshGlyphMarkers();
            RefreshSkillRangeMarkers();

            if (_Tilemap != null)
            {
                foreach (KeyValuePair<GridCoord, Vector3Int> lEntry in _TilePositionsByCoord)
                {
                    if (_CellsByCoord.TryGetValue(lEntry.Key, out CellDefinition lCell))
                        _Tilemap.SetColor(lEntry.Value, ResolveCellColor(lEntry.Key, lCell));
                }
            }

            foreach (KeyValuePair<GridCoord, SpriteRenderer> lEntry in _RenderersByCoord)
            {
                if (lEntry.Value == null || !_CellsByCoord.TryGetValue(lEntry.Key, out CellDefinition lCell))
                    continue;

                lEntry.Value.color = ResolveCellColor(lEntry.Key, lCell);
            }
        }

        private Color ResolveCellColor(GridCoord pCoord, CellDefinition pCell)
        {
            bool lIsReachable = _ReachableCells.Contains(pCoord);
            bool lIsSkillReachable = _SkillReachableCells.Contains(pCoord);
            bool lIsBlockedReachable = _BlockedSkillReachableCells.Contains(pCoord);
            bool lIsPreviewed = _PreviewCells.Contains(pCoord);
            bool lIsGlyph = _GlyphCells.Contains(pCoord);
            bool lIsHovered = _HasHoveredCell && _HoveredCell == pCoord;
            bool lUsesSkillRangePrefabs = UsesSkillRangePreviewPrefabs();
            bool lUsesMovePreviewPrefab = _MoveRangePreviewPrefab != null;
            bool lUsesAreaPreviewPrefab = _AreaPreviewPrefab != null;
            bool lUsesGlyphPreviewPrefab = _GlyphPreviewPrefab != null;

            if (lIsHovered && lIsReachable)
                return _HoveredReachableColor;
            if (lIsHovered && lIsSkillReachable)
                return _HoveredReachableColor;
            if (lIsHovered && lIsBlockedReachable)
                return _HoveredBlockedReachableColor;
            if (lIsHovered && lIsPreviewed)
                return _HoveredPreviewColor;
            if (lIsHovered && lIsGlyph)
                return _HoveredGlyphColor;
            if (lIsHovered)
                return pCell.IsWalkable ? _HoveredColor : _HoveredBlockedColor;
            if (!lUsesAreaPreviewPrefab && lIsPreviewed)
                return _PreviewColor;
            if (!lUsesMovePreviewPrefab && lIsReachable)
                return _ReachableColor;
            if (!lUsesSkillRangePrefabs && lIsSkillReachable)
                return _ReachableColor;
            if (!lUsesSkillRangePrefabs && lIsBlockedReachable)
                return _BlockedReachableColor;
            if (!lUsesGlyphPreviewPrefab && lIsGlyph)
                return _GlyphColor;
            if (_OccupiedColorsByCoord.TryGetValue(pCoord, out Color lOccupiedColor))
                return lOccupiedColor;
            if (_SpawnerCells.Contains(pCoord))
                return _SpawnerColor;

            return pCell.IsWalkable ? _WalkableColor : _BlockedColor;
        }

        private Color ResolveOccupiedColor(UnitRuntime pUnit, UnitId pActiveUnitId)
        {
            if (pUnit != null && pUnit.Id == pActiveUnitId)
                return _ActiveOccupiedColor;

            return pUnit != null && pUnit.Team == Team.Enemy ? _EnemyOccupiedColor : _PlayerOccupiedColor;
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

        private bool UsesSkillRangePreviewPrefabs()
        {
            return _SkillRangePreviewPrefab != null || _SkillBlockedRangePreviewPrefab != null;
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

        private void RefreshSkillRangeMarkers()
        {
            SyncPreviewMarkers(_SkillRangeMarkersByCoord, _SkillReachableCells, _SkillRangePreviewPrefab);
            SyncPreviewMarkers(_BlockedSkillRangeMarkersByCoord, _BlockedSkillReachableCells, _SkillBlockedRangePreviewPrefab);
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

            SortingGroup[] lSortingGroups = pMarker.GetComponentsInChildren<SortingGroup>(true);
            for (int lIndex = 0; lIndex < lSortingGroups.Length; lIndex++)
                lSortingGroups[lIndex].sortingOrder = lSortingOrder;

            SpriteRenderer[] lRenderers = pMarker.GetComponentsInChildren<SpriteRenderer>(true);
            for (int lIndex = 0; lIndex < lRenderers.Length; lIndex++)
                lRenderers[lIndex].sortingOrder = lSortingOrder;
        }
    }
}
