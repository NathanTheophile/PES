#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Networking
#endregion

using TacticalPort.Matchmaking;

namespace TacticalPort.Networking
{
    internal readonly struct PurrNetConnectionPlan
    {
        #region _____________________________/ VALUES

        public readonly PurrNetConnectionRole Role;
        public readonly MatchSessionDescriptor Session;
        public readonly MatchServerEndpoint Endpoint;
        public readonly bool CanStartWithoutSession;

        #endregion

        #region _____________________________/ ACCESSORS

        public bool HasSession => Session != null;
        public string MatchId => HasSession ? Session.MatchId : string.Empty;
        public MatchConnectionMode Mode => HasSession ? Session.ConnectionMode : MatchConnectionMode.Offline;

        #endregion

        public PurrNetConnectionPlan(PurrNetConnectionRole pRole, MatchSessionDescriptor pSession, MatchServerEndpoint pEndpoint, bool pCanStartWithoutSession)
        {
            Role = pRole;
            Session = pSession;
            Endpoint = pEndpoint;
            CanStartWithoutSession = pCanStartWithoutSession;
        }
    }
}
