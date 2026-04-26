using System;
using System.Collections.Generic;
using TacticalPort.Core.Interfaces;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core.Runtime
{
    public sealed class SkillExecutionContext
    {
        #region _____________________________| VALUES

        private readonly IReadOnlyDictionary<UnitId, UnitRuntime> _UnitsById;
        private readonly Func<UnitRuntime, GridCoord, bool> _TryRelocateUnit;
        private readonly Func<UnitRuntime, UnitRuntime, bool> _TrySwapUnits;
        private readonly Func<UnitDefinition, SkillSummonTeamRule, UnitRuntime, GridCoord, UnitRuntime> _TrySummonUnit;
        private readonly Action<GridGlyphRuntime> _AddGlyph;

        #endregion

        #region _____________________________| INIT

        public SkillExecutionContext(
            IGridService pGridService,
            IReadOnlyDictionary<UnitId, UnitRuntime> pUnitsById,
            Func<UnitRuntime, GridCoord, bool> pTryRelocateUnit,
            Func<UnitRuntime, UnitRuntime, bool> pTrySwapUnits,
            Func<UnitDefinition, SkillSummonTeamRule, UnitRuntime, GridCoord, UnitRuntime> pTrySummonUnit,
            Action<GridGlyphRuntime> pAddGlyph)
        {
            GridService = pGridService;
            _UnitsById = pUnitsById;
            _TryRelocateUnit = pTryRelocateUnit;
            _TrySwapUnits = pTrySwapUnits;
            _TrySummonUnit = pTrySummonUnit;
            _AddGlyph = pAddGlyph;
        }

        #endregion

        #region _____________________________| ACCESSORS

        public IGridService GridService { get; }

        #endregion

        #region _____________________________| HELPERS

        public bool TryGetUnit(UnitId pUnitId, out UnitRuntime pUnit) => _UnitsById.TryGetValue(pUnitId, out pUnit);

        public bool TryRelocateUnit(UnitRuntime pUnit, GridCoord pDestination)
        {
            return _TryRelocateUnit != null && _TryRelocateUnit.Invoke(pUnit, pDestination);
        }

        public bool TrySwapUnits(UnitRuntime pFirstUnit, UnitRuntime pSecondUnit)
        {
            return _TrySwapUnits != null && _TrySwapUnits.Invoke(pFirstUnit, pSecondUnit);
        }

        public UnitRuntime TrySummonUnit(UnitDefinition pDefinition, SkillSummonTeamRule pTeamRule, UnitRuntime pSummoner, GridCoord pDestination)
        {
            return _TrySummonUnit != null ? _TrySummonUnit.Invoke(pDefinition, pTeamRule, pSummoner, pDestination) : null;
        }

        public void AddGlyph(GridGlyphRuntime pGlyph)
        {
            _AddGlyph?.Invoke(pGlyph);
        }

        #endregion
    }
}
