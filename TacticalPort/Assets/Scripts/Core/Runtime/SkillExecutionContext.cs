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

        private readonly IReadOnlyDictionary<BattleUnitId, BattleUnitRuntime> _UnitsById;
        private readonly Func<BattleUnitRuntime, GridCoord, bool> _TryRelocateUnit;
        private readonly Func<BattleUnitRuntime, BattleUnitRuntime, bool> _TrySwapUnits;
        private readonly Func<BattleUnitDefinition, SkillSummonTeamRule, BattleUnitRuntime, GridCoord, BattleUnitRuntime> _TrySummonUnit;
        private readonly Action<GridGlyphRuntime> _AddGlyph;

        #endregion

        #region _____________________________| INIT

        public SkillExecutionContext(
            IGridService pGridService,
            IReadOnlyDictionary<BattleUnitId, BattleUnitRuntime> pUnitsById,
            Func<BattleUnitRuntime, GridCoord, bool> pTryRelocateUnit,
            Func<BattleUnitRuntime, BattleUnitRuntime, bool> pTrySwapUnits,
            Func<BattleUnitDefinition, SkillSummonTeamRule, BattleUnitRuntime, GridCoord, BattleUnitRuntime> pTrySummonUnit,
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

        public bool TryGetUnit(BattleUnitId pUnitId, out BattleUnitRuntime pUnit) => _UnitsById.TryGetValue(pUnitId, out pUnit);

        public bool TryRelocateUnit(BattleUnitRuntime pUnit, GridCoord pDestination)
        {
            return _TryRelocateUnit != null && _TryRelocateUnit.Invoke(pUnit, pDestination);
        }

        public bool TrySwapUnits(BattleUnitRuntime pFirstUnit, BattleUnitRuntime pSecondUnit)
        {
            return _TrySwapUnits != null && _TrySwapUnits.Invoke(pFirstUnit, pSecondUnit);
        }

        public BattleUnitRuntime TrySummonUnit(BattleUnitDefinition pDefinition, SkillSummonTeamRule pTeamRule, BattleUnitRuntime pSummoner, GridCoord pDestination)
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
