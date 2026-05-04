#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Matchmaking
#endregion

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

namespace TacticalPort.Matchmaking
{
    public sealed class UgsQuickMatchService : MonoBehaviour, IQuickMatchService
    {
        #region _____________________________/ VALUES

        [SerializeField] private string _DefaultQueueName = "quickmatch1v1unranked";
        [SerializeField] private string _DefaultMapId = "poutch";
        [SerializeField] private bool _ClearSessionBeforeSignIn;
        [SerializeField] private bool _LogTicketEvents;

        #endregion

        #region _____________________________| TICKET

        public async Task<MatchTicketSnapshot> CreateTicketAsync(QuickMatchRequest pRequest, CancellationToken pCancellationToken)
        {
            PlayerIdentity lIdentity = await UgsAuthentication.SignInAnonymouslyAsync(_ClearSessionBeforeSignIn, this, pCancellationToken);
            string lQueueName = ResolveQueueName(pRequest);

            CreateTicketResponse lTicket = await MatchmakerService.Instance.CreateTicketAsync(
                new List<Player> { new Player(lIdentity.PlayerId, BuildPlayerData(pRequest)) },
                new CreateTicketOptions(lQueueName));

            pCancellationToken.ThrowIfCancellationRequested();

            string lTicketId = lTicket.Id;

            if (_LogTicketEvents)
                Debug.Log($"[UGS QuickMatch] Ticket created. PlayerId={lIdentity.PlayerId}, Queue={lQueueName}, TicketId={lTicketId}", this);

            return new MatchTicketSnapshot
            {
                TicketId = lTicketId,
                Status = MatchTicketStatus.Searching
            };
        }

        public async Task<MatchTicketSnapshot> PollTicketAsync(string pTicketId, CancellationToken pCancellationToken)
        {
            pCancellationToken.ThrowIfCancellationRequested();

            TicketStatusResponse lStatus = await MatchmakerService.Instance.GetTicketAsync(pTicketId);
            pCancellationToken.ThrowIfCancellationRequested();

            MatchTicketSnapshot lSnapshot = BuildSnapshot(pTicketId, lStatus);
            if (_LogTicketEvents)
                Debug.Log($"[UGS QuickMatch] Ticket status. TicketId={pTicketId}, Status={lSnapshot.Status}, MatchId={lSnapshot.Manifest?.MatchId}, Reason={lSnapshot.FailureReason}", this);

            return lSnapshot;
        }

        public async Task CancelTicketAsync(string pTicketId, CancellationToken pCancellationToken)
        {
            pCancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(pTicketId))
                return;

            await MatchmakerService.Instance.DeleteTicketAsync(pTicketId);

            if (_LogTicketEvents)
                Debug.Log($"[UGS QuickMatch] Ticket cancelled. TicketId={pTicketId}", this);
        }

        private string ResolveQueueName(QuickMatchRequest pRequest) =>
            !string.IsNullOrWhiteSpace(pRequest?.QueueName) ? pRequest.QueueName : _DefaultQueueName;

        private static Dictionary<string, object> BuildPlayerData(QuickMatchRequest pRequest)
        {
            if (string.IsNullOrWhiteSpace(pRequest?.TeamPresetId))
                return null;

            return new Dictionary<string, object>
            {
                { "teamPresetId", pRequest.TeamPresetId }
            };
        }

        #endregion

        #region _____________________________| SNAPSHOT

        private MatchTicketSnapshot BuildSnapshot(string pTicketId, TicketStatusResponse pStatus)
        {
            if (pStatus?.Value == null)
                return SearchingSnapshot(pTicketId);

            return pStatus.Value switch
            {
                MatchIdAssignment lAssignment => BuildSnapshot(pTicketId, lAssignment.Status.ToString(), lAssignment.MatchId, lAssignment.Message),
                IpPortAssignment lAssignment => BuildEndpointSnapshot(pTicketId, lAssignment),
                CustomAssignment lAssignment => BuildSnapshot(pTicketId, lAssignment.Status.ToString(), lAssignment.MatchId, lAssignment.Message),
                NoneAssignment lAssignment => BuildSnapshot(pTicketId, lAssignment.Status.ToString(), string.Empty, lAssignment.Message),
                MultiplayAssignment lAssignment => BuildSnapshot(pTicketId, lAssignment.Status.ToString(), lAssignment.MatchId, lAssignment.Message),
                _ => new MatchTicketSnapshot
                {
                    TicketId = pTicketId,
                    Status = MatchTicketStatus.Failed,
                    FailureReason = $"Unsupported assignment type: {pStatus.Type?.Name ?? pStatus.Value.GetType().Name}"
                }
            };
        }

        private MatchTicketSnapshot BuildEndpointSnapshot(string pTicketId, IpPortAssignment pAssignment)
        {
            MatchTicketSnapshot lSnapshot = BuildSnapshot(pTicketId, pAssignment.Status.ToString(), pAssignment.MatchId, pAssignment.Message);
            if (lSnapshot.Status == MatchTicketStatus.Found && pAssignment.Port is > 0 and <= ushort.MaxValue && !string.IsNullOrWhiteSpace(pAssignment.Ip))
            {
                lSnapshot.ServerEndpoint = new MatchServerEndpoint
                {
                    IpAddress = pAssignment.Ip,
                    Port = (ushort)pAssignment.Port.Value,
                    AllocationId = pAssignment.MatchId ?? string.Empty
                };
            }

            return lSnapshot;
        }

        private MatchTicketSnapshot BuildSnapshot(string pTicketId, string pStatus, string pMatchId, string pFailureReason)
        {
            MatchTicketStatus lStatus = ToTicketStatus(pStatus);
            MatchManifest lManifest = lStatus == MatchTicketStatus.Found
                ? BuildManifest(pMatchId)
                : null;

            return new MatchTicketSnapshot
            {
                TicketId = pTicketId,
                Status = lStatus,
                Manifest = lManifest,
                FailureReason = IsFailureStatus(pStatus) ? ResolveFailureReason(pStatus, pFailureReason) : string.Empty
            };
        }

        private MatchManifest BuildManifest(string pMatchId) =>
            new MatchManifest
            {
                MatchId = pMatchId ?? string.Empty,
                MapId = _DefaultMapId
            };

        private static MatchTicketSnapshot SearchingSnapshot(string pTicketId) =>
            new MatchTicketSnapshot
            {
                TicketId = pTicketId,
                Status = MatchTicketStatus.Searching
            };

        private static MatchTicketStatus ToTicketStatus(string pStatus) =>
            pStatus switch
            {
                "Found" => MatchTicketStatus.Found,
                "InProgress" => MatchTicketStatus.Searching,
                "Failed" => MatchTicketStatus.Failed,
                "Timeout" => MatchTicketStatus.Failed,
                _ => MatchTicketStatus.None
            };

        private static bool IsFailureStatus(string pStatus) =>
            pStatus == "Failed" || pStatus == "Timeout";

        private static string ResolveFailureReason(string pStatus, string pFailureReason) =>
            !string.IsNullOrWhiteSpace(pFailureReason) ? pFailureReason : pStatus;

        #endregion
    }
}
