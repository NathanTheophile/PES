#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using PurrNet;
using PurrNet.Modules;
using TacticalPort.App;
using TacticalPort.Bootstrap;
using TacticalPort.Combat;
using TacticalPort.Core;
using TacticalPort.Matchmaking;
using TacticalPort.Shared;
using TacticalPort.State;
using UnityEngine;

namespace TacticalPort.Networking
{
    public sealed class PurrNetCombatBridge : NetworkBehaviour, ICombatCommandSink, IMatchCombatNetworkBridge, IPlayerEvents
    {
        #region _____________________________/ VALUES

        private const float NETWORK_MAINTENANCE_INTERVAL_SECONDS = 0.1f;

        [SerializeField] private CombatBootstrap _Bootstrap;
        [SerializeField] private bool _AllowOfflineFallback = true;
        [SerializeField] private bool _RequireRemoteReady = true;
        [Tooltip("Development fallback until the dedicated server receives an authoritative allocation manifest.")]
        [SerializeField] private bool _AcceptClientManifestWhenServerHasNoManifest = true;
        [SerializeField, Min(0.1f)] private float _HandshakeRetrySeconds = 1f;
        [SerializeField] private bool _LogSlotAssignments = true;

        [Header("Match Slots")]
        [Tooltip("Keep enabled only for host playtests. Dedicated/local server mode should leave this disabled.")]
        [SerializeField] private bool _ServerActsAsPlayer;
        [SerializeField] private MatchPlayerSlot _OfflineControlledSlot = MatchPlayerSlot.TeamA;
        [SerializeField] private MatchPlayerSlot _ServerPlayerSlot = MatchPlayerSlot.TeamA;
        [SerializeField] private MatchManifest _MatchManifest;

        private readonly CombatPlayerSlotRegistry _PlayerSlots = new CombatPlayerSlotRegistry();
        private readonly CombatReadyState _ReadyState = new CombatReadyState();
        private PlayersManager _ServerPlayers;
        private PlayersManager _ClientPlayers;
        private bool _IsServerSubscribed;
        private bool _IsClientSubscribed;
        private bool _HasServerSlotConfirmation;
        private float _NextHandshakeTime;
        private float _NextNetworkMaintenanceTime;
        private NetworkManager _ResolvedNetworkManager;

        #endregion

        #region _____________________________/ ACCESSORS

        public bool UsesRemoteAuthority => IsOnlineClientAuthority();
        public bool CanRunAuthoritativeSimulation => IsOnlineServerAuthority() || (!IsNetworkSessionActive() && _AllowOfflineFallback);
        public bool CanRunEnemyAi => !IsNetworkSessionActive() && _AllowOfflineFallback;
        public MatchManifest MatchManifest => _MatchManifest;

        public bool CanLocallyControlTeam(Team pTeam) => GetLocalRelation(pTeam) == CombatTeamRelation.Own;

        public bool TryGetSlotForTeam(Team pTeam, out MatchPlayerSlot pSlot)
        {
            pSlot = CombatTeamUtility.ToSlot(pTeam);
            return pSlot != MatchPlayerSlot.None;
        }

        public bool TryGetLocalPlayerSlot(out MatchPlayerSlot pSlot) => TryResolveLocalControlledSlot(out pSlot);

        public CombatTeamRelation GetLocalRelation(Team pTeam) =>
            TryResolveLocalControlledSlot(out MatchPlayerSlot lSlot)
                ? CombatTeamUtility.ResolveRelation(pTeam, lSlot)
                : CombatTeamRelation.Neutral;

        #endregion

        #region _____________________________| UNITY

        private void Awake() => CacheMissingReferences();

        private void OnEnable()
        {
            RunNetworkMaintenanceTick();
        }

        private void OnDisable()
        {
            UnsubscribeNetworkModules();
        }

        private void Update()
        {
            if (Time.unscaledTime < _NextNetworkMaintenanceTime)
                return;

            _NextNetworkMaintenanceTime = Time.unscaledTime + NETWORK_MAINTENANCE_INTERVAL_SECONDS;
            RunNetworkMaintenanceTick();
        }

        private void OnValidate() => CacheMissingReferences();

