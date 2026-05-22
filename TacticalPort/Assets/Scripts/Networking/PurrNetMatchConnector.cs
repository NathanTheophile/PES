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
using TacticalPort.App;
using TacticalPort.Matchmaking;
using UnityEngine;

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

    public sealed class PurrNetMatchConnector : MonoBehaviour, IPurrNetManifestHandshakeContext, IMatchConnectionController
    {
        #region _____________________________/ VALUES

        private const float NETWORK_MAINTENANCE_INTERVAL_SECONDS = 0.1f;

        [Header("Runtime References")]
        [Tooltip("Shared match context that provides the current session descriptor and manifest.")]
        [SerializeField] private MatchRuntimeContext _MatchContext;
        [Tooltip("PurrNet NetworkManager controlled by this connector.")]
        [SerializeField] private NetworkManager _NetworkManager;
        [Tooltip("UDP transport used by local server and dedicated server flows.")]
        [SerializeField] private UDPTransport _UdpTransport;
        [Tooltip("UTP transport used by custom Relay lobby flows.")]
        [SerializeField] private UTPTransport _UtpTransport;

        [Header("Role Policy")]
        [Tooltip("Default connection role. Auto lets build type and match session choose host/server/client.")]
        [SerializeField] private PurrNetConnectionRole _ConnectionRole = PurrNetConnectionRole.Auto;
        [Tooltip("Enable only when a ParrelSync clone is intentionally used as an extra local server. Normal client clones should leave this disabled.")]
        [SerializeField] private bool _AllowCloneServerRole;
        [Tooltip("Starts the network connection automatically when MatchRuntimeContext receives a match.")]
        [SerializeField] private bool _ConnectOnMatchFound = true;
        [Tooltip("Allows server builds or configured server roles to listen before a match context exists.")]
        [SerializeField] private bool _AllowServerWithoutMatchContext = true;

        [Header("Endpoint Policy")]
        [Tooltip("Legacy compatibility toggle. Future custom/dedicated flows should keep this enabled so session endpoints drive the transport.")]
        [SerializeField] private bool _UseMatchEndpointWhenAvailable = true;
        [Tooltip("Dedicated server builds on EdgeGap receive the internal UDP port through ARBITRIUM_PORT_<PORT_NAME>_INTERNAL.")]
        [SerializeField] private bool _UseEdgeGapInjectedServerPort = true;
        [Tooltip("EdgeGap port name used to build the injected environment variable name.")]
        [SerializeField] private string _EdgeGapPortName = "gameport";

        [Header("Session Lifecycle")]
        [Tooltip("Disables NetworkManager autostart so this connector owns connection timing.")]
        [SerializeField] private bool _DisableNetworkManagerAutoStart = true;
        [Tooltip("Stops the active network connection when MatchRuntimeContext is cleared.")]
        [SerializeField] private bool _StopWhenMatchContextClears = true;
        [Tooltip("Lets an idle server in the main menu accept a replacement match context from a new client handshake.")]
        [SerializeField] private bool _AllowServerMatchReplacementInMainMenu = true;
        [Tooltip("Frontend scene name used to decide if server-side match replacement is allowed.")]
        [SerializeField] private string _MainMenuSceneName = "S_MainMenu";

        [Header("Retry And Debug")]
        [Tooltip("Delay before retrying a failed or dropped connection attempt.")]
        [SerializeField, Min(0.25f)] private float _ConnectionRetrySeconds = 1f;
        [Tooltip("Delay between client composition handshake attempts while the server waits for the full manifest.")]
        [SerializeField, Min(0.25f)] private float _CompositionHandshakeRetrySeconds = 1f;
        [Tooltip("Logs connection planning, transport changes, state changes, and manifest handshakes.")]
        [SerializeField] private bool _LogEvents = true;

        private bool _HasRequestedConnection;
        private float _NextConnectionRetryTime;
        private bool _HasLoggedCloneServerRoleWarning;
        private float _NextNetworkMaintenanceTime;
        private PurrNetConnectionStateLogger _ConnectionStateLogger;
        private PurrNetManifestHandshakeSync _ManifestHandshakeSync;
        private PurrNetRelayPreparationController _RelayPreparationController;

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
            EnsureConnectionStateLogger();
            _ConnectionStateLogger.Subscribe(_NetworkManager);

            if (_MatchContext != null)
            {
                _MatchContext.Changed -= HandleMatchContextChanged;
                _MatchContext.Changed += HandleMatchContextChanged;
            }

            ConnectIfReady();
        }

        private void OnDisable()
        {
            CancelRelayPreparation();
            _ManifestHandshakeSync?.UnsubscribeServerHandshake();
            _ManifestHandshakeSync?.UnsubscribeClientManifestSync();
            _ConnectionStateLogger?.Unsubscribe(_NetworkManager);

            if (_MatchContext != null)
                _MatchContext.Changed -= HandleMatchContextChanged;
        }

        private void OnDestroy()
        {
            _RelayPreparationController?.Dispose();
            _RelayPreparationController = null;
        }

        private void OnValidate() => CacheMissingReferences();

        private void Update()
        {
            if (Time.unscaledTime < _NextNetworkMaintenanceTime)
                return;

            _NextNetworkMaintenanceTime = Time.unscaledTime + NETWORK_MAINTENANCE_INTERVAL_SECONDS;
            RunNetworkMaintenanceTick();
        }

        #endregion

        #region _____________________________| CONNECT

        public void StartConnection() => ConnectIfReady(true);

        public string DescribeConnectionState()
        {
            PurrNetConnectionPlan lPlan = BuildConnectionPlan();
            string lManagerState = _NetworkManager == null
                ? "NetworkManager=None"
                : $"NetworkManager={_NetworkManager.name}, Offline={_NetworkManager.isOffline}, Client={_NetworkManager.isClient}, Server={_NetworkManager.isServer}, ClientState={_NetworkManager.clientState}, ServerState={_NetworkManager.serverState}";

            string lTransport = _NetworkManager?.transport != null ? _NetworkManager.transport.GetType().Name : "None";
            string lEndpoint = DescribeEndpoint(lPlan);

            return $"Role={lPlan.Role}, Mode={lPlan.Mode}, Requested={_HasRequestedConnection}, Transport={lTransport}, {lManagerState}, {lEndpoint}, MatchId={lPlan.MatchId}";
        }

        public void StopConnection()
        {
            CancelRelayPreparation();

            if (_NetworkManager == null || _NetworkManager.isOffline)
                return;

            _ManifestHandshakeSync?.UnsubscribeClientManifestSync();
            _ManifestHandshakeSync?.UnsubscribeServerHandshake();

            if (_NetworkManager.isClient)
                _NetworkManager.StopClient();

            if (_NetworkManager.isServer)
                _NetworkManager.StopServer();

            _HasRequestedConnection = false;
            _NextConnectionRetryTime = 0f;
            _RelayPreparationController?.ResetPreparedJoinCode();
            _ManifestHandshakeSync?.ResetSessionState();

            if (_LogEvents)
                Debug.Log("[PurrNet Match Connector] Network stopped.", this);
        }

        private void HandleMatchContextChanged(MatchRuntimeContext pContext)
        {
            if (pContext != null && pContext.HasMatch)
            {
                ConnectIfReady();
                return;
            }

            _HasRequestedConnection = false;
            _NextConnectionRetryTime = 0f;
            CancelRelayPreparation();
            _RelayPreparationController?.ResetPreparedJoinCode();
            _ManifestHandshakeSync?.ResetSessionState();

            if (_StopWhenMatchContextClears)
                StopConnection();

            if (ShouldRestartIdleServer())
                ConnectIfReady(true);
        }

        private void ConnectIfReady(bool pForce = false)
        {
            if (!_ConnectOnMatchFound && !pForce)
                return;

            if (_RelayPreparationController?.IsPreparing == true && !pForce)
                return;

            if (_HasRequestedConnection && !pForce)
                return;

            PurrNetConnectionPlan lPlan = BuildConnectionPlan();
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
            if (_NetworkManager == null || !_HasRequestedConnection || !_NetworkManager.isOffline || _RelayPreparationController?.IsPreparing == true)
                return;

            PurrNetConnectionPlan lPlan = BuildConnectionPlan();
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

            ConnectIfReady(true);
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

        private PurrNetConnectionPlan BuildConnectionPlan()
        {
            TryGetSessionDescriptor(out MatchSessionDescriptor lSession);
            PurrNetConnectionPlan lPlan = PurrNetConnectionPlanner.Build(
                lSession,
                _ConnectionRole,
                _AllowCloneServerRole,
                _AllowServerWithoutMatchContext,
                out bool lShouldLogCloneServerRoleWarning);

            if (lShouldLogCloneServerRoleWarning)
                LogCloneServerRoleWarning();

            return lPlan;
        }

        private void LogCloneServerRoleWarning()
        {
            if (_HasLoggedCloneServerRoleWarning || !_LogEvents)
                return;

            _HasLoggedCloneServerRoleWarning = true;
            Debug.LogWarning("[PurrNet Match Connector] Clone has Connection Role=Server, but clone server override is disabled. Starting as Client to avoid UDP port binding conflicts.", this);
        }

        private bool TryPrepareTransport(PurrNetConnectionPlan pPlan, out bool pIsPending)
        {
            pIsPending = false;

            if (IsRelayMode(pPlan.Mode))
                return TryPrepareRelayTransport(pPlan, out pIsPending);

            return TryPrepareUdpTransport(pPlan);
        }

        private bool TryPrepareUdpTransport(PurrNetConnectionPlan pPlan)
        {
            bool lConfigured = PurrNetTransportConfigurator.TryPrepareUdpTransport(
                _NetworkManager,
                _UdpTransport,
                pPlan,
                _UseMatchEndpointWhenAvailable,
                _UseEdgeGapInjectedServerPort,
                _EdgeGapPortName,
                out string lEdgeGapLogMessage);

            if (_LogEvents && !string.IsNullOrWhiteSpace(lEdgeGapLogMessage))
                Debug.Log($"[PurrNet Match Connector] {lEdgeGapLogMessage}", this);

            if (!lConfigured && _NetworkManager != null && !_NetworkManager.isOffline)
                Debug.LogWarning("[PurrNet Match Connector] Cannot switch transport while NetworkManager is online.", this);

            return lConfigured;
        }

        private bool TryPrepareRelayTransport(PurrNetConnectionPlan pPlan, out bool pIsPending)
        {
            bool lConfigured = PurrNetTransportConfigurator.TryPrepareRelayTransport(
                _NetworkManager,
                _UtpTransport,
                pPlan,
                _RelayPreparationController?.PreparedRelayJoinCode ?? string.Empty,
                out pIsPending,
                out string lRelayJoinCode,
                out string lFailure);

            if (!lConfigured)
            {
                if (!string.IsNullOrWhiteSpace(lFailure))
                    Debug.LogWarning($"[PurrNet Match Connector] {lFailure}", this);
                else if (_NetworkManager != null && !_NetworkManager.isOffline)
                    Debug.LogWarning("[PurrNet Match Connector] Cannot switch transport while NetworkManager is online.", this);

                return false;
            }

            if (pIsPending)
                BeginPrepareRelayClient(lRelayJoinCode);

            return true;
        }

        private void BeginPrepareRelayClient(string pRelayJoinCode)
        {
            EnsureRelayPreparationController();
            _HasRequestedConnection = true;
            _RelayPreparationController.BeginPrepareClient(
                pRelayJoinCode,
                _UtpTransport,
                HandleRelayPreparationCompleted,
                LogRelayPreparation,
                LogRelayPreparationWarning);
        }

        private void HandleRelayPreparationCompleted(bool pIsReady, string pRelayJoinCode)
        {
            if (this == null || !isActiveAndEnabled)
                return;

            _HasRequestedConnection = false;

            if (!pIsReady)
            {
                _RelayPreparationController?.ResetPreparedJoinCode();
                _NextConnectionRetryTime = Time.unscaledTime + _ConnectionRetrySeconds;
                return;
            }

            ConnectIfReady(true);
        }

        private void CancelRelayPreparation()
        {
            _RelayPreparationController?.Cancel();
        }

        private void LogRelayPreparation(string pMessage)
        {
            if (!_LogEvents)
                return;

            Debug.Log($"[PurrNet Match Connector] {pMessage}", this);
        }

        private void LogRelayPreparationWarning(string pMessage) =>
            Debug.LogWarning($"[PurrNet Match Connector] {pMessage}", this);

        private void DisableAutoStartIfNeeded()
        {
            if (!_DisableNetworkManagerAutoStart || _NetworkManager == null)
                return;

            _NetworkManager.startServerFlags = StartFlags.None;
            _NetworkManager.startClientFlags = StartFlags.None;
        }

        private void RunNetworkMaintenanceTick()
        {
            MaintainConnection();
            EnsureManifestHandshakeSync();
            _ManifestHandshakeSync.SubscribeServerHandshakeIfReady();
            _ManifestHandshakeSync.SubscribeClientManifestSyncIfReady();
            _ManifestHandshakeSync.SendClientCompositionHandshakeIfReady(_CompositionHandshakeRetrySeconds);
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

        private void EnsureManifestHandshakeSync()
        {
            _ManifestHandshakeSync ??= new PurrNetManifestHandshakeSync(this);
        }

        private void EnsureConnectionStateLogger()
        {
            _ConnectionStateLogger ??= new PurrNetConnectionStateLogger(this, () => _LogEvents, DescribeConnectionState);
        }

        private void EnsureRelayPreparationController()
        {
            _RelayPreparationController ??= new PurrNetRelayPreparationController();
        }

        private bool ValidateReferences(PurrNetConnectionPlan pPlan)
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
            PurrNetConnectionPlanner.IsRelayMode(pMode);

        private string DescribeEndpoint(PurrNetConnectionPlan pPlan)
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

        MatchRuntimeContext IPurrNetManifestHandshakeContext.MatchContext => _MatchContext;
        NetworkManager IPurrNetManifestHandshakeContext.NetworkManager => _NetworkManager;
        UDPTransport IPurrNetManifestHandshakeContext.UdpTransport => _UdpTransport;
        bool IPurrNetManifestHandshakeContext.AllowServerMatchReplacementInMainMenu => _AllowServerMatchReplacementInMainMenu;
        string IPurrNetManifestHandshakeContext.MainMenuSceneName => _MainMenuSceneName;
        PurrNetConnectionPlan IPurrNetManifestHandshakeContext.BuildConnectionPlanForManifest() => BuildConnectionPlan();

        void IPurrNetManifestHandshakeContext.LogManifest(string pMessage)
        {
            if (!_LogEvents)
                return;

            Debug.Log($"[PurrNet Match Connector] {pMessage}", this);
        }

        void IPurrNetManifestHandshakeContext.LogManifestWarning(string pMessage)
        {
            if (!_LogEvents)
                return;

            Debug.LogWarning($"[PurrNet Match Connector] {pMessage}", this);
        }

        private bool ShouldRestartIdleServer() =>
            _StopWhenMatchContextClears &&
            BuildConnectionPlan().Role == PurrNetConnectionRole.Server &&
            _AllowServerWithoutMatchContext &&
            isActiveAndEnabled;

        #endregion
    }
}
