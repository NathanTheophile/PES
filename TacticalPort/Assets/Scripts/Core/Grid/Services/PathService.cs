#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Shared;

namespace TacticalPort.Core
{
    public sealed class PathService : IPathService
    {
        #region _____________________________/ VALUES

        private readonly IGridService _GridService;
        private readonly MinCostFrontier _Frontier = new MinCostFrontier();
        private readonly Dictionary<GridCoord, GridCoord> _CameFrom = new Dictionary<GridCoord, GridCoord>();
        private readonly Dictionary<GridCoord, int> _Costs = new Dictionary<GridCoord, int>();
        private readonly List<GridCoord> _PathBuffer = new List<GridCoord>();
        private readonly List<GridCoord> _ReachableResultBuffer = new List<GridCoord>();
        private static readonly GridCoord[] CardinalOffsets =
        {
            new GridCoord(1, 0),
            new GridCoord(-1, 0),
            new GridCoord(0, 1),
            new GridCoord(0, -1)
        };

        #endregion

        #region _____________________________| INIT

        public PathService(IGridService pGridService)
        {
            _GridService = pGridService;
        }

        #endregion

        #region _____________________________| PATHING

        public PathResult FindPath(GridCoord pOrigin, GridCoord pDestination, int pMaxCost, UnitId pMovingUnitId)
        {
            if (!_GridService.IsWalkable(pOrigin) || !_GridService.IsWalkable(pDestination))
                return PathResult.Failed("Origin or destination is not walkable.");

            if (!CanOccupy(pDestination, pMovingUnitId))
                return PathResult.Failed("Destination is occupied.");

            if (pOrigin == pDestination)
            {
                _PathBuffer.Clear();
                _PathBuffer.Add(pOrigin);
                return PathResult.Succeeded(_PathBuffer, 0);
            }

            PreparePathingBuffers(pOrigin);

            while (TryPopLowestCost(out GridCoord lCurrent))
            {
                if (lCurrent == pDestination)
                    break;

                for (int lIndex = 0; lIndex < CardinalOffsets.Length; lIndex++)
                {
                    GridCoord lNeighbour = ResolveNeighbour(lCurrent, lIndex);
                    if (!CanOccupy(lNeighbour, pMovingUnitId))
                        continue;

                    int lNextCost = _Costs[lCurrent] + _GridService.GetMovementCost(lNeighbour);
                    if (lNextCost > pMaxCost)
                        continue;

                    if (!_Costs.TryGetValue(lNeighbour, out int lExistingCost) || lNextCost < lExistingCost)
                    {
                        _Costs[lNeighbour] = lNextCost;
                        _CameFrom[lNeighbour] = lCurrent;
                        _Frontier.Enqueue(lNeighbour, lNextCost);
                    }
                }
            }

            if (!_Costs.TryGetValue(pDestination, out int lTotalCost))
                return PathResult.Failed("No valid path to destination.");

            return PathResult.Succeeded(ReconstructPath(pOrigin, pDestination, _CameFrom), lTotalCost);
        }

        public IReadOnlyCollection<GridCoord> GetReachableCells(GridCoord pOrigin, int pMaxCost, UnitId pMovingUnitId)
        {
            PreparePathingBuffers(pOrigin);

            while (TryPopLowestCost(out GridCoord lCurrent))
            {
                for (int lIndex = 0; lIndex < CardinalOffsets.Length; lIndex++)
                {
                    GridCoord lNeighbour = ResolveNeighbour(lCurrent, lIndex);
                    if (!CanOccupy(lNeighbour, pMovingUnitId))
                        continue;

                    int lNextCost = _Costs[lCurrent] + _GridService.GetMovementCost(lNeighbour);
                    if (lNextCost > pMaxCost)
                        continue;

                    if (_Costs.TryGetValue(lNeighbour, out int lExistingCost) && lExistingCost <= lNextCost)
                        continue;

                    _Costs[lNeighbour] = lNextCost;
                    _Frontier.Enqueue(lNeighbour, lNextCost);
                }
            }

            _ReachableResultBuffer.Clear();
            foreach (GridCoord lCoord in _Costs.Keys)
                _ReachableResultBuffer.Add(lCoord);

            return _ReachableResultBuffer;
        }

        #endregion

        #region _____________________________| HELPERS