        void IPlayerEvents.OnPlayerConnected(PlayerID pPlayerId, bool pIsReconnect, bool pAsServer)
        {
            if (!pAsServer || pPlayerId == PlayerID.Server)
                return;

            if (_LogSlotAssignments)
                Debug.Log($"[PurrNet Combat Bridge] Player connected. Waiting for match handshake. PurrNetPlayer={pPlayerId}", this);
        }

        void IPlayerEvents.OnPlayerDisconnected(PlayerID pPlayerId, bool pAsServer)
        {
            if (!pAsServer)
                return;

            if (_PlayerSlots.Clear(pPlayerId, out MatchPlayerSlot lClearedSlot))
                _ReadyState.SetReady(lClearedSlot, false);
        }

        #endregion

        #region _____________________________| CONFIGURE

        public void ConfigureServerPlayerMode(bool pServerActsAsPlayer, MatchPlayerSlot pServerPlayerSlot)
        {
            _ServerActsAsPlayer = pServerActsAsPlayer;
            _ServerPlayerSlot = pServerPlayerSlot;
        }

        public void InitializeMatch(MatchManifest pManifest) => InitializeMatch(pManifest, default);

        public void InitializeMatch(MatchManifest pManifest, PlayerIdentity pLocalPlayer)
        {
            _MatchManifest = pManifest;
            _PlayerSlots.Reset(_MatchManifest, pLocalPlayer);
            _ReadyState.Reset();
            _HasServerSlotConfirmation = !HasOnlineMatchContext() || IsOnlineServerAuthority();
            _NextHandshakeTime = 0f;

            if (_LogSlotAssignments && !string.IsNullOrWhiteSpace(_PlayerSlots.LocalPlayerId))
                Debug.Log($"[PurrNet Combat Bridge] Match initialized. PlayerId={_PlayerSlots.LocalPlayerId}, LocalSlot={_PlayerSlots.LocalPlayerSlot}, MatchId={_MatchManifest?.MatchId}", this);

            SubscribeNetworkModulesIfReady();
            SendPendingHandshakeIfReady(true);
        }

        public void AssignPlayerSlot(PlayerID pPlayer, MatchPlayerSlot pSlot)
        {
            if (!IsOnlineServerAuthority() || pPlayer == PlayerID.Server || pSlot == MatchPlayerSlot.None)
                return;

            string lPlayerId = _PlayerSlots.ResolvePlayerIdForSlot(_MatchManifest, pSlot);
            if (!_PlayerSlots.TryAssign(pPlayer, pSlot, out string lFailure))
            {
                SendSlotAssignmentFailure(pPlayer, lPlayerId, _MatchManifest != null ? _MatchManifest.MatchId : string.Empty, lFailure);
                return;
            }

            if (_LogSlotAssignments)
                Debug.Log($"[PurrNet Combat Bridge] Assigned PurrNetPlayer={pPlayer} to {pSlot}.", this);

            SendSlotAssignment(pPlayer, pSlot, string.Empty);
        }

        #endregion

        #region _____________________________| SUBMIT

        public BattleActionResult Submit(BattleCommand pCommand)
        {
            if (_Bootstrap == null)
                return BattleActionResult.Failed(pCommand.ActionType, "PurrNet combat bridge is missing its CombatBootstrap reference.");

            if (TrySubmitWithPurrNetBroadcast(pCommand, out BattleActionResult lBroadcastResult))
                return lBroadcastResult;

            if (HasOnlineMatchContext())
                return BattleActionResult.Failed(pCommand.ActionType, "Online combat transport is not ready yet.");

            if (IsNetworkSessionActive() && !_AllowOfflineFallback)
                return BattleActionResult.Failed(pCommand.ActionType, "PurrNet combat bridge has no online match context.");

            return _AllowOfflineFallback
                ? _Bootstrap.ExecuteLocalCommand(pCommand)
                : BattleActionResult.Failed(pCommand.ActionType, "Offline combat fallback is disabled.");
        }

        #endregion

        #region _____________________________| BROADCASTS

