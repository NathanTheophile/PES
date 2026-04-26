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
        bool TryGetOccupant(GridCoord coordinate, out BattleUnitId unitId);
        bool TryGetUnitPosition(BattleUnitId unitId, out GridCoord coordinate);
        bool CanUnitOccupy(BattleUnitId unitId, GridCoord coordinate);
        IReadOnlyCollection<GridCoord> GetOccupiedCells(BattleUnitId unitId);
        IReadOnlyCollection<GridGlyphRuntime> GetGlyphsAt(GridCoord coordinate);
        IReadOnlyCollection<GridCoord> GetNeighbours(GridCoord coordinate);
        void RegisterUnitFootprint(BattleUnitId unitId, IReadOnlyCollection<GridCoord> occupiedCellOffsets);
        bool TryPlaceUnit(BattleUnitId unitId, GridCoord coordinate);
        bool TryMoveUnit(BattleUnitId unitId, GridCoord destination);
        bool RemoveUnit(BattleUnitId unitId);
        void AddOrReplaceGlyph(GridGlyphRuntime glyph);
        void AdvancePersistentEffects();
    }
}
