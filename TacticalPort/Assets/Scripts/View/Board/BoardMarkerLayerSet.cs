#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using System.Collections.Generic;
using TacticalPort.Shared;
using UnityEngine;
using UnityEngine.Rendering;

namespace TacticalPort.View
{
    internal enum BoardOccupiedCellVisualState
    {
        None,
        Player,
        Enemy,
        Active
    }

    internal sealed class BoardMarkerLayerSet
    {
        private readonly BoardMarkerLayer _SpawnerMarkers = new BoardMarkerLayer();
        private readonly BoardMarkerLayer _PlayerOccupiedMarkers = new BoardMarkerLayer();
        private readonly BoardMarkerLayer _EnemyOccupiedMarkers = new BoardMarkerLayer();
        private readonly BoardMarkerLayer _ActiveOccupiedMarkers = new BoardMarkerLayer();
        private readonly BoardMarkerLayer _HoverMarkers = new BoardMarkerLayer();
        private readonly BoardMarkerLayer _MoveRangeMarkers = new BoardMarkerLayer();
        private readonly BoardMarkerLayer _AreaPreviewMarkers = new BoardMarkerLayer();
        private readonly BoardMarkerLayer _GlyphMarkers = new BoardMarkerLayer();
        private readonly BoardMarkerLayer _TelegraphMarkers = new BoardMarkerLayer();
        private readonly BoardMarkerLayer _SkillRangeMarkers = new BoardMarkerLayer();
        private readonly BoardMarkerLayer _BlockedSkillRangeMarkers = new BoardMarkerLayer();
        private readonly List<GridCoord> _OccupiedCellsBuffer = new List<GridCoord>();
        private readonly Func<GridCoord, Vector3> _ResolveWorldPosition;
        private readonly Func<Transform> _ResolveRoot;
        private readonly Func<int> _ResolveSortingStep;
        private readonly Func<int> _ResolveSortingOffset;
        private readonly Func<int> _ResolveSortingLayerId;

        public BoardMarkerLayerSet(
            Func<GridCoord, Vector3> pResolveWorldPosition,
            Func<Transform> pResolveRoot,
            Func<int> pResolveSortingStep,
            Func<int> pResolveSortingOffset,
            Func<int> pResolveSortingLayerId)
        {
            _ResolveWorldPosition = pResolveWorldPosition;
            _ResolveRoot = pResolveRoot;
            _ResolveSortingStep = pResolveSortingStep;
            _ResolveSortingOffset = pResolveSortingOffset;
            _ResolveSortingLayerId = pResolveSortingLayerId;
        }

        public void Clear()
        {
            _SpawnerMarkers.Clear();
            _PlayerOccupiedMarkers.Clear();
            _EnemyOccupiedMarkers.Clear();
            _ActiveOccupiedMarkers.Clear();
            _HoverMarkers.Clear();
            _MoveRangeMarkers.Clear();
            _AreaPreviewMarkers.Clear();
            _GlyphMarkers.Clear();
            _TelegraphMarkers.Clear();
            _SkillRangeMarkers.Clear();
            _BlockedSkillRangeMarkers.Clear();
        }

        public void SyncSpawner(bool pVisible, IReadOnlyCollection<GridCoord> pCells, GameObject pPrefab)
        {
            if (!pVisible)
            {
                _SpawnerMarkers.Clear();
                return;
            }

            _SpawnerMarkers.Sync(pCells, pPrefab, ResolveWorldPosition, ResolveRoot, ApplyMarkerSorting);
        }

        public void SyncOccupied(
            IReadOnlyDictionary<GridCoord, BoardOccupiedCellVisualState> pStates,
            GameObject pPlayerPrefab,
            GameObject pEnemyPrefab,
            GameObject pActivePrefab)
        {
            SyncOccupiedState(pStates, BoardOccupiedCellVisualState.Player, _PlayerOccupiedMarkers, pPlayerPrefab);
            SyncOccupiedState(pStates, BoardOccupiedCellVisualState.Enemy, _EnemyOccupiedMarkers, pEnemyPrefab);
            SyncOccupiedState(pStates, BoardOccupiedCellVisualState.Active, _ActiveOccupiedMarkers, pActivePrefab);
        }

        public void SyncHover(IReadOnlyCollection<GridCoord> pCells, GameObject pPrefab) =>
            _HoverMarkers.Sync(pCells, pPrefab, ResolveWorldPosition, ResolveRoot, ApplyMarkerSorting);

        public void SyncMovement(IReadOnlyCollection<GridCoord> pCells, GameObject pPrefab) =>
            _MoveRangeMarkers.Sync(pCells, pPrefab, ResolveWorldPosition, ResolveRoot, ApplyMarkerSorting);

        public void SyncAreaPreview(IReadOnlyCollection<GridCoord> pCells, GameObject pPrefab) =>
            _AreaPreviewMarkers.Sync(pCells, pPrefab, ResolveWorldPosition, ResolveRoot, ApplyMarkerSorting);

        public void SyncGlyphs(IReadOnlyCollection<GridCoord> pCells, GameObject pPrefab) =>
            _GlyphMarkers.Sync(pCells, pPrefab, ResolveWorldPosition, ResolveRoot, ApplyMarkerSorting);