        private bool TrySubmitWithPurrNetBroadcast(BattleCommand pCommand, out BattleActionResult pResult)
        {
            pResult = null;
            if (!HasOnlineMatchContext())
                return false;

            if (IsOnlineServerAuthority())
            {
                CombatCommandPacket lServerPacket = CombatCommandPacket.From(pCommand);
                pResult = ExecuteAuthoritativeCommand(lServerPacket, PlayerID.Server);
                if (ShouldBroadcastAcceptedCommand(pCommand, pResult))
                    SendAcceptedCommandToAll(lServerPacket);
                else if (pCommand.Type == BattleCommandType.ReadyPlacement)
                    _Bootstrap.ApplyRemoteCommandResult(pResult);

                return true;
            }

            if (!IsOnlineClientAuthority())
            {
                pResult = BattleActionResult.Failed(pCommand.ActionType, $"Online combat client is not connected. {DescribeNetworkState()}");
                return true;
            }

            SubscribeNetworkModulesIfReady();
            SendPendingHandshakeIfReady(true);

            if (!_HasServerSlotConfirmation)
            {
                pResult = BattleActionResult.Failed(pCommand.ActionType, "Waiting for online slot confirmation.");
                return true;
            }

            if (_ClientPlayers == null)
            {
                pResult = BattleActionResult.Failed(pCommand.ActionType, "PurrNet client player module is not ready.");
                return true;
            }

            _ClientPlayers.SendToServer(new PurrNetCombatCommandMessage
            {
                PlayerId = _PlayerSlots.LocalPlayerId,
                MatchId = _MatchManifest.MatchId,
                Command = CombatCommandPacket.From(pCommand)
            });

            pResult = BattleActionResult.Succeeded(pCommand.ActionType, "Command submitted.");
            return true;
        }

        private void SubscribeNetworkModulesIfReady()
        {
            NetworkManager lManager = ResolveNetworkManager();
            if (lManager == null)
                return;

            if (!_IsServerSubscribed && lManager.isServer && lManager.TryGetModule(true, out PlayersManager lServerPlayers))
            {
                _ServerPlayers = lServerPlayers;
                _ServerPlayers.Subscribe<PurrNetCombatHandshakeMessage>(HandleHandshakeMessage);
                _ServerPlayers.Subscribe<PurrNetCombatCommandMessage>(HandleCommandMessage);
                _IsServerSubscribed = true;
            }

            if (!_IsClientSubscribed && lManager.isClient && lManager.TryGetModule(false, out PlayersManager lClientPlayers))
            {
                _ClientPlayers = lClientPlayers;
                _ClientPlayers.Subscribe<PurrNetCombatSlotAssignmentMessage>(HandleSlotAssignmentMessage);
                _ClientPlayers.Subscribe<PurrNetCombatCommandResultMessage>(HandleCommandResultMessage);
                _ClientPlayers.Subscribe<PurrNetCombatAcceptedCommandMessage>(HandleAcceptedCommandMessage);
                _IsClientSubscribed = true;
            }
        }

        private void RunNetworkMaintenanceTick()
        {
            SubscribeNetworkModulesIfReady();
            SendPendingHandshakeIfReady();
        }

        private void UnsubscribeNetworkModules()
        {
            if (_IsServerSubscribed && _ServerPlayers != null)
            {
                _ServerPlayers.Unsubscribe<PurrNetCombatHandshakeMessage>(HandleHandshakeMessage);
                _ServerPlayers.Unsubscribe<PurrNetCombatCommandMessage>(HandleCommandMessage);
            }

            if (_IsClientSubscribed && _ClientPlayers != null)
            {
                _ClientPlayers.Unsubscribe<PurrNetCombatSlotAssignmentMessage>(HandleSlotAssignmentMessage);
                _ClientPlayers.Unsubscribe<PurrNetCombatCommandResultMessage>(HandleCommandResultMessage);
                _ClientPlayers.Unsubscribe<PurrNetCombatAcceptedCommandMessage>(HandleAcceptedCommandMessage);
            }

            _ServerPlayers = null;
            _ClientPlayers = null;
            _IsServerSubscribed = false;
            _IsClientSubscribed = false;
        }

