#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Shared
#endregion

namespace TacticalPort.Shared
{
    public static class CombatTeamUtility
    {
        public static MatchPlayerSlot ToSlot(Team pTeam) =>
            pTeam == Team.TeamA
                ? MatchPlayerSlot.TeamA
                : pTeam == Team.TeamB
                    ? MatchPlayerSlot.TeamB
                    : MatchPlayerSlot.None;

        public static Team ToTeam(MatchPlayerSlot pSlot) =>
            pSlot == MatchPlayerSlot.TeamA
                ? Team.TeamA
                : pSlot == MatchPlayerSlot.TeamB
                    ? Team.TeamB
                    : Team.Neutral;

        public static Team GetOpponent(Team pTeam) =>
            pTeam == Team.TeamA
                ? Team.TeamB
                : pTeam == Team.TeamB
                    ? Team.TeamA
                    : Team.Neutral;

        public static CombatTeamRelation ResolveRelation(Team pTeam, MatchPlayerSlot pLocalSlot)
        {
            if (pTeam == Team.Neutral || pLocalSlot == MatchPlayerSlot.None)
                return CombatTeamRelation.Neutral;

            MatchPlayerSlot lTeamSlot = ToSlot(pTeam);
            if (lTeamSlot == MatchPlayerSlot.None)
                return CombatTeamRelation.Neutral;

            return lTeamSlot == pLocalSlot ? CombatTeamRelation.Own : CombatTeamRelation.Opponent;
        }
    }
}
