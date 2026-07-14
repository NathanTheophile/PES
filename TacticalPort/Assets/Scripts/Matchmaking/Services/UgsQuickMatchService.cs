#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Matchmaking
#endregion

using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TacticalPort.Shared;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Http;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using MatchmakerPlayer = Unity.Services.Matchmaker.Models.Player;
using MatchmakerTeam = Unity.Services.Matchmaker.Models.Team;

namespace TacticalPort.Matchmaking
{
    public sealed class UgsQuickMatchService : MonoBehaviour, IQuickMatchService
    {
        private const string EdgeGapRequestIdCustomDataKey = "edgegapRequestId";
        private const string RequestIdCustomDataKey = "requestId";

        #region _____________________________/ VALUES

        [SerializeField] private string _DefaultQueueName = "quickmatch1v1unranked";
        [SerializeField] private string _DefaultMapId = "alpha-1";
        [SerializeField] private bool _ClearSessionBeforeSignIn;
        [SerializeField] private bool _FetchMatchmakingResults = true;
        [SerializeField] private bool _LogTicketEvents;

        #endregion

        public void PreserveAuthenticationSession() => _ClearSessionBeforeSignIn = false;

        #region _____________________________| TICKET

        public async Task<MatchTicketSnapshot> CreateTicketAsync(QuickMatchRequest pRequest, CancellationToken pCancellationToken)
        {
            PlayerIdentity lIdentity = await ResolvePlayerIdentityAsync(pRequest, pCancellationToken);
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
            if (_FetchMatchmakingResults && lSnapshot.Status == MatchTicketStatus.Found && lSnapshot.Manifest != null)
                await TryPopulateManifestFromMatchmakerAsync(lSnapshot.Manifest, pCancellationToken);

            lSnapshot = ValidateFoundSnapshot(lSnapshot);

            if (_LogTicketEvents)
                Debug.Log($"[UGS QuickMatch] Ticket status. TicketId={pTicketId}, Status={lSnapshot.Status}, MatchId={lSnapshot.Manifest?.MatchId}, Players={lSnapshot.Manifest?.Players?.Count ?? 0}, Reason={lSnapshot.FailureReason}", this);

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

        private async Task<PlayerIdentity> ResolvePlayerIdentityAsync(QuickMatchRequest pRequest, CancellationToken pCancellationToken)
        {
            if (pRequest != null && pRequest.Player.IsValid)
                return pRequest.Player;

            return await UgsAuthentication.SignInAnonymouslyAsync(_ClearSessionBeforeSignIn, this, pCancellationToken);
        }

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
                    AllocationId = ResolveAllocationId(pAssignment)
                };
            }