        private void SendPendingHandshakeIfReady(bool pForce = false)
        {
            if (!HasOnlineMatchContext() || !IsOnlineClientAuthority() || string.IsNullOrWhiteSpace(_PlayerSlots.LocalPlayerId) || _HasServerSlotConfirmation)
                return;

            if (_ClientPlayers == null)
                return;

            if (!pForce && Time.unscaledTime < _NextHandshakeTime)
                return;

            _NextHandshakeTime = Time.unscaledTime + _HandshakeRetrySeconds;
            if (CombatTeamCompositionState.HasLocalSelection)
            {
                MatchCombatCompositionImporter.ExportLocalSelection(
                    new PlayerIdentity(_PlayerSlots.LocalPlayerId, string.Empty),
                    _MatchManifest);
            }

            _ClientPlayers.SendToServer(new PurrNetCombatHandshakeMessage
            {
                PlayerId = _PlayerSlots.LocalPlayerId,
                MatchId = _MatchManifest.MatchId,
                ManifestJson = JsonUtility.ToJson(_MatchManifest)
            });
        }

        private void HandleHandshakeMessage(PlayerID pPlayer, PurrNetCombatHandshakeMessage pMessage, bool pAsServer)
        {
            if (!pAsServer || pPlayer == PlayerID.Server)
                return;

            if (!TryMatchesMessage(pMessage.MatchId))
            {
                SendSlotAssignmentFailure(pPlayer, pMessage.PlayerId, pMessage.MatchId, "Match handshake failed: server is running another match.");
                return;
            }

            if (!TryImportManifest(pMessage.MatchId, pMessage.ManifestJson, out string lImportFailure))
            {
                SendSlotAssignmentFailure(pPlayer, pMessage.PlayerId, pMessage.MatchId, lImportFailure);
                return;
            }

            if (_MatchManifest == null || !_MatchManifest.TryGetSlot(pMessage.PlayerId, out MatchPlayerSlot lSlot))
            {
                SendSlotAssignmentFailure(pPlayer, pMessage.PlayerId, pMessage.MatchId, $"No match slot found for player '{pMessage.PlayerId}'.");
                return;
            }

            AssignPlayerSlot(pPlayer, lSlot);
        }

        private void HandleCommandMessage(PlayerID pPlayer, PurrNetCombatCommandMessage pMessage, bool pAsServer)
        {
            if (!pAsServer || pPlayer == PlayerID.Server)
                return;

            if (!TryMatchesMessage(pMessage.MatchId))
            {
                SendCommandResult(
                    pPlayer,
                    pMessage.PlayerId,
                    BattleActionResult.Failed(ResolveActionType(pMessage.Command), "Command rejected: match id does not match the server match."),
                    pMessage.MatchId);
                return;
            }

            if (!TryEnsurePlayerSlot(pPlayer, pMessage.PlayerId, pMessage.MatchId, out BattleActionResult lSlotFailure))
            {
                SendCommandResult(pPlayer, pMessage.PlayerId, lSlotFailure);
                return;
            }

            BattleActionResult lResult = ExecuteAuthoritativeCommand(pMessage.Command, pPlayer);
            if (pMessage.Command.TryToCommand(out BattleCommand lCommand) && ShouldBroadcastAcceptedCommand(lCommand, lResult))
            {
                SendAcceptedCommandToAll(pMessage.Command);
                return;
            }

            SendCommandResult(pPlayer, pMessage.PlayerId, lResult);
        }

        private void HandleSlotAssignmentMessage(PlayerID pPlayer, PurrNetCombatSlotAssignmentMessage pMessage, bool pAsServer)
        {
            if (pAsServer || !IsLocalPlayerMessage(pMessage.PlayerId, pMessage.MatchId))
                return;

            if (!string.IsNullOrWhiteSpace(pMessage.FailureReason))
            {
                _HasServerSlotConfirmation = false;
                Debug.LogWarning($"[PurrNet Combat Bridge] {pMessage.FailureReason}", this);
                _Bootstrap?.ApplyRemoteCommandResult(BattleActionResult.Failed(BattleActionType.Placement, pMessage.FailureReason));
                return;
            }

            _PlayerSlots.SetLocalSlot((MatchPlayerSlot)pMessage.Slot);
            _HasServerSlotConfirmation = _PlayerSlots.LocalPlayerSlot != MatchPlayerSlot.None;

            if (_LogSlotAssignments)
                Debug.Log($"[PurrNet Combat Bridge] Local slot confirmed by server: {_PlayerSlots.LocalPlayerSlot}.", this);

            _Bootstrap?.ApplyRemoteCommandResult(BattleActionResult.Succeeded(BattleActionType.Placement, $"Online slot confirmed: {_PlayerSlots.LocalPlayerSlot}."));
        }

