#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Matchmaking
#endregion

using System;
using System.Collections.Generic;
using TacticalPort.Shared;
using Unity.Services.Relay.Models;

namespace TacticalPort.Matchmaking
{
    [Serializable]
    public sealed class PartyLobbyRequest
    {
        public string LobbyName = "Private Match";
        public string TeamPresetId = string.Empty;
        public int MaxPlayers = 2;
    }

    [Serializable]
    public sealed class PartyLobbySnapshot
    {
        public string LobbyId = string.Empty;
        public string JoinCode = string.Empty;
        public string RelayJoinCode = string.Empty;
        public List<MatchPlayerAssignment> Players = new List<MatchPlayerAssignment>();
        public MatchManifest Manifest;
        public MatchServerEndpoint ServerEndpoint;
        [NonSerialized] public Allocation RelayAllocation;
    }

    [Serializable]
    public sealed class QuickMatchRequest
    {
        public string QueueName = "quickmatch1v1unranked";
        public string TeamPresetId = string.Empty;
        public string Region = string.Empty;
        public PlayerIdentity Player;
    }

    public enum MatchTicketStatus
    {
        None = 0,
        Searching = 1,
        Found = 2,
        Failed = 3,
        Cancelled = 4
    }

    [Serializable]
    public sealed class MatchSessionPlayer
    {
        public string PlayerId = string.Empty;
        public MatchPlayerSlot Slot = MatchPlayerSlot.None;
        public bool IsLocalPlayer;
        public string TeamPresetId = string.Empty;
        public List<string> UnitIds = new List<string>();
    }

    [Serializable]
    public sealed class MatchSessionDescriptor
    {
        public string MatchId = string.Empty;
        public string LocalPlayerId = string.Empty;
        public MatchPlayerSlot LocalPlayerSlot = MatchPlayerSlot.None;
        public string LobbyId = string.Empty;
        public string LobbyJoinCode = string.Empty;
        public string RelayJoinCode = string.Empty;
        public List<MatchSessionPlayer> Players = new List<MatchSessionPlayer>();
        public MatchConnectionMode ConnectionMode = MatchConnectionMode.Offline;
        public MatchServerEndpoint Endpoint;
        [NonSerialized] public Allocation RelayAllocation;
        public bool IsTrusted;
        public bool ReportsRankedResults;

        public bool HasEndpoint => Endpoint != null && Endpoint.IsValid;
        public bool HasRelay => RelayAllocation != null || !string.IsNullOrWhiteSpace(RelayJoinCode);
        public bool IsNetworked => ConnectionMode != MatchConnectionMode.Offline;
        public bool IsCustomMatch => MatchTrustPolicy.IsCustom(ConnectionMode);
        public bool UsesDedicatedAuthority => MatchTrustPolicy.UsesDedicatedAuthority(ConnectionMode);
        public bool CanReportRankedResults => MatchTrustPolicy.CanReportRankedResults(ConnectionMode, IsTrusted, ReportsRankedResults);
        public string TrustLabel => MatchTrustPolicy.BuildTrustLabel(ConnectionMode, IsTrusted, ReportsRankedResults);

        public static bool TryCreate(PlayerIdentity pLocalPlayer, MatchTicketSnapshot pTicket, out MatchSessionDescriptor pDescriptor)
        {
            pDescriptor = null;
            MatchManifest lManifest = pTicket?.Manifest;
            if (!pLocalPlayer.IsValid || pTicket?.Status != MatchTicketStatus.Found || lManifest == null || !lManifest.TryValidatePlayerAssignments(out _))
                return false;

            pDescriptor = new MatchSessionDescriptor
            {
                MatchId = lManifest.MatchId,
                LocalPlayerId = pLocalPlayer.PlayerId,
                LobbyId = pTicket.LobbyId,
                LobbyJoinCode = pTicket.LobbyJoinCode,
                RelayJoinCode = pTicket.RelayJoinCode,
                ConnectionMode = pTicket.ConnectionMode,
                Endpoint = CopyEndpoint(pTicket.ServerEndpoint),
                RelayAllocation = pTicket.RelayAllocation,
                IsTrusted = pTicket.IsTrusted,
                ReportsRankedResults = pTicket.ReportsRankedResults
            };

            if (lManifest.TryGetSlot(pLocalPlayer.PlayerId, out MatchPlayerSlot lLocalSlot))
                pDescriptor.LocalPlayerSlot = lLocalSlot;

            if (lManifest.Players != null)
            {
                for (int lIndex = 0; lIndex < lManifest.Players.Count; lIndex++)
                    pDescriptor.Players.Add(CopyPlayer(lManifest.Players[lIndex], pLocalPlayer.PlayerId));
            }

            return true;
        }

        private static MatchSessionPlayer CopyPlayer(MatchPlayerAssignment pAssignment, string pLocalPlayerId)
        {
            MatchSessionPlayer lPlayer = new MatchSessionPlayer();
            if (pAssignment == null)
                return lPlayer;

            lPlayer.PlayerId = pAssignment.PlayerId;
            lPlayer.Slot = pAssignment.Slot;
            lPlayer.IsLocalPlayer = pAssignment.PlayerId == pLocalPlayerId;
            lPlayer.TeamPresetId = pAssignment.TeamPresetId;

            if (pAssignment.UnitIds != null)
            {
                for (int lIndex = 0; lIndex < pAssignment.UnitIds.Count; lIndex++)
                {
                    string lUnitId = pAssignment.UnitIds[lIndex];
                    if (!string.IsNullOrWhiteSpace(lUnitId))
                        lPlayer.UnitIds.Add(lUnitId);
                }
            }

            return lPlayer;
        }

        private static MatchServerEndpoint CopyEndpoint(MatchServerEndpoint pEndpoint) =>
            pEndpoint == null
                ? null
                : new MatchServerEndpoint
                {
                    IpAddress = pEndpoint.IpAddress,
                    Port = pEndpoint.Port,
                    AllocationId = pEndpoint.AllocationId
                };
    }

    [Serializable]
    public sealed class MatchTicketSnapshot
    {
        public string TicketId = string.Empty;
        public string LobbyId = string.Empty;
        public string LobbyJoinCode = string.Empty;
        public string RelayJoinCode = string.Empty;
        public MatchTicketStatus Status = MatchTicketStatus.None;
        public MatchManifest Manifest;
        public MatchServerEndpoint ServerEndpoint;
        [NonSerialized] public Allocation RelayAllocation;
        public string FailureReason = string.Empty;
        public MatchConnectionMode ConnectionMode = MatchConnectionMode.QuickMatchLocalServer;
        public bool IsTrusted = true;
        public bool ReportsRankedResults;

        public bool IsCustomMatch => MatchTrustPolicy.IsCustom(ConnectionMode);
        public bool UsesDedicatedAuthority => MatchTrustPolicy.UsesDedicatedAuthority(ConnectionMode);
        public bool CanReportRankedResults => MatchTrustPolicy.CanReportRankedResults(ConnectionMode, IsTrusted, ReportsRankedResults);
        public string TrustLabel => MatchTrustPolicy.BuildTrustLabel(ConnectionMode, IsTrusted, ReportsRankedResults);
    }

    [Serializable]
    public sealed class MatchAllocationRequest
    {
        public string TicketId = string.Empty;
        public string MatchId = string.Empty;
        public string MapId = string.Empty;
        public string QueueName = string.Empty;
        public string LocalPlayerId = string.Empty;
        public MatchManifest Manifest;
    }
}
