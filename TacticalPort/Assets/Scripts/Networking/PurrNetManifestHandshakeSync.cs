#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Networking
#endregion

using PurrNet;
using PurrNet.Modules;
using PurrNet.Transports;
using TacticalPort.Matchmaking;
using TacticalPort.Shared;
using TacticalPort.State;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TacticalPort.Networking
{
    internal interface IPurrNetManifestHandshakeContext
    {
        MatchRuntimeContext MatchContext { get; }
        NetworkManager NetworkManager { get; }
        UDPTransport UdpTransport { get; }
        bool AllowServerMatchReplacementInMainMenu { get; }
        string MainMenuSceneName { get; }

        PurrNetConnectionPlan BuildConnectionPlanForManifest();
        void LogManifest(string pMessage);
        void LogManifestWarning(string pMessage);
    }

    internal sealed class PurrNetManifestHandshakeSync
    {
        #region _____________________________/ VALUES

        private readonly IPurrNetManifestHandshakeContext _Context;
        private PlayersManager _ServerPlayers;
        private PlayersManager _ClientPlayers;
        private bool _IsServerHandshakeSubscribed;
        private bool _IsClientManifestSubscribed;
        private float _NextCompositionHandshakeTime;
        private bool _HasLoggedMissingLocalComposition;
        private MatchManifest _PendingServerManifest;

        #endregion

        #region _____________________________| INIT

        public PurrNetManifestHandshakeSync(IPurrNetManifestHandshakeContext pContext)
        {
            _Context = pContext;
        }

        #endregion

        #region _____________________________| LIFECYCLE

        public void ResetSessionState()
        {
            _NextCompositionHandshakeTime = 0f;
            _HasLoggedMissingLocalComposition = false;
            _PendingServerManifest = null;
        }

        public void SubscribeServerHandshakeIfReady()
        {
            NetworkManager lNetworkManager = _Context.NetworkManager;
            if (_IsServerHandshakeSubscribed || lNetworkManager == null || !lNetworkManager.isServer)
                return;

            if (!lNetworkManager.TryGetModule(true, out PlayersManager lPlayers))
                return;

            _ServerPlayers = lPlayers;
            _ServerPlayers.Subscribe<PurrNetCombatHandshakeMessage>(HandleCombatHandshake);
            _ServerPlayers.Subscribe<PurrNetCustomMatchJoinRequestMessage>(HandleCustomMatchJoinRequest);
            _IsServerHandshakeSubscribed = true;
        }

        public void UnsubscribeServerHandshake()
        {
            if (!_IsServerHandshakeSubscribed || _ServerPlayers == null)
                return;

            _ServerPlayers.Unsubscribe<PurrNetCombatHandshakeMessage>(HandleCombatHandshake);
            _ServerPlayers.Unsubscribe<PurrNetCustomMatchJoinRequestMessage>(HandleCustomMatchJoinRequest);
            _ServerPlayers = null;
            _IsServerHandshakeSubscribed = false;
        }

        public void SubscribeClientManifestSyncIfReady()
        {
            NetworkManager lNetworkManager = _Context.NetworkManager;
            if (_IsClientManifestSubscribed || lNetworkManager == null || !lNetworkManager.isClient)
                return;

            if (!lNetworkManager.TryGetModule(false, out PlayersManager lPlayers))
                return;

            _ClientPlayers = lPlayers;
            _ClientPlayers.Subscribe<PurrNetMatchManifestMessage>(HandleMatchManifestMessage);
            _IsClientManifestSubscribed = true;
        }

        public void UnsubscribeClientManifestSync()
        {
            if (!_IsClientManifestSubscribed || _ClientPlayers == null)
                return;

            _ClientPlayers.Unsubscribe<PurrNetMatchManifestMessage>(HandleMatchManifestMessage);
            _ClientPlayers = null;
            _IsClientManifestSubscribed = false;
        }

        #endregion

        #region _____________________________| CLIENT HANDSHAKE

        public void SendClientCompositionHandshakeIfReady(float pCompositionHandshakeRetrySeconds)
        {
            MatchRuntimeContext lMatchContext = _Context.MatchContext;
            if (_ClientPlayers == null || lMatchContext == null || !lMatchContext.HasMatch)
                return;

            MatchManifest lManifest = lMatchContext.Manifest;
            if (lManifest == null || lManifest.HasAllPlayerCompositions)
                return;

            PlayerIdentity lLocalPlayer = lMatchContext.LocalPlayer;
            if (!lLocalPlayer.IsValid || !TryApplyLocalComposition(lManifest, lLocalPlayer.PlayerId))
                return;

            if (Time.unscaledTime < _NextCompositionHandshakeTime)
                return;

            _NextCompositionHandshakeTime = Time.unscaledTime + pCompositionHandshakeRetrySeconds;

            if (PurrNetConnectionPlanner.IsCustomJoinMode(lMatchContext.ConnectionMode))
            {
                _ClientPlayers.SendToServer(new PurrNetCustomMatchJoinRequestMessage
                {
                    PlayerId = lLocalPlayer.PlayerId,
                    MatchId = lManifest.MatchId,
                    ManifestJson = JsonUtility.ToJson(lManifest)
                });
                _Context.LogManifest($"Sent custom join composition to host. PlayerId={lLocalPlayer.PlayerId}, MatchId={lManifest.MatchId}");
                return;
            }

            if (PurrNetConnectionPlanner.IsCustomHostMode(lMatchContext.ConnectionMode))
                return;

            _ClientPlayers.SendToServer(new PurrNetCombatHandshakeMessage
            {
                PlayerId = lLocalPlayer.PlayerId,
                MatchId = lManifest.MatchId,
                ManifestJson = JsonUtility.ToJson(lManifest)
            });
            _Context.LogManifest($"Sent combat composition to server. PlayerId={lLocalPlayer.PlayerId}, MatchId={lManifest.MatchId}");
        }

        private bool TryApplyLocalComposition(MatchManifest pManifest, string pPlayerId)
        {
            if (MatchCombatCompositionImporter.ExportLocalSelection(
                    new PlayerIdentity(pPlayerId, string.Empty),
                    pManifest))
                return true;

            if (!_HasLoggedMissingLocalComposition)
            {
                _HasLoggedMissingLocalComposition = true;
                _Context.LogManifestWarning("Cannot send match composition: no saved local team selection was found. Open Team Selection, save a team, then start the match again.");
            }

            return false;
        }

        #endregion

        #region _____________________________| SERVER HANDSHAKE

        private void HandleCombatHandshake(PlayerID pPlayer, PurrNetCombatHandshakeMessage pMessage, bool pAsServer)
        {
            MatchRuntimeContext lMatchContext = _Context.MatchContext;
            if (!pAsServer || lMatchContext == null)
                return;

            MatchManifest lCurrentManifest = ResolveServerManifestForHandshake(pMessage.MatchId);
            if (!CombatMatchManifestImporter.TryImport(lCurrentManifest, pMessage.MatchId, pMessage.ManifestJson, true, out MatchManifest lManifest, out string lFailure))
            {
                _Context.LogManifestWarning($"Server rejected match manifest from player {pPlayer}. {lFailure}");
                return;
            }

            if (lMatchContext.HasMatch)
            {
                if (lMatchContext.MatchId != pMessage.MatchId)
                {
                    _PendingServerManifest = lManifest;
                    if (!_PendingServerManifest.HasAllPlayerCompositions)
                    {
                        _Context.LogManifest($"Server replacing stale match context and waiting for all compositions. PreviousMatchId={lMatchContext.MatchId}, IncomingMatchId={pMessage.MatchId}");
                        return;
                    }

                    _PendingServerManifest = null;
                    lMatchContext.SetQuickMatchResult(new PlayerIdentity("server", "Server"), BuildServerTicket(lManifest));
                    BroadcastManifestIfReady(lManifest);
                    return;
                }

                lMatchContext.RefreshCompositionsFromManifest();
                BroadcastManifestIfReady(lMatchContext.Manifest);
                return;
            }

            _PendingServerManifest = lManifest;
            if (!_PendingServerManifest.HasAllPlayerCompositions)
            {
                _Context.LogManifest($"Server waiting for all player compositions. MatchId={_PendingServerManifest.MatchId}");
                return;
            }

            _PendingServerManifest = null;
            lMatchContext.SetQuickMatchResult(new PlayerIdentity("server", "Server"), BuildServerTicket(lManifest));
            BroadcastManifestIfReady(lManifest);
            _Context.LogManifest($"Server match context received from player {pPlayer}. MatchId={lManifest.MatchId}");
        }

        private void HandleCustomMatchJoinRequest(PlayerID pPlayer, PurrNetCustomMatchJoinRequestMessage pMessage, bool pAsServer)
        {
            MatchRuntimeContext lMatchContext = _Context.MatchContext;
            if (!pAsServer || lMatchContext == null || !lMatchContext.HasMatch)
                return;

            if (!PurrNetConnectionPlanner.IsCustomHostMode(lMatchContext.ConnectionMode) || lMatchContext.MatchId != pMessage.MatchId)
            {
                _Context.LogManifestWarning($"Rejected custom join from {pPlayer}. HostMode={lMatchContext.ConnectionMode}, HostMatchId={lMatchContext.MatchId}, IncomingMatchId={pMessage.MatchId}");
                return;
            }

            if (!PurrNetMatchManifestMessageParser.TryParseCustomJoinManifest(pMessage, out MatchManifest lIncomingManifest, out string lFailure))
            {
                _Context.LogManifestWarning($"Rejected custom join from {pPlayer}. {lFailure}");
                return;
            }

            if (!lIncomingManifest.TryGetAssignment(pMessage.PlayerId, out MatchPlayerAssignment lIncomingPlayer) || lIncomingPlayer.Slot != MatchPlayerSlot.TeamB)
            {
                _Context.LogManifestWarning($"Rejected custom join from {pPlayer}: player '{pMessage.PlayerId}' is not assigned to TeamB.");
                return;
            }

            MatchManifest lHostManifest = lMatchContext.Manifest;
            lHostManifest.SetAssignment(
                MatchPlayerSlot.TeamB,
                pMessage.PlayerId,
                lIncomingPlayer.TeamPresetId,
                lIncomingPlayer.UnitIds,
                lIncomingPlayer.UnitBuilds);
            lMatchContext.SetManifest(lHostManifest);
            BroadcastManifestIfReady(lHostManifest);
            _Context.LogManifest($"Custom match player joined. PlayerId={pMessage.PlayerId}, MatchId={pMessage.MatchId}");
        }

        #endregion

        #region _____________________________| CLIENT MANIFEST

        private void HandleMatchManifestMessage(PlayerID pPlayer, PurrNetMatchManifestMessage pMessage, bool pAsServer)
        {
            MatchRuntimeContext lMatchContext = _Context.MatchContext;
            if (pAsServer || lMatchContext == null || !lMatchContext.HasMatch)
                return;

            if (PurrNetConnectionPlanner.IsCustomJoinMode(lMatchContext.ConnectionMode))
            {
                if (PurrNetMatchManifestMessageParser.TryAcceptCustomServerManifest(pMessage, lMatchContext.MatchId, lMatchContext.LocalPlayer.PlayerId, out MatchManifest lCustomManifest, out string lCustomFailure))
                {
                    lMatchContext.SetManifest(lCustomManifest);
                    _Context.LogManifest($"Client received custom match manifest. MatchId={lCustomManifest.MatchId}");
                }
                else
                    _Context.LogManifestWarning($"Client rejected custom match manifest sync. {lCustomFailure}");

                return;
            }

            if (!CombatMatchManifestImporter.TryImport(lMatchContext.Manifest, pMessage.MatchId, pMessage.ManifestJson, false, out MatchManifest lManifest, out string lFailure))
            {
                _Context.LogManifestWarning($"Client rejected match manifest sync. {lFailure}");
                return;
            }

            lMatchContext.SetManifest(lManifest);
            _Context.LogManifest($"Client received full match manifest. MatchId={lManifest.MatchId}");
        }

        #endregion

        #region _____________________________| HELPERS

        private MatchManifest ResolveServerManifestForHandshake(string pIncomingMatchId)
        {
            MatchRuntimeContext lMatchContext = _Context.MatchContext;
            if (lMatchContext == null || !lMatchContext.HasMatch)
                return ResetPendingManifestIfMatchChanged(pIncomingMatchId);

            if (lMatchContext.MatchId == pIncomingMatchId)
                return lMatchContext.Manifest;

            if (CanReplaceServerMatchInCurrentScene())
                return ResetPendingManifestIfMatchChanged(pIncomingMatchId);

            return lMatchContext.Manifest;
        }

        private MatchManifest ResetPendingManifestIfMatchChanged(string pIncomingMatchId)
        {
            if (_PendingServerManifest != null && _PendingServerManifest.MatchId != pIncomingMatchId)
                _PendingServerManifest = null;

            return _PendingServerManifest;
        }

        private bool CanReplaceServerMatchInCurrentScene() =>
            _Context.AllowServerMatchReplacementInMainMenu &&
            !string.IsNullOrWhiteSpace(_Context.MainMenuSceneName) &&
            IsServerInstance() &&
            SceneManager.GetActiveScene().name == _Context.MainMenuSceneName;

        private bool IsServerInstance()
        {
            NetworkManager lNetworkManager = _Context.NetworkManager;
            return (lNetworkManager != null && lNetworkManager.isServer) ||
                   _Context.BuildConnectionPlanForManifest().Role == PurrNetConnectionRole.Server;
        }

        private void BroadcastManifestIfReady(MatchManifest pManifest)
        {
            if (_ServerPlayers == null || pManifest == null || !pManifest.HasAllPlayerCompositions)
                return;

            _ServerPlayers.SendToAll(new PurrNetMatchManifestMessage
            {
                MatchId = pManifest.MatchId,
                ManifestJson = JsonUtility.ToJson(pManifest)
            });
        }

        private MatchTicketSnapshot BuildServerTicket(MatchManifest pManifest)
        {
            UDPTransport lUdpTransport = _Context.UdpTransport;
            return new MatchTicketSnapshot
            {
                TicketId = string.Empty,
                Status = MatchTicketStatus.Found,
                Manifest = pManifest,
                ServerEndpoint = new MatchServerEndpoint
                {
                    IpAddress = lUdpTransport != null ? lUdpTransport.address : "127.0.0.1",
                    Port = lUdpTransport != null ? lUdpTransport.serverPort : (ushort)5000,
                    AllocationId = pManifest != null ? pManifest.MatchId : string.Empty
                },
                ConnectionMode = MatchConnectionMode.DedicatedServer,
                IsTrusted = false,
                ReportsRankedResults = false
            };
        }

        #endregion
    }
}