        private void HandleCommandResultMessage(PlayerID pPlayer, PurrNetCombatCommandResultMessage pMessage, bool pAsServer)
        {
            if (pAsServer || !IsLocalPlayerMessage(pMessage.PlayerId, pMessage.MatchId))
                return;

            _Bootstrap?.ApplyRemoteCommandResult(pMessage.Result.ToResult());
        }

        private void HandleAcceptedCommandMessage(PlayerID pPlayer, PurrNetCombatAcceptedCommandMessage pMessage, bool pAsServer)
        {
            if (pAsServer || !TryMatchesMessage(pMessage.MatchId))
                return;

            if (pMessage.Command.TryToCommand(out BattleCommand lCommand))
            {
                _Bootstrap?.ExecuteLocalCommand(lCommand);
                HandleServerChecksumValidation(pMessage.ServerChecksum);
            }
        }

        private void SendSlotAssignment(PlayerID pPlayer, MatchPlayerSlot pSlot, string pFailureReason)
        {
            if (_ServerPlayers == null)
                return;

            string lPlayerId = _PlayerSlots.ResolvePlayerIdForSlot(_MatchManifest, pSlot);
            _ServerPlayers.Send(pPlayer, new PurrNetCombatSlotAssignmentMessage
            {
                PlayerId = lPlayerId,
                MatchId = _MatchManifest != null ? _MatchManifest.MatchId : string.Empty,
                Slot = (int)pSlot,
                FailureReason = pFailureReason ?? string.Empty
            });
        }

        private void SendSlotAssignmentFailure(PlayerID pPlayer, string pPlayerId, string pMatchId, string pFailureReason)
        {
            if (_ServerPlayers == null)
                return;

            _ServerPlayers.Send(pPlayer, new PurrNetCombatSlotAssignmentMessage
            {
                PlayerId = pPlayerId ?? string.Empty,
                MatchId = pMatchId ?? string.Empty,
                Slot = (int)MatchPlayerSlot.None,
                FailureReason = string.IsNullOrWhiteSpace(pFailureReason) ? "Player slot assignment failed." : pFailureReason
            });
        }

        private void SendCommandResult(PlayerID pPlayer, string pPlayerId, BattleActionResult pResult, string pMatchId = null)
        {
            if (_ServerPlayers == null)
                return;

            _ServerPlayers.Send(pPlayer, new PurrNetCombatCommandResultMessage
            {
                PlayerId = pPlayerId ?? ResolvePlayerIdForPlayer(pPlayer),
                MatchId = pMatchId ?? (_MatchManifest != null ? _MatchManifest.MatchId : string.Empty),
                Result = CombatCommandResultPacket.From(pResult)
            });
        }

        private void SendAcceptedCommandToAll(CombatCommandPacket pPacket)
        {
            if (_ServerPlayers == null || _Bootstrap?.BattleService == null)
                return;

            _ServerPlayers.SendToAll(new PurrNetCombatAcceptedCommandMessage
            {
                MatchId = _MatchManifest != null ? _MatchManifest.MatchId : string.Empty,
                Command = pPacket,
                ServerChecksum = CombatStateChecksum.Compute(_Bootstrap.BattleService)
            });
        }

        #endregion

        #region _____________________________| HELPERS

