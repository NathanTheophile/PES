using TacticalPort.Shared;

namespace TacticalPort.Core.Runtime
{
    public sealed class SkillTarget
    {
        #region _____________________________| INIT

        private SkillTarget(GridCoord pCell)
        {
            Cell = pCell;
        }

        #endregion

        #region _____________________________| ACCESSORS

        public GridCoord Cell { get; }

        #endregion

        #region _____________________________| FACTORIES

        public static SkillTarget ForCell(GridCoord pCell) => new SkillTarget(pCell);

        #endregion
    }
}
