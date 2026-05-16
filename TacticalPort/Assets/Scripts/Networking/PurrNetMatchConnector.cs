#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Networking
#endregion

using PurrNet;
using PurrNet.Modules;
using PurrNet.Transports;
using PurrNet.UTP;
using PurrNet.Utils;
using System;
using System.Text;
using System.Threading.Tasks;
using TacticalPort.Matchmaking;
using TacticalPort.Shared;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TacticalPort.Networking
{
    public enum PurrNetConnectionRole
    {
        Disabled = 0,
        Auto = 1,
        Host = 2,
        Server = 3,
        Client = 4
    }

    public sealed class PurrNetMatchConnector : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private MatchRuntimeContext _MatchContext;
        [SerializeField] private NetworkManager _NetworkManager;
        [SerializeField] private UDPTransport _UdpTransport;
        [SerializeField] private UTPTransport _UtpTransport;
        [SerializeField] private PurrNetConnectionRole _ConnectionRole = PurrNetConnectionRole.Auto;
        [Tooltip("Enable only when a ParrelSync clone is intentionally used as an extra local server. Normal client clones should leave this disabled.")]
        [SerializeField] private bool _AllowCloneServerRole;
        [SerializeField] private bool _ConnectOnMatchFound = true;
        [SerializeField] private bool _AllowServerWithoutMatchContext = true;
        [Tooltip("Legacy compatibility toggle. Future custom/dedicated flows should keep this enabled so session endpoints drive the transport.")]
        [SerializeField] private bool _UseMatchEndpointWhenAvailable = true;
        [Tooltip("Dedicated server builds on EdgeGap receive the internal UDP port through ARBITRIUM_PORT_<PORT_NAME>_INTERNAL.")]
        [SerializeField] private bool _UseEdgeGapInjectedServerPort = true;
        [SerializeField] private string _EdgeGapPortName = "gameport";
        [SerializeField] private bool _DisableNetworkManagerAutoStart = true;
        [SerializeField] private bool _StopWhenMatchContextClears = true;
        [SerializeField] private bool _AllowServerMatchReplacementInMainMenu = true;
        [SerializeField] private string _MainMenuSceneName = "S_MainMenu";
        [SerializeField, Min(0.25f)] private float _ConnectionRetrySeconds = 1f;
        [SerializeField, Min(0.25f)] private float _CompositionHandshakeRetrySeconds = 1f;
        [SerializeField] private bool _LogEvents = true;

        private bool _HasRequestedConnection;
        private float _NextConnectionRetryTime;
        private float _NextCompositionHandshakeTime;
        private PlayersManager _ServerPlayers;
        private PlayersManager _ClientPlayers;
        private bool _IsServerHandshakeSubscribed;
        private bool _IsClientManifestSubscribed;
        private ConnectionState _LastLoggedClientState = ConnectionState.Disconnected;
        private ConnectionState _LastLoggedServerState = ConnectionState.Disconnected;
        private bool _HasLoggedCloneServerRoleWarning;
        private bool _HasLoggedMissingLocalComposition;
        private bool _IsPreparingRelayConnection;
        private string _PreparedRelayJoinCode = string.Empty;
        private MatchManifest _PendingServerManifest;

        #endregion

        #region _____________________________/ ACCESSORS

        public bool IsConnected => _NetworkManager != null && !_NetworkManager.isOffline;
        public PurrNetConnectionRole CurrentRole => BuildConnectionPlan().Role;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            CacheMissingReferences();
            DisableAutoStartIfNeeded();
        }

        private void OnEnable()
        {
            CacheMissingReferences();
            SubscribeConnectionStateLogs();

            if (_MatchContext != null)
            {
                _MatchContext.Changed -= HandleMatchContextChanged;
                _MatchContext.Changed += HandleMatchContextChanged;
            }

            TryConnect();
        }

        private void OnDisable()
        {
            UnsubscribeServerHandshake();
            UnsubscribeClientManifestSync();
            UnsubscribeConnectionStateLogs();

            if (_MatchContext != null)
                _MatchContext.Changed -= HandleMatchContextChanged;
        }

        private void OnValidate() => CacheMissingReferences();

        private void Update()
        {
            MaintainConnection();
            TrySubscribeServerHandshake();
            TrySubscribeClientManifestSync();
            TrySendClientCompositionHandshake();
        }

        #endregion

        #region _____________________________| CONNECT

        public void StartConnection() => TryConnect(true);

        public string DescribeConnectionState()
        {
            ConnectionPlan lPlan = BuildConnectionPlan();
            string lManagerState = _NetworkManager == null
                ? "NetworkManager=None"
                : $"NetworkManager={_NetworkManager.name}, Offline={_NetworkManager.isOffline}, Client={_NetworkManager.isClient}, Server={_NetworkManager.isServer}, ClientState={_NetworkManager.clientState}, ServerState={_NetworkManager.serverState}";

            string lTransport = _NetworkManager?.transport != null ? _NetworkManager.transport.GetType().Name : "None";
            string lEndpoint = DescribeEndpoint(lPlan);

            return $"Role={lPlan.Role}, Mode={lPlan.Mode}, Requested={_HasRequestedConnection}, Transport={lTransport}, {lManagerState}, {lEndpoint}, MatchId={lPlan.MatchId}";
        }

        public void StopConnection()
        {
            if (_NetworkManager == null || _NetworkManager.isOffline)
                return;

            UnsubscribeClientManifestSync();
            UnsubscribeServerHandshake();

            if (_NetworkManager.isClient)
                _NetworkManager.StopClient();

            if (_NetworkManager.isServer)
                _NetworkManager.StopServer();

            _HasRequestedConnection = false;
            _NextConnectionRetryTime = 0f;
            _NextCompositionHandshakeTime = 0f;
            _HasLoggedMissingLocalComposition = false;
            _IsPreparingRelayConnection = false;
            _PreparedRelayJoinCode = string.Empty;
            _PendingServerManifest = null;

            if (_LogEvents)
                Debug.Log("[PurrNet Match Connector] Network stopped.", this);
        }

        private void HandleMatchContextChanged(MatchRuntimeContext pContext)
        {
            if (pContext != null && pContext.HasMatch)
            {
                TryConnect();
                return;
            }

            _HasRequestedConnection = false;
            _NextConnectionRetryTime = 0f;
            _NextCompositionHandshakeTime = 0f;
            _HasLoggedMissingLocalComposition = false;
            _IsPreparingRelayConnection = false;
            _PreparedRelayJoinCode = string.Empty;

            if (_StopWhenMatchContextClears)
                StopConnection();

            if (ShouldRestartIdleServer())
                TryConnect(true);
        }

        private void TryConnect(bool pForce = false)
        {
            if (!_ConnectOnMatchFound && !pForce)
                return;

            if (_IsPreparingRelayConnection && !pForce)
                return;

            if (_HasRequestedConnection && !pForce)
                return;

            ConnectionPlan lPlan = BuildConnectionPlan();
            PurrNetConnectionRole lRole = lPlan.Role;
            if (!ValidateReferences(lPlan))
                return;

            if (!lPlan.CanStartWithoutSession && !lPlan.HasSession)
                return;

            if (!_NetworkManager.isOffline)
            {
                _HasRequestedConnection = true;

                if (_LogEvents && pForce)
                    Debug.Log($"[PurrNet Match Connector] Connection already active. {DescribeConnectionState()}", this);

                return;
            }

            if (lRole == PurrNetConnectionRole.Disabled)
                return;

            if (!TryPrepareTransport(lPlan, out bool lIsPending))
                return;

            if (lIsPending)
                return;

            _HasRequestedConnection = true;

            if (_LogEvents)
                Debug.Log($"[PurrNet Match Connector] Starting {lRole}. {DescribeConnectionState()}", this);

            if (lRole == PurrNetConnectionRole.Host)
                _NetworkManager.StartHost();
            else if (lRole == PurrNetConnectionRole.Server)
                _NetworkManager.StartServer();
            else if (lRole == PurrNetConnectionRole.Client)
                _NetworkManager.StartClient();
        }

        #endregion

        #region _____________________________| HELPERS

        private void MaintainConnection()
        {
            if (_NetworkManager == null || !_HasRequestedConnection || !_NetworkManager.isOffline || _IsPreparingRelayConnection)
                return;

            ConnectionPlan lPlan = BuildConnectionPlan();
            if (!lPlan.HasSession && !lPlan.CanStartWithoutSession)
                return;

            if (IsConnectionInProgress(lPlan.Role))
                return;

            if (Time.unscaledTime < _NextConnectionRetryTime)
                return;

            _NextConnectionRetryTime = Time.unscaledTime + _ConnectionRetrySeconds;
            _HasRequestedConnection = false;

            if (_LogEvents)
                Debug.Log($"[PurrNet Match Connector] Retrying {lPlan.Role} connection.", this);

            TryConnect(true);
        }

        private bool IsConnectionInProgress(PurrNetConnectionRole pRole)
        {
            if (_NetworkManager == null)
                return false;

            bool lClientConnecting = _NetworkManager.clientState is ConnectionState.Connecting or ConnectionState.Disconnecting;
            bool lServerConnecting = _NetworkManager.serverState is ConnectionState.Connecting or ConnectionState.Disconnecting;

            return pRole switch
            {
                PurrNetConnectionRole.Client => lClientConnecting,
                PurrNetConnectionRole.Server => lServerConnecting,
                PurrNetConnectionRole.Host => lClientConnecting || lServerConnecting,
                _ => false
            };
        }

        private ConnectionPlan BuildConnectionPlan()
        {
            TryGetSessionDescriptor(out MatchSessionDescriptor lSession);
            PurrNetConnectionRole lRole = ResolveRole(lSession);

            return new ConnectionPlan(
                lRole,
                lSession,
                lSession?.Endpoint,
                lRole == PurrNetConnectionRole.Server && (_AllowServerWithoutMatchContext || ApplicationContext.isServerBuild));
        }

        private PurrNetConnectionRole ResolveRole(MatchSessionDescriptor pSession)
        {
            if (_ConnectionRole == PurrNetConnectionRole.Disabled)
                return PurrNetConnectionRole.Disabled;

            if (pSession != null && ShouldSessionOverrideInspectorRole(pSession.ConnectionMode))
                return ResolveSessionRole(pSession);

            if (ApplicationContext.isServerBuild)
                return PurrNetConnectionRole.Server;

            if (_ConnectionRole != PurrNetConnectionRole.Auto)
            {
                if (_ConnectionRole == PurrNetConnectionRole.Server && ApplicationContext.isClone && !ApplicationContext.isServerBuild && !_AllowCloneServerRole)
                {
                    LogCloneServerRoleWarning();
                    return PurrNetConnectionRole.Client;
                }

                return _ConnectionRole;
            }

            if (pSession != null)
                return ResolveSessionRole(pSession);

            if (ApplicationContext.isClone || ApplicationContext.isClientBuild)
                return PurrNetConnectionRole.Client;

            return PurrNetConnectionRole.Host;
        }

        private void LogCloneServerRoleWarning()
        {
            if (_HasLoggedCloneServerRoleWarning || !_LogEvents)
                return;

            _HasLoggedCloneServerRoleWarning = true;
            Debug.LogWarning("[PurrNet Match Connector] Clone has Connection Role=Server, but clone server override is disabled. Starting as Client to avoid UDP port binding conflicts.", this);
        }

        private static bool ShouldSessionOverrideInspectorRole(MatchConnectionMode pMode) =>
            pMode is MatchConnectionMode.CustomHost or MatchConnectionMode.CustomJoin
                or MatchConnectionMode.CustomRelayHost or MatchConnectionMode.CustomRelayJoin
                or MatchConnectionMode.DedicatedServer;

        private PurrNetConnectionRole ResolveSessionRole(MatchSessionDescriptor pSession) =>
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

        private bool TryPrepareTransport(ConnectionPlan pPlan, out bool pIsPending)
        {
            pIsPending = false;

            if (IsRelayMode(pPlan.Mode))
                return TryPrepareRelayTransport(pPlan, out pIsPending);

            return TryPrepareUdpTransport(pPlan);
        }

        private bool TryPrepareUdpTransport(ConnectionPlan pPlan)
        {
            MatchServerEndpoint lEndpoint = pPlan.Endpoint;
            bool lUseSessionEndpoint = _UseMatchEndpointWhenAvailable || ShouldSessionOverrideInspectorRole(pPlan.Mode);
            if (lUseSessionEndpoint && lEndpoint != null && lEndpoint.IsValid)
            {
                _UdpTransport.address = lEndpoint.IpAddress;
                _UdpTransport.serverPort = lEndpoint.Port;
            }
            else
                TryApplyEdgeGapInjectedServerPort(pPlan);

            return TrySetNetworkTransport(_UdpTransport);
        }

        private void TryApplyEdgeGapInjectedServerPort(ConnectionPlan pPlan)
        {
            if (!_UseEdgeGapInjectedServerPort || pPlan.Role != PurrNetConnectionRole.Server || _UdpTransport == null)
                return;

            if (!TryReadEdgeGapPort(_EdgeGapPortName, out ushort lPort, out string lVariableName))
                return;

            _UdpTransport.address = "0.0.0.0";
            _UdpTransport.serverPort = lPort;

            if (_LogEvents)
                Debug.Log($"[PurrNet Match Connector] EdgeGap UDP port applied from {lVariableName}. Listening on 0.0.0.0:{lPort}", this);
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

        private bool TryPrepareRelayTransport(ConnectionPlan pPlan, out bool pIsPending)
        {
            pIsPending = false;

            if (!TrySetNetworkTransport(_UtpTransport))
                return false;

            _UtpTransport.peerToPeer = true;
            _UtpTransport.dedicatedServer = false;
            _UtpTransport.address = string.IsNullOrWhiteSpace(pPlan.Session?.LobbyId) ? "relay" : pPlan.Session.LobbyId;

            if (pPlan.Role == PurrNetConnectionRole.Host || pPlan.Role == PurrNetConnectionRole.Server)
            {
                if (pPlan.Session?.RelayAllocation == null)
                {
                    Debug.LogWarning("[PurrNet Match Connector] Custom Relay host is missing the Relay allocation.", this);
                    return false;
                }

                return _UtpTransport.InitializeRelayServer(pPlan.Session.RelayAllocation);
            }

            if (pPlan.Role != PurrNetConnectionRole.Client)
                return true;

            string lRelayJoinCode = pPlan.Session?.RelayJoinCode;
            if (string.IsNullOrWhiteSpace(lRelayJoinCode))
            {
                Debug.LogWarning("[PurrNet Match Connector] Custom Relay join is missing the Relay join code.", this);
                return false;
            }

            if (_PreparedRelayJoinCode == lRelayJoinCode)
                return true;

            BeginPrepareRelayClient(lRelayJoinCode);
            pIsPending = true;
            return true;
        }

        private async void BeginPrepareRelayClient(string pRelayJoinCode)
        {
            _IsPreparingRelayConnection = true;
            _HasRequestedConnection = true;

            if (_LogEvents)
                Debug.Log($"[PurrNet Match Connector] Preparing Relay client. RelayCode={pRelayJoinCode}", this);

            bool lIsReady = false;
            try
            {
                lIsReady = await _UtpTransport.InitializeRelayClient(pRelayJoinCode);
            }
            catch (System.Exception pException)
            {
                Debug.LogWarning($"[PurrNet Match Connector] Relay client preparation failed. Reason={pException.Message}", this);
            }

            if (this == null || !isActiveAndEnabled)
                return;

            _IsPreparingRelayConnection = false;
            _HasRequestedConnection = false;

            if (!lIsReady)
            {
                _PreparedRelayJoinCode = string.Empty;
                _NextConnectionRetryTime = Time.unscaledTime + _ConnectionRetrySeconds;
                return;
            }

            _PreparedRelayJoinCode = pRelayJoinCode;
            TryConnect(true);
        }

        private bool TrySetNetworkTransport(GenericTransport pTransport)
        {
            if (pTransport == null)
                return false;

            if (_NetworkManager.transport == pTransport)
                return true;

            if (!_NetworkManager.isOffline)
            {
                Debug.LogWarning("[PurrNet Match Connector] Cannot switch transport while NetworkManager is online.", this);
                return false;
            }

            _NetworkManager.transport = pTransport;
            return true;
        }

        private void DisableAutoStartIfNeeded()
        {
            if (!_DisableNetworkManagerAutoStart || _NetworkManager == null)
                return;

            _NetworkManager.startServerFlags = StartFlags.None;
            _NetworkManager.startClientFlags = StartFlags.None;
        }

        private void CacheMissingReferences()
        {
            if (_MatchContext == null)
                _MatchContext = GetComponent<MatchRuntimeContext>();

            if (_NetworkManager == null)
                _NetworkManager = GetComponent<NetworkManager>();

            if (_UdpTransport == null)
                _UdpTransport = GetComponent<UDPTransport>();

            if (_UtpTransport == null)
                _UtpTransport = GetComponent<UTPTransport>();
        }

        private void SubscribeConnectionStateLogs()
        {
            if (_NetworkManager == null)
                return;

            _NetworkManager.onClientConnectionState -= HandleClientConnectionState;
            _NetworkManager.onServerConnectionState -= HandleServerConnectionState;
            _NetworkManager.onClientConnectionState += HandleClientConnectionState;
            _NetworkManager.onServerConnectionState += HandleServerConnectionState;
        }

        private void UnsubscribeConnectionStateLogs()
        {
            if (_NetworkManager == null)
                return;

            _NetworkManager.onClientConnectionState -= HandleClientConnectionState;
            _NetworkManager.onServerConnectionState -= HandleServerConnectionState;
        }

        private void HandleClientConnectionState(ConnectionState pState)
        {
            if (!_LogEvents || _LastLoggedClientState == pState)
                return;

            _LastLoggedClientState = pState;
            Debug.Log($"[PurrNet Match Connector] Client connection state: {pState}. {DescribeConnectionState()}", this);
        }

        private void HandleServerConnectionState(ConnectionState pState)
        {
            if (!_LogEvents || _LastLoggedServerState == pState)
                return;

            _LastLoggedServerState = pState;
            Debug.Log($"[PurrNet Match Connector] Server connection state: {pState}. {DescribeConnectionState()}", this);
        }

        private bool ValidateReferences(ConnectionPlan pPlan)
        {
            bool lIsValid = true;
            lIsValid &= LogMissingReference(_MatchContext, nameof(_MatchContext));
            lIsValid &= LogMissingReference(_NetworkManager, nameof(_NetworkManager));

            if (pPlan.Role == PurrNetConnectionRole.Disabled)
                return lIsValid;

            lIsValid &= IsRelayMode(pPlan.Mode)
                ? LogMissingReference(_UtpTransport, nameof(_UtpTransport))
                : LogMissingReference(_UdpTransport, nameof(_UdpTransport));

            return lIsValid;
        }

        private static bool IsRelayMode(MatchConnectionMode pMode) =>
            pMode is MatchConnectionMode.CustomRelayHost or MatchConnectionMode.CustomRelayJoin;

        private static bool IsCustomHostMode(MatchConnectionMode pMode) =>
            pMode is MatchConnectionMode.CustomHost or MatchConnectionMode.CustomRelayHost;

        private static bool IsCustomJoinMode(MatchConnectionMode pMode) =>
            pMode is MatchConnectionMode.CustomJoin or MatchConnectionMode.CustomRelayJoin;

        private string DescribeEndpoint(ConnectionPlan pPlan)
        {
            if (IsRelayMode(pPlan.Mode))
                return $"RelayLobby={pPlan.Session?.LobbyId}, LobbyCode={pPlan.Session?.LobbyJoinCode}, RelayCode={pPlan.Session?.RelayJoinCode}";

            return _UdpTransport == null
                ? "Endpoint=None"
                : $"Endpoint={_UdpTransport.address}:{_UdpTransport.serverPort}";
        }

        private bool TryGetSessionDescriptor(out MatchSessionDescriptor pSession)
        {
            pSession = null;
            return _MatchContext != null && _MatchContext.TryGetSessionDescriptor(out pSession);
        }

        private bool LogMissingReference(UnityEngine.Object pReference, string pFieldName)
        {
            if (pReference != null)
                return true;

            Debug.LogWarning($"{nameof(PurrNetMatchConnector)} is missing reference '{pFieldName}'.", this);
            return false;
        }

        private void TrySubscribeServerHandshake()
        {
            if (_IsServerHandshakeSubscribed || _NetworkManager == null || !_NetworkManager.isServer)
                return;

            if (!_NetworkManager.TryGetModule(true, out PlayersManager lPlayers))
                return;

            _ServerPlayers = lPlayers;
            _ServerPlayers.Subscribe<PurrNetCombatHandshakeMessage>(HandleCombatHandshake);
            _ServerPlayers.Subscribe<PurrNetCustomMatchJoinRequestMessage>(HandleCustomMatchJoinRequest);
            _IsServerHandshakeSubscribed = true;
        }

        private void UnsubscribeServerHandshake()
        {
            if (!_IsServerHandshakeSubscribed || _ServerPlayers == null)
                return;

            _ServerPlayers.Unsubscribe<PurrNetCombatHandshakeMessage>(HandleCombatHandshake);
            _ServerPlayers.Unsubscribe<PurrNetCustomMatchJoinRequestMessage>(HandleCustomMatchJoinRequest);
            _ServerPlayers = null;
            _IsServerHandshakeSubscribed = false;
        }

        private void TrySubscribeClientManifestSync()
        {
            if (_IsClientManifestSubscribed || _NetworkManager == null || !_NetworkManager.isClient)
                return;

            if (!_NetworkManager.TryGetModule(false, out PlayersManager lPlayers))
                return;

            _ClientPlayers = lPlayers;
            _ClientPlayers.Subscribe<PurrNetMatchManifestMessage>(HandleMatchManifestMessage);
            _IsClientManifestSubscribed = true;
        }

        private void UnsubscribeClientManifestSync()
        {
            if (!_IsClientManifestSubscribed || _ClientPlayers == null)
                return;

            _ClientPlayers.Unsubscribe<PurrNetMatchManifestMessage>(HandleMatchManifestMessage);
            _ClientPlayers = null;
            _IsClientManifestSubscribed = false;
        }

        private void TrySendClientCompositionHandshake()
        {
            if (_ClientPlayers == null || _MatchContext == null || !_MatchContext.HasMatch)
                return;

            MatchManifest lManifest = _MatchContext.Manifest;
            if (lManifest == null || lManifest.HasAllPlayerCompositions)
                return;

            PlayerIdentity lLocalPlayer = _MatchContext.LocalPlayer;
            if (!lLocalPlayer.IsValid || !TryApplyLocalComposition(lManifest, lLocalPlayer.PlayerId))
                return;

            if (Time.unscaledTime < _NextCompositionHandshakeTime)
                return;

            _NextCompositionHandshakeTime = Time.unscaledTime + _CompositionHandshakeRetrySeconds;

            if (IsCustomJoinMode(_MatchContext.ConnectionMode))
            {
                _ClientPlayers.SendToServer(new PurrNetCustomMatchJoinRequestMessage
                {
                    PlayerId = lLocalPlayer.PlayerId,
                    MatchId = lManifest.MatchId,
                    ManifestJson = JsonUtility.ToJson(lManifest)
                });
                LogCompositionHandshake($"Sent custom join composition to host. PlayerId={lLocalPlayer.PlayerId}, MatchId={lManifest.MatchId}");
                return;
            }

            if (IsCustomHostMode(_MatchContext.ConnectionMode))
                return;

            _ClientPlayers.SendToServer(new PurrNetCombatHandshakeMessage
            {
                PlayerId = lLocalPlayer.PlayerId,
                MatchId = lManifest.MatchId,
                ManifestJson = JsonUtility.ToJson(lManifest)
            });
            LogCompositionHandshake($"Sent combat composition to server. PlayerId={lLocalPlayer.PlayerId}, MatchId={lManifest.MatchId}");
        }

        private bool TryApplyLocalComposition(MatchManifest pManifest, string pPlayerId)
        {
            if (!CombatTeamCompositionState.HasLocalSelection)
                TeamSelectionState.LoadSavedUnitIds();

            if (CombatTeamCompositionState.LocalSelectedUnitIds.Count > 0)
            {
                pManifest.SetUnitIds(pPlayerId, CombatTeamCompositionState.LocalSelectedUnitIds);
                return true;
            }

            if (CombatTeamCompositionState.LocalSelectedUnits.Count > 0)
            {
                pManifest.SetUnitIds(pPlayerId, CombatTeamCompositionState.LocalSelectedUnits);
                return true;
            }

            if (_LogEvents && !_HasLoggedMissingLocalComposition)
            {
                _HasLoggedMissingLocalComposition = true;
                Debug.LogWarning("[PurrNet Match Connector] Cannot send match composition: no saved local team selection was found. Open Team Selection, save a team, then start the match again.", this);
            }

            return false;
        }

        private void LogCompositionHandshake(string pMessage)
        {
            if (_LogEvents)
                Debug.Log($"[PurrNet Match Connector] {pMessage}", this);
        }

        private void HandleCombatHandshake(PlayerID pPlayer, PurrNetCombatHandshakeMessage pMessage, bool pAsServer)
        {
            if (!pAsServer || _MatchContext == null)
                return;

            MatchManifest lCurrentManifest = ResolveServerManifestForHandshake(pMessage.MatchId);
            if (!CombatMatchManifestImporter.TryImport(lCurrentManifest, pMessage.MatchId, pMessage.ManifestJson, true, out MatchManifest lManifest, out string lFailure))
            {
                if (_LogEvents)
                    Debug.LogWarning($"[PurrNet Match Connector] Server rejected match manifest from player {pPlayer}. {lFailure}", this);

                return;
            }

            if (_MatchContext.HasMatch)
            {
                if (_MatchContext.MatchId != pMessage.MatchId)
                {
                    _PendingServerManifest = lManifest;
                    if (!_PendingServerManifest.HasAllPlayerCompositions)
                    {
                        if (_LogEvents)
                            Debug.Log($"[PurrNet Match Connector] Server replacing stale match context and waiting for all compositions. PreviousMatchId={_MatchContext.MatchId}, IncomingMatchId={pMessage.MatchId}", this);

                        return;
                    }

                    _PendingServerManifest = null;
                    _MatchContext.SetQuickMatchResult(new PlayerIdentity("server", "Server"), BuildServerTicket(lManifest));
                    BroadcastManifestIfReady(lManifest);
                    return;
                }

                _MatchContext.RefreshCompositionsFromManifest();
                BroadcastManifestIfReady(_MatchContext.Manifest);
                return;
            }

            _PendingServerManifest = lManifest;
            if (!_PendingServerManifest.HasAllPlayerCompositions)
            {
                if (_LogEvents)
                    Debug.Log($"[PurrNet Match Connector] Server waiting for all player compositions. MatchId={_PendingServerManifest.MatchId}", this);

                return;
            }

            _PendingServerManifest = null;
            _MatchContext.SetQuickMatchResult(new PlayerIdentity("server", "Server"), BuildServerTicket(lManifest));
            BroadcastManifestIfReady(lManifest);

            if (_LogEvents)
                Debug.Log($"[PurrNet Match Connector] Server match context received from player {pPlayer}. MatchId={lManifest.MatchId}", this);
        }

        private void HandleCustomMatchJoinRequest(PlayerID pPlayer, PurrNetCustomMatchJoinRequestMessage pMessage, bool pAsServer)
        {
            if (!pAsServer || _MatchContext == null || !_MatchContext.HasMatch)
                return;

            if (!IsCustomHostMode(_MatchContext.ConnectionMode) || _MatchContext.MatchId != pMessage.MatchId)
            {
                if (_LogEvents)
                    Debug.LogWarning($"[PurrNet Match Connector] Rejected custom join from {pPlayer}. HostMode={_MatchContext.ConnectionMode}, HostMatchId={_MatchContext.MatchId}, IncomingMatchId={pMessage.MatchId}", this);

                return;
            }

            if (!TryParseCustomJoinManifest(pMessage, out MatchManifest lIncomingManifest, out string lFailure))
            {
                if (_LogEvents)
                    Debug.LogWarning($"[PurrNet Match Connector] Rejected custom join from {pPlayer}. {lFailure}", this);

                return;
            }

            if (!lIncomingManifest.TryGetAssignment(pMessage.PlayerId, out MatchPlayerAssignment lIncomingPlayer) || lIncomingPlayer.Slot != MatchPlayerSlot.TeamB)
            {
                if (_LogEvents)
                    Debug.LogWarning($"[PurrNet Match Connector] Rejected custom join from {pPlayer}: player '{pMessage.PlayerId}' is not assigned to TeamB.", this);

                return;
            }

            MatchManifest lHostManifest = _MatchContext.Manifest;
            lHostManifest.SetAssignment(MatchPlayerSlot.TeamB, pMessage.PlayerId, lIncomingPlayer.TeamPresetId, lIncomingPlayer.UnitIds);
            _MatchContext.SetManifest(lHostManifest);
            BroadcastManifestIfReady(lHostManifest);

            if (_LogEvents)
                Debug.Log($"[PurrNet Match Connector] Custom match player joined. PlayerId={pMessage.PlayerId}, MatchId={pMessage.MatchId}", this);
        }

        private bool TryParseCustomJoinManifest(PurrNetCustomMatchJoinRequestMessage pMessage, out MatchManifest pManifest, out string pFailure)
        {
            pManifest = null;
            pFailure = string.Empty;

            if (string.IsNullOrWhiteSpace(pMessage.PlayerId) || string.IsNullOrWhiteSpace(pMessage.MatchId))
            {
                pFailure = "Custom join request is missing player id or match id.";
                return false;
            }

            try
            {
                pManifest = string.IsNullOrWhiteSpace(pMessage.ManifestJson) ? null : JsonUtility.FromJson<MatchManifest>(pMessage.ManifestJson);
            }
            catch (System.Exception pException)
            {
                pFailure = $"Custom join request has an invalid manifest. {pException.Message}";
                return false;
            }

            if (pManifest == null || pManifest.MatchId != pMessage.MatchId || !pManifest.TryValidatePlayerAssignments(out pFailure))
            {
                pFailure = string.IsNullOrWhiteSpace(pFailure) ? "Custom join request has an invalid manifest." : pFailure;
                return false;
            }

            return true;
        }

        private MatchManifest ResolveServerManifestForHandshake(string pIncomingMatchId)
        {
            if (_MatchContext == null || !_MatchContext.HasMatch)
                return ResetPendingManifestIfMatchChanged(pIncomingMatchId);

            if (_MatchContext.MatchId == pIncomingMatchId)
                return _MatchContext.Manifest;

            if (CanReplaceServerMatchInCurrentScene())
                return ResetPendingManifestIfMatchChanged(pIncomingMatchId);

            return _MatchContext.Manifest;
        }

        private MatchManifest ResetPendingManifestIfMatchChanged(string pIncomingMatchId)
        {
            if (_PendingServerManifest != null && _PendingServerManifest.MatchId != pIncomingMatchId)
                _PendingServerManifest = null;

            return _PendingServerManifest;
        }

        private bool CanReplaceServerMatchInCurrentScene() =>
            _AllowServerMatchReplacementInMainMenu &&
            !string.IsNullOrWhiteSpace(_MainMenuSceneName) &&
            IsServerInstance() &&
            SceneManager.GetActiveScene().name == _MainMenuSceneName;

        private bool IsServerInstance() =>
            (_NetworkManager != null && _NetworkManager.isServer) ||
            BuildConnectionPlan().Role == PurrNetConnectionRole.Server;

        private void HandleMatchManifestMessage(PlayerID pPlayer, PurrNetMatchManifestMessage pMessage, bool pAsServer)
        {
            if (pAsServer || _MatchContext == null || !_MatchContext.HasMatch)
                return;

            if (IsCustomJoinMode(_MatchContext.ConnectionMode))
            {
                if (TryAcceptCustomServerManifest(pMessage, out MatchManifest lCustomManifest, out string lCustomFailure))
                {
                    _MatchContext.SetManifest(lCustomManifest);

                    if (_LogEvents)
                        Debug.Log($"[PurrNet Match Connector] Client received custom match manifest. MatchId={lCustomManifest.MatchId}", this);
                }
                else if (_LogEvents)
                    Debug.LogWarning($"[PurrNet Match Connector] Client rejected custom match manifest sync. {lCustomFailure}", this);

                return;
            }

            if (!CombatMatchManifestImporter.TryImport(_MatchContext.Manifest, pMessage.MatchId, pMessage.ManifestJson, false, out MatchManifest lManifest, out string lFailure))
            {
                if (_LogEvents)
                    Debug.LogWarning($"[PurrNet Match Connector] Client rejected match manifest sync. {lFailure}", this);

                return;
            }

            _MatchContext.SetManifest(lManifest);

            if (_LogEvents)
                Debug.Log($"[PurrNet Match Connector] Client received full match manifest. MatchId={lManifest.MatchId}", this);
        }

        private bool TryAcceptCustomServerManifest(PurrNetMatchManifestMessage pMessage, out MatchManifest pManifest, out string pFailure)
        {
            pManifest = null;
            pFailure = string.Empty;

            try
            {
                pManifest = string.IsNullOrWhiteSpace(pMessage.ManifestJson) ? null : JsonUtility.FromJson<MatchManifest>(pMessage.ManifestJson);
            }
            catch (System.Exception pException)
            {
                pFailure = $"Invalid manifest. {pException.Message}";
                return false;
            }

            if (pManifest == null || pManifest.MatchId != _MatchContext.MatchId || !pManifest.TryValidatePlayerAssignments(out pFailure))
            {
                pFailure = string.IsNullOrWhiteSpace(pFailure) ? "Invalid custom match manifest." : pFailure;
                return false;
            }

            if (!pManifest.TryGetSlot(_MatchContext.LocalPlayer.PlayerId, out MatchPlayerSlot lSlot) || lSlot != MatchPlayerSlot.TeamB)
            {
                pFailure = $"Custom match manifest does not assign local player '{_MatchContext.LocalPlayer.PlayerId}' to TeamB.";
                return false;
            }

            return true;
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

        private MatchTicketSnapshot BuildServerTicket(MatchManifest pManifest) =>
            new MatchTicketSnapshot
            {
                TicketId = string.Empty,
                Status = MatchTicketStatus.Found,
                Manifest = pManifest,
                ServerEndpoint = new MatchServerEndpoint
                {
                    IpAddress = _UdpTransport != null ? _UdpTransport.address : "127.0.0.1",
                    Port = _UdpTransport != null ? _UdpTransport.serverPort : (ushort)5000,
                    AllocationId = pManifest != null ? pManifest.MatchId : string.Empty
                },
                ConnectionMode = MatchConnectionMode.DedicatedServer,
                IsTrusted = false,
                ReportsRankedResults = false
            };

        private bool ShouldRestartIdleServer() =>
            _StopWhenMatchContextClears &&
            BuildConnectionPlan().Role == PurrNetConnectionRole.Server &&
            _AllowServerWithoutMatchContext &&
            isActiveAndEnabled;

        private readonly struct ConnectionPlan
        {
            public readonly PurrNetConnectionRole Role;
            public readonly MatchSessionDescriptor Session;
            public readonly MatchServerEndpoint Endpoint;
            public readonly bool CanStartWithoutSession;

            public ConnectionPlan(PurrNetConnectionRole pRole, MatchSessionDescriptor pSession, MatchServerEndpoint pEndpoint, bool pCanStartWithoutSession)
            {
                Role = pRole;
                Session = pSession;
                Endpoint = pEndpoint;
                CanStartWithoutSession = pCanStartWithoutSession;
            }

            public bool HasSession => Session != null;
            public string MatchId => HasSession ? Session.MatchId : string.Empty;
            public MatchConnectionMode Mode => HasSession ? Session.ConnectionMode : MatchConnectionMode.Offline;
        }

        #endregion
    }
}
