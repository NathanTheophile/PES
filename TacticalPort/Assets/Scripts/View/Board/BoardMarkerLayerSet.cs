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
        Player,
        Enemy,
        Active
    }

    internal sealed class BoardMarkerLayerSet
    {
        private readonly BoardMarkerLayer _OwnSpawnerMarkers = new BoardMarkerLayer();
        private readonly BoardMarkerLayer _OpponentSpawnerMarkers = new BoardMarkerLayer();
        private readonly BoardMarkerLayer _PlayerOccupiedMarkers = new BoardMarkerLayer();
        private readonly BoardMarkerLayer _EnemyOccupiedMarkers = new BoardMarkerLayer();
        private readonly BoardMarkerLayer _ActiveOccupiedMarkers = new BoardMarkerLayer();
        private readonly BoardMarkerLayer _HoverMarkers = new BoardMarkerLayer();
        private readonly BoardMarkerLayer _MobilityPerTurnMarkers = new BoardMarkerLayer();
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
            _OwnSpawnerMarkers.Clear();
            _OpponentSpawnerMarkers.Clear();
            _PlayerOccupiedMarkers.Clear();
            _EnemyOccupiedMarkers.Clear();
            _ActiveOccupiedMarkers.Clear();
            _HoverMarkers.Clear();
            _MobilityPerTurnMarkers.Clear();
            _AreaPreviewMarkers.Clear();
            _GlyphMarkers.Clear();
            _TelegraphMarkers.Clear();
            _SkillRangeMarkers.Clear();
            _BlockedSkillRangeMarkers.Clear();
        }

        public void SyncSpawners(
            bool pVisible,
            Vector3 pSpawnerWorldOffset,
            IReadOnlyCollection<GridCoord> pOwnCells,
            GameObject pOwnPrefab,
            IReadOnlyCollection<GridCoord> pOpponentCells,
            GameObject pOpponentPrefab)
        {
            if (!pVisible)
            {
                _OwnSpawnerMarkers.Clear();
                _OpponentSpawnerMarkers.Clear();
                return;
            }

            _OwnSpawnerMarkers.Sync(pOwnCells, pOwnPrefab, ResolveWorldPosition, ResolveRoot, ApplyMarkerSorting, pSpawnerWorldOffset);
            _OpponentSpawnerMarkers.Sync(pOpponentCells, pOpponentPrefab, ResolveWorldPosition, ResolveRoot, ApplyMarkerSorting, pSpawnerWorldOffset);
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
            _MobilityPerTurnMarkers.Sync(pCells, pPrefab, ResolveWorldPosition, ResolveRoot, ApplyMarkerSorting);

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

        private Vector3 ResolveWorldPosition(GridCoord pCoord) => _ResolveWorldPosition?.Invoke(pCoord) ?? Vector3.zero;

        private Transform ResolveRoot() => _ResolveRoot?.Invoke();

        private void ApplyMarkerSorting(BoardMarkerRuntimeMarker pMarker, GridCoord pCoord)
        {
            if (pMarker == null || pMarker.GameObject == null)
                return;

            int lSortingStep = Mathf.Max(1, _ResolveSortingStep?.Invoke() ?? 1);
            int lSortingOffset = _ResolveSortingOffset?.Invoke() ?? 0;
            int lSortingOrder = -((pCoord.X + pCoord.Y) * lSortingStep) + lSortingOffset;
            int lSortingLayerId = _ResolveSortingLayerId?.Invoke() ?? 0;

            SortingGroup[] lSortingGroups = pMarker.SortingGroups;
            for (int lIndex = 0; lIndex < lSortingGroups.Length; lIndex++)
            {
                if (lSortingGroups[lIndex] == null)
                    continue;

                lSortingGroups[lIndex].sortingLayerID = lSortingLayerId;
                lSortingGroups[lIndex].sortingOrder = lSortingOrder;
            }

            Renderer[] lRenderers = pMarker.Renderers;
            for (int lIndex = 0; lIndex < lRenderers.Length; lIndex++)
            {
                if (lRenderers[lIndex] == null)
                    continue;

                lRenderers[lIndex].sortingLayerID = lSortingLayerId;
                lRenderers[lIndex].sortingOrder = lSortingOrder;
            }
        }

        private sealed class BoardMarkerRuntimeMarker
        {
            public BoardMarkerRuntimeMarker(GameObject pGameObject, GameObject pSourcePrefab)
            {
                GameObject = pGameObject;
                SourcePrefab = pSourcePrefab;
                SortingGroups = pGameObject != null
                    ? pGameObject.GetComponentsInChildren<SortingGroup>(true)
                    : Array.Empty<SortingGroup>();
                Renderers = pGameObject != null
                    ? pGameObject.GetComponentsInChildren<Renderer>(true)
                    : Array.Empty<Renderer>();
            }

            public GameObject GameObject { get; }
            public GameObject SourcePrefab { get; }
            public SortingGroup[] SortingGroups { get; }
            public Renderer[] Renderers { get; }
        }

        private sealed class BoardMarkerLayer
        {
            private readonly Dictionary<GridCoord, BoardMarkerRuntimeMarker> _RuntimeMarkers = new Dictionary<GridCoord, BoardMarkerRuntimeMarker>();
            private readonly List<BoardMarkerRuntimeMarker> _PooledMarkers = new List<BoardMarkerRuntimeMarker>();
            private readonly HashSet<GridCoord> _TargetCoords = new HashSet<GridCoord>();
            private readonly List<GridCoord> _CoordsToRemove = new List<GridCoord>();

            public void Sync(
                IReadOnlyCollection<GridCoord> pCoords,
                GameObject pPrefab,
                Func<GridCoord, Vector3> pResolveWorldPosition,
                Func<Transform> pResolveRoot,
                Action<BoardMarkerRuntimeMarker, GridCoord> pApplySorting,
                Vector3 pWorldOffset = default)
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
                foreach (KeyValuePair<GridCoord, BoardMarkerRuntimeMarker> lEntry in _RuntimeMarkers)
                {
                    if (!_TargetCoords.Contains(lEntry.Key))
                        _CoordsToRemove.Add(lEntry.Key);
                }

                for (int lIndex = 0; lIndex < _CoordsToRemove.Count; lIndex++)
                    RemoveMarker(_CoordsToRemove[lIndex]);

                foreach (GridCoord lCoord in _TargetCoords)
                {
                    if (!_RuntimeMarkers.TryGetValue(lCoord, out BoardMarkerRuntimeMarker lMarker) || lMarker == null || lMarker.GameObject == null)
                    {
                        lMarker = AcquireMarker(pPrefab, pResolveRoot);
                        _RuntimeMarkers[lCoord] = lMarker;
                    }

                    GameObject lMarkerObject = lMarker.GameObject;
                    lMarkerObject.transform.position = (pResolveWorldPosition != null ? pResolveWorldPosition.Invoke(lCoord) : Vector3.zero) + pWorldOffset;
                    pApplySorting?.Invoke(lMarker, lCoord);
                    lMarkerObject.SetActive(true);
                }
            }

            public void Clear()
            {
                foreach (BoardMarkerRuntimeMarker lMarker in _RuntimeMarkers.Values)
                    DestroyMarker(lMarker);

                for (int lIndex = 0; lIndex < _PooledMarkers.Count; lIndex++)
                    DestroyMarker(_PooledMarkers[lIndex]);

                _RuntimeMarkers.Clear();
                _PooledMarkers.Clear();
                _TargetCoords.Clear();
                _CoordsToRemove.Clear();
            }

            private void RemoveMarker(GridCoord pCoord)
            {
                if (!_RuntimeMarkers.TryGetValue(pCoord, out BoardMarkerRuntimeMarker lMarker))
                    return;

                ReleaseMarker(lMarker);
                _RuntimeMarkers.Remove(pCoord);
            }

            private BoardMarkerRuntimeMarker AcquireMarker(GameObject pPrefab, Func<Transform> pResolveRoot)
            {
                for (int lIndex = _PooledMarkers.Count - 1; lIndex >= 0; lIndex--)
                {
                    BoardMarkerRuntimeMarker lMarker = _PooledMarkers[lIndex];
                    if (lMarker == null || lMarker.GameObject == null)
                    {
                        _PooledMarkers.RemoveAt(lIndex);
                        continue;
                    }

                    if (lMarker.SourcePrefab != pPrefab)
                        continue;

                    _PooledMarkers.RemoveAt(lIndex);
                    Transform lRoot = pResolveRoot != null ? pResolveRoot.Invoke() : null;
                    lMarker.GameObject.transform.SetParent(lRoot, false);
                    return lMarker;
                }

                GameObject lMarkerObject = UnityEngine.Object.Instantiate(pPrefab, pResolveRoot != null ? pResolveRoot.Invoke() : null);
                return new BoardMarkerRuntimeMarker(lMarkerObject, pPrefab);
            }

            private void ReleaseMarker(BoardMarkerRuntimeMarker pMarker)
            {
                if (pMarker == null || pMarker.GameObject == null)
                    return;

                if (!Application.isPlaying)
                {
                    DestroyMarker(pMarker);
                    return;
                }

                pMarker.GameObject.SetActive(false);
                _PooledMarkers.Add(pMarker);
            }

            private static void DestroyMarker(BoardMarkerRuntimeMarker pMarker)
            {
                if (pMarker == null || pMarker.GameObject == null)
                    return;

                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(pMarker.GameObject);
                else
                    UnityEngine.Object.DestroyImmediate(pMarker.GameObject);
            }
        }
    }
}