        public void SyncTelegraphs(IReadOnlyCollection<GridCoord> pCells, GameObject pPrefab) =>
            _TelegraphMarkers.Sync(pCells, pPrefab, ResolveWorldPosition, ResolveRoot, ApplyMarkerSorting);

        public void SyncSkillRange(
            IReadOnlyCollection<GridCoord> pReachableCells,
            IReadOnlyCollection<GridCoord> pBlockedCells,
            GameObject pReachablePrefab,
            GameObject pBlockedPrefab)
        {
            _SkillRangeMarkers.Sync(pReachableCells, pReachablePrefab, ResolveWorldPosition, ResolveRoot, ApplyMarkerSorting);
            _BlockedSkillRangeMarkers.Sync(pBlockedCells, pBlockedPrefab, ResolveWorldPosition, ResolveRoot, ApplyMarkerSorting);
        }

        private void SyncOccupiedState(
            IReadOnlyDictionary<GridCoord, BoardOccupiedCellVisualState> pStates,
            BoardOccupiedCellVisualState pState,
            BoardMarkerLayer pLayer,
            GameObject pPrefab)
        {
            _OccupiedCellsBuffer.Clear();
            if (pStates != null)
            {
                foreach (KeyValuePair<GridCoord, BoardOccupiedCellVisualState> lEntry in pStates)
                {
                    if (lEntry.Value == pState)
                        _OccupiedCellsBuffer.Add(lEntry.Key);
                }
            }

            pLayer.Sync(_OccupiedCellsBuffer, pPrefab, ResolveWorldPosition, ResolveRoot, ApplyMarkerSorting);
        }

        private Vector3 ResolveWorldPosition(GridCoord pCoord) =>
            _ResolveWorldPosition != null ? _ResolveWorldPosition.Invoke(pCoord) : Vector3.zero;

        private Transform ResolveRoot() => _ResolveRoot != null ? _ResolveRoot.Invoke() : null;

        private void ApplyMarkerSorting(GameObject pMarker, GridCoord pCoord)
        {
            if (pMarker == null)
                return;

            int lSortingStep = Mathf.Max(1, _ResolveSortingStep != null ? _ResolveSortingStep.Invoke() : 1);
            int lSortingOffset = _ResolveSortingOffset != null ? _ResolveSortingOffset.Invoke() : 0;
            int lSortingOrder = -((pCoord.X + pCoord.Y) * lSortingStep) + lSortingOffset;
            int lSortingLayerId = _ResolveSortingLayerId != null ? _ResolveSortingLayerId.Invoke() : 0;

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

        private sealed class BoardMarkerLayer
        {
            private readonly Dictionary<GridCoord, GameObject> _RuntimeMarkers = new Dictionary<GridCoord, GameObject>();
            private readonly HashSet<GridCoord> _TargetCoords = new HashSet<GridCoord>();
            private readonly List<GridCoord> _CoordsToRemove = new List<GridCoord>();

            public void Sync(
                IReadOnlyCollection<GridCoord> pCoords,
                GameObject pPrefab,
                Func<GridCoord, Vector3> pResolveWorldPosition,
                Func<Transform> pResolveRoot,
                Action<GameObject, GridCoord> pApplySorting)
            {
                if (pPrefab == null)
                {
                    Clear();
                    return;
                }

                _TargetCoords.Clear();
                if (pCoords != null)
                {
                    foreach (GridCoord lCoord in pCoords)
                        _TargetCoords.Add(lCoord);
                }

                _CoordsToRemove.Clear();
                foreach (KeyValuePair<GridCoord, GameObject> lEntry in _RuntimeMarkers)
                {
                    if (!_TargetCoords.Contains(lEntry.Key))
                        _CoordsToRemove.Add(lEntry.Key);
                }

                for (int lIndex = 0; lIndex < _CoordsToRemove.Count; lIndex++)
                    RemoveMarker(_CoordsToRemove[lIndex]);

                foreach (GridCoord lCoord in _TargetCoords)
                {
                    if (!_RuntimeMarkers.TryGetValue(lCoord, out GameObject lMarker) || lMarker == null)
                    {
                        lMarker = UnityEngine.Object.Instantiate(pPrefab, pResolveRoot != null ? pResolveRoot.Invoke() : null);
                        _RuntimeMarkers[lCoord] = lMarker;
                    }

                    lMarker.transform.position = pResolveWorldPosition != null ? pResolveWorldPosition.Invoke(lCoord) : Vector3.zero;
                    pApplySorting?.Invoke(lMarker, lCoord);
                    lMarker.SetActive(true);
                }
            }

            public void Clear()
            {
                foreach (GameObject lMarker in _RuntimeMarkers.Values)
                    DestroyMarker(lMarker);

                _RuntimeMarkers.Clear();
                _TargetCoords.Clear();
                _CoordsToRemove.Clear();
            }

            private void RemoveMarker(GridCoord pCoord)
            {
                if (!_RuntimeMarkers.TryGetValue(pCoord, out GameObject lMarker))
                    return;

                DestroyMarker(lMarker);
                _RuntimeMarkers.Remove(pCoord);
            }

            private static void DestroyMarker(GameObject pMarker)
            {
                if (pMarker == null)
                    return;

                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(pMarker);
                else
                    UnityEngine.Object.DestroyImmediate(pMarker);
            }
        }
    }
}
