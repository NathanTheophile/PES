#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Networking
#endregion

using PurrNet.Utils;
using TacticalPort.Matchmaking;

namespace TacticalPort.Networking
{
    internal static class PurrNetConnectionPlanner
    {
        #region _____________________________| PLAN

        public static PurrNetConnectionPlan Build(
            MatchSessionDescriptor pSession,
            PurrNetConnectionRole pConnectionRole,
            bool pAllowCloneServerRole,
            bool pAllowServerWithoutMatchContext,
            out bool pShouldLogCloneServerRoleWarning)
        {
            PurrNetConnectionRole lRole = ResolveRole(pSession, pConnectionRole, pAllowCloneServerRole, out pShouldLogCloneServerRoleWarning);
            return new PurrNetConnectionPlan(
                lRole,
                pSession,
                pSession?.Endpoint,
                lRole == PurrNetConnectionRole.Server && (pAllowServerWithoutMatchContext || ApplicationContext.isServerBuild));
        }

        public static bool ShouldSessionOverrideInspectorRole(MatchConnectionMode pMode) =>
            pMode is MatchConnectionMode.CustomHost or MatchConnectionMode.CustomJoin
                or MatchConnectionMode.CustomRelayHost or MatchConnectionMode.CustomRelayJoin
                or MatchConnectionMode.DedicatedServer;

        public static bool IsRelayMode(MatchConnectionMode pMode) =>
            pMode is MatchConnectionMode.CustomRelayHost or MatchConnectionMode.CustomRelayJoin;

        public static bool IsCustomHostMode(MatchConnectionMode pMode) =>
            pMode is MatchConnectionMode.CustomHost or MatchConnectionMode.CustomRelayHost;

        public static bool IsCustomJoinMode(MatchConnectionMode pMode) =>
            pMode is MatchConnectionMode.CustomJoin or MatchConnectionMode.CustomRelayJoin;

        #endregion

        #region _____________________________| HELPERS

        private static PurrNetConnectionRole ResolveRole(
            MatchSessionDescriptor pSession,
            PurrNetConnectionRole pConnectionRole,
            bool pAllowCloneServerRole,
            out bool pShouldLogCloneServerRoleWarning)
        {
            pShouldLogCloneServerRoleWarning = false;

            if (pConnectionRole == PurrNetConnectionRole.Disabled)
                return PurrNetConnectionRole.Disabled;

            if (pSession != null && ShouldSessionOverrideInspectorRole(pSession.ConnectionMode))
                return ResolveSessionRole(pSession);

            if (ApplicationContext.isServerBuild)
                return PurrNetConnectionRole.Server;

            if (pConnectionRole != PurrNetConnectionRole.Auto)
            {
                if (pConnectionRole == PurrNetConnectionRole.Server && ApplicationContext.isClone && !ApplicationContext.isServerBuild && !pAllowCloneServerRole)
                {
                    pShouldLogCloneServerRoleWarning = true;
                    return PurrNetConnectionRole.Client;
                }

                return pConnectionRole;
            }

            if (pSession != null)
                return ResolveSessionRole(pSession);

            if (ApplicationContext.isClone || ApplicationContext.isClientBuild)
                return PurrNetConnectionRole.Client;

            return PurrNetConnectionRole.Host;
        }

        private static PurrNetConnectionRole ResolveSessionRole(MatchSessionDescriptor pSession) =>
            pSession.ConnectionMode switch
            {
                MatchConnectionMode.CustomHost => PurrNetConnectionRole.Host,
                MatchConnectionMode.CustomJoin => PurrNetConnectionRole.Client,
                MatchConnectionMode.CustomRelayHost => PurrNetConnectionRole.Server,
                MatchConnectionMode.CustomRelayJoin => PurrNetConnectionRole.Client,
                MatchConnectionMode.QuickMatchLocalServer => PurrNetConnectionRole.Client,
                MatchConnectionMode.DedicatedServer => ApplicationContext.isServerBuild ? PurrNetConnectionRole.Server : PurrNetConnectionRole.Client,
                _ => PurrNetConnectionRole.Disabled
            };

        #endregion
    }
}
