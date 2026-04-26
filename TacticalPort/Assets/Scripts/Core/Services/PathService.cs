using System.Collections.Generic;
using TacticalPort.Core.Interfaces;
using TacticalPort.Core.Runtime;
using TacticalPort.Shared;

namespace TacticalPort.Core.Services
{
    public sealed class PathService : IPathService
    {
        #region _____________________________| VALUES

        private readonly IGridService _GridService;

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
                return PathResult.Succeeded(new[] { pOrigin }, 0);

            List<GridCoord> lFrontier = new List<GridCoord> { pOrigin };
            Dictionary<GridCoord, GridCoord> lCameFrom = new Dictionary<GridCoord, GridCoord>();
            Dictionary<GridCoord, int> lCosts = new Dictionary<GridCoord, int> { [pOrigin] = 0 };

            while (lFrontier.Count > 0)
            {
                GridCoord lCurrent = PopLowestCost(lFrontier, lCosts);

                if (lCurrent == pDestination)
                    break;

                foreach (GridCoord lNeighbour in _GridService.GetNeighbours(lCurrent))
                {
                    if (!CanOccupy(lNeighbour, pMovingUnitId))
                        continue;

                    int lNextCost = lCosts[lCurrent] + _GridService.GetMovementCost(lNeighbour);
                    if (lNextCost > pMaxCost)
                        continue;

                    if (!lCosts.TryGetValue(lNeighbour, out int lExistingCost) || lNextCost < lExistingCost)
                    {
                        lCosts[lNeighbour] = lNextCost;
                        lCameFrom[lNeighbour] = lCurrent;

                        if (!lFrontier.Contains(lNeighbour))
                            lFrontier.Add(lNeighbour);
                    }
                }
            }

            if (!lCosts.TryGetValue(pDestination, out int lTotalCost))
                return PathResult.Failed("No valid path to destination.");

            return PathResult.Succeeded(ReconstructPath(pOrigin, pDestination, lCameFrom), lTotalCost);
        }

        public IReadOnlyCollection<GridCoord> GetReachableCells(GridCoord pOrigin, int pMaxCost, UnitId pMovingUnitId)
        {
            List<GridCoord> lFrontier = new List<GridCoord> { pOrigin };
            Dictionary<GridCoord, int> lCosts = new Dictionary<GridCoord, int> { [pOrigin] = 0 };

            while (lFrontier.Count > 0)
            {
                GridCoord lCurrent = PopLowestCost(lFrontier, lCosts);

                foreach (GridCoord lNeighbour in _GridService.GetNeighbours(lCurrent))
                {
                    if (!CanOccupy(lNeighbour, pMovingUnitId))
                        continue;

                    int lNextCost = lCosts[lCurrent] + _GridService.GetMovementCost(lNeighbour);
                    if (lNextCost > pMaxCost)
                        continue;

                    if (lCosts.TryGetValue(lNeighbour, out int lExistingCost) && lExistingCost <= lNextCost)
                        continue;

                    lCosts[lNeighbour] = lNextCost;
                    if (!lFrontier.Contains(lNeighbour))
                        lFrontier.Add(lNeighbour);
                }
            }

            return new List<GridCoord>(lCosts.Keys);
        }

        #endregion

        #region _____________________________| HELPERS

        private bool CanOccupy(GridCoord pCoordinate, UnitId pMovingUnitId) =>
            _GridService.CanUnitOccupy(pMovingUnitId, pCoordinate);

        private static GridCoord PopLowestCost(IList<GridCoord> pFrontier, IReadOnlyDictionary<GridCoord, int> pCosts)
        {
            int lLowestIndex = 0;
            int lLowestCost = pCosts[pFrontier[0]];

            for (int lIndex = 1; lIndex < pFrontier.Count; lIndex++)
            {
                GridCoord lCandidate = pFrontier[lIndex];
                int lCandidateCost = pCosts[lCandidate];

                if (lCandidateCost < lLowestCost)
                {
                    lLowestIndex = lIndex;
                    lLowestCost = lCandidateCost;
                }
            }

            GridCoord lNext = pFrontier[lLowestIndex];
            pFrontier.RemoveAt(lLowestIndex);
            return lNext;
        }

        private static IReadOnlyList<GridCoord> ReconstructPath(
            GridCoord pOrigin,
            GridCoord pDestination,
            IReadOnlyDictionary<GridCoord, GridCoord> pCameFrom)
        {
            List<GridCoord> lPath = new List<GridCoord> { pDestination };
            GridCoord lCurrent = pDestination;

            while (lCurrent != pOrigin && pCameFrom.TryGetValue(lCurrent, out GridCoord lPrevious))
            {
                lCurrent = lPrevious;
                lPath.Add(lCurrent);
            }

            lPath.Reverse();
            return lPath;
        }

        #endregion
    }
}
