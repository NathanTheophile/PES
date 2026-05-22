#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Matchmaking
#endregion

using System;
using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.Matchmaking
{
    public sealed class MatchRuntimeContext : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private bool _LogChanges;
        [Tooltip("Optional local catalog used until player compositions are loaded from Cloud Save.")]
        [SerializeField] private CombatTeamPresetCatalog _TeamPresetCatalog;
        [SerializeField] private int _DefaultCustomMatchPort = 7777;

        private PlayerIdentity _LocalPlayer;
        private MatchTicketSnapshot _QuickMatchTicket;

        #endregion

        #region _____________________________/ ACCESSORS

        public event Action<MatchRuntimeContext> Changed;

        public PlayerIdentity LocalPlayer => _LocalPlayer;
        public MatchTicketSnapshot QuickMatchTicket => _QuickMatchTicket;
        public MatchManifest Manifest => _QuickMatchTicket?.Manifest;
        public MatchServerEndpoint ServerEndpoint => _QuickMatchTicket?.ServerEndpoint;
        public string LobbyId => _QuickMatchTicket != null ? _QuickMatchTicket.LobbyId : string.Empty;
        public string LobbyJoinCode => _QuickMatchTicket != null ? _QuickMatchTicket.LobbyJoinCode : string.Empty;
        public string RelayJoinCode => _QuickMatchTicket != null ? _QuickMatchTicket.RelayJoinCode : string.Empty;
        public MatchConnectionMode ConnectionMode => _QuickMatchTicket != null ? _QuickMatchTicket.ConnectionMode : MatchConnectionMode.Offline;

        public bool HasMatch =>
            _LocalPlayer.IsValid &&
            _QuickMatchTicket?.Status == MatchTicketStatus.Found &&
            _QuickMatchTicket.Manifest != null &&
            !string.IsNullOrWhiteSpace(_QuickMatchTicket.Manifest.MatchId) &&
            _QuickMatchTicket.Manifest.TryValidatePlayerAssignments(out _);

        public string MatchId => Manifest != null ? Manifest.MatchId : string.Empty;

        public bool TryGetSessionDescriptor(out MatchSessionDescriptor pDescriptor) =>
            MatchSessionDescriptor.TryCreate(_LocalPlayer, _QuickMatchTicket, out pDescriptor);

        #endregion

        #region _____________________________| CONTEXT

        public void SetQuickMatchResult(PlayerIdentity pLocalPlayer, MatchTicketSnapshot pTicket)
        {
            SetMatchSession(pLocalPlayer, pTicket);
        }

        public void SetCustomHostSession(PlayerIdentity pLocalPlayer, MatchServerEndpoint pEndpoint)
        {
            SetMatchSession(pLocalPlayer, BuildCustomTicket(pLocalPlayer, pEndpoint, MatchConnectionMode.CustomHost));
        }

        public void SetCustomJoinSession(PlayerIdentity pLocalPlayer, MatchServerEndpoint pEndpoint)
        {
            SetMatchSession(pLocalPlayer, BuildCustomTicket(pLocalPlayer, pEndpoint, MatchConnectionMode.CustomJoin));
        }

        public void SetDedicatedServerTestSession(PlayerIdentity pLocalPlayer, MatchServerEndpoint pEndpoint, MatchPlayerSlot pLocalSlot)
        {
            SetMatchSession(pLocalPlayer, BuildDedicatedServerTestTicket(pLocalPlayer, pEndpoint, pLocalSlot));
        }

        public void SetCustomRelayHostSession(PlayerIdentity pLocalPlayer, PartyLobbySnapshot pLobby)
        {
            SetMatchSession(pLocalPlayer, BuildCustomRelayTicket(pLocalPlayer, pLobby, MatchConnectionMode.CustomRelayHost));
        }

        public void SetCustomRelayJoinSession(PlayerIdentity pLocalPlayer, PartyLobbySnapshot pLobby)
        {
            SetMatchSession(pLocalPlayer, BuildCustomRelayTicket(pLocalPlayer, pLobby, MatchConnectionMode.CustomRelayJoin));
        }

        private void SetMatchSession(PlayerIdentity pLocalPlayer, MatchTicketSnapshot pTicket)
        {
            _LocalPlayer = pLocalPlayer;
            _QuickMatchTicket = pTicket;
            RefreshCompositionsFromManifest();

            if (_LogChanges)
                Debug.Log($"[Match Runtime] Match context updated. PlayerId={_LocalPlayer.PlayerId}, TicketId={_QuickMatchTicket?.TicketId}, MatchId={MatchId}", this);

            Changed?.Invoke(this);
        }

        public void SetManifest(MatchManifest pManifest)
        {
            if (_QuickMatchTicket == null || pManifest == null)
                return;

            _QuickMatchTicket.Manifest = pManifest;
            RefreshCompositionsFromManifest();

            if (_LogChanges)
                Debug.Log($"[Match Runtime] Match manifest updated. PlayerId={_LocalPlayer.PlayerId}, MatchId={MatchId}", this);

            Changed?.Invoke(this);
        }

        public void RefreshCompositionsFromManifest()
        {
            MatchCombatCompositionImporter.ImportLocalSelection(_LocalPlayer, Manifest, _TeamPresetCatalog);
        }

        public void Clear()
        {
            _LocalPlayer = default;
            _QuickMatchTicket = null;
            MatchCombatCompositionImporter.ClearMatchCompositions();

            if (_LogChanges)
                Debug.Log("[Match Runtime] Match context cleared.", this);

            Changed?.Invoke(this);
        }

        #endregion

        #region _____________________________| CUSTOM MATCH

        private MatchTicketSnapshot BuildCustomTicket(PlayerIdentity pLocalPlayer, MatchServerEndpoint pEndpoint, MatchConnectionMode pMode)
        {
            MatchServerEndpoint lEndpoint = NormalizeEndpoint(pEndpoint);
            MatchManifest lManifest = new MatchManifest
            {
                MatchId = BuildCustomMatchId(lEndpoint),
                MapId = "custom-direct"
            };

            if (pMode == MatchConnectionMode.CustomHost)
            {
                lManifest.Players.Add(new MatchPlayerAssignment { PlayerId = pLocalPlayer.PlayerId, Slot = MatchPlayerSlot.TeamA });
                lManifest.Players.Add(new MatchPlayerAssignment { PlayerId = "custom-guest-pending", Slot = MatchPlayerSlot.TeamB });
            }
            else
            {
                lManifest.Players.Add(new MatchPlayerAssignment { PlayerId = "custom-host-pending", Slot = MatchPlayerSlot.TeamA });
                lManifest.Players.Add(new MatchPlayerAssignment { PlayerId = pLocalPlayer.PlayerId, Slot = MatchPlayerSlot.TeamB });
            }

            return new MatchTicketSnapshot
            {
                TicketId = lManifest.MatchId,
                Status = MatchTicketStatus.Found,
                Manifest = lManifest,
                ServerEndpoint = lEndpoint,
                ConnectionMode = pMode,
                IsTrusted = false,
                ReportsRankedResults = false
            };
        }

        private MatchServerEndpoint NormalizeEndpoint(MatchServerEndpoint pEndpoint)
        {
            ushort lPort = pEndpoint != null && pEndpoint.Port > 0 ? pEndpoint.Port : (ushort)Mathf.Clamp(_DefaultCustomMatchPort, 1, ushort.MaxValue);
            string lIpAddress = pEndpoint != null && !string.IsNullOrWhiteSpace(pEndpoint.IpAddress) ? pEndpoint.IpAddress : "127.0.0.1";
            return new MatchServerEndpoint
            {
                IpAddress = lIpAddress,
                Port = lPort,
                AllocationId = BuildCustomMatchId(lPort)
            };
        }

        private static string BuildCustomMatchId(MatchServerEndpoint pEndpoint) => BuildCustomMatchId(pEndpoint != null ? pEndpoint.Port : (ushort)7777);
        private static string BuildCustomMatchId(ushort pPort) => $"custom-direct-{pPort}";

        private MatchTicketSnapshot BuildDedicatedServerTestTicket(PlayerIdentity pLocalPlayer, MatchServerEndpoint pEndpoint, MatchPlayerSlot pLocalSlot)
        {
            MatchServerEndpoint lEndpoint = NormalizeEndpoint(pEndpoint);
            MatchPlayerSlot lLocalSlot = pLocalSlot == MatchPlayerSlot.TeamB ? MatchPlayerSlot.TeamB : MatchPlayerSlot.TeamA;
            MatchPlayerSlot lRemoteSlot = lLocalSlot == MatchPlayerSlot.TeamA ? MatchPlayerSlot.TeamB : MatchPlayerSlot.TeamA;

            MatchManifest lManifest = new MatchManifest
            {
                MatchId = BuildDedicatedServerTestMatchId(lEndpoint),
                MapId = "poutch"
            };

            lManifest.Players.Add(new MatchPlayerAssignment { PlayerId = pLocalPlayer.PlayerId, Slot = lLocalSlot });
            lManifest.Players.Add(new MatchPlayerAssignment { PlayerId = $"dedicated-test-{lRemoteSlot}-pending", Slot = lRemoteSlot });

            return new MatchTicketSnapshot
            {
                TicketId = lManifest.MatchId,
                Status = MatchTicketStatus.Found,
                Manifest = lManifest,
                ServerEndpoint = lEndpoint,
                ConnectionMode = MatchConnectionMode.DedicatedServer,
                IsTrusted = false,
                ReportsRankedResults = false
            };
        }

        private static string BuildDedicatedServerTestMatchId(MatchServerEndpoint pEndpoint) =>
            pEndpoint == null ? "dedicated-direct" : $"dedicated-direct-{pEndpoint.IpAddress}-{pEndpoint.Port}";

        private MatchTicketSnapshot BuildCustomRelayTicket(PlayerIdentity pLocalPlayer, PartyLobbySnapshot pLobby, MatchConnectionMode pMode)
        {
            MatchManifest lManifest = pLobby?.Manifest ?? BuildFallbackRelayManifest(pLocalPlayer, pLobby, pMode);
            if (string.IsNullOrWhiteSpace(lManifest.MatchId))
                lManifest.MatchId = BuildCustomRelayMatchId(pLobby?.LobbyId);

            if (string.IsNullOrWhiteSpace(lManifest.MapId))
                lManifest.MapId = "custom-relay";

            return new MatchTicketSnapshot
            {
                TicketId = lManifest.MatchId,
                LobbyId = pLobby?.LobbyId ?? string.Empty,
                LobbyJoinCode = pLobby?.JoinCode ?? string.Empty,
                RelayJoinCode = pLobby?.RelayJoinCode ?? string.Empty,
                Status = MatchTicketStatus.Found,
                Manifest = lManifest,
                ServerEndpoint = pLobby?.ServerEndpoint,
                RelayAllocation = pLobby?.RelayAllocation,
                ConnectionMode = pMode,
                IsTrusted = false,
                ReportsRankedResults = false
            };
        }

        private static MatchManifest BuildFallbackRelayManifest(PlayerIdentity pLocalPlayer, PartyLobbySnapshot pLobby, MatchConnectionMode pMode)
        {
            MatchManifest lManifest = new MatchManifest
            {
                MatchId = BuildCustomRelayMatchId(pLobby?.LobbyId),
                MapId = "custom-relay"
            };

            if (pMode == MatchConnectionMode.CustomRelayHost)
            {
                lManifest.Players.Add(new MatchPlayerAssignment { PlayerId = pLocalPlayer.PlayerId, Slot = MatchPlayerSlot.TeamA });
                lManifest.Players.Add(new MatchPlayerAssignment { PlayerId = "custom-guest-pending", Slot = MatchPlayerSlot.TeamB });
            }
            else
            {
                lManifest.Players.Add(new MatchPlayerAssignment { PlayerId = "custom-host-pending", Slot = MatchPlayerSlot.TeamA });
                lManifest.Players.Add(new MatchPlayerAssignment { PlayerId = pLocalPlayer.PlayerId, Slot = MatchPlayerSlot.TeamB });
            }

            return lManifest;
        }

        private static string BuildCustomRelayMatchId(string pLobbyId) =>
            string.IsNullOrWhiteSpace(pLobbyId) ? "custom-relay-pending" : $"custom-relay-{pLobbyId}";

        #endregion
    }
}
