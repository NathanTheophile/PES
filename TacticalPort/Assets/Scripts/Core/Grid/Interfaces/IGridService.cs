#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core
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
        IReadOnlyCollection<GridGlyphRuntime> GetAllGlyphs();
        void RegisterUnitFootprint(UnitId unitId, IReadOnlyCollection<GridCoord> occupiedCellOffsets);
        bool TryPlaceUnit(UnitId unitId, GridCoord coordinate);
        bool TryMoveUnit(UnitId unitId, GridCoord destination);
        bool RemoveUnit(UnitId unitId);
        void AddOrReplaceGlyph(GridGlyphRuntime glyph);
        void AdvancePersistentEffects();
    }
}
