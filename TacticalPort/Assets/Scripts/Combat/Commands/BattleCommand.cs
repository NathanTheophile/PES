#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Core;
using TacticalPort.Shared;

namespace TacticalPort.Combat
{
    public readonly struct BattleCommand
    {
        #region _____________________________| INIT

        private BattleCommand(BattleCommandType pType, UnitId pUnitId, SkillId pSkillId, GridCoord pCell)
        {
            Type = pType;
            UnitId = pUnitId;
            SkillId = pSkillId;
            Cell = pCell;
        }

        #endregion

        #region _____________________________/ ACCESSORS

        public BattleCommandType Type { get; }
        public UnitId UnitId { get; }
        public SkillId SkillId { get; }
        public GridCoord Cell { get; }

        public BattleActionType ActionType =>
            Type switch
            {
                BattleCommandType.Move => BattleActionType.Move,
                BattleCommandType.UseSkill => BattleActionType.Skill,
                BattleCommandType.EndTurn => BattleActionType.EndTurn,
                BattleCommandType.PlaceUnit => BattleActionType.Placement,
                BattleCommandType.ReadyPlacement => BattleActionType.Placement,
                _ => BattleActionType.Move
            };

        #endregion

        #region _____________________________| FACTORIES

        public static BattleCommand Move(UnitId pUnitId, GridCoord pDestination) =>
            new BattleCommand(BattleCommandType.Move, pUnitId, SkillId.None, pDestination);

        public static BattleCommand UseSkill(UnitId pUnitId, SkillId pSkillId, GridCoord pTargetCell) =>
            new BattleCommand(BattleCommandType.UseSkill, pUnitId, pSkillId, pTargetCell);

        public static BattleCommand EndTurn(UnitId pUnitId) =>
            new BattleCommand(BattleCommandType.EndTurn, pUnitId, SkillId.None, default);

        public static BattleCommand PlaceUnit(UnitId pUnitId, GridCoord pDestination) =>
            new BattleCommand(BattleCommandType.PlaceUnit, pUnitId, SkillId.None, pDestination);

        public static BattleCommand ReadyPlacement() =>
            new BattleCommand(BattleCommandType.ReadyPlacement, UnitId.None, SkillId.None, default);

        #endregion
    }
}
