#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Core;
using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEngine;
using UnityEngine.Serialization;
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
        [Tooltip("Optional world offset applied to board markers. Keep zero for 3D tile feedback prefabs with their own local height.")]
        [SerializeField] private Vector3 _GroundMarkerWorldOffset = Vector3.zero;
        [Tooltip("World Y offset applied to spawn feedback marker roots above their tile.")]
        [SerializeField] private float _SpawnerMarkerYOffset = 0.5f;
        [Tooltip("Vertical offset applied to preview markers displayed on cells occupied by units.")]
        [SerializeField] private float _OccupiedPreviewYOffset = -0.05f;

        [Header("Cell State Prefabs")]
        [FormerlySerializedAs("_TeamASpawnerPreviewPrefab")]
        [Tooltip("Feedback prefab instantiated on the local player's spawn cells.")]
        [SerializeField] private GameObject _OwnSpawnerPreviewPrefab;
        [FormerlySerializedAs("_TeamBSpawnerPreviewPrefab")]
        [Tooltip("Feedback prefab instantiated on the opponent's spawn cells.")]
        [SerializeField] private GameObject _OpponentSpawnerPreviewPrefab;
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

        private readonly Dictionary<GridCoord, CellDefinition> _CellsByCoord = new Dictionary<GridCoord, CellDefinition>();
        private readonly Dictionary<GridCoord, BoardOccupiedCellVisualState> _OccupiedStatesByCoord = new Dictionary<GridCoord, BoardOccupiedCellVisualState>();
        private readonly HashSet<GridCoord> _SpawnerCells = new HashSet<GridCoord>();
        private readonly Dictionary<MatchPlayerSlot, HashSet<GridCoord>> _SpawnerCellsBySlot = new Dictionary<MatchPlayerSlot, HashSet<GridCoord>>();
        private readonly HashSet<GridCoord> _ReachableCells = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _SkillReachableCells = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _BlockedSkillReachableCells = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _PreviewCells = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _GlyphCells = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _TelegraphCells = new HashSet<GridCoord>();
        private readonly HashSet<GridCoord> _HoveredCells = new HashSet<GridCoord>();
        private BattleScenarioDefinition _Scenario;
        private BoardAuthoring3D _Board3DAuthoring;
        private bool _ShowSpawnerCells = true;
        private MatchPlayerSlot _VisibleSpawnerSlot = MatchPlayerSlot.None;
        private MatchPlayerSlot _LocalPlayerSlot = MatchPlayerSlot.TeamA;
        private BoardMarkerLayerSet _MarkerLayers;
        private int _BoardSortingLayerId;
        private int _CellStateBatchDepth;
        private bool _HasPendingCellStateRefresh;
        private bool _HasHoveredCell;
        private GridCoord _HoveredCell;
#if UNITY_EDITOR
        private bool _HasScheduledBoardRebuild;
#endif

        #endregion

        #region _____________________________/ ACCESSORS

        public BattleScenarioDefinition Scenario => _Scenario;
        public BoardAuthoring3D Board3DAuthoring => _Board3DAuthoring;

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
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            ScheduleBoardRebuild();
            return;
#else
            RebuildBoard();
#endif
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

                RebuildBoard();
            };
        }
