#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Matchmaking
#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace TacticalPort.Matchmaking
{
    public enum QuickMatchFlowStatus
    {
        Idle = 0,
        SigningIn = 1,
        CreatingTicket = 2,
        Searching = 3,
        Found = 4,
        Failed = 5,
        Cancelled = 6
    }

    [Serializable]
    public sealed class QuickMatchFlowSnapshot
    {
        public QuickMatchFlowStatus Status = QuickMatchFlowStatus.Idle;
        public PlayerIdentity Player;
        public MatchTicketSnapshot Ticket;
        public string Message = string.Empty;
    }

    public sealed class QuickMatchFlowController : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [Header("Service Sources")]
        [Tooltip("Source implementing IPlayerIdentityService, usually UgsPlayerIdentityService in S_Bootstrap.")]
        [SerializeField] private MonoBehaviour _PlayerIdentityServiceSource;
        [Tooltip("Source implementing IQuickMatchService, usually UgsQuickMatchService in S_Bootstrap.")]
        [SerializeField] private MonoBehaviour _QuickMatchServiceSource;
        [Tooltip("Optional. Source implementing IGameServerAllocator for local validation or EdgeGap allocation.")]
        [SerializeField] private MonoBehaviour _GameServerAllocatorSource;
        [Tooltip("Runtime context populated when a match is found.")]
        [SerializeField] private MatchRuntimeContext _MatchContext;

        [Header("Match Request")]
        [Tooltip("UGS Matchmaker queue name used when creating a quick match ticket.")]
        [SerializeField] private string _QueueName = "quickmatch1v1unranked";
        [Tooltip("Team preset id included in the ticket request. Empty keeps the current local fallback behavior.")]
        [SerializeField] private string _TeamPresetId = string.Empty;

        [Header("Connection Policy")]
        [Tooltip("Connection mode used when no dedicated server endpoint or allocator is available.")]
        [SerializeField] private MatchConnectionMode _ConnectionModeWithoutAllocator = MatchConnectionMode.QuickMatchLocalServer;
        [Tooltip("Connection mode used when Matchmaker or an allocator provides a server endpoint.")]
        [SerializeField] private MatchConnectionMode _ConnectionModeWithAllocator = MatchConnectionMode.DedicatedServer;
        [Tooltip("Keep disabled until a trusted ranked backend validates result reporting.")]
        [SerializeField] private bool _ReportsRankedResultsWithAllocator;

        [Header("Polling And Cleanup")]
        [Tooltip("Delay between Matchmaker ticket polling requests.")]
        [SerializeField, Min(1f)] private float _PollIntervalSeconds = 5f;
        [Tooltip("Maximum time spent searching before the ticket is cancelled locally.")]
        [SerializeField, Min(10f)] private float _TimeoutSeconds = 90f;
        [Tooltip("Cancels the active Matchmaker ticket if this controller is destroyed while a search is pending.")]
        [SerializeField] private bool _CancelPendingTicketOnDestroy = true;

        [Header("Debug")]
        [Tooltip("Logs quick match state changes and allocation details.")]
        [SerializeField] private bool _LogEvents = true;

        private IPlayerIdentityService _PlayerIdentityService;
        private IQuickMatchService _QuickMatchService;
        private IGameServerAllocator _GameServerAllocator;
        private CancellationTokenSource _CancellationTokenSource;
        private QuickMatchFlowSnapshot _LastSnapshot = new QuickMatchFlowSnapshot();
        private string _CurrentTicketId;
        private bool _IsRunning;
        private bool _TicketCompleted;
        private bool _CancelRequested;

        #endregion

        #region _____________________________/ ACCESSORS

        public event Action<QuickMatchFlowSnapshot> StateChanged;

        public bool IsRunning => _IsRunning;
        public QuickMatchFlowSnapshot LastSnapshot => _LastSnapshot;

        #endregion

        #region _____________________________| CONFIGURE

        public void ConfigureServices(MonoBehaviour pPlayerIdentityServiceSource, MonoBehaviour pQuickMatchServiceSource) =>
            ConfigureServices(pPlayerIdentityServiceSource, pQuickMatchServiceSource, _MatchContext);

        public void ConfigureServices(MonoBehaviour pPlayerIdentityServiceSource, MonoBehaviour pQuickMatchServiceSource, MatchRuntimeContext pMatchContext)
        {
            ConfigureServices(pPlayerIdentityServiceSource, pQuickMatchServiceSource, _GameServerAllocatorSource, pMatchContext);
        }

        public void ConfigureServices(MonoBehaviour pPlayerIdentityServiceSource, MonoBehaviour pQuickMatchServiceSource, MonoBehaviour pGameServerAllocatorSource, MatchRuntimeContext pMatchContext)
        {
            _PlayerIdentityServiceSource = pPlayerIdentityServiceSource;
            _QuickMatchServiceSource = pQuickMatchServiceSource;
            _GameServerAllocatorSource = pGameServerAllocatorSource;
            _MatchContext = pMatchContext;
            _PlayerIdentityService = null;
            _QuickMatchService = null;
            _GameServerAllocator = null;
        }

        #endregion

        #region _____________________________| UNITY

        private void OnDestroy()
        {
            _CancellationTokenSource?.Cancel();
            _CancellationTokenSource?.Dispose();
            _CancellationTokenSource = null;

            if (_CancelPendingTicketOnDestroy && !_TicketCompleted && !string.IsNullOrWhiteSpace(_CurrentTicketId))
                _ = CancelTicketAsync(_CurrentTicketId);
        }

        #endregion

        #region _____________________________| FLOW

        [ContextMenu("Start Quick Match")]
        public void StartQuickMatch()
        {
            if (_IsRunning)
            {
                Debug.LogWarning($"{nameof(QuickMatchFlowController)} is already searching.", this);
                return;
            }

            if (!ResolveServices())
            {
                Publish(QuickMatchFlowStatus.Failed, default, null, "Runtime matchmaking services are not configured. Start from S_Bootstrap.");
                return;
            }

            _CancellationTokenSource?.Cancel();
            _CancellationTokenSource?.Dispose();
            _CancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(_TimeoutSeconds));
            _MatchContext?.Clear();
            _ = RunQuickMatchAsync(_CancellationTokenSource.Token);
        }

        [ContextMenu("Cancel Quick Match")]
        public void CancelQuickMatch()
        {
            if (!_IsRunning)
                return;

            _CancelRequested = true;
            _CancellationTokenSource?.Cancel();
        }

        public void SetTeamPresetId(string pTeamPresetId) =>
            _TeamPresetId = pTeamPresetId ?? string.Empty;

        public void ResetToIdle()
        {
            if (_IsRunning)
            {
                CancelQuickMatch();
                return;
            }

            _CancellationTokenSource?.Cancel();
            _CancellationTokenSource?.Dispose();
            _CancellationTokenSource = null;
            _CurrentTicketId = string.Empty;
            _TicketCompleted = true;
            _CancelRequested = false;
            Publish(QuickMatchFlowStatus.Idle, default, null, "Ready for quick match.");
        }

        private async Task RunQuickMatchAsync(CancellationToken pCancellationToken)
        {
            _IsRunning = true;
            _TicketCompleted = false;
            _CancelRequested = false;
            _CurrentTicketId = string.Empty;

            PlayerIdentity lIdentity = default;

            try
            {
                Publish(QuickMatchFlowStatus.SigningIn, lIdentity, null, "Signing in.");
                lIdentity = await _PlayerIdentityService.SignInAsync(pCancellationToken);

                Publish(QuickMatchFlowStatus.CreatingTicket, lIdentity, null, "Creating quick match ticket.");
                MatchTicketSnapshot lTicket = await _QuickMatchService.CreateTicketAsync(BuildRequest(lIdentity), pCancellationToken);
                _CurrentTicketId = lTicket.TicketId;

                Publish(QuickMatchFlowStatus.Searching, lIdentity, lTicket, "Searching for an opponent.");
                await PollTicketAsync(lIdentity, _CurrentTicketId, pCancellationToken);
            }
            catch (OperationCanceledException)
            {
                QuickMatchFlowStatus lStatus = _CancelRequested ? QuickMatchFlowStatus.Cancelled : QuickMatchFlowStatus.Failed;
                string lMessage = _CancelRequested ? "Quick match cancelled." : "Quick match timed out.";
                await CancelTicketAsync(_CurrentTicketId);
                Publish(lStatus, lIdentity, _LastSnapshot.Ticket, lMessage);
            }
            catch (Exception pException)
            {
                await CancelTicketAsync(_CurrentTicketId);
                Debug.LogError($"{nameof(QuickMatchFlowController)} failed: {pException}", this);
                Publish(QuickMatchFlowStatus.Failed, lIdentity, _LastSnapshot.Ticket, pException.Message);
            }
            finally
            {
                _IsRunning = false;
            }
        }

        private async Task PollTicketAsync(PlayerIdentity pIdentity, string pTicketId, CancellationToken pCancellationToken)
        {
            while (!pCancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(_PollIntervalSeconds), pCancellationToken);

                MatchTicketSnapshot lTicket = await _QuickMatchService.PollTicketAsync(pTicketId, pCancellationToken);
                QuickMatchFlowStatus lStatus = ToFlowStatus(lTicket.Status);
                Publish(lStatus, pIdentity, lTicket, BuildStatusMessage(lTicket));

                if (!IsTerminalStatus(lStatus))
                    continue;

                _TicketCompleted = true;
                if (lStatus == QuickMatchFlowStatus.Found)
                {
                    await PrepareFoundTicketAsync(pIdentity, lTicket, pCancellationToken);
                    _MatchContext?.SetQuickMatchResult(pIdentity, lTicket);
                }

                if (lStatus == QuickMatchFlowStatus.Failed)
                    await CancelTicketAsync(pTicketId);

                return;
            }
        }

        private async Task CancelTicketAsync(string pTicketId)
        {
            if (string.IsNullOrWhiteSpace(pTicketId) || _QuickMatchService == null)
                return;

            _TicketCompleted = true;

            try
            {
                await _QuickMatchService.CancelTicketAsync(pTicketId, CancellationToken.None);
            }
            catch (Exception pException)
            {
                Debug.LogWarning($"{nameof(QuickMatchFlowController)} failed to cancel ticket '{pTicketId}': {pException.Message}", this);
            }
        }

        private QuickMatchRequest BuildRequest(PlayerIdentity pIdentity) =>
            new QuickMatchRequest
            {
                QueueName = _QueueName,
                TeamPresetId = _TeamPresetId,
                Player = pIdentity
            };

        private async Task PrepareFoundTicketAsync(PlayerIdentity pIdentity, MatchTicketSnapshot pTicket, CancellationToken pCancellationToken)
        {
            if (pTicket == null || pTicket.Status != MatchTicketStatus.Found)
                return;

            _GameServerAllocator ??= _GameServerAllocatorSource as IGameServerAllocator;
            if (pTicket.ServerEndpoint != null && pTicket.ServerEndpoint.IsValid)
            {
                ApplyTrustPolicy(pTicket, _ConnectionModeWithAllocator);
                return;
            }

            if (_GameServerAllocator == null)
            {
                ApplyTrustPolicy(pTicket, _ConnectionModeWithoutAllocator);
                return;
            }

            MatchServerEndpoint lEndpoint = await _GameServerAllocator.AllocateServerAsync(BuildAllocationRequest(pIdentity, pTicket), pCancellationToken);
            pTicket.ServerEndpoint = lEndpoint;
            ApplyTrustPolicy(pTicket, _ConnectionModeWithAllocator);

            if (_LogEvents)
                Debug.Log($"[QuickMatch Flow] Server endpoint allocated. MatchId={pTicket.Manifest?.MatchId}, Endpoint={lEndpoint.IpAddress}:{lEndpoint.Port}, AllocationId={lEndpoint.AllocationId}, Trust={pTicket.TrustLabel}", this);
        }

        private void ApplyTrustPolicy(MatchTicketSnapshot pTicket, MatchConnectionMode pMode)
        {
            if (pTicket == null)
                return;

            pTicket.ConnectionMode = pMode;
            pTicket.IsTrusted = MatchTrustPolicy.UsesDedicatedAuthority(pMode);
            pTicket.ReportsRankedResults = pMode == MatchConnectionMode.DedicatedServer && _ReportsRankedResultsWithAllocator;
        }

        private MatchAllocationRequest BuildAllocationRequest(PlayerIdentity pIdentity, MatchTicketSnapshot pTicket) =>
            new MatchAllocationRequest
            {
                TicketId = pTicket?.TicketId ?? string.Empty,
                MatchId = pTicket?.Manifest?.MatchId ?? string.Empty,
                MapId = pTicket?.Manifest?.MapId ?? string.Empty,
                QueueName = _QueueName,
                LocalPlayerId = pIdentity.PlayerId,
                Manifest = pTicket?.Manifest
            };

        #endregion

        #region _____________________________| HELPERS

        private bool ResolveServices()
        {
            _PlayerIdentityService = _PlayerIdentityServiceSource as IPlayerIdentityService;
            _QuickMatchService = _QuickMatchServiceSource as IQuickMatchService;
            _GameServerAllocator = _GameServerAllocatorSource as IGameServerAllocator;

            if (_PlayerIdentityService == null)
                Debug.LogWarning($"{nameof(QuickMatchFlowController)} needs a source implementing {nameof(IPlayerIdentityService)}.", this);

            if (_QuickMatchService == null)
                Debug.LogWarning($"{nameof(QuickMatchFlowController)} needs a source implementing {nameof(IQuickMatchService)}.", this);

            return _PlayerIdentityService != null && _QuickMatchService != null;
        }

        private void Publish(QuickMatchFlowStatus pStatus, PlayerIdentity pIdentity, MatchTicketSnapshot pTicket, string pMessage)
        {
            _LastSnapshot = new QuickMatchFlowSnapshot
            {
                Status = pStatus,
                Player = pIdentity,
                Ticket = pTicket,
                Message = pMessage ?? string.Empty
            };

            if (_LogEvents)
                Debug.Log($"[QuickMatch Flow] Status={pStatus}, PlayerId={pIdentity.PlayerId}, TicketId={pTicket?.TicketId}, MatchId={pTicket?.Manifest?.MatchId}, Message={_LastSnapshot.Message}", this);

            StateChanged?.Invoke(_LastSnapshot);
        }

        private static QuickMatchFlowStatus ToFlowStatus(MatchTicketStatus pStatus) =>
            pStatus switch
            {
                MatchTicketStatus.Searching => QuickMatchFlowStatus.Searching,
                MatchTicketStatus.Found => QuickMatchFlowStatus.Found,
                MatchTicketStatus.Failed => QuickMatchFlowStatus.Failed,
                MatchTicketStatus.Cancelled => QuickMatchFlowStatus.Cancelled,
                _ => QuickMatchFlowStatus.Searching
            };

        private static bool IsTerminalStatus(QuickMatchFlowStatus pStatus) =>
            pStatus == QuickMatchFlowStatus.Found ||
            pStatus == QuickMatchFlowStatus.Failed ||
            pStatus == QuickMatchFlowStatus.Cancelled;

        private static string BuildStatusMessage(MatchTicketSnapshot pTicket) =>
            pTicket?.Status switch
            {
                MatchTicketStatus.Searching => "Searching for an opponent.",
                MatchTicketStatus.Found => BuildFoundStatusMessage(pTicket),
                MatchTicketStatus.Failed => !string.IsNullOrWhiteSpace(pTicket.FailureReason) ? pTicket.FailureReason : "Quick match failed.",
                MatchTicketStatus.Cancelled => "Quick match cancelled.",
                _ => "Waiting for ticket status."
            };

        private static string BuildFoundStatusMessage(MatchTicketSnapshot pTicket) =>
            pTicket?.ServerEndpoint != null && pTicket.ServerEndpoint.IsValid
                ? $"Match found. Server {pTicket.ServerEndpoint.IpAddress}:{pTicket.ServerEndpoint.Port}."
                : "Match found.";

        #endregion
    }
}
