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
    public sealed class UgsQuickMatchDebugRunner : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private UgsPlayerIdentityService _PlayerIdentityService;
        [SerializeField] private UgsQuickMatchService _QuickMatchService;
        [SerializeField] private bool _RunOnStart;
        [SerializeField] private string _QueueName = "quickmatch1v1unranked";
        [SerializeField] private string _TeamPresetId = string.Empty;
        [SerializeField, Min(1f)] private float _PollIntervalSeconds = 5f;
        [SerializeField, Min(10f)] private float _TimeoutSeconds = 90f;
        [SerializeField] private bool _CancelPendingTicketOnDestroy = true;

        private CancellationTokenSource _CancellationTokenSource;
        private string _CurrentTicketId;
        private bool _IsRunning;
        private bool _TicketCompleted;

        #endregion

        #region _____________________________| UNITY

        private void Start()
        {
            if (_RunOnStart)
                RunQuickMatchDebug();
        }

        private void OnDestroy()
        {
            _CancellationTokenSource?.Cancel();
            _CancellationTokenSource?.Dispose();
            _CancellationTokenSource = null;

            if (_CancelPendingTicketOnDestroy && !_TicketCompleted && !string.IsNullOrWhiteSpace(_CurrentTicketId))
                _ = CancelTicketAsync(_CurrentTicketId);
        }

        #endregion

        #region _____________________________| DEBUG RUN

        [ContextMenu("Run UGS Quick Match Debug")]
        public void RunQuickMatchDebug()
        {
            if (_IsRunning)
            {
                Debug.LogWarning("[UGS QuickMatch Debug] A quick match debug run is already running.", this);
                return;
            }

            if (!ValidateReferences())
                return;

            _CancellationTokenSource?.Cancel();
            _CancellationTokenSource?.Dispose();
            _CancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(_TimeoutSeconds));
            _ = RunQuickMatchDebugAsync(_CancellationTokenSource.Token);
        }

        private async Task RunQuickMatchDebugAsync(CancellationToken pCancellationToken)
        {
            _IsRunning = true;
            _TicketCompleted = false;
            _CurrentTicketId = string.Empty;

            try
            {
                PlayerIdentity lIdentity = await _PlayerIdentityService.SignInAsync(pCancellationToken);
                Debug.Log($"[UGS QuickMatch Debug] Signed in. PlayerId={lIdentity.PlayerId}, DisplayName={lIdentity.DisplayName}", this);

                MatchTicketSnapshot lTicket = await _QuickMatchService.CreateTicketAsync(BuildRequest(), pCancellationToken);
                _CurrentTicketId = lTicket.TicketId;
                Debug.Log($"[UGS QuickMatch Debug] Ticket created. Queue={_QueueName}, TicketId={_CurrentTicketId}", this);

                await PollTicketAsync(_CurrentTicketId, pCancellationToken);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[UGS QuickMatch Debug] Quick match debug run cancelled or timed out.", this);
                await CancelTicketAsync(_CurrentTicketId);
            }
            catch (Exception pException)
            {
                Debug.LogError($"[UGS QuickMatch Debug] Failed: {pException}", this);
                await CancelTicketAsync(_CurrentTicketId);
            }
            finally
            {
                _IsRunning = false;
            }
        }

        private async Task PollTicketAsync(string pTicketId, CancellationToken pCancellationToken)
        {
            while (!pCancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(_PollIntervalSeconds), pCancellationToken);

                MatchTicketSnapshot lSnapshot = await _QuickMatchService.PollTicketAsync(pTicketId, pCancellationToken);
                LogTicketStatus(lSnapshot);

                if (!IsTerminalStatus(lSnapshot.Status))
                    continue;

                _TicketCompleted = true;
                if (lSnapshot.Status == MatchTicketStatus.Failed)
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
                Debug.Log($"[UGS QuickMatch Debug] Ticket cancelled. TicketId={pTicketId}", this);
            }
            catch (Exception pException)
            {
                Debug.LogWarning($"[UGS QuickMatch Debug] Ticket cleanup failed: {pException.Message}", this);
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

        private bool ValidateReferences()
        {
            if (_PlayerIdentityService == null)
                Debug.LogWarning($"{nameof(UgsQuickMatchDebugRunner)} is missing reference '{nameof(_PlayerIdentityService)}'.", this);

            if (_QuickMatchService == null)
                Debug.LogWarning($"{nameof(UgsQuickMatchDebugRunner)} is missing reference '{nameof(_QuickMatchService)}'.", this);

            return _PlayerIdentityService != null && _QuickMatchService != null;
        }

        private void LogTicketStatus(MatchTicketSnapshot pSnapshot)
        {
            string lMatchId = pSnapshot.Manifest != null ? pSnapshot.Manifest.MatchId : string.Empty;
            string lEndpoint = pSnapshot.ServerEndpoint != null && pSnapshot.ServerEndpoint.IsValid
                ? $"{pSnapshot.ServerEndpoint.IpAddress}:{pSnapshot.ServerEndpoint.Port}"
                : string.Empty;

            Debug.Log($"[UGS QuickMatch Debug] Ticket status. Status={pSnapshot.Status}, MatchId={lMatchId}, Endpoint={lEndpoint}, Reason={pSnapshot.FailureReason}", this);
        }

        private static bool IsTerminalStatus(MatchTicketStatus pStatus) =>
            pStatus == MatchTicketStatus.Found ||
            pStatus == MatchTicketStatus.Failed ||
            pStatus == MatchTicketStatus.Cancelled;

        #endregion
    }
}
