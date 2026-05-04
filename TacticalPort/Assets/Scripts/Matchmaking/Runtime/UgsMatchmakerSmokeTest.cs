#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Matchmaking
#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

namespace TacticalPort.Matchmaking
{
    public sealed class UgsMatchmakerSmokeTest : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private string _QueueName = "quickmatch1v1unranked";
        [SerializeField] private bool _RunOnStart;
        [SerializeField] private bool _ClearSessionBeforeSignIn;
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
                RunSmokeTest();
        }

        private void OnDestroy()
        {
            _CancellationTokenSource?.Cancel();
            _CancellationTokenSource?.Dispose();
            _CancellationTokenSource = null;

            if (_CancelPendingTicketOnDestroy && !_TicketCompleted && !string.IsNullOrWhiteSpace(_CurrentTicketId))
                _ = DeleteTicketAsync(_CurrentTicketId);
        }

        #endregion

        #region _____________________________| SMOKE TEST

        [ContextMenu("Run UGS Matchmaker Smoke Test")]
        public void RunSmokeTest()
        {
            if (_IsRunning)
            {
                Debug.LogWarning("[UGS SmokeTest] A smoke test is already running.", this);
                return;
            }

            _CancellationTokenSource?.Cancel();
            _CancellationTokenSource?.Dispose();
            _CancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(_TimeoutSeconds));
            _ = RunSmokeTestAsync(_CancellationTokenSource.Token);
        }

        private async Task RunSmokeTestAsync(CancellationToken pCancellationToken)
        {
            _IsRunning = true;
            _TicketCompleted = false;
            _CurrentTicketId = string.Empty;

            try
            {
                PlayerIdentity lIdentity = await EnsureSignedInAsync(_ClearSessionBeforeSignIn);

                CreateTicketResponse lTicket = await MatchmakerService.Instance.CreateTicketAsync(
                    new List<Player> { new Player(lIdentity.PlayerId) },
                    new CreateTicketOptions(_QueueName));

                _CurrentTicketId = lTicket.Id;
                Debug.Log($"[UGS SmokeTest] Ticket created. PlayerId={lIdentity.PlayerId}, Queue={_QueueName}, TicketId={_CurrentTicketId}", this);

                await PollTicketAsync(_CurrentTicketId, pCancellationToken);
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[UGS SmokeTest] Smoke test cancelled or timed out.", this);
                await DeleteTicketAsync(_CurrentTicketId);
            }
            catch (Exception pException)
            {
                Debug.LogError($"[UGS SmokeTest] Failed: {pException}", this);
                await DeleteTicketAsync(_CurrentTicketId);
            }
            finally
            {
                _IsRunning = false;
            }
        }

        private Task<PlayerIdentity> EnsureSignedInAsync(bool pClearSessionBeforeSignIn)
        {
            return UgsAuthentication.SignInAnonymouslyAsync(pClearSessionBeforeSignIn, this, CancellationToken.None);
        }

        private async Task PollTicketAsync(string pTicketId, CancellationToken pCancellationToken)
        {
            while (!pCancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(_PollIntervalSeconds), pCancellationToken);

                TicketStatusResponse lStatus = await MatchmakerService.Instance.GetTicketAsync(pTicketId);
                string lDescription = DescribeAssignment(lStatus, out bool lIsTerminal);
                Debug.Log($"[UGS SmokeTest] Ticket status: {lDescription}", this);

                if (!lIsTerminal)
                    continue;

                _TicketCompleted = true;
                if (IsFailureStatus(lStatus))
                    await DeleteTicketAsync(pTicketId);

                return;
            }
        }

        private static async Task DeleteTicketAsync(string pTicketId)
        {
            if (string.IsNullOrWhiteSpace(pTicketId))
                return;

            try
            {
                await MatchmakerService.Instance.DeleteTicketAsync(pTicketId);
                Debug.Log($"[UGS SmokeTest] Ticket deleted. TicketId={pTicketId}");
            }
            catch (Exception pException)
            {
                Debug.LogWarning($"[UGS SmokeTest] Ticket cleanup failed: {pException.Message}");
            }
        }

        #endregion

        #region _____________________________| ASSIGNMENT LOGS

        private static string DescribeAssignment(TicketStatusResponse pResponse, out bool pIsTerminal)
        {
            pIsTerminal = false;

            if (pResponse?.Value == null)
                return "No assignment yet.";

            switch (pResponse.Value)
            {
                case MatchIdAssignment lAssignment:
                    pIsTerminal = IsTerminalStatus(lAssignment.Status.ToString());
                    return $"Type={lAssignment.AssignmentType}, Status={lAssignment.Status}, MatchId={lAssignment.MatchId}, Message={lAssignment.Message}";

                case IpPortAssignment lAssignment:
                    pIsTerminal = IsTerminalStatus(lAssignment.Status.ToString());
                    return $"Type={lAssignment.AssignmentType}, Status={lAssignment.Status}, MatchId={lAssignment.MatchId}, Endpoint={lAssignment.Ip}:{lAssignment.Port}, Message={lAssignment.Message}";

                case CustomAssignment lAssignment:
                    pIsTerminal = IsTerminalStatus(lAssignment.Status.ToString());
                    return $"Type={lAssignment.AssignmentType}, Status={lAssignment.Status}, MatchId={lAssignment.MatchId}, CustomData={lAssignment.CustomData?.Count ?? 0}, Message={lAssignment.Message}";

                case NoneAssignment lAssignment:
                    pIsTerminal = IsTerminalStatus(lAssignment.Status.ToString());
                    return $"Type={lAssignment.AssignmentType}, Status={lAssignment.Status}, Message={lAssignment.Message}";

                case MultiplayAssignment lAssignment:
                    pIsTerminal = IsTerminalStatus(lAssignment.Status.ToString());
                    return $"Type={lAssignment.AssignmentType}, Status={lAssignment.Status}, MatchId={lAssignment.MatchId}, Message={lAssignment.Message}";

                default:
                    return $"Unsupported assignment type: {pResponse.Type?.Name ?? pResponse.Value.GetType().Name}";
            }
        }

        private static bool IsFailureStatus(TicketStatusResponse pResponse)
        {
            if (pResponse?.Value == null)
                return false;

            return pResponse.Value switch
            {
                MatchIdAssignment lAssignment => IsFailureStatus(lAssignment.Status.ToString()),
                IpPortAssignment lAssignment => IsFailureStatus(lAssignment.Status.ToString()),
                CustomAssignment lAssignment => IsFailureStatus(lAssignment.Status.ToString()),
                NoneAssignment lAssignment => IsFailureStatus(lAssignment.Status.ToString()),
                MultiplayAssignment lAssignment => IsFailureStatus(lAssignment.Status.ToString()),
                _ => false
            };
        }

        private static bool IsTerminalStatus(string pStatus) =>
            pStatus == "Found" || IsFailureStatus(pStatus);

        private static bool IsFailureStatus(string pStatus) =>
            pStatus == "Failed" || pStatus == "Timeout";

        #endregion
    }
}
