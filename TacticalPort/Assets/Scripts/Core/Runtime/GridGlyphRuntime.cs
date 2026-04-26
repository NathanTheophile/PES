using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core.Runtime
{
    public sealed class GridGlyphRuntime
    {
        public GridGlyphRuntime(
            BattleUnitId pSourceUnitId,
            BattleTeam pSourceTeam,
            GridCoord pCell,
            int pPower,
            int pRemainingTurns,
            SkillGlyphTargetRule pTargetRule,
            string pSourceSkillId)
        {
            SourceUnitId = pSourceUnitId;
            SourceTeam = pSourceTeam;
            Cell = pCell;
            Power = pPower < 0 ? 0 : pPower;
            RemainingTurns = pRemainingTurns <= 0 ? 1 : pRemainingTurns;
            TargetRule = pTargetRule;
            SourceSkillId = pSourceSkillId ?? string.Empty;
        }

        public BattleUnitId SourceUnitId { get; }
        public BattleTeam SourceTeam { get; }
        public GridCoord Cell { get; }
        public int Power { get; }
        public int RemainingTurns { get; private set; }
        public SkillGlyphTargetRule TargetRule { get; }
        public string SourceSkillId { get; }
        public bool IsExpired => RemainingTurns <= 0;

        public bool CanAffect(BattleUnitRuntime pUnit)
        {
            if (pUnit == null || !pUnit.IsAlive)
                return false;

            switch (TargetRule)
            {
                case SkillGlyphTargetRule.AlliesOnly:
                    return pUnit.Team == SourceTeam;

                case SkillGlyphTargetRule.EnemiesOnly:
                    return pUnit.Team != SourceTeam;

                default:
                    return true;
            }
        }

        public void AdvanceTurn()
        {
            if (RemainingTurns > 0)
                RemainingTurns--;
        }
    }
}
