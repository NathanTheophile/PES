using System;
using System.Collections.Generic;
using TacticalPort.Core.Interfaces;
using TacticalPort.Core.Runtime;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core.Services
{
    public sealed class GridService : IGridService
    {
        #region _____________________________| VALUES

        private readonly Dictionary<GridCoord, CellDefinition> _CellsByCoordinate = new Dictionary<GridCoord, CellDefinition>();
        private readonly Dictionary<GridCoord, UnitId> _OccupantsByCoordinate = new Dictionary<GridCoord, UnitId>();
        private readonly Dictionary<UnitId, GridCoord> _CoordinatesByUnit = new Dictionary<UnitId, GridCoord>();
        private readonly Dictionary<UnitId, List<GridCoord>> _FootprintsByUnit = new Dictionary<UnitId, List<GridCoord>>();
        private readonly Dictionary<GridCoord, List<GridGlyphRuntime>> _GlyphsByCoordinate = new Dictionary<GridCoord, List<GridGlyphRuntime>>();
        private BattleScenarioDefinition _Scenario;

        #endregion

        #region _____________________________| ACCESSORS

        public int Width => _Scenario != null ? _Scenario.Width : 0;
        public int Height => _Scenario != null ? _Scenario.Height : 0;

        #endregion

        #region _____________________________| SETUP

        public void Initialize(BattleScenarioDefinition pScenario)
        {
            if (pScenario == null)
                throw new ArgumentNullException(nameof(pScenario));

            if (pScenario.Width <= 0 || pScenario.Height <= 0)
                throw new InvalidOperationException("Grid dimensions must be strictly positive.");

            _Scenario = pScenario;
            _CellsByCoordinate.Clear();
            _OccupantsByCoordinate.Clear();
            _CoordinatesByUnit.Clear();
            _FootprintsByUnit.Clear();
            _GlyphsByCoordinate.Clear();

            foreach (CellDefinition lCell in pScenario.EnumerateCells())
            {
                if (lCell == null)
                    continue;

                _CellsByCoordinate[lCell.Coordinate.ToRuntime()] = lCell;
            }
        }

        #endregion

        #region _____________________________| QUERY

        public bool IsInside(GridCoord pCoordinate) => pCoordinate.X >= 0 && pCoordinate.Y >= 0 && pCoordinate.X < Width && pCoordinate.Y < Height;

        public bool IsWalkable(GridCoord pCoordinate)
        {
            if (!IsInside(pCoordinate))
                return false;

            return _CellsByCoordinate.TryGetValue(pCoordinate, out CellDefinition lCell) ? lCell.IsWalkable : true;
        }

        public bool BlocksLineOfSight(GridCoord pCoordinate)
        {
            if (!IsInside(pCoordinate))
                return true;

            return _CellsByCoordinate.TryGetValue(pCoordinate, out CellDefinition lCell) && lCell.BlocksLineOfSight;
        }

        public int GetMovementCost(GridCoord pCoordinate)
        {
            if (!IsWalkable(pCoordinate))
                return int.MaxValue;

            if (_CellsByCoordinate.TryGetValue(pCoordinate, out CellDefinition lCell))
                return Math.Max(1, lCell.MovementCost > 0 ? lCell.MovementCost : _Scenario.DefaultMovementCost);

            return _Scenario != null ? Math.Max(1, _Scenario.DefaultMovementCost) : 1;
        }

        public bool IsOccupied(GridCoord pCoordinate) => _OccupantsByCoordinate.ContainsKey(pCoordinate);
        public bool TryGetOccupant(GridCoord pCoordinate, out UnitId pUnitId) => _OccupantsByCoordinate.TryGetValue(pCoordinate, out pUnitId);
        public bool TryGetUnitPosition(UnitId pUnitId, out GridCoord pCoordinate) => _CoordinatesByUnit.TryGetValue(pUnitId, out pCoordinate);

        public bool CanUnitOccupy(UnitId pUnitId, GridCoord pCoordinate)
        {
            foreach (GridCoord lCell in EnumerateOccupiedCells(pUnitId, pCoordinate))
            {
                if (!IsWalkable(lCell))
                    return false;

                if (_OccupantsByCoordinate.TryGetValue(lCell, out UnitId lOccupant) && lOccupant != pUnitId)
                    return false;
            }

            return true;
        }

        public IReadOnlyCollection<GridCoord> GetOccupiedCells(UnitId pUnitId)
        {
            if (!_CoordinatesByUnit.TryGetValue(pUnitId, out GridCoord lCoordinate))
                return Array.Empty<GridCoord>();

            return new List<GridCoord>(EnumerateOccupiedCells(pUnitId, lCoordinate));
        }

        public IReadOnlyCollection<GridGlyphRuntime> GetGlyphsAt(GridCoord pCoordinate)
        {
            if (!_GlyphsByCoordinate.TryGetValue(pCoordinate, out List<GridGlyphRuntime> lGlyphs) || lGlyphs == null || lGlyphs.Count == 0)
                return Array.Empty<GridGlyphRuntime>();

            return lGlyphs.AsReadOnly();
        }

        public IReadOnlyCollection<GridCoord> GetNeighbours(GridCoord pCoordinate)
        {
            List<GridCoord> lNeighbours = new List<GridCoord>();

            foreach (GridCoord lNeighbour in pCoordinate.GetCardinalNeighbors())
            {
                if (IsWalkable(lNeighbour))
                    lNeighbours.Add(lNeighbour);
            }

            return lNeighbours;
        }

        #endregion

        #region _____________________________| OCCUPANCY

        public void RegisterUnitFootprint(UnitId pUnitId, IReadOnlyCollection<GridCoord> pOccupiedCellOffsets)
        {
            List<GridCoord> lOffsets = new List<GridCoord>();

            if (pOccupiedCellOffsets != null)
            {
                HashSet<GridCoord> lUniqueOffsets = new HashSet<GridCoord>();
                foreach (GridCoord lOffset in pOccupiedCellOffsets)
                {
                    if (lUniqueOffsets.Add(lOffset))
                        lOffsets.Add(lOffset);
                }
            }

            if (lOffsets.Count == 0)
                lOffsets.Add(new GridCoord(0, 0));

            _FootprintsByUnit[pUnitId] = lOffsets;
        }

        public bool TryPlaceUnit(UnitId pUnitId, GridCoord pCoordinate)
        {
            if (!pUnitId.IsValid || _CoordinatesByUnit.ContainsKey(pUnitId) || !CanUnitOccupy(pUnitId, pCoordinate))
                return false;

            foreach (GridCoord lCell in EnumerateOccupiedCells(pUnitId, pCoordinate))
                _OccupantsByCoordinate[lCell] = pUnitId;

            _CoordinatesByUnit[pUnitId] = pCoordinate;
            return true;
        }

        public bool TryMoveUnit(UnitId pUnitId, GridCoord pDestination)
        {
            if (!pUnitId.IsValid || !_CoordinatesByUnit.TryGetValue(pUnitId, out GridCoord lOrigin) || !CanUnitOccupy(pUnitId, pDestination))
                return false;

            foreach (GridCoord lCell in EnumerateOccupiedCells(pUnitId, lOrigin))
                _OccupantsByCoordinate.Remove(lCell);

            foreach (GridCoord lCell in EnumerateOccupiedCells(pUnitId, pDestination))
                _OccupantsByCoordinate[lCell] = pUnitId;

            _CoordinatesByUnit[pUnitId] = pDestination;
            return true;
        }

        public bool RemoveUnit(UnitId pUnitId)
        {
            if (!_CoordinatesByUnit.TryGetValue(pUnitId, out GridCoord lCoordinate))
                return false;

            _CoordinatesByUnit.Remove(pUnitId);

            foreach (GridCoord lCell in EnumerateOccupiedCells(pUnitId, lCoordinate))
                _OccupantsByCoordinate.Remove(lCell);

            return true;
        }

        #endregion

        #region _____________________________| GLYPHS

        public void AddOrReplaceGlyph(GridGlyphRuntime pGlyph)
        {
            if (pGlyph == null || !IsInside(pGlyph.Cell))
                return;

            if (!_GlyphsByCoordinate.TryGetValue(pGlyph.Cell, out List<GridGlyphRuntime> lGlyphs) || lGlyphs == null)
            {
                lGlyphs = new List<GridGlyphRuntime>();
                _GlyphsByCoordinate[pGlyph.Cell] = lGlyphs;
            }

            for (int lIndex = lGlyphs.Count - 1; lIndex >= 0; lIndex--)
            {
                GridGlyphRuntime lExistingGlyph = lGlyphs[lIndex];
                if (lExistingGlyph == null || lExistingGlyph.SourceSkillId == pGlyph.SourceSkillId)
                    lGlyphs.RemoveAt(lIndex);
            }

            lGlyphs.Add(pGlyph);
        }

        public void AdvancePersistentEffects()
        {
            List<GridCoord> lCoordinates = new List<GridCoord>(_GlyphsByCoordinate.Keys);

            for (int lCoordIndex = 0; lCoordIndex < lCoordinates.Count; lCoordIndex++)
            {
                GridCoord lCoordinate = lCoordinates[lCoordIndex];
                if (!_GlyphsByCoordinate.TryGetValue(lCoordinate, out List<GridGlyphRuntime> lGlyphs) || lGlyphs == null)
                    continue;

                for (int lGlyphIndex = lGlyphs.Count - 1; lGlyphIndex >= 0; lGlyphIndex--)
                {
                    GridGlyphRuntime lGlyph = lGlyphs[lGlyphIndex];
                    if (lGlyph == null)
                    {
                        lGlyphs.RemoveAt(lGlyphIndex);
                        continue;
                    }

                    lGlyph.AdvanceTurn();
                    if (lGlyph.IsExpired)
                        lGlyphs.RemoveAt(lGlyphIndex);
                }

                if (lGlyphs.Count == 0)
                    _GlyphsByCoordinate.Remove(lCoordinate);
            }
        }

        #endregion

        #region _____________________________| HELPERS

        private IEnumerable<GridCoord> EnumerateOccupiedCells(UnitId pUnitId, GridCoord pAnchor)
        {
            if (!_FootprintsByUnit.TryGetValue(pUnitId, out List<GridCoord> lOffsets) || lOffsets == null || lOffsets.Count == 0)
            {
                yield return pAnchor;
                yield break;
            }

            for (int lIndex = 0; lIndex < lOffsets.Count; lIndex++)
            {
                GridCoord lOffset = lOffsets[lIndex];
                yield return new GridCoord(pAnchor.X + lOffset.X, pAnchor.Y + lOffset.Y);
            }
        }

        #endregion
    }
}
