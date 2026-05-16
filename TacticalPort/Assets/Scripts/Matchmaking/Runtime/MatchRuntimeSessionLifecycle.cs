#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Matchmaking
#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using TacticalPort.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TacticalPort.Matchmaking
{
    public sealed class MatchRuntimeSessionLifecycle : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private MatchRuntimeContext _MatchContext;
        [SerializeField] private QuickMatchFlowController _QuickMatchFlowController;
        [SerializeField] private PurrNetMatchConnector _PurrNetMatchConnector;
        [Tooltip("Optional. Assign a release service so dedicated allocations can be released on cleanup.")]
        [SerializeField] private MonoBehaviour _GameServerAllocatorSource;
        [Tooltip("Optional. Assign UgsCustomRelayLobbyService so Relay lobbies can be left/deleted on cleanup.")]
        [SerializeField] private MonoBehaviour _PartyLobbyServiceSource;
        [SerializeField] private string _MainMenuSceneName = "S_MainMenu";
        [SerializeField] private bool _ClearMatchOnMainMenuReturn = true;
        [SerializeField] private bool _ReleaseServerAllocationOnSessionEnd = true;
        [SerializeField] private bool _LogEvents = true;

        private IGameServerAllocationReleaser _GameServerAllocationReleaser;
        private IPartyLobbyService _PartyLobbyService;
        private bool _IsEndingSession;

        #endregion

        #region _____________________________| UNITY

        private void Awake() => CacheMissingReferences();

        private void OnEnable()
        {
            CacheMissingReferences();
            SceneManager.activeSceneChanged -= HandleActiveSceneChanged;
            SceneManager.activeSceneChanged += HandleActiveSceneChanged;
        }

        private void OnDisable() => SceneManager.activeSceneChanged -= HandleActiveSceneChanged;

        private void OnApplicationQuit() => EndCurrentMatchSessionIfNeeded("Application quitting.");

        private void OnDestroy() => EndCurrentMatchSessionIfNeeded("Runtime services destroyed.");

        private void OnValidate() => CacheMissingReferences();

        #endregion

        #region _____________________________| CONFIGURE

        public void ConfigureGameServerAllocator(MonoBehaviour pGameServerAllocatorSource)
        {
            _GameServerAllocatorSource = pGameServerAllocatorSource;
            _GameServerAllocationReleaser = _GameServerAllocatorSource as IGameServerAllocationReleaser;
        }

        public void ConfigurePartyLobbyService(MonoBehaviour pPartyLobbyServiceSource)
        {
            _PartyLobbyServiceSource = pPartyLobbyServiceSource;
            _PartyLobbyService = _PartyLobbyServiceSource as IPartyLobbyService;
        }

        #endregion

        #region _____________________________| SCENES

        private void HandleActiveSceneChanged(Scene pPreviousScene, Scene pCurrentScene)
        {
            if (!_ClearMatchOnMainMenuReturn || string.IsNullOrWhiteSpace(_MainMenuSceneName))
                return;

            if (pCurrentScene.name != _MainMenuSceneName || _MatchContext == null || !_MatchContext.HasMatch)
                return;

            EndCurrentMatchSession("Returned to main menu.");
        }

        [ContextMenu("End Current Match Session")]
        public void EndCurrentMatchSession() => EndCurrentMatchSession("Manual cleanup.");

        private void EndCurrentMatchSessionIfNeeded(string pReason)
        {
            if (_MatchContext == null || !_MatchContext.HasMatch)
                return;

            EndCurrentMatchSession(pReason);
        }

        private void EndCurrentMatchSession(string pReason)
        {
            if (_IsEndingSession)
                return;

            _IsEndingSession = true;
            MatchServerEndpoint lEndpoint = _MatchContext?.ServerEndpoint;
            MatchConnectionMode lConnectionMode = _MatchContext != null ? _MatchContext.ConnectionMode : MatchConnectionMode.Offline;
            string lLobbyId = _MatchContext?.LobbyId ?? string.Empty;

            _PurrNetMatchConnector?.StopConnection();
            _MatchContext?.Clear();
            _QuickMatchFlowController?.ResetToIdle();
            TryReleaseServerAllocation(lEndpoint, lConnectionMode);
            TryLeavePartyLobby(lLobbyId, lConnectionMode);

            if (_LogEvents)
                Debug.Log($"[Match Runtime] Match session ended. Reason={pReason}", this);

            _IsEndingSession = false;
        }

        private void TryReleaseServerAllocation(MatchServerEndpoint pEndpoint, MatchConnectionMode pConnectionMode)
        {
            if (!_ReleaseServerAllocationOnSessionEnd || pConnectionMode != MatchConnectionMode.DedicatedServer || pEndpoint == null || !pEndpoint.IsValid)
                return;

            _GameServerAllocationReleaser ??= _GameServerAllocatorSource as IGameServerAllocationReleaser;
            if (_GameServerAllocationReleaser == null)
                return;

            _ = ReleaseServerAllocationAsync(pEndpoint);
        }

        private async Task ReleaseServerAllocationAsync(MatchServerEndpoint pEndpoint)
        {
            try
            {
                await _GameServerAllocationReleaser.ReleaseServerAsync(pEndpoint, CancellationToken.None);

                if (_LogEvents)
                    Debug.Log($"[Match Runtime] Server allocation released. AllocationId={pEndpoint.AllocationId}", this);
            }
            catch (Exception pException)
            {
                Debug.LogWarning($"[Match Runtime] Server allocation release failed. AllocationId={pEndpoint.AllocationId}, Reason={pException.Message}", this);
            }
        }

        private void TryLeavePartyLobby(string pLobbyId, MatchConnectionMode pConnectionMode)
        {
            if (!IsCustomRelayMode(pConnectionMode) || string.IsNullOrWhiteSpace(pLobbyId))
                return;

            _PartyLobbyService ??= _PartyLobbyServiceSource as IPartyLobbyService;
            if (_PartyLobbyService == null)
                return;

            _ = LeavePartyLobbyAsync(pLobbyId);
        }

        private async Task LeavePartyLobbyAsync(string pLobbyId)
        {
            try
            {
                await _PartyLobbyService.LeaveLobbyAsync(pLobbyId, CancellationToken.None);

                if (_LogEvents)
                    Debug.Log($"[Match Runtime] Custom lobby left. LobbyId={pLobbyId}", this);
            }
            catch (Exception pException)
            {
                Debug.LogWarning($"[Match Runtime] Custom lobby cleanup failed. LobbyId={pLobbyId}, Reason={pException.Message}", this);
            }
        }

        private static bool IsCustomRelayMode(MatchConnectionMode pConnectionMode) =>
            pConnectionMode is MatchConnectionMode.CustomRelayHost or MatchConnectionMode.CustomRelayJoin;

        #endregion

        #region _____________________________| HELPERS

        private void CacheMissingReferences()
        {
            if (_MatchContext == null)
                _MatchContext = GetComponent<MatchRuntimeContext>();

            if (_QuickMatchFlowController == null)
                _QuickMatchFlowController = GetComponent<QuickMatchFlowController>();

            if (_PurrNetMatchConnector == null)
                _PurrNetMatchConnector = GetComponent<PurrNetMatchConnector>();

            if (_PartyLobbyServiceSource == null)
                _PartyLobbyServiceSource = FindPartyLobbyServiceSource();

            if (_GameServerAllocatorSource == null)
                _GameServerAllocatorSource = FindGameServerAllocationReleaserSource();

            _GameServerAllocationReleaser = _GameServerAllocatorSource as IGameServerAllocationReleaser;
            _PartyLobbyService = _PartyLobbyServiceSource as IPartyLobbyService;
        }

        private MonoBehaviour FindGameServerAllocationReleaserSource()
        {
            MonoBehaviour[] lBehaviours = GetComponents<MonoBehaviour>();
            for (int lIndex = 0; lIndex < lBehaviours.Length; lIndex++)
            {
                if (lBehaviours[lIndex] is IGameServerAllocationReleaser)
                    return lBehaviours[lIndex];
            }

            return null;
        }

        private MonoBehaviour FindPartyLobbyServiceSource()
        {
            MonoBehaviour[] lBehaviours = GetComponents<MonoBehaviour>();
            for (int lIndex = 0; lIndex < lBehaviours.Length; lIndex++)
            {
                if (lBehaviours[lIndex] is IPartyLobbyService)
                    return lBehaviours[lIndex];
            }

            return null;
        }

        #endregion
    }
}