        private BattleActionResult ExecuteAuthoritativeCommand(CombatCommandPacket pPacket, PlayerID pSender)
        {
            if (_Bootstrap == null)
                return BattleActionResult.Failed(BattleActionType.Move, $"Command from {pSender} failed: missing combat bootstrap.");

            if (!pPacket.TryToCommand(out BattleCommand lCommand))
                return BattleActionResult.Failed(BattleActionType.Move, $"Command from {pSender} failed: unsupported battle command.");

            if (lCommand.Type == BattleCommandType.ReadyPlacement)
                return ExecuteReadyCommand(pSender);

            if (!TryValidateSenderCommand(pSender, lCommand, out BattleActionResult lFailure))
                return lFailure;

            ResetReadyIfPlacementChanged(pSender, lCommand);
            return _Bootstrap.ExecuteLocalCommand(lCommand);
        }

        private void CacheMissingReferences()
        {
            if (_Bootstrap == null)
                _Bootstrap = GetComponent<CombatBootstrap>() ?? GetComponentInParent<CombatBootstrap>();
        }

        private BattleActionResult ExecuteReadyCommand(PlayerID pSender)
        {
            if (!ValidateReadyCommand(pSender, out BattleActionResult lFailure))
                return lFailure;

            SetReady(pSender, true);
            if (!_ReadyState.AreRequiredPlayersReady(_RequireRemoteReady, ResolveReadyFallbackSlot(pSender)))
                return BattleActionResult.Succeeded(BattleActionType.Placement, _ReadyState.ResolveStatus(_RequireRemoteReady));

            return _Bootstrap.ExecuteLocalCommand(BattleCommand.ReadyPlacement());
        }

        private bool TryValidateSenderCommand(PlayerID pSender, BattleCommand pCommand, out BattleActionResult pFailure)
        {
            pFailure = null;

            if (pCommand.Type == BattleCommandType.ReadyPlacement)
                return ValidateReadyCommand(pSender, out pFailure);

            if (_Bootstrap.BattleService == null || !_Bootstrap.BattleService.TryGetUnit(pCommand.UnitId, out UnitRuntime lUnit) || lUnit == null)
            {
                pFailure = BattleActionResult.Failed(pCommand.ActionType, $"Command from {pSender} failed: unknown unit.");
                return false;
            }

            if (!TryResolveControlledTeam(pSender, out Team lControlledTeam) || lUnit.Team != lControlledTeam)
            {
                pFailure = BattleActionResult.Failed(pCommand.ActionType, $"Command from {pSender} failed: unit is not controlled by this player.");
                return false;
            }

            return true;
        }

        private bool ValidateReadyCommand(PlayerID pSender, out BattleActionResult pFailure)
        {
            pFailure = null;
            if (!TryResolvePlayerSlot(pSender, out MatchPlayerSlot lSlot))
            {
                pFailure = BattleActionResult.Failed(BattleActionType.Placement, $"Ready from {pSender} failed: no slot assigned.");
                return false;
            }

            if (_Bootstrap == null)
            {
                pFailure = BattleActionResult.Failed(BattleActionType.Placement, "Ready failed: combat bootstrap is missing.");
                return false;
            }

            return _Bootstrap.TryValidatePlacementReadyForSlot(lSlot, out pFailure);
        }

        private bool ShouldBroadcastAcceptedCommand(BattleCommand pCommand, BattleActionResult pResult)
        {
            if (pResult == null || !pResult.IsSuccess)
                return false;

            return pCommand.Type != BattleCommandType.ReadyPlacement || !IsPlacementActive();
        }

        private bool IsPlacementActive() =>
            _Bootstrap != null && _Bootstrap.IsPlacementPhaseActive;

        private void ResetReadyIfPlacementChanged(PlayerID pSender, BattleCommand pCommand)
        {
            if (pCommand.Type == BattleCommandType.PlaceUnit && TryResolvePlayerSlot(pSender, out MatchPlayerSlot lSlot))
                _ReadyState.SetReady(lSlot, false);
        }

        private void SetReady(PlayerID pPlayer, bool pValue)
        {
            if (TryResolvePlayerSlot(pPlayer, out MatchPlayerSlot lSlot))
                _ReadyState.SetReady(lSlot, pValue);
        }

