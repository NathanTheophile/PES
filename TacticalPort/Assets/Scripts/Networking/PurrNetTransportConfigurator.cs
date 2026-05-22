#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Networking
#endregion

using PurrNet;
using PurrNet.Transports;
using PurrNet.UTP;
using System;
using System.Text;
using TacticalPort.Matchmaking;
using UnityEngine;

namespace TacticalPort.Networking
{
    internal static class PurrNetTransportConfigurator
    {
        #region _____________________________| CONFIGURE

        public static bool TryPrepareUdpTransport(
            NetworkManager pNetworkManager,
            UDPTransport pUdpTransport,
            PurrNetConnectionPlan pPlan,
            bool pUseMatchEndpointWhenAvailable,
            bool pUseEdgeGapInjectedServerPort,
            string pEdgeGapPortName,
            out string pEdgeGapLogMessage)
        {
            pEdgeGapLogMessage = string.Empty;

            if (pUdpTransport == null)
                return false;

            MatchServerEndpoint lEndpoint = pPlan.Endpoint;
            bool lUseSessionEndpoint = pUseMatchEndpointWhenAvailable || PurrNetConnectionPlanner.ShouldSessionOverrideInspectorRole(pPlan.Mode);
            if (lUseSessionEndpoint && lEndpoint != null && lEndpoint.IsValid)
            {
                pUdpTransport.address = lEndpoint.IpAddress;
                pUdpTransport.serverPort = lEndpoint.Port;
            }
            else
                ApplyEdgeGapInjectedServerPortIfAvailable(pUdpTransport, pPlan, pUseEdgeGapInjectedServerPort, pEdgeGapPortName, out pEdgeGapLogMessage);

            return TrySetNetworkTransport(pNetworkManager, pUdpTransport);
        }

        public static bool TryPrepareRelayTransport(
            NetworkManager pNetworkManager,
            UTPTransport pUtpTransport,
            PurrNetConnectionPlan pPlan,
            string pPreparedRelayJoinCode,
            out bool pIsPending,
            out string pRelayJoinCode,
            out string pFailure)
        {
            pIsPending = false;
            pRelayJoinCode = string.Empty;
            pFailure = string.Empty;

            if (pUtpTransport == null)
                return false;

            if (!TrySetNetworkTransport(pNetworkManager, pUtpTransport))
                return false;

            pUtpTransport.peerToPeer = true;
            pUtpTransport.dedicatedServer = false;
            pUtpTransport.address = string.IsNullOrWhiteSpace(pPlan.Session?.LobbyId) ? "relay" : pPlan.Session.LobbyId;

            if (pPlan.Role == PurrNetConnectionRole.Host || pPlan.Role == PurrNetConnectionRole.Server)
            {
                if (pPlan.Session?.RelayAllocation == null)
                {
                    pFailure = "Custom Relay host is missing the Relay allocation.";
                    return false;
                }

                return pUtpTransport.InitializeRelayServer(pPlan.Session.RelayAllocation);
            }

            if (pPlan.Role != PurrNetConnectionRole.Client)
                return true;

            pRelayJoinCode = pPlan.Session?.RelayJoinCode;
            if (string.IsNullOrWhiteSpace(pRelayJoinCode))
            {
                pFailure = "Custom Relay join is missing the Relay join code.";
                return false;
            }

            if (pPreparedRelayJoinCode == pRelayJoinCode)
                return true;

            pIsPending = true;
            return true;
        }

        public static bool TrySetNetworkTransport(NetworkManager pNetworkManager, GenericTransport pTransport)
        {
            if (pNetworkManager == null || pTransport == null)
                return false;

            if (pNetworkManager.transport == pTransport)
                return true;

            if (!pNetworkManager.isOffline)
                return false;

            pNetworkManager.transport = pTransport;
            return true;
        }

        #endregion

        #region _____________________________| HELPERS

        private static void ApplyEdgeGapInjectedServerPortIfAvailable(
            UDPTransport pUdpTransport,
            PurrNetConnectionPlan pPlan,
            bool pUseEdgeGapInjectedServerPort,
            string pEdgeGapPortName,
            out string pLogMessage)
        {
            pLogMessage = string.Empty;
            if (!pUseEdgeGapInjectedServerPort || pPlan.Role != PurrNetConnectionRole.Server || pUdpTransport == null)
                return;

            if (!TryReadEdgeGapPort(pEdgeGapPortName, out ushort lPort, out string lVariableName))
                return;

            pUdpTransport.address = "0.0.0.0";
            pUdpTransport.serverPort = lPort;
            pLogMessage = $"EdgeGap UDP port applied from {lVariableName}. Listening on 0.0.0.0:{lPort}";
        }

        private static bool TryReadEdgeGapPort(string pPortName, out ushort pPort, out string pVariableName)
        {
            string[] lVariableNames =
            {
                BuildEdgeGapPortEnvironmentVariableName(pPortName),
                "ARBITRIUM_PORT_GAMEPORT_INTERNAL",
                "ARBITRIUM_PORT_GAME_PORT_INTERNAL"
            };

            for (int lIndex = 0; lIndex < lVariableNames.Length; lIndex++)
            {
                if (TryReadPortEnvironmentVariable(lVariableNames[lIndex], out pPort))
                {
                    pVariableName = lVariableNames[lIndex];
                    return true;
                }
            }

            pPort = 0;
            pVariableName = string.Empty;
            return false;
        }

        private static bool TryReadPortEnvironmentVariable(string pVariableName, out ushort pPort)
        {
            pPort = 0;
            string lValue = Environment.GetEnvironmentVariable(pVariableName);
            return ushort.TryParse(lValue, out pPort) && pPort > 0;
        }

        private static string BuildEdgeGapPortEnvironmentVariableName(string pPortName) =>
            $"ARBITRIUM_PORT_{NormalizeEnvironmentToken(pPortName)}_INTERNAL";

        private static string NormalizeEnvironmentToken(string pValue)
        {
            if (string.IsNullOrWhiteSpace(pValue))
                return "GAMEPORT";

            StringBuilder lBuilder = new StringBuilder(pValue.Length);
            for (int lIndex = 0; lIndex < pValue.Length; lIndex++)
            {
                char lCharacter = pValue[lIndex];
                lBuilder.Append(char.IsLetterOrDigit(lCharacter) ? char.ToUpperInvariant(lCharacter) : '_');
            }

            return lBuilder.ToString();
        }

        #endregion
    }
}
