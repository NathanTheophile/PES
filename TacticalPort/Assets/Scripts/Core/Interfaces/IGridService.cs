using System.Collections.Generic;
using TacticalPort.Core.Runtime;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core.Interfaces
{
    public interface IGridService
    {
        int Width { get; }
        int Height { get; }

        void Initialize(BattleScenarioDefinition pScenario);
        bool IsInside(GridCoord coordinate);
        bool IsWalkable(GridCoord coordinate);
        bool BlocksLineOfSight(GridCoord coordinate);
        int GetMovementCost(GridCoord coordinate);
        bool IsOccupied(GridCoord coordinate);
        bool TryGetOccupant(GridCoord coordinate, out UnitId unitId);
        bool TryGetUnitPosition(UnitId unitId, out GridCoord coordinate);
        bool CanUnitOccupy(UnitId unitId, GridCoord coordinate);
        IReadOnlyCollection<GridCoord> GetOccupiedCells(UnitId unitId);
        IReadOnlyCollection<GridGlyphRuntime> GetGlyphsAt(GridCoord coordinate);
        IReadOnlyCollection<GridCoord> GetNeighbours(GridCoord coordinate);
        void RegisterUnitFootprint(UnitId unitId, IReadOnlyCollection<GridCoord> occupiedCellOffsets);
        bool TryPlaceUnit(UnitId unitId, GridCoord coordinate);
        bool TryMoveUnit(UnitId unitId, GridCoord destination);
        bool RemoveUnit(UnitId unitId);
        void AddOrReplaceGlyph(GridGlyphRuntime glyph);
        void AdvancePersistentEffects();
    }
}