        private void HandleServerChecksumValidation(int pServerChecksum)
        {
            if (_Bootstrap?.BattleService == null)
                return;

            int lLocalChecksum = CombatStateChecksum.Compute(_Bootstrap.BattleService);
            if (lLocalChecksum == pServerChecksum)
                return;

            Debug.LogWarning($"Combat state checksum mismatch. Local: {lLocalChecksum}. Server: {pServerChecksum}.", this);
            _Bootstrap.ApplyRemoteCommandResult(BattleActionResult.Failed(BattleActionType.Move, "Network state mismatch detected."));
        }

        private bool TryResolveLocalControlledSlot(out MatchPlayerSlot pSlot)
        {
            if (HasOnlineMatchContext() && IsOnlineClientAuthority() && !_HasServerSlotConfirmation)
            {
                pSlot = MatchPlayerSlot.None;
                return false;
            }

            return _PlayerSlots.TryResolveLocalControlledSlot(
                HasOnlineMatchContext(),
                IsOnlineServerAuthority(),
                isSpawned,
                _ServerActsAsPlayer,
                IsServerOnlyNetwork(),
                _OfflineControlledSlot,
                _ServerPlayerSlot,
                out pSlot);
        }

        private bool TryResolveControlledTeam(PlayerID pPlayer, out Team pTeam)
        {
            return _PlayerSlots.TryResolveControlledTeam(
                pPlayer,
                _ServerActsAsPlayer,
                IsServerOnlyNetwork(),
                _ServerPlayerSlot,
                out pTeam);
        }

        private bool TryResolvePlayerSlot(PlayerID pPlayer, out MatchPlayerSlot pSlot)
        {
            return _PlayerSlots.TryResolvePlayerSlot(
                pPlayer,
                _ServerActsAsPlayer,
                IsServerOnlyNetwork(),
                _ServerPlayerSlot,
                out pSlot);
        }

        private bool TryImportManifest(string pMatchId, string pManifestJson, out string pFailure)
        {
            bool lImported = CombatMatchManifestImporter.TryImport(
                _MatchManifest,
                pMatchId,
                pManifestJson,
                _AcceptClientManifestWhenServerHasNoManifest,
                out MatchManifest lResolvedManifest,
                out pFailure);

            if (lImported)
                _MatchManifest = lResolvedManifest;

            return lImported;
        }

        private bool HasOnlineMatchContext() =>
            _MatchManifest != null && !string.IsNullOrWhiteSpace(_MatchManifest.MatchId);

        private NetworkManager ResolveNetworkManager()
        {
            if (networkManager != null && !networkManager.isOffline)
                return networkManager;

            if (NetworkManager.main != null && !NetworkManager.main.isOffline)
                return NetworkManager.main;

            if (_ResolvedNetworkManager != null && !_ResolvedNetworkManager.isOffline)
                return _ResolvedNetworkManager;

            NetworkManager[] lManagers = FindObjectsByType<NetworkManager>(FindObjectsInactive.Exclude);
            for (int lIndex = 0; lIndex < lManagers.Length; lIndex++)
            {
                NetworkManager lManager = lManagers[lIndex];
                if (lManager != null && !lManager.isOffline)
                {
                    _ResolvedNetworkManager = lManager;
                    return _ResolvedNetworkManager;
                }
            }

            if (networkManager != null)
                return networkManager;

            if (NetworkManager.main != null)
                return NetworkManager.main;

            if (_ResolvedNetworkManager != null)
                return _ResolvedNetworkManager;

            return lManagers.Length > 0 ? lManagers[0] : null;
        }

        private bool TryEnsurePlayerSlot(PlayerID pPlayer, string pPlayerId, string pMatchId, out BattleActionResult pFailure)
        {
            pFailure = null;

            if (!TryMatchesMessage(pMatchId))
            {
                pFailure = BattleActionResult.Failed(BattleActionType.Placement, "Command rejected: match id does not match the server match.");
                return false;
            }

            if (TryResolvePlayerSlot(pPlayer, out MatchPlayerSlot lAssignedSlot))
            {
                if (!string.IsNullOrWhiteSpace(pPlayerId)
                    && _MatchManifest != null
                    && _MatchManifest.TryGetSlot(pPlayerId, out MatchPlayerSlot lClaimedSlot)
                    && lClaimedSlot != lAssignedSlot)
                {
                    pFailure = BattleActionResult.Failed(BattleActionType.Placement, $"Command from {pPlayer} rejected: player id does not match the assigned slot.");
                    return false;
                }

                return true;
            }

            if (TryAssignSlotFromPlayerId(pPlayer, pPlayerId, out string lAssignFailure))
                return true;

            if (string.IsNullOrWhiteSpace(pPlayerId))
            {
                pFailure = BattleActionResult.Failed(BattleActionType.Placement, $"Command from {pPlayer} rejected: slot handshake is not confirmed.");
                return false;
            }

            pFailure = BattleActionResult.Failed(BattleActionType.Placement, $"Command from {pPlayer} rejected: slot handshake is not confirmed for player '{pPlayerId}'. {lAssignFailure}");
            return false;
        }

