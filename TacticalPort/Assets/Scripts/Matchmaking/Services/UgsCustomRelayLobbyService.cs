#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Matchmaking
#endregion

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TacticalPort.Shared;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace TacticalPort.Matchmaking
{
    public sealed class UgsCustomRelayLobbyService : MonoBehaviour, IPartyLobbyService
    {
        #region _____________________________/ VALUES

        private const string RelayJoinCodeKey = "relayJoinCode";
        private const string MapIdKey = "mapId";
        private const string PlayerSlotKey = "slot";
        private const string TeamPresetIdKey = "teamPresetId";
        private const string ReadyKey = "ready";

        [SerializeField] private string _LobbyName = "Private Match";
        [SerializeField] private string _MapId = "custom-relay";
        [SerializeField, Min(2)] private int _MaxPlayers = 2;
        [SerializeField, Min(1)] private int _RelayMaxConnections = 1;
        [Tooltip("Leave empty to let UGS pick the best Relay region.")]
        [SerializeField] private string _RelayRegion = string.Empty;
        [Tooltip("Normally false. The identity service can clear clone sessions before this service is called.")]
        [SerializeField] private bool _ClearSessionBeforeSignIn;
        [SerializeField, Min(5f)] private float _HeartbeatIntervalSeconds = 15f;
        [SerializeField] private bool _LogEvents = true;

        private string _CurrentLobbyId = string.Empty;
        private string _CurrentPlayerId = string.Empty;
        private bool _IsCurrentHost;
        private bool _IsHeartbeatPending;
        private float _NextHeartbeatTime;

        #endregion

        #region _____________________________| UNITY

        private void Update()
        {
            if (!_IsCurrentHost || string.IsNullOrWhiteSpace(_CurrentLobbyId) || Time.unscaledTime < _NextHeartbeatTime || _IsHeartbeatPending)
                return;

            _NextHeartbeatTime = Time.unscaledTime + _HeartbeatIntervalSeconds;
            _ = SendHeartbeatAsync(_CurrentLobbyId);
        }

        #endregion

        #region _____________________________| LOBBY

        public async Task<PartyLobbySnapshot> CreateLobbyAsync(PartyLobbyRequest pRequest, CancellationToken pCancellationToken)
        {
            PlayerIdentity lIdentity = await SignInAsync(pCancellationToken);
            Allocation lAllocation = await RelayService.Instance.CreateAllocationAsync(Mathf.Max(1, _RelayMaxConnections), NormalizeRegion());
            pCancellationToken.ThrowIfCancellationRequested();

            string lRelayJoinCode = await RelayService.Instance.GetJoinCodeAsync(lAllocation.AllocationId);
            pCancellationToken.ThrowIfCancellationRequested();

            string lTeamPresetId = pRequest?.TeamPresetId ?? string.Empty;
            int lMaxPlayers = Mathf.Max(2, pRequest?.MaxPlayers ?? _MaxPlayers);
            string lLobbyName = string.IsNullOrWhiteSpace(pRequest?.LobbyName) ? _LobbyName : pRequest.LobbyName.Trim();

            Lobby lLobby = null;
            try
            {
                lLobby = await LobbyService.Instance.CreateLobbyAsync(
                    lLobbyName,
                    lMaxPlayers,
                    new CreateLobbyOptions
                    {
                        IsPrivate = true,
                        Player = BuildPlayer(lIdentity.PlayerId, MatchPlayerSlot.TeamA, lTeamPresetId, false),
                        Data = BuildLobbyData(lRelayJoinCode)
                    });

                pCancellationToken.ThrowIfCancellationRequested();
                CacheCurrentLobby(lLobby, lIdentity.PlayerId, true);
            }
            catch
            {
                await TryDeleteCreatedLobbyAsync(lLobby);
                throw;
            }

            PartyLobbySnapshot lSnapshot = BuildSnapshot(lLobby, lIdentity.PlayerId, MatchPlayerSlot.TeamA, lTeamPresetId, lRelayJoinCode, lAllocation);

            if (_LogEvents)
                Debug.Log($"[UGS Custom Relay] Lobby created. LobbyId={lSnapshot.LobbyId}, LobbyCode={lSnapshot.JoinCode}, RelayCode={lSnapshot.RelayJoinCode}", this);

            return lSnapshot;
        }

        public async Task<PartyLobbySnapshot> JoinLobbyAsync(string pJoinCode, string pTeamPresetId, CancellationToken pCancellationToken)
        {
            if (string.IsNullOrWhiteSpace(pJoinCode))
                throw new ArgumentException("Lobby join code is empty.", nameof(pJoinCode));

            PlayerIdentity lIdentity = await SignInAsync(pCancellationToken);
            Lobby lLobby = null;
            try
            {
                lLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(
                    pJoinCode.Trim(),
                    new JoinLobbyByCodeOptions
                    {
                        Player = BuildPlayer(lIdentity.PlayerId, MatchPlayerSlot.TeamB, pTeamPresetId, false)
                    });

                pCancellationToken.ThrowIfCancellationRequested();
                CacheCurrentLobby(lLobby, lIdentity.PlayerId, false);
            }
            catch
            {
                await TryRemoveJoinedPlayerAsync(lLobby, lIdentity.PlayerId);
                throw;
            }

            string lRelayJoinCode = ReadLobbyData(lLobby, RelayJoinCodeKey);
            if (string.IsNullOrWhiteSpace(lRelayJoinCode))
                throw new InvalidOperationException("Joined lobby does not contain a Relay join code.");

            PartyLobbySnapshot lSnapshot = BuildSnapshot(lLobby, lIdentity.PlayerId, MatchPlayerSlot.TeamB, pTeamPresetId, lRelayJoinCode, null);

            if (_LogEvents)
                Debug.Log($"[UGS Custom Relay] Lobby joined. LobbyId={lSnapshot.LobbyId}, LobbyCode={lSnapshot.JoinCode}, RelayCode={lSnapshot.RelayJoinCode}", this);

            return lSnapshot;
        }

        public async Task<PartyLobbySnapshot> SetReadyAsync(string pLobbyId, bool pIsReady, CancellationToken pCancellationToken)
        {
            string lLobbyId = ResolveLobbyId(pLobbyId);
            if (string.IsNullOrWhiteSpace(lLobbyId))
                throw new InvalidOperationException("No active custom lobby.");

            PlayerIdentity lIdentity = await SignInAsync(pCancellationToken);
            Lobby lLobby = await LobbyService.Instance.UpdatePlayerAsync(
                lLobbyId,
                lIdentity.PlayerId,
                new UpdatePlayerOptions
                {
                    Data = new Dictionary<string, PlayerDataObject>
                    {
                        [ReadyKey] = BuildPlayerData(pIsReady ? "true" : "false")
                    }
                });

            pCancellationToken.ThrowIfCancellationRequested();
            string lRelayJoinCode = ReadLobbyData(lLobby, RelayJoinCodeKey);
            return BuildSnapshot(lLobby, lIdentity.PlayerId, ReadPlayerSlot(lLobby, lIdentity.PlayerId, MatchPlayerSlot.None), string.Empty, lRelayJoinCode, null);
        }

        public async Task LeaveLobbyAsync(string pLobbyId, CancellationToken pCancellationToken)
        {
            string lLobbyId = ResolveLobbyId(pLobbyId);
            if (string.IsNullOrWhiteSpace(lLobbyId))
                return;

            pCancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (_IsCurrentHost)
                    await LobbyService.Instance.DeleteLobbyAsync(lLobbyId);
                else
                    await LobbyService.Instance.RemovePlayerAsync(lLobbyId, _CurrentPlayerId);

                if (_LogEvents)
                    Debug.Log($"[UGS Custom Relay] Lobby left. LobbyId={lLobbyId}", this);
            }
            catch (Exception pException)
            {
                Debug.LogWarning($"[UGS Custom Relay] Lobby cleanup failed. LobbyId={lLobbyId}, Reason={pException.Message}", this);
            }
            finally
            {
                if (lLobbyId == _CurrentLobbyId)
                    ClearCurrentLobby();
            }
        }

        #endregion

        #region _____________________________| SNAPSHOT

        private PartyLobbySnapshot BuildSnapshot(Lobby pLobby, string pLocalPlayerId, MatchPlayerSlot pLocalSlot, string pTeamPresetId, string pRelayJoinCode, Allocation pRelayAllocation)
        {
            MatchManifest lManifest = BuildManifest(pLobby, pLocalPlayerId, pLocalSlot, pTeamPresetId);
            return new PartyLobbySnapshot
            {
                LobbyId = pLobby?.Id ?? string.Empty,
                JoinCode = pLobby?.LobbyCode ?? string.Empty,
                RelayJoinCode = pRelayJoinCode ?? string.Empty,
                Manifest = lManifest,
                RelayAllocation = pRelayAllocation,
                Players = lManifest.Players
            };
        }

        private MatchManifest BuildManifest(Lobby pLobby, string pLocalPlayerId, MatchPlayerSlot pLocalSlot, string pTeamPresetId)
        {
            MatchManifest lManifest = new MatchManifest
            {
                MatchId = BuildMatchId(pLobby),
                MapId = string.IsNullOrWhiteSpace(_MapId) ? "custom-relay" : _MapId
            };

            AddLobbyPlayers(lManifest, pLobby);
            EnsureSlot(lManifest, MatchPlayerSlot.TeamA, ResolveHostPlayerId(pLobby, pLocalPlayerId, pLocalSlot), pLocalSlot == MatchPlayerSlot.TeamA ? pTeamPresetId : string.Empty);
            EnsureSlot(lManifest, MatchPlayerSlot.TeamB, pLocalSlot == MatchPlayerSlot.TeamB ? pLocalPlayerId : "custom-guest-pending", pLocalSlot == MatchPlayerSlot.TeamB ? pTeamPresetId : string.Empty);
            return lManifest;
        }

        private static void AddLobbyPlayers(MatchManifest pManifest, Lobby pLobby)
        {
            if (pManifest == null || pLobby?.Players == null)
                return;

            for (int lIndex = 0; lIndex < pLobby.Players.Count; lIndex++)
            {
                Player lPlayer = pLobby.Players[lIndex];
                if (lPlayer == null || string.IsNullOrWhiteSpace(lPlayer.Id) || !TryReadPlayerSlot(lPlayer, out MatchPlayerSlot lSlot))
                    continue;

                pManifest.SetAssignment(lSlot, lPlayer.Id, ReadPlayerData(lPlayer, TeamPresetIdKey), null);
            }
        }

        private static void EnsureSlot(MatchManifest pManifest, MatchPlayerSlot pSlot, string pPlayerId, string pTeamPresetId)
        {
            if (pManifest == null || pManifest.TryGetAssignment(pSlot, out _) || string.IsNullOrWhiteSpace(pPlayerId))
                return;

            pManifest.SetAssignment(pSlot, pPlayerId, pTeamPresetId ?? string.Empty, null);
        }

        #endregion

        #region _____________________________| HELPERS

        private Task<PlayerIdentity> SignInAsync(CancellationToken pCancellationToken) =>
            UgsAuthentication.SignInAnonymouslyAsync(_ClearSessionBeforeSignIn, this, pCancellationToken);

        private Player BuildPlayer(string pPlayerId, MatchPlayerSlot pSlot, string pTeamPresetId, bool pIsReady) =>
            new Player(pPlayerId, data: new Dictionary<string, PlayerDataObject>
            {
                [PlayerSlotKey] = BuildPlayerData(pSlot.ToString()),
                [TeamPresetIdKey] = BuildPlayerData(pTeamPresetId ?? string.Empty),
                [ReadyKey] = BuildPlayerData(pIsReady ? "true" : "false")
            });

        private Dictionary<string, DataObject> BuildLobbyData(string pRelayJoinCode) =>
            new Dictionary<string, DataObject>
            {
                [RelayJoinCodeKey] = new DataObject(DataObject.VisibilityOptions.Member, pRelayJoinCode ?? string.Empty),
                [MapIdKey] = new DataObject(DataObject.VisibilityOptions.Member, string.IsNullOrWhiteSpace(_MapId) ? "custom-relay" : _MapId)
            };

        private static PlayerDataObject BuildPlayerData(string pValue) =>
            new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, pValue ?? string.Empty);

        private static bool TryReadPlayerSlot(Player pPlayer, out MatchPlayerSlot pSlot) =>
            Enum.TryParse(ReadPlayerData(pPlayer, PlayerSlotKey), true, out pSlot) && pSlot != MatchPlayerSlot.None;

        private static MatchPlayerSlot ReadPlayerSlot(Lobby pLobby, string pPlayerId, MatchPlayerSlot pFallback)
        {
            if (pLobby?.Players != null)
            {
                for (int lIndex = 0; lIndex < pLobby.Players.Count; lIndex++)
                {
                    Player lPlayer = pLobby.Players[lIndex];
                    if (lPlayer != null && lPlayer.Id == pPlayerId && TryReadPlayerSlot(lPlayer, out MatchPlayerSlot lSlot))
                        return lSlot;
                }
            }

            return pFallback;
        }

        private static string ReadPlayerData(Player pPlayer, string pKey) =>
            pPlayer?.Data != null && pPlayer.Data.TryGetValue(pKey, out PlayerDataObject lData) ? lData?.Value ?? string.Empty : string.Empty;

        private static string ReadLobbyData(Lobby pLobby, string pKey) =>
            pLobby?.Data != null && pLobby.Data.TryGetValue(pKey, out DataObject lData) ? lData?.Value ?? string.Empty : string.Empty;

        private string ResolveHostPlayerId(Lobby pLobby, string pLocalPlayerId, MatchPlayerSlot pLocalSlot)
        {
            if (!string.IsNullOrWhiteSpace(pLobby?.HostId))
                return pLobby.HostId;

            return pLocalSlot == MatchPlayerSlot.TeamA ? pLocalPlayerId : "custom-host-pending";
        }

        private static string BuildMatchId(Lobby pLobby) =>
            string.IsNullOrWhiteSpace(pLobby?.Id) ? "custom-relay-pending" : $"custom-relay-{pLobby.Id}";

        private string NormalizeRegion() => string.IsNullOrWhiteSpace(_RelayRegion) ? null : _RelayRegion.Trim();

        private string ResolveLobbyId(string pLobbyId) => string.IsNullOrWhiteSpace(pLobbyId) ? _CurrentLobbyId : pLobbyId.Trim();

        private void CacheCurrentLobby(Lobby pLobby, string pPlayerId, bool pIsHost)
        {
            _CurrentLobbyId = pLobby?.Id ?? string.Empty;
            _CurrentPlayerId = pPlayerId ?? string.Empty;
            _IsCurrentHost = pIsHost;
            _NextHeartbeatTime = Time.unscaledTime + _HeartbeatIntervalSeconds;
        }

        private void ClearCurrentLobby()
        {
            _CurrentLobbyId = string.Empty;
            _CurrentPlayerId = string.Empty;
            _IsCurrentHost = false;
            _IsHeartbeatPending = false;
        }

        private async Task SendHeartbeatAsync(string pLobbyId)
        {
            _IsHeartbeatPending = true;
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(pLobbyId);
            }
            catch (Exception pException)
            {
                if (_LogEvents)
                    Debug.LogWarning($"[UGS Custom Relay] Lobby heartbeat failed. LobbyId={pLobbyId}, Reason={pException.Message}", this);
            }
            finally
            {
                _IsHeartbeatPending = false;
            }
        }

        private async Task TryDeleteCreatedLobbyAsync(Lobby pLobby)
        {
            if (string.IsNullOrWhiteSpace(pLobby?.Id))
                return;

            try
            {
                await LobbyService.Instance.DeleteLobbyAsync(pLobby.Id);
            }
            catch (Exception pException)
            {
                if (_LogEvents)
                    Debug.LogWarning($"[UGS Custom Relay] Cancel cleanup failed after lobby creation. LobbyId={pLobby.Id}, Reason={pException.Message}", this);
            }
        }

        private async Task TryRemoveJoinedPlayerAsync(Lobby pLobby, string pPlayerId)
        {
            if (string.IsNullOrWhiteSpace(pLobby?.Id) || string.IsNullOrWhiteSpace(pPlayerId))
                return;

            try
            {
                await LobbyService.Instance.RemovePlayerAsync(pLobby.Id, pPlayerId);
            }
            catch (Exception pException)
            {
                if (_LogEvents)
                    Debug.LogWarning($"[UGS Custom Relay] Cancel cleanup failed after lobby join. LobbyId={pLobby.Id}, PlayerId={pPlayerId}, Reason={pException.Message}", this);
            }
        }

        #endregion
    }
}
