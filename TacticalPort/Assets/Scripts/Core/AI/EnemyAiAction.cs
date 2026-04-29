#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion




using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core
{
    public enum EnemyAiActionType
    {
        None = 0,
        Move = 1,
        UseSkill = 2,
        EndTurn = 3
    }

    public readonly struct EnemyAiAction
    {
        private EnemyAiAction(EnemyAiActionType pType, GridCoord pDestination, SkillDefinition pSkill, SkillTarget pTarget, string pReason, bool pRequestsKiteAfterUse)
        {
            Type = pType;
            Destination = pDestination;
            Skill = pSkill;
            Target = pTarget;
            Reason = pReason ?? string.Empty;
            RequestsKiteAfterUse = pRequestsKiteAfterUse;
        }

        public EnemyAiActionType Type { get; }
        public GridCoord Destination { get; }
        public SkillDefinition Skill { get; }
        public SkillTarget Target { get; }
        public string Reason { get; }
        public bool RequestsKiteAfterUse { get; }

        public static EnemyAiAction Move(GridCoord pDestination, string pReason = null) =>
            new EnemyAiAction(EnemyAiActionType.Move, pDestination, null, null, pReason, false);

        public static EnemyAiAction UseSkill(SkillDefinition pSkill, SkillTarget pTarget, string pReason = null, bool pRequestsKiteAfterUse = false) =>
            new EnemyAiAction(EnemyAiActionType.UseSkill, default, pSkill, pTarget, pReason, pRequestsKiteAfterUse);

        public static EnemyAiAction EndTurn(string pReason = null) =>
            new EnemyAiAction(EnemyAiActionType.EndTurn, default, null, null, pReason, false);
    }
}