        private bool TryAssignSlotFromPlayerId(PlayerID pPlayer, string pPlayerId, out string pFailure)
        {
            pFailure = string.Empty;
            if (string.IsNullOrWhiteSpace(pPlayerId))
                return false;

            if (_MatchManifest == null || !_MatchManifest.TryGetSlot(pPlayerId, out MatchPlayerSlot lSlot))
            {
                pFailure = "Player id is not present in the match manifest.";
                return false;
            }

            if (!_PlayerSlots.TryAssign(pPlayer, lSlot, out pFailure))
                return false;

            SendSlotAssignment(pPlayer, lSlot, string.Empty);

            if (_LogSlotAssignments)
                Debug.Log($"[PurrNet Combat Bridge] Late assigned PurrNetPlayer={pPlayer} to {lSlot} from PlayerId={pPlayerId}.", this);

            return true;
        }

        private MatchPlayerSlot ResolveReadyFallbackSlot(PlayerID pSender) =>
            TryResolvePlayerSlot(pSender, out MatchPlayerSlot lSlot) ? lSlot : MatchPlayerSlot.TeamA;

        private bool IsOnlineClientAuthority()
        {
            NetworkManager lManager = ResolveNetworkManager();
            if (lManager != null)
                return lManager.isClient && !lManager.isServer;

            return isSpawned && isClient && !isServer;
        }

        private string DescribeNetworkState()
        {
            NetworkManager lManager = ResolveNetworkManager();
            return lManager != null
                ? $"NetworkManager={lManager.name}, Offline={lManager.isOffline}, Client={lManager.isClient}, Server={lManager.isServer}."
                : "No NetworkManager found.";
        }

        private bool IsServerOnlyNetwork()
        {
            NetworkManager lManager = ResolveNetworkManager();
            if (lManager != null)
                return lManager.isServerOnly;

            return isServerOnly;
        }

        private bool TryMatchesMessage(string pMatchId)
        {
            if (string.IsNullOrWhiteSpace(pMatchId))
                return false;

            return _MatchManifest == null
                   || string.IsNullOrWhiteSpace(_MatchManifest.MatchId)
                   || _MatchManifest.MatchId == pMatchId;
        }

        private bool IsLocalPlayerMessage(string pPlayerId, string pMatchId) =>
            TryMatchesMessage(pMatchId)
            && !string.IsNullOrWhiteSpace(_PlayerSlots.LocalPlayerId)
            && _PlayerSlots.LocalPlayerId == pPlayerId;

        private string ResolvePlayerIdForPlayer(PlayerID pPlayer)
        {
            return _PlayerSlots.ResolvePlayerIdForPlayer(
                _MatchManifest,
                pPlayer,
                _ServerActsAsPlayer,
                IsServerOnlyNetwork(),
                _ServerPlayerSlot);
        }

        private static BattleActionType ResolveActionType(CombatCommandPacket pPacket) =>
            pPacket.TryToCommand(out BattleCommand lCommand) ? lCommand.ActionType : BattleActionType.Move;

        private bool IsOnlineServerAuthority()
        {
            NetworkManager lManager = ResolveNetworkManager();
            if (lManager != null)
                return lManager.isServer;

            return isSpawned && isServer;
        }

        private bool IsNetworkSessionActive() =>
            HasOnlineMatchContext()
            || isSpawned
            || ResolveNetworkManager() is NetworkManager lManager && !lManager.isOffline;

        #endregion
    }
}
