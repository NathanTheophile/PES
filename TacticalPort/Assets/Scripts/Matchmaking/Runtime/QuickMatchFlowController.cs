#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
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

        [SerializeField] private MonoBehaviour _PlayerIdentityServiceSource;
        [SerializeField] private MonoBehaviour _QuickMatchServiceSource;
        [SerializeField] private string _QueueName = "quickmatch1v1unranked";
        [SerializeField] private string _TeamPresetId = string.Empty;
        [SerializeField, Min(1f)] private float _PollIntervalSeconds = 5f;
        [SerializeField, Min(10f)] private float _TimeoutSeconds = 90f;
        [SerializeField] private bool _CancelPendingTicketOnDestroy = true;
        [SerializeField] private bool _LogEvents = true;

        private IPlayerIdentityService _PlayerIdentityService;
        private IQuickMatchService _QuickMatchService;
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
                return;

            _CancellationTokenSource?.Cancel();
            _CancellationTokenSource?.Dispose();
            _CancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(_TimeoutSeconds));
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
                MatchTicketSnapshot lTicket = await _QuickMatchService.CreateTicketAsync(BuildRequest(), pCancellationToken);
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

        private QuickMatchRequest BuildRequest() =>
            new QuickMatchRequest
            {
                QueueName = _QueueName,
                TeamPresetId = _TeamPresetId
            };

        #endregion

        #region _____________________________| HELPERS

        private bool ResolveServices()
        {
            _PlayerIdentityService = _PlayerIdentityServiceSource as IPlayerIdentityService;
            _QuickMatchService = _QuickMatchServiceSource as IQuickMatchService;

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
                MatchTicketStatus.Found => "Match found.",
                MatchTicketStatus.Failed => !string.IsNullOrWhiteSpace(pTicket.FailureReason) ? pTicket.FailureReason : "Quick match failed.",
                MatchTicketStatus.Cancelled => "Quick match cancelled.",
                _ => "Waiting for ticket status."
            };

        #endregion
    }
}
