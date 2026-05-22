#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Matchmaking
#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using TacticalPort.App;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace TacticalPort.Matchmaking
{
    public sealed class MatchRuntimeSessionLifecycle : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [Header("Runtime References")]
        [Tooltip("Shared match context cleared when the session ends or when returning to frontend.")]
        [SerializeField] private MatchRuntimeContext _MatchContext;
        [Tooltip("Quick match flow reset when a match session is cleaned up.")]
        [SerializeField] private QuickMatchFlowController _QuickMatchFlowController;
        [FormerlySerializedAs("_PurrNetMatchConnector")]
        [Tooltip("Component implementing IMatchConnectionController, stopped before the match context is cleared.")]
        [SerializeField] private MonoBehaviour _MatchConnectionControllerSource;

        [Header("Cleanup Services")]
        [Tooltip("Optional. Source implementing IGameServerAllocationReleaser so dedicated allocations can be released on cleanup.")]
        [SerializeField] private MonoBehaviour _GameServerAllocatorSource;
        [Tooltip("Optional. Assign UgsCustomRelayLobbyService so Relay lobbies can be left/deleted on cleanup.")]
        [SerializeField] private MonoBehaviour _PartyLobbyServiceSource;

        [Header("Frontend Return")]
        [Tooltip("Frontend scene name that triggers match cleanup when it becomes active.")]
        [SerializeField] private string _MainMenuSceneName = "S_MainMenu";
        [Tooltip("Ends the current match session automatically when the frontend scene becomes active.")]
        [SerializeField] private bool _ClearMatchOnMainMenuReturn = true;

        [Header("Cleanup Policy")]
        [Tooltip("Releases dedicated server allocation ids when the current match session ends.")]
        [SerializeField] private bool _ReleaseServerAllocationOnSessionEnd = true;

        [Header("Debug")]
        [Tooltip("Logs session cleanup, server release, and custom lobby cleanup events.")]
        [SerializeField] private bool _LogEvents = true;

        private IGameServerAllocationReleaser _GameServerAllocationReleaser;
        private IPartyLobbyService _PartyLobbyService;
        private IMatchConnectionController _MatchConnectionController;
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

            ResolveMatchConnectionController()?.StopConnection();
            _MatchContext?.Clear();
            _QuickMatchFlowController?.ResetToIdle();
            ReleaseServerAllocationIfNeeded(lEndpoint, lConnectionMode);
            LeavePartyLobbyIfNeeded(lLobbyId, lConnectionMode);

            if (_LogEvents)
                Debug.Log($"[Match Runtime] Match session ended. Reason={pReason}", this);

            _IsEndingSession = false;
        }

        private void ReleaseServerAllocationIfNeeded(MatchServerEndpoint pEndpoint, MatchConnectionMode pConnectionMode)
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

        private void LeavePartyLobbyIfNeeded(string pLobbyId, MatchConnectionMode pConnectionMode)
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

            if (_MatchConnectionControllerSource == null)
                _MatchConnectionControllerSource = FindMatchConnectionControllerSource();

            if (_PartyLobbyServiceSource == null)
                _PartyLobbyServiceSource = FindPartyLobbyServiceSource();

            if (_GameServerAllocatorSource == null)
                _GameServerAllocatorSource = FindGameServerAllocationReleaserSource();

            _GameServerAllocationReleaser = _GameServerAllocatorSource as IGameServerAllocationReleaser;
            _PartyLobbyService = _PartyLobbyServiceSource as IPartyLobbyService;
            _MatchConnectionController = _MatchConnectionControllerSource as IMatchConnectionController;
        }

        private IMatchConnectionController ResolveMatchConnectionController()
        {
            _MatchConnectionController ??= _MatchConnectionControllerSource as IMatchConnectionController;
            if (_MatchConnectionController != null)
                return _MatchConnectionController;

            if (_MatchConnectionControllerSource != null)
                Debug.LogWarning($"Assigned match connection controller '{_MatchConnectionControllerSource.name}' does not implement {nameof(IMatchConnectionController)}.", this);

            return null;
        }

        private MonoBehaviour FindMatchConnectionControllerSource()
        {
            MonoBehaviour[] lBehaviours = GetComponents<MonoBehaviour>();
            for (int lIndex = 0; lIndex < lBehaviours.Length; lIndex++)
            {
                if (lBehaviours[lIndex] is IMatchConnectionController)
                    return lBehaviours[lIndex];
            }

            return null;
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
