#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Shared;

namespace TacticalPort.Core
{
    internal sealed class ResolvedSkillTarget
    {
        public GridCoord TargetCell;
        public UnitRuntime PrimaryTargetUnit;
        public List<GridCoord> AffectedCells = new List<GridCoord>();
        public List<UnitRuntime> AffectedUnits = new List<UnitRuntime>();
        public List<string> UsageTargetKeys = new List<string>();
    }
}
