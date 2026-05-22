#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Networking
#endregion

using TacticalPort.Shared;

namespace TacticalPort.Networking
{
    internal sealed class CombatReadyState
    {
        #region _____________________________/ VALUES

        private bool _IsTeamAReady;
        private bool _IsTeamBReady;

        #endregion

        #region _____________________________| STATE

        public void Reset()
        {
            _IsTeamAReady = false;
            _IsTeamBReady = false;
        }

        public void SetReady(MatchPlayerSlot pSlot, bool pValue)
        {
            if (pSlot == MatchPlayerSlot.TeamA)
                _IsTeamAReady = pValue;
            else if (pSlot == MatchPlayerSlot.TeamB)
                _IsTeamBReady = pValue;
        }

        public bool AreRequiredPlayersReady(bool pRequireRemoteReady, MatchPlayerSlot pLocalSlot = MatchPlayerSlot.TeamA) =>
            pRequireRemoteReady ? _IsTeamAReady && _IsTeamBReady : IsReady(pLocalSlot);

        public string ResolveStatus(bool pRequireRemoteReady) =>
            pRequireRemoteReady
                ? $"Waiting for opponent. TeamA ready: {_IsTeamAReady}. TeamB ready: {_IsTeamBReady}."
                : "Ready.";

        private bool IsReady(MatchPlayerSlot pSlot) =>
            pSlot == MatchPlayerSlot.TeamA
                ? _IsTeamAReady
                : pSlot == MatchPlayerSlot.TeamB && _IsTeamBReady;

        #endregion
    }
}
