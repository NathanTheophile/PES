#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Matchmaking
#endregion

using System;
using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;
using Unity.Services.Relay.Models;

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
        public List<string> UnitIds = new List<string>();
    }

    [Serializable]
    public sealed class MatchManifest
    {
        public string MatchId = string.Empty;
        public string MapId = string.Empty;
        public List<MatchPlayerAssignment> Players = new List<MatchPlayerAssignment>();

        public bool HasPlayerAssignments => Players != null && Players.Count > 0;

        public bool HasAllPlayerCompositions
        {
            get
            {
                if (Players == null || Players.Count == 0)
                    return false;

                for (int lIndex = 0; lIndex < Players.Count; lIndex++)
                {
                    MatchPlayerAssignment lPlayer = Players[lIndex];
                    if (lPlayer == null || lPlayer.UnitIds == null || lPlayer.UnitIds.Count == 0)
                        return false;
                }

                return true;
            }
        }

        public bool TryGetSlot(string pPlayerId, out MatchPlayerSlot pSlot)
        {
            if (!string.IsNullOrWhiteSpace(pPlayerId) && Players != null)
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

        public bool TryGetPlayerId(MatchPlayerSlot pSlot, out string pPlayerId)
        {
            if (pSlot != MatchPlayerSlot.None && Players != null)
            {
                for (int lIndex = 0; lIndex < Players.Count; lIndex++)
                {
                    MatchPlayerAssignment lPlayer = Players[lIndex];
                    if (lPlayer != null && lPlayer.Slot == pSlot && !string.IsNullOrWhiteSpace(lPlayer.PlayerId))
                    {
                        pPlayerId = lPlayer.PlayerId;
                        return true;
                    }
                }
            }

            pPlayerId = string.Empty;
            return false;
        }

        public bool TryGetAssignment(string pPlayerId, out MatchPlayerAssignment pAssignment)
        {
            pAssignment = FindAssignment(pPlayerId);
            return pAssignment != null;
        }

        public bool TryGetAssignment(MatchPlayerSlot pSlot, out MatchPlayerAssignment pAssignment)
        {
            if (pSlot != MatchPlayerSlot.None && Players != null)
            {
                for (int lIndex = 0; lIndex < Players.Count; lIndex++)
                {
                    MatchPlayerAssignment lPlayer = Players[lIndex];
                    if (lPlayer != null && lPlayer.Slot == pSlot)
                    {
                        pAssignment = lPlayer;
                        return true;
                    }
                }
            }

            pAssignment = null;
            return false;
        }

        public bool TryGetUnitIds(MatchPlayerSlot pSlot, out IReadOnlyList<string> pUnitIds)
        {
            if (pSlot != MatchPlayerSlot.None && Players != null)
            {
                for (int lIndex = 0; lIndex < Players.Count; lIndex++)
                {
                    MatchPlayerAssignment lPlayer = Players[lIndex];
                    if (lPlayer != null && lPlayer.Slot == pSlot && lPlayer.UnitIds != null && lPlayer.UnitIds.Count > 0)
                    {
                        pUnitIds = lPlayer.UnitIds;
                        return true;
                    }
                }
            }

            pUnitIds = null;
            return false;
        }

        public void SetUnitIds(string pPlayerId, IReadOnlyList<UnitDefinition> pUnits)
        {
            MatchPlayerAssignment lAssignment = FindAssignment(pPlayerId);
            if (lAssignment == null)
                return;

            lAssignment.UnitIds ??= new List<string>();
            lAssignment.UnitIds.Clear();

            if (pUnits == null)
                return;

            for (int lIndex = 0; lIndex < pUnits.Count; lIndex++)
            {
                UnitDefinition lUnit = pUnits[lIndex];
                if (lUnit != null && !string.IsNullOrWhiteSpace(lUnit.Id))
                    lAssignment.UnitIds.Add(lUnit.Id);
            }
        }

        public void SetUnitIds(string pPlayerId, IReadOnlyList<string> pUnitIds)
        {
            MatchPlayerAssignment lAssignment = FindAssignment(pPlayerId);
            if (lAssignment != null)
                CopyUnitIds(pUnitIds, lAssignment);
        }

        public void SetAssignment(MatchPlayerSlot pSlot, string pPlayerId, string pTeamPresetId, IReadOnlyList<string> pUnitIds)
        {
            if (pSlot == MatchPlayerSlot.None || string.IsNullOrWhiteSpace(pPlayerId))
                return;

            Players ??= new List<MatchPlayerAssignment>();
            if (!TryGetAssignment(pSlot, out MatchPlayerAssignment lAssignment))
            {
                lAssignment = new MatchPlayerAssignment { Slot = pSlot };
                Players.Add(lAssignment);
            }

            lAssignment.PlayerId = pPlayerId;
            lAssignment.TeamPresetId = pTeamPresetId ?? string.Empty;
            CopyUnitIds(pUnitIds, lAssignment);
        }

        public void MergeCompositions(MatchManifest pOther)
        {
            if (pOther?.Players == null || Players == null)
                return;

            for (int lIndex = 0; lIndex < pOther.Players.Count; lIndex++)
            {
                MatchPlayerAssignment lIncoming = pOther.Players[lIndex];
                MatchPlayerAssignment lLocal = lIncoming != null ? FindAssignment(lIncoming.PlayerId) : null;
                if (lLocal == null)
                    continue;

                if (lIncoming.UnitIds != null && lIncoming.UnitIds.Count > 0)
                    CopyUnitIds(lIncoming.UnitIds, lLocal);
            }
        }

        public bool TryValidatePlayerAssignments(out string pFailure)
        {
            pFailure = string.Empty;

            if (Players == null || Players.Count == 0)
            {
                pFailure = "Match manifest has no player slot assignments.";
                return false;
            }

            int lTeamACount = 0;
            int lTeamBCount = 0;
            HashSet<string> lPlayerIds = new HashSet<string>();
            for (int lIndex = 0; lIndex < Players.Count; lIndex++)
            {
                MatchPlayerAssignment lPlayer = Players[lIndex];
                if (lPlayer == null || string.IsNullOrWhiteSpace(lPlayer.PlayerId))
                {
                    pFailure = "Match manifest contains an empty player assignment.";
                    return false;
                }

                if (!lPlayerIds.Add(lPlayer.PlayerId))
                {
                    pFailure = $"Match manifest contains duplicate player assignment '{lPlayer.PlayerId}'.";
                    return false;
                }

                if (lPlayer.Slot == MatchPlayerSlot.TeamA)
                    lTeamACount++;
                else if (lPlayer.Slot == MatchPlayerSlot.TeamB)
                    lTeamBCount++;
                else
                {
                    pFailure = $"Match manifest contains invalid slot '{lPlayer.Slot}' for player '{lPlayer.PlayerId}'.";
                    return false;
                }
            }

            if (lTeamACount == 1 && lTeamBCount == 1)
                return true;

            pFailure = $"Match manifest expected one TeamA player and one TeamB player, got TeamA={lTeamACount}, TeamB={lTeamBCount}.";
            return false;
        }

        public bool HasSamePlayerAssignments(MatchManifest pOther)
        {
            if (pOther == null || MatchId != pOther.MatchId || Players == null || pOther.Players == null || Players.Count != pOther.Players.Count)
                return false;

            for (int lIndex = 0; lIndex < Players.Count; lIndex++)
            {
                MatchPlayerAssignment lPlayer = Players[lIndex];
                if (lPlayer == null || !pOther.TryGetSlot(lPlayer.PlayerId, out MatchPlayerSlot lOtherSlot) || lOtherSlot != lPlayer.Slot)
                    return false;
            }

            return true;
        }

        private MatchPlayerAssignment FindAssignment(string pPlayerId)
        {
            if (string.IsNullOrWhiteSpace(pPlayerId) || Players == null)
                return null;

            for (int lIndex = 0; lIndex < Players.Count; lIndex++)
            {
                MatchPlayerAssignment lPlayer = Players[lIndex];
                if (lPlayer != null && lPlayer.PlayerId == pPlayerId)
                    return lPlayer;
            }

            return null;
        }

        private static void CopyUnitIds(IReadOnlyList<string> pUnitIds, MatchPlayerAssignment pTarget)
        {
            pTarget.UnitIds ??= new List<string>();
            pTarget.UnitIds.Clear();

            if (pUnitIds == null)
                return;

            for (int lIndex = 0; lIndex < pUnitIds.Count; lIndex++)
            {
                string lUnitId = pUnitIds[lIndex];
                if (!string.IsNullOrWhiteSpace(lUnitId))
                    pTarget.UnitIds.Add(lUnitId);
            }
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

    public enum MatchConnectionMode
    {
        Offline = 0,
        CustomHost = 1,
        CustomJoin = 2,
        QuickMatchLocalServer = 3,
        DedicatedServer = 4,
        CustomRelayHost = 5,
        CustomRelayJoin = 6
    }

    public static class MatchTrustPolicy
    {
        public static bool IsCustom(MatchConnectionMode pMode) =>
            pMode is MatchConnectionMode.CustomHost or MatchConnectionMode.CustomJoin
                or MatchConnectionMode.CustomRelayHost or MatchConnectionMode.CustomRelayJoin;

        public static bool UsesDedicatedAuthority(MatchConnectionMode pMode) =>
            pMode is MatchConnectionMode.QuickMatchLocalServer or MatchConnectionMode.DedicatedServer;

        public static bool CanReportRankedResults(MatchConnectionMode pMode, bool pIsTrusted, bool pReportsRankedResults) =>
            pMode == MatchConnectionMode.DedicatedServer && pIsTrusted && pReportsRankedResults;

        public static string BuildTrustLabel(MatchConnectionMode pMode, bool pIsTrusted, bool pReportsRankedResults)
        {
            if (IsCustom(pMode))
                return "Custom unranked";

            if (CanReportRankedResults(pMode, pIsTrusted, pReportsRankedResults))
                return "Ranked authoritative";

            return pMode switch
            {
                MatchConnectionMode.QuickMatchLocalServer => "Local server test",
                MatchConnectionMode.DedicatedServer => "Authoritative unranked",
                _ => "Offline"
            };
        }
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