        private bool CanOccupy(GridCoord pCoordinate, UnitId pMovingUnitId) =>
            _GridService.CanUnitOccupy(pMovingUnitId, pCoordinate);

        private void PreparePathingBuffers(GridCoord pOrigin)
        {
            _Frontier.Clear();
            _CameFrom.Clear();
            _Costs.Clear();
            _Costs[pOrigin] = 0;
            _Frontier.Enqueue(pOrigin, 0);
        }

        private static GridCoord ResolveNeighbour(GridCoord pCoordinate, int pOffsetIndex)
        {
            GridCoord lOffset = CardinalOffsets[pOffsetIndex];
            return new GridCoord(pCoordinate.X + lOffset.X, pCoordinate.Y + lOffset.Y);
        }

        private bool TryPopLowestCost(out GridCoord pCoord)
        {
            while (_Frontier.TryDequeue(out GridCoord lNext, out int lQueuedCost))
            {
                if (_Costs.TryGetValue(lNext, out int lCurrentCost) && lCurrentCost == lQueuedCost)
                {
                    pCoord = lNext;
                    return true;
                }
            }

            pCoord = default;
            return false;
        }

        private IReadOnlyList<GridCoord> ReconstructPath(
            GridCoord pOrigin,
            GridCoord pDestination,
            IReadOnlyDictionary<GridCoord, GridCoord> pCameFrom)
        {
            _PathBuffer.Clear();
            _PathBuffer.Add(pDestination);
            GridCoord lCurrent = pDestination;

            while (lCurrent != pOrigin && pCameFrom.TryGetValue(lCurrent, out GridCoord lPrevious))
            {
                lCurrent = lPrevious;
                _PathBuffer.Add(lCurrent);
            }

            _PathBuffer.Reverse();
            return _PathBuffer;
        }

        private sealed class MinCostFrontier
        {
            private readonly List<Entry> _Entries = new List<Entry>();

            public void Clear() => _Entries.Clear();

            public void Enqueue(GridCoord pCoord, int pCost)
            {
                _Entries.Add(new Entry(pCoord, pCost));
                HeapifyUp(_Entries.Count - 1);
            }

            public bool TryDequeue(out GridCoord pCoord, out int pCost)
            {
                if (_Entries.Count == 0)
                {
                    pCoord = default;
                    pCost = 0;
                    return false;
                }

                Entry lEntry = _Entries[0];
                int lLastIndex = _Entries.Count - 1;
                _Entries[0] = _Entries[lLastIndex];
                _Entries.RemoveAt(lLastIndex);

                if (_Entries.Count > 0)
                    HeapifyDown(0);

                pCoord = lEntry.Coord;
                pCost = lEntry.Cost;
                return true;
            }

            private void HeapifyUp(int pIndex)
            {
                while (pIndex > 0)
                {
                    int lParentIndex = (pIndex - 1) / 2;
                    if (_Entries[lParentIndex].Cost <= _Entries[pIndex].Cost)
                        break;

                    Swap(lParentIndex, pIndex);
                    pIndex = lParentIndex;
                }
            }

            private void HeapifyDown(int pIndex)
            {
                while (true)
                {
                    int lLeftIndex = pIndex * 2 + 1;
                    int lRightIndex = lLeftIndex + 1;
                    int lLowestIndex = pIndex;

                    if (lLeftIndex < _Entries.Count && _Entries[lLeftIndex].Cost < _Entries[lLowestIndex].Cost)
                        lLowestIndex = lLeftIndex;

                    if (lRightIndex < _Entries.Count && _Entries[lRightIndex].Cost < _Entries[lLowestIndex].Cost)
                        lLowestIndex = lRightIndex;

                    if (lLowestIndex == pIndex)
                        break;

                    Swap(pIndex, lLowestIndex);
                    pIndex = lLowestIndex;
                }
            }

            private void Swap(int pFirstIndex, int pSecondIndex)
            {
                Entry lEntry = _Entries[pFirstIndex];
                _Entries[pFirstIndex] = _Entries[pSecondIndex];
                _Entries[pSecondIndex] = lEntry;
            }

            private readonly struct Entry
            {
                public Entry(GridCoord pCoord, int pCost)
                {
                    Coord = pCoord;
                    Cost = pCost;
                }

                public GridCoord Coord { get; }
                public int Cost { get; }
            }
        }

        #endregion
    }
}
