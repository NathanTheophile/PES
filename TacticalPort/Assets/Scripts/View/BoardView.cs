using System.Collections.Generic;
using TacticalPort.Core.Runtime;
using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.View
{
    public sealed class BoardView : MonoBehaviour
    {
        #region _____________________________| VALUES

        [Header("Layout")]
        [SerializeField] private Vector3 _Origin;
        [SerializeField] private Vector2 _CellWorldSize = new Vector2(1f, 0.5f);
        [SerializeField] private int _SortingStep = 10;
        [SerializeField] private bool _ApplyCellVisualTransform = true;
        [SerializeField] private float _CellVisualRotationZ = 45f;
        [SerializeField] private Vector3 _CellVisualScale = new Vector3(0.5f, 0.18f, 1f);

        [Header("Display")]
        [SerializeField] private Transform _CellRoot;
        [SerializeField] private Color _WalkableColor = Color.white;
        [SerializeField] private Color _BlockedColor = Color.gray;
        [SerializeField] private Color _PlayerOccupiedColor = new Color(0.27f, 0.47f, 0.85f, 1f);
        [SerializeField] private Color _EnemyOccupiedColor = new Color(0.86f, 0.27f, 0.27f, 1f);
        [SerializeField] private Color _ActiveOccupiedColor = new Color(1f, 0.62f, 0.12f, 1f);
        [SerializeField] private Color _ReachableColor = new Color(0.21f, 0.57f, 0.92f, 1f);
        [SerializeField] private Color _PreviewColor = new Color(0.94f, 0.58f, 0.2f, 1f);
        [SerializeField] private Color _HoveredColor = new Color(0.98f, 0.86f, 0.34f, 1f);
        [SerializeField] private Color _HoveredBlockedColor = new Color(0.78f, 0.44f, 0.28f, 1f);
        [SerializeField] private Color _HoveredReachableColor = new Color(0.37f, 0.9f, 1f, 1f);
        [SerializeField] private Color _HoveredPreviewColor = new Color(1f, 0.78f, 0.37f, 1f);

        private readonly Dictionary<GridCoord, BattleGridCellDefinition> _CellsByCoord = new Dictionary<GridCoord, BattleGridCellDefinition>();
        private readonly Dictionary<GridCoord, SpriteRenderer> _RenderersByCoord = new Dictionary<GridCoord, SpriteRenderer>();
        private readonly Dictionary<GridCoord, Color> _OccupiedColorsByCoord = new Dictionary<GridCoord, Color>();
        private readonly HashSet<GridCoord> _ReachableCells = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _PreviewCells = new HashSet<GridCoord>();
        private BattleScenarioDefinition _Scenario;
        private bool _HasHoveredCell;
        private GridCoord _HoveredCell;

        #endregion

        #region _____________________________| ACCESSORS

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
            RebuildBoard();
        }

        #endregion

        #region _____________________________| BOARD

        public void Configure(BattleScenarioDefinition pValue)
        {
            _Scenario = pValue;
            RebuildBoard();
        }

        public Vector3 GetWorldPosition(GridCoord pCoord)
        {
            float lHalfWidth = _CellWorldSize.x * 0.5f;
            float lHalfHeight = _CellWorldSize.y * 0.5f;

            return _Origin + new Vector3(
                (pCoord.X - pCoord.Y) * lHalfWidth,
                (pCoord.X + pCoord.Y) * lHalfHeight,
                0f);
        }

        public bool TryGetGridCoord(Vector3 pWorldPosition, out GridCoord pCoord)
        {
            pCoord = default;

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

        public void SetOccupiedCells(IReadOnlyCollection<BattleUnitRuntime> pUnits, BattleUnitId pActiveUnitId)
        {
            _OccupiedColorsByCoord.Clear();

            if (pUnits != null)
            {
                foreach (BattleUnitRuntime lUnit in pUnits)
                {
                    if (lUnit == null || !lUnit.IsAlive)
                        continue;

                    Color lOccupancyColor = ResolveOccupiedColor(lUnit, pActiveUnitId);
                    foreach (GridCoord lCell in lUnit.EnumerateOccupiedCells())
                        _OccupiedColorsByCoord[lCell] = lOccupancyColor;
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

            foreach (SceneBoardCell lCell in EnumerateSceneCells())
            {
                if (lCell == null)
                    continue;

                GridCoord lCoord = lCell.GridCoord;
                lCell.transform.position = GetWorldPosition(lCoord);
                ApplyCellVisualTransform(lCell.transform);

                _CellsByCoord[lCoord] = new BattleGridCellDefinition
                {
                    Coordinate = new SerializableGridCoord(lCoord.X, lCoord.Y),
                    IsWalkable = lCell.IsWalkable,
                    MovementCost = lCell.MovementCost
                };

                SpriteRenderer lRenderer = lCell.GetComponentInChildren<SpriteRenderer>(true);
                if (lRenderer == null)
                    continue;

                lRenderer.sortingOrder = ResolveSortingOrder(lCoord);
                _RenderersByCoord[lCoord] = lRenderer;
            }

            RefreshCellStates();
        }

        #endregion

        #region _____________________________| HELPERS

        public void SyncCellTransform(SceneBoardCell pCell)
        {
            if (pCell == null)
                return;

            pCell.transform.position = GetWorldPosition(pCell.GridCoord);
            ApplyCellVisualTransform(pCell.transform);

            SpriteRenderer lRenderer = pCell.GetComponentInChildren<SpriteRenderer>(true);
            if (lRenderer != null)
                lRenderer.sortingOrder = ResolveSortingOrder(pCell.GridCoord);
        }

        private void ClearBoardData()
        {
            _CellsByCoord.Clear();
            _RenderersByCoord.Clear();
            _OccupiedColorsByCoord.Clear();
            _ReachableCells.Clear();
            _PreviewCells.Clear();
            _HasHoveredCell = false;
        }

        private void CacheMissingReferences()
        {
            _CellRoot ??= transform;
        }

        private void ApplyCellVisualTransform(Transform pCellTransform)
        {
            if (!_ApplyCellVisualTransform || pCellTransform == null)
                return;

            pCellTransform.localRotation = Quaternion.Euler(0f, 0f, _CellVisualRotationZ);
            pCellTransform.localScale = _CellVisualScale;
        }

        private IEnumerable<SceneBoardCell> EnumerateSceneCells()
        {
            Transform lRoot = _CellRoot != null ? _CellRoot : transform;
            return lRoot.GetComponentsInChildren<SceneBoardCell>(true);
        }

        private int ResolveSortingOrder(GridCoord pCoord)
        {
            return -((pCoord.X + pCoord.Y) * Mathf.Max(1, _SortingStep));
        }

        private void RefreshCellStates()
        {
            foreach (KeyValuePair<GridCoord, SpriteRenderer> lEntry in _RenderersByCoord)
            {
                if (lEntry.Value == null || !_CellsByCoord.TryGetValue(lEntry.Key, out BattleGridCellDefinition lCell))
                    continue;

                lEntry.Value.color = ResolveCellColor(lEntry.Key, lCell);
            }
        }

        private Color ResolveCellColor(GridCoord pCoord, BattleGridCellDefinition pCell)
        {
            bool lIsReachable = _ReachableCells.Contains(pCoord);
            bool lIsPreviewed = _PreviewCells.Contains(pCoord);
            bool lIsHovered = _HasHoveredCell && _HoveredCell == pCoord;

            if (lIsHovered && lIsReachable)
                return _HoveredReachableColor;

            if (lIsHovered && lIsPreviewed)
                return _HoveredPreviewColor;

            if (lIsHovered)
                return pCell.IsWalkable ? _HoveredColor : _HoveredBlockedColor;

            if (lIsPreviewed)
                return _PreviewColor;

            if (lIsReachable)
                return _ReachableColor;

            if (_OccupiedColorsByCoord.TryGetValue(pCoord, out Color lOccupiedColor))
                return lOccupiedColor;

            return pCell.IsWalkable ? _WalkableColor : _BlockedColor;
        }

        private Color ResolveOccupiedColor(BattleUnitRuntime pUnit, BattleUnitId pActiveUnitId)
        {
            if (pUnit != null && pUnit.Id == pActiveUnitId)
                return _ActiveOccupiedColor;

            return pUnit != null && pUnit.Team == BattleTeam.Enemy
                ? _EnemyOccupiedColor
                : _PlayerOccupiedColor;
        }

        #endregion
    }
}
