#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Matchmaking
#endregion

using System;
using System.Collections.Generic;
using TacticalPort.Shared;

namespace TacticalPort.Matchmaking
{
    [Serializable]
    public readonly struct PlayerIdentity
    {
        public readonly string PlayerId;
        public readonly string DisplayName;

        public PlayerIdentity(string pPlayerId, string pDisplayName)
        {
            PlayerId = pPlayerId;
            DisplayName = pDisplayName;
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(PlayerId);
    }

    [Serializable]
    public sealed class MatchPlayerAssignment
    {
        public string PlayerId = string.Empty;
        public MatchPlayerSlot Slot = MatchPlayerSlot.None;
        public string TeamPresetId = string.Empty;
    }

    [Serializable]
    public sealed class MatchManifest
    {
        public string MatchId = string.Empty;
        public string MapId = string.Empty;
        public List<MatchPlayerAssignment> Players = new List<MatchPlayerAssignment>();

        public bool TryGetSlot(string pPlayerId, out MatchPlayerSlot pSlot)
        {
            if (!string.IsNullOrWhiteSpace(pPlayerId))
            {
                for (int lIndex = 0; lIndex < Players.Count; lIndex++)
                {
                    MatchPlayerAssignment lPlayer = Players[lIndex];
                    if (lPlayer != null && lPlayer.PlayerId == pPlayerId)
                    {
                        pSlot = lPlayer.Slot;
                        return pSlot != MatchPlayerSlot.None;
                    }
                }
            }

            pSlot = MatchPlayerSlot.None;
            return false;
        }
    }

    [Serializable]
    public sealed class MatchServerEndpoint
    {
        public string IpAddress = "127.0.0.1";
        public ushort Port = 7777;
        public string AllocationId = string.Empty;

        public bool IsValid => !string.IsNullOrWhiteSpace(IpAddress) && Port > 0;
    }

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
        public List<MatchPlayerAssignment> Players = new List<MatchPlayerAssignment>();
        public MatchManifest Manifest;
        public MatchServerEndpoint ServerEndpoint;
    }

    [Serializable]
    public sealed class QuickMatchRequest
    {
        public string QueueName = "quickmatch1v1unranked";
        public string TeamPresetId = string.Empty;
        public string Region = string.Empty;
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
    public sealed class MatchTicketSnapshot
    {
        public string TicketId = string.Empty;
        public MatchTicketStatus Status = MatchTicketStatus.None;
        public MatchManifest Manifest;
        public MatchServerEndpoint ServerEndpoint;
        public string FailureReason = string.Empty;
    }

    [Serializable]
    public sealed class MatchAllocationRequest
    {
        public string MatchId = string.Empty;
        public string MapId = string.Empty;
        public MatchManifest Manifest;
    }
}
