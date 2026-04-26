using System;
using TacticalPort.Shared;

namespace TacticalPort.Core.Runtime
{
    public sealed class SkillTarget
    {
        #region _____________________________| INIT

        private SkillTarget(SkillTargetType pTargetType, GridCoord pCell, BattleUnitId pUnitId)
        {
            TargetType = pTargetType;
            Cell = pCell;
            UnitId = pUnitId;
        }

        #endregion

        #region _____________________________| ACCESSORS

        public SkillTargetType TargetType { get; }
        public GridCoord Cell { get; }
        public BattleUnitId UnitId { get; }

        #endregion

        #region _____________________________| FACTORIES

        public static SkillTarget ForSelf(BattleUnitId pUnitId) => new SkillTarget(SkillTargetType.Self, new GridCoord(0, 0), pUnitId);
        public static SkillTarget ForUnit(BattleUnitId pUnitId) => new SkillTarget(SkillTargetType.Unit, new GridCoord(0, 0), pUnitId);
        public static SkillTarget ForCell(GridCoord pCell) => new SkillTarget(SkillTargetType.Cell, pCell, BattleUnitId.None);

        #endregion

        #region _____________________________| HELPERS

        public void EnsureCompatibleWith(SkillTargetType pExpectedType)
        {
            if (TargetType != pExpectedType)
                throw new InvalidOperationException($"Target type {TargetType} is incompatible with expected {pExpectedType}.");
        }

        #endregion
    }
}
