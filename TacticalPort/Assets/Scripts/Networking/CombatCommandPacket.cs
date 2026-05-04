#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using PurrNet.Packing;
using TacticalPort.Combat;
using TacticalPort.Shared;

namespace TacticalPort.Networking
{
    public struct CombatCommandPacket : IPackedAuto
    {
        public int Type;
        public int UnitIdValue;
        public string SkillIdValue;
        public int CellX;
        public int CellY;

        public bool TryToCommand(out BattleCommand pCommand)
        {
            UnitId lUnitId = new UnitId(UnitIdValue);
            GridCoord lCell = new GridCoord(CellX, CellY);

            switch ((BattleCommandType)Type)
            {
                case BattleCommandType.Move:
                    pCommand = BattleCommand.Move(lUnitId, lCell);
                    return true;

                case BattleCommandType.UseSkill:
                    pCommand = BattleCommand.UseSkill(lUnitId, new SkillId(SkillIdValue), lCell);
                    return true;

                case BattleCommandType.EndTurn:
                    pCommand = BattleCommand.EndTurn(lUnitId);
                    return true;

                case BattleCommandType.PlaceUnit:
                    pCommand = BattleCommand.PlaceUnit(lUnitId, lCell);
                    return true;

                case BattleCommandType.ReadyPlacement:
                    pCommand = BattleCommand.ReadyPlacement();
                    return true;

                default:
                    pCommand = default;
                    return false;
            }
        }

        public static CombatCommandPacket From(BattleCommand pCommand) =>
            new CombatCommandPacket
            {
                Type = (int)pCommand.Type,
                UnitIdValue = pCommand.UnitId.Value,
                SkillIdValue = pCommand.SkillId.Value,
                CellX = pCommand.Cell.X,
                CellY = pCommand.Cell.Y
            };
    }
}