            return lSnapshot;
        }

        private static string ResolveAllocationId(IpPortAssignment pAssignment)
        {
            if (TryReadCustomData(pAssignment?.CustomData, EdgeGapRequestIdCustomDataKey, out string lRequestId))
                return lRequestId;

            if (TryReadCustomData(pAssignment?.CustomData, RequestIdCustomDataKey, out lRequestId))
                return lRequestId;

            return pAssignment?.MatchId ?? string.Empty;
        }

        private static bool TryReadCustomData(IReadOnlyDictionary<string, IDeserializable> pCustomData, string pKey, out string pValue)
        {
            pValue = string.Empty;
            if (pCustomData == null || string.IsNullOrWhiteSpace(pKey) || !pCustomData.TryGetValue(pKey, out IDeserializable lValue) || lValue == null)
                return false;

            pValue = lValue.GetAsString()?.Trim().Trim('"') ?? string.Empty;
            return !string.IsNullOrWhiteSpace(pValue);
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

        private static MatchTicketSnapshot ValidateFoundSnapshot(MatchTicketSnapshot pSnapshot)
        {
            if (pSnapshot == null || pSnapshot.Status != MatchTicketStatus.Found)
                return pSnapshot;

            if (pSnapshot.Manifest != null && pSnapshot.Manifest.TryValidatePlayerAssignments(out _))
                return pSnapshot;

            return new MatchTicketSnapshot
            {
                TicketId = pSnapshot.TicketId,
                Status = MatchTicketStatus.Failed,
                Manifest = pSnapshot.Manifest,
                ServerEndpoint = pSnapshot.ServerEndpoint,
                FailureReason = "Match found, but player slot assignments are missing or invalid."
            };
        }

        private async Task TryPopulateManifestFromMatchmakerAsync(MatchManifest pManifest, CancellationToken pCancellationToken)
        {
            if (pManifest == null || string.IsNullOrWhiteSpace(pManifest.MatchId))
                return;

            try
            {
                StoredMatchmakingResults lResults = await MatchmakerService.Instance.GetMatchmakingResultsAsync(pManifest.MatchId);
                pCancellationToken.ThrowIfCancellationRequested();
                PopulateManifestFromResults(pManifest, lResults);
            }
            catch (System.OperationCanceledException)
            {
                throw;
            }
            catch (System.Exception pException)
            {
                Debug.LogWarning($"[UGS QuickMatch] Could not fetch matchmaking results for manifest assignments: {pException.Message}", this);
            }
        }

        private static void PopulateManifestFromResults(MatchManifest pManifest, StoredMatchmakingResults pResults)
        {
            if (pManifest == null || pResults?.MatchProperties == null)
                return;

            pManifest.Players.Clear();

            Dictionary<string, string> lPresetIdsByPlayerId = BuildTeamPresetLookup(pResults.MatchProperties.Players);
            IReadOnlyList<MatchmakerTeam> lTeams = pResults.MatchProperties.Teams;
            if (lTeams != null && lTeams.Count > 0)
            {
                for (int lTeamIndex = 0; lTeamIndex < lTeams.Count; lTeamIndex++)
                {
                    MatchPlayerSlot lSlot = ResolveSlotForTeamIndex(lTeamIndex);
                    if (lSlot == MatchPlayerSlot.None)
                        continue;

                    List<string> lPlayerIds = lTeams[lTeamIndex]?.PlayerIds;
                    if (lPlayerIds == null)
                        continue;

                    for (int lPlayerIndex = 0; lPlayerIndex < lPlayerIds.Count; lPlayerIndex++)
                    {
                        string lPlayerId = lPlayerIds[lPlayerIndex];
                        AddPlayerAssignment(pManifest, lPlayerId, lSlot, ResolveTeamPresetId(lPresetIdsByPlayerId, lPlayerId));
                    }
                }

                if (pManifest.Players.Count > 0)
                    return;
            }

            IReadOnlyList<MatchmakerPlayer> lPlayers = pResults.MatchProperties.Players;
            if (lPlayers == null)
                return;

            for (int lIndex = 0; lIndex < lPlayers.Count; lIndex++)
                AddPlayerAssignment(pManifest, lPlayers[lIndex]?.Id, ResolveSlotForTeamIndex(lIndex), ResolveTeamPresetId(lPlayers[lIndex]));
        }

        private static MatchPlayerSlot ResolveSlotForTeamIndex(int pIndex) =>
            pIndex == 0
                ? MatchPlayerSlot.TeamA
                : pIndex == 1
                    ? MatchPlayerSlot.TeamB
                    : MatchPlayerSlot.None;

        private static void AddPlayerAssignment(MatchManifest pManifest, string pPlayerId, MatchPlayerSlot pSlot, string pTeamPresetId)
        {
            if (pManifest == null || string.IsNullOrWhiteSpace(pPlayerId) || pSlot == MatchPlayerSlot.None)
                return;

            pManifest.Players.Add(new MatchPlayerAssignment
            {
                PlayerId = pPlayerId,
                Slot = pSlot,
                TeamPresetId = pTeamPresetId ?? string.Empty
            });
        }

        private static string ResolveTeamPresetId(MatchmakerPlayer pPlayer)
        {
            return TryReadPlayerData(pPlayer, "teamPresetId", out string lTeamPresetId)
                ? lTeamPresetId
                : string.Empty;
        }

        private static Dictionary<string, string> BuildTeamPresetLookup(IReadOnlyList<MatchmakerPlayer> pPlayers)
        {
            Dictionary<string, string> lResult = new Dictionary<string, string>();
            if (pPlayers == null)
                return lResult;

            for (int lIndex = 0; lIndex < pPlayers.Count; lIndex++)
            {
                MatchmakerPlayer lPlayer = pPlayers[lIndex];
                if (lPlayer == null || string.IsNullOrWhiteSpace(lPlayer.Id))
                    continue;

                lResult[lPlayer.Id] = ResolveTeamPresetId(lPlayer);
            }

            return lResult;
        }

        private static string ResolveTeamPresetId(IReadOnlyDictionary<string, string> pPresetIdsByPlayerId, string pPlayerId) =>
            !string.IsNullOrWhiteSpace(pPlayerId) && pPresetIdsByPlayerId != null && pPresetIdsByPlayerId.TryGetValue(pPlayerId, out string lPresetId)
                ? lPresetId
                : string.Empty;

        private static bool TryReadPlayerData(MatchmakerPlayer pPlayer, string pKey, out string pValue)
        {
            pValue = string.Empty;
            if (pPlayer == null || string.IsNullOrWhiteSpace(pKey))
                return false;

            object lPlayerData = TryGetPropertyValue(pPlayer, "CustomData")
                ?? TryGetPropertyValue(pPlayer, "Data")
                ?? TryGetPropertyValue(pPlayer, "Properties");

            if (lPlayerData is IDictionary lDictionary && lDictionary.Contains(pKey) && lDictionary[pKey] != null)
            {
                pValue = lDictionary[pKey].ToString();
                return !string.IsNullOrWhiteSpace(pValue);
            }

            return false;
        }

        private static object TryGetPropertyValue(object pSource, string pPropertyName)
        {
            return pSource?.GetType().GetProperty(pPropertyName)?.GetValue(pSource);
        }

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
                "Pending" => MatchTicketStatus.Searching,
                "None" => MatchTicketStatus.Searching,
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
