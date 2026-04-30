#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Shared;

namespace TacticalPort.Core
{
    public sealed class SkillTarget
    {
        #region _____________________________| INIT

        private SkillTarget(GridCoord pCell)
        {
            Cell = pCell;
        }

        #endregion

        #region _____________________________/ ACCESSORS

        public GridCoord Cell { get; }

        #endregion

        #region _____________________________| FACTORIES

        public static SkillTarget ForCell(GridCoord pCell) => new SkillTarget(pCell);

        #endregion
    }
}