#endif

        #endregion

        #region _____________________________| CONFIGURE

        public void Configure(BattleScenarioDefinition pValue)
        {
            _Scenario = pValue;
            RebuildBoard();
        }

        public Vector3 GetWorldPosition(GridCoord pCoord) =>
            _Board3DAuthoring != null && _Board3DAuthoring.HasTiles ? _Board3DAuthoring.GetWorldPosition(pCoord) : Vector3.zero;

        public Vector3 GetMarkerWorldPosition(GridCoord pCoord) =>
            GetWorldPosition(pCoord) + ResolveMarkerWorldOffset();

        public bool TryGetGridCoord(Vector3 pWorldPosition, out GridCoord pCoord)
        {
            pCoord = default;
            return _Board3DAuthoring != null && _Board3DAuthoring.HasTiles && _Board3DAuthoring.TryGetGridCoord(pWorldPosition, out pCoord);
        }

        public bool TryGetGridCoord(Ray pWorldRay, out GridCoord pCoord)
        {
            pCoord = default;
            return _Board3DAuthoring != null && _Board3DAuthoring.HasTiles && _Board3DAuthoring.TryGetGridCoord(pWorldRay, out pCoord);
        }

        public bool ContainsCell(GridCoord pCoord) => _CellsByCoord.ContainsKey(pCoord);

        public bool IsSpawnerCell(GridCoord pCoord) => _SpawnerCells.Contains(pCoord);

        public bool IsSpawnerCell(GridCoord pCoord, MatchPlayerSlot pSlot) =>
            pSlot == MatchPlayerSlot.None
                ? IsSpawnerCell(pCoord)
                : _SpawnerCellsBySlot.TryGetValue(pSlot, out HashSet<GridCoord> lCells) && lCells.Contains(pCoord);

        public IReadOnlyCollection<GridCoord> GetSpawnerCells() => new List<GridCoord>(_SpawnerCells);

        public IReadOnlyCollection<GridCoord> GetSpawnerCells(MatchPlayerSlot pSlot) =>
            pSlot != MatchPlayerSlot.None && _SpawnerCellsBySlot.TryGetValue(pSlot, out HashSet<GridCoord> lCells)
                ? new List<GridCoord>(lCells)
                : GetSpawnerCells();

        public void BeginCellStateUpdate() => _CellStateBatchDepth++;

        public void EndCellStateUpdate()
        {
            if (_CellStateBatchDepth <= 0)
                return;

            _CellStateBatchDepth--;
            if (_CellStateBatchDepth == 0 && _HasPendingCellStateRefresh)
                RefreshCellStates();
        }

        public void SetSpawnerCellsVisible(bool pVisible)
        {
            SetSpawnerCellsVisible(pVisible, MatchPlayerSlot.None);
        }

        public void SetSpawnerCellsVisible(bool pVisible, MatchPlayerSlot pVisibleSlot)
        {
            if (_ShowSpawnerCells == pVisible && _VisibleSpawnerSlot == pVisibleSlot)
                return;

            _ShowSpawnerCells = pVisible;
            _VisibleSpawnerSlot = pVisibleSlot;
            RefreshCellStates();
        }

        public void SetLocalPlayerSlot(MatchPlayerSlot pSlot)
        {
            if (pSlot == MatchPlayerSlot.None || _LocalPlayerSlot == pSlot)
                return;

            _LocalPlayerSlot = pSlot;
            RefreshCellStates();
        }

        public bool TryGetCellDefinition(GridCoord pCoord, out CellDefinition pCellDefinition) =>
            _CellsByCoord.TryGetValue(pCoord, out pCellDefinition);

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

                    BoardOccupiedCellVisualState lState = ResolveOccupiedVisualState(lUnit, pActiveUnitId);
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
            Build3DBoard();
            RefreshCellStates();
        }

        #endregion

        #region _____________________________| HELPERS

        private void ClearBoardData()
        {
            EnsureMarkerLayers();
            _CellsByCoord.Clear();
            _OccupiedStatesByCoord.Clear();
            _SpawnerCells.Clear();
            _SpawnerCellsBySlot.Clear();
            _ReachableCells.Clear();
            _SkillReachableCells.Clear();
            _BlockedSkillReachableCells.Clear();
            _PreviewCells.Clear();
            _GlyphCells.Clear();
            _TelegraphCells.Clear();
            _HoveredCells.Clear();
            _HasHoveredCell = false;
            _BoardSortingLayerId = 0;
            _MarkerLayers.Clear();
        }

        private void CacheMissingReferences()
        {
            _SkillPreviewRoot ??= transform;
            _Board3DAuthoring ??= GetComponent<BoardAuthoring3D>() ?? GetComponentInChildren<BoardAuthoring3D>(true);
            EnsureMarkerLayers();
        }

        private void EnsureMarkerLayers()
        {
            _MarkerLayers ??= new BoardMarkerLayerSet(
                GetPreviewMarkerWorldPosition,
                ResolveMarkerRoot,
                () => _SortingStep,
                () => _PreviewSortingOffset,
                () => _BoardSortingLayerId);
        }

        private void RefreshCellStates()
        {
            if (_CellStateBatchDepth > 0)
            {
                _HasPendingCellStateRefresh = true;
                return;
            }

            _HasPendingCellStateRefresh = false;
            EnsureMarkerLayers();
            _MarkerLayers.SyncSpawners(
                _ShowSpawnerCells,
                Vector3.up * _SpawnerMarkerYOffset,
                ResolveVisibleSpawnerCells(_LocalPlayerSlot),
                _OwnSpawnerPreviewPrefab,
                ResolveVisibleSpawnerCells(ResolveOpponentSlot()),
                _OpponentSpawnerPreviewPrefab);
            _MarkerLayers.SyncOccupied(
                _OccupiedStatesByCoord,
                _PlayerOccupiedPreviewPrefab,
                _EnemyOccupiedPreviewPrefab,
                _ActiveOccupiedPreviewPrefab);

            _HoveredCells.Clear();
            if (_HasHoveredCell && _CellsByCoord.ContainsKey(_HoveredCell))
                _HoveredCells.Add(_HoveredCell);

            _MarkerLayers.SyncHover(_HoveredCells, _HoverPreviewPrefab);
            _MarkerLayers.SyncMovement(_ReachableCells, _MoveRangePreviewPrefab);
            _MarkerLayers.SyncAreaPreview(_PreviewCells, _AreaPreviewPrefab);
            _MarkerLayers.SyncGlyphs(_GlyphCells, _GlyphPreviewPrefab);
            _MarkerLayers.SyncTelegraphs(_TelegraphCells, _TelegraphPreviewPrefab);
            _MarkerLayers.SyncSkillRange(
                _SkillReachableCells,
                _BlockedSkillReachableCells,
                _SkillRangePreviewPrefab,
                _SkillBlockedRangePreviewPrefab);
        }

        private BoardOccupiedCellVisualState ResolveOccupiedVisualState(UnitRuntime pUnit, UnitId pActiveUnitId) =>
            pUnit != null && pUnit.Id == pActiveUnitId
                ? BoardOccupiedCellVisualState.Active
                : pUnit != null && CombatTeamUtility.ResolveRelation(pUnit.Team, _LocalPlayerSlot) == CombatTeamRelation.Opponent
                    ? BoardOccupiedCellVisualState.Enemy
                    : BoardOccupiedCellVisualState.Player;

        private void Build3DBoard()
        {
            if (_Board3DAuthoring == null || !_Board3DAuthoring.HasTiles)
                return;

            _BoardSortingLayerId = 0;
            foreach (GridCoord lCoord in _Board3DAuthoring.EnumerateAuthoredCoordinates())
            {
                if (!_Board3DAuthoring.TryGetCellDefinition(lCoord, out CellDefinition lCellDefinition))
                    continue;

                _CellsByCoord[lCoord] = lCellDefinition;
                if (_Board3DAuthoring.TryGetTile(lCoord, out BoardTileAuthoring lTile) && lTile != null)
                    AddSpawnerCell(lCoord, lTile.AssignedTeam);
            }
        }

        private Transform ResolveMarkerRoot() => _SkillPreviewRoot != null ? _SkillPreviewRoot : transform;

        private Vector3 ResolveMarkerWorldOffset() => _GroundMarkerWorldOffset;

        private Vector3 GetPreviewMarkerWorldPosition(GridCoord pCoord) =>
            GetMarkerWorldPosition(pCoord) + ResolveOccupiedPreviewOffset(pCoord);

        private Vector3 ResolveOccupiedPreviewOffset(GridCoord pCoord) =>
            _OccupiedStatesByCoord.ContainsKey(pCoord) ? Vector3.up * _OccupiedPreviewYOffset : Vector3.zero;

        private IReadOnlyCollection<GridCoord> ResolveVisibleSpawnerCells(MatchPlayerSlot pSlot)
        {
            if (pSlot == MatchPlayerSlot.None)
                return System.Array.Empty<GridCoord>();

            if (_VisibleSpawnerSlot != MatchPlayerSlot.None && _VisibleSpawnerSlot != pSlot)
                return System.Array.Empty<GridCoord>();

            return _SpawnerCellsBySlot.TryGetValue(pSlot, out HashSet<GridCoord> lCells)
                ? lCells
                : System.Array.Empty<GridCoord>();
        }

        private MatchPlayerSlot ResolveOpponentSlot() =>
            _LocalPlayerSlot == MatchPlayerSlot.TeamA
                ? MatchPlayerSlot.TeamB
                : _LocalPlayerSlot == MatchPlayerSlot.TeamB
                    ? MatchPlayerSlot.TeamA
                    : MatchPlayerSlot.None;

        private void AddSpawnerCell(GridCoord pCoord, MatchPlayerSlot pSlot)
        {
            if (pSlot == MatchPlayerSlot.None)
                return;

            _SpawnerCells.Add(pCoord);
            if (!_SpawnerCellsBySlot.TryGetValue(pSlot, out HashSet<GridCoord> lCells))
            {
                lCells = new HashSet<GridCoord>();
                _SpawnerCellsBySlot[pSlot] = lCells;
            }

            lCells.Add(pCoord);
        }

        #endregion
    }
}
