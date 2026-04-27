using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.Core.Runtime
{
    public sealed class GridGlyphRuntime
    {
        public GridGlyphRuntime(
            UnitId pSourceUnitId,
            Team pSourceTeam,
            GridCoord pCell,
            int pPower,
            int pRemainingTurns,
            SkillGlyphTargetRule pTargetRule,
            string pSourceSkillId,
            string pGlyphGroupId = null)
        {
            SourceUnitId = pSourceUnitId;
            SourceTeam = pSourceTeam;
            Cell = pCell;
            Power = pPower < 0 ? 0 : pPower;
            RemainingTurns = pRemainingTurns <= 0 ? 1 : pRemainingTurns;
            TargetRule = pTargetRule;
            SourceSkillId = pSourceSkillId ?? string.Empty;
            GlyphGroupId = string.IsNullOrWhiteSpace(pGlyphGroupId) ? SourceSkillId : pGlyphGroupId;
        }

        public UnitId SourceUnitId { get; }
        public Team SourceTeam { get; }
        public GridCoord Cell { get; }
        public int Power { get; }
        public int RemainingTurns { get; private set; }
        public SkillGlyphTargetRule TargetRule { get; }
        public string SourceSkillId { get; }
        public string GlyphGroupId { get; }
        public bool IsExpired => RemainingTurns <= 0;

        public bool CanAffect(UnitRuntime pUnit)
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
