#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using PurrNet;
using TacticalPort.Bootstrap;
using TacticalPort.Combat;
using TacticalPort.Core;
using TacticalPort.Matchmaking;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.Networking
{
    public sealed class PurrNetCombatBridge : NetworkBehaviour, ICombatCommandSink, IPlayerEvents
    {
        #region _____________________________/ VALUES

        [SerializeField] private CombatBootstrap _Bootstrap;
        [SerializeField] private bool _AllowOfflineFallback = true;
        [SerializeField] private Team _OfflineControlledTeam = Team.Player;
        [SerializeField] private Team _ServerControlledTeam = Team.Player;
        [SerializeField] private Team _ClientControlledTeam = Team.Enemy;
        [SerializeField] private bool _RequireRemoteReady = true;

        [Header("Match Slots")]
        [Tooltip("Keep enabled for host playtests. Disable on dedicated server builds where both players are remote clients.")]
        [SerializeField] private bool _ServerActsAsPlayer = true;
        [SerializeField] private MatchPlayerSlot _OfflineControlledSlot = MatchPlayerSlot.TeamA;
        [SerializeField] private MatchPlayerSlot _ServerPlayerSlot = MatchPlayerSlot.TeamA;
        [SerializeField] private MatchManifest _MatchManifest;

        private bool _IsTeamAReady;
        private bool _IsTeamBReady;
        private PlayerID? _TeamAPlayer;
        private PlayerID? _TeamBPlayer;
        private MatchPlayerSlot _LocalPlayerSlot = MatchPlayerSlot.None;

        #endregion

        #region _____________________________/ ACCESSORS

        public bool UsesRemoteAuthority => isSpawned && isClient && !isServer;
        public bool CanRunAuthoritativeSimulation => !isSpawned ? _AllowOfflineFallback : isServer;
        public bool CanRunEnemyAi => !isSpawned && _AllowOfflineFallback;
        public MatchManifest MatchManifest => _MatchManifest;

        public bool CanLocallyControlTeam(Team pTeam)
        {
            return TryResolveLocalControlledSlot(out MatchPlayerSlot lSlot) && ResolveTeam(lSlot) == pTeam;
        }

        public bool TryGetSlotForTeam(Team pTeam, out MatchPlayerSlot pSlot)
        {
            pSlot = ResolveSlot(pTeam);
            return pSlot != MatchPlayerSlot.None;
        }

        #endregion

        #region _____________________________| UNITY

        private void Awake() => CacheMissingReferences();

        private void OnValidate() => CacheMissingReferences();

        void IPlayerEvents.OnPlayerConnected(PlayerID pPlayerId, bool pIsReconnect, bool pAsServer)
        {
            if (!pAsServer || pPlayerId == PlayerID.Server)
                return;

            AssignRemotePlayerSlot(pPlayerId);
        }

        void IPlayerEvents.OnPlayerDisconnected(PlayerID pPlayerId, bool pAsServer)
        {
            if (!pAsServer)
                return;

            if (_TeamAPlayer.HasValue && _TeamAPlayer.Value == pPlayerId)
            {
                _TeamAPlayer = null;
                _IsTeamAReady = false;
            }

            if (_TeamBPlayer.HasValue && _TeamBPlayer.Value == pPlayerId)
            {
                _TeamBPlayer = null;
                _IsTeamBReady = false;
            }
        }

        #endregion

        #region _____________________________| CONFIGURE

        public void InitializeMatch(MatchManifest pManifest)
        {
            _MatchManifest = pManifest;
            _IsTeamAReady = false;
            _IsTeamBReady = false;
            _TeamAPlayer = null;
            _TeamBPlayer = null;
            _LocalPlayerSlot = MatchPlayerSlot.None;
        }

        public void AssignPlayerSlot(PlayerID pPlayer, MatchPlayerSlot pSlot)
        {
            if (!isServer || pPlayer == PlayerID.Server || pSlot == MatchPlayerSlot.None)
                return;

            ClearPlayerSlot(pPlayer);
            if (pSlot == MatchPlayerSlot.TeamA)
                _TeamAPlayer = pPlayer;
            else if (pSlot == MatchPlayerSlot.TeamB)
                _TeamBPlayer = pPlayer;

            AssignLocalSlotTargetRpc(pPlayer, (int)pSlot);
        }

        #endregion

        #region _____________________________| SUBMIT

        public BattleActionResult Submit(BattleCommand pCommand)
        {
            if (_Bootstrap == null)
                return BattleActionResult.Failed(pCommand.ActionType, "PurrNet combat bridge is missing its CombatBootstrap reference.");

            if (!isSpawned)
            {
                return _AllowOfflineFallback
                    ? _Bootstrap.ExecuteLocalCommand(pCommand)
                    : BattleActionResult.Failed(pCommand.ActionType, "PurrNet combat bridge is not spawned.");
            }

            CombatCommandPacket lPacket = CombatCommandPacket.From(pCommand);
            if (isServer)
            {
                BattleActionResult lResult = ExecuteAuthoritativeCommand(lPacket, PlayerID.Server);
                if (ShouldBroadcastAcceptedCommand(pCommand, lResult))
                    ApplyAcceptedCommandObserversRpc(lPacket, CombatStateChecksum.Compute(_Bootstrap.BattleService));
                else if (pCommand.Type == BattleCommandType.ReadyPlacement)
                    _Bootstrap.ApplyRemoteCommandResult(lResult);

                return lResult;
            }

            SubmitCommandServerRpc(lPacket);
            return BattleActionResult.Succeeded(pCommand.ActionType, "Command submitted.");
        }

        #endregion

        #region _____________________________| RPCS

        [ServerRpc(requireOwnership: false)]
        private void SubmitCommandServerRpc(CombatCommandPacket pPacket, RPCInfo pInfo = default)
        {
            BattleActionResult lResult = ExecuteAuthoritativeCommand(pPacket, pInfo.sender);
            if (pPacket.TryToCommand(out BattleCommand lCommand) && ShouldBroadcastAcceptedCommand(lCommand, lResult))
            {
                ApplyAcceptedCommandObserversRpc(pPacket, CombatStateChecksum.Compute(_Bootstrap.BattleService));
                return;
            }

            ApplyCommandResultTargetRpc(pInfo.sender, CombatCommandResultPacket.From(lResult));
        }

        [ObserversRpc]
        private void ApplyAcceptedCommandObserversRpc(CombatCommandPacket pPacket, int pServerChecksum)
        {
            if (isServer)
                return;

            if (pPacket.TryToCommand(out BattleCommand lCommand))
            {
                _Bootstrap?.ExecuteLocalCommand(lCommand);
                ValidateServerChecksum(pServerChecksum);
            }
        }

        [TargetRpc]
        private void ApplyCommandResultTargetRpc(PlayerID pPlayer, CombatCommandResultPacket pResult)
        {
            _Bootstrap?.ApplyRemoteCommandResult(pResult.ToResult());
        }

        [TargetRpc]
        private void AssignLocalSlotTargetRpc(PlayerID pPlayer, int pSlot)
        {
            _LocalPlayerSlot = (MatchPlayerSlot)pSlot;
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
            if (!AreRequiredPlayersReady())
                return BattleActionResult.Succeeded(BattleActionType.Placement, ResolveReadyStatus());

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
            if (TryResolvePlayerSlot(pSender, out _))
                return true;

            pFailure = BattleActionResult.Failed(BattleActionType.Placement, $"Ready from {pSender} failed: no slot assigned.");
            return false;
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
                SetReady(lSlot, false);
        }

        private void SetReady(PlayerID pPlayer, bool pValue)
        {
            if (TryResolvePlayerSlot(pPlayer, out MatchPlayerSlot lSlot))
                SetReady(lSlot, pValue);
        }

        private void SetReady(MatchPlayerSlot pSlot, bool pValue)
        {
            if (pSlot == MatchPlayerSlot.TeamA)
                _IsTeamAReady = pValue;
            else if (pSlot == MatchPlayerSlot.TeamB)
                _IsTeamBReady = pValue;
        }

        private bool AreRequiredPlayersReady()
        {
            if (!_RequireRemoteReady)
                return _IsTeamAReady;

            return _IsTeamAReady && _IsTeamBReady;
        }

        private string ResolveReadyStatus()
        {
            return _RequireRemoteReady
                ? $"Waiting for opponent. TeamA ready: {_IsTeamAReady}. TeamB ready: {_IsTeamBReady}."
                : "Ready.";
        }

        private void ValidateServerChecksum(int pServerChecksum)
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
            if (!isSpawned)
            {
                pSlot = _OfflineControlledSlot != MatchPlayerSlot.None ? _OfflineControlledSlot : ResolveSlot(_OfflineControlledTeam);
                return pSlot != MatchPlayerSlot.None;
            }

            if (isServer)
            {
                pSlot = _ServerActsAsPlayer
                    ? _ServerPlayerSlot != MatchPlayerSlot.None ? _ServerPlayerSlot : ResolveSlot(_ServerControlledTeam)
                    : MatchPlayerSlot.None;
                return pSlot != MatchPlayerSlot.None;
            }

            pSlot = _LocalPlayerSlot != MatchPlayerSlot.None ? _LocalPlayerSlot : ResolveSlot(_ClientControlledTeam);
            return pSlot != MatchPlayerSlot.None;
        }

        private bool TryResolveControlledTeam(PlayerID pPlayer, out Team pTeam)
        {
            if (TryResolvePlayerSlot(pPlayer, out MatchPlayerSlot lSlot))
            {
                pTeam = ResolveTeam(lSlot);
                return pTeam != Team.Neutral;
            }

            pTeam = Team.Neutral;
            return false;
        }

        private bool TryResolvePlayerSlot(PlayerID pPlayer, out MatchPlayerSlot pSlot)
        {
            if (pPlayer == PlayerID.Server)
            {
                pSlot = _ServerActsAsPlayer
                    ? _ServerPlayerSlot != MatchPlayerSlot.None ? _ServerPlayerSlot : ResolveSlot(_ServerControlledTeam)
                    : MatchPlayerSlot.None;
                return pSlot != MatchPlayerSlot.None;
            }

            TryCachePlayersFromNetworkManager();
            if (_TeamAPlayer.HasValue && _TeamAPlayer.Value == pPlayer)
            {
                pSlot = MatchPlayerSlot.TeamA;
                return true;
            }

            if (_TeamBPlayer.HasValue && _TeamBPlayer.Value == pPlayer)
            {
                pSlot = MatchPlayerSlot.TeamB;
                return true;
            }

            pSlot = MatchPlayerSlot.None;
            return false;
        }

        private void AssignRemotePlayerSlot(PlayerID pPlayer)
        {
            if (_TeamAPlayer.HasValue && _TeamAPlayer.Value == pPlayer)
                return;

            if (_TeamBPlayer.HasValue && _TeamBPlayer.Value == pPlayer)
                return;

            MatchPlayerSlot lSlot = ResolveNextFreeRemoteSlot();
            if (lSlot == MatchPlayerSlot.None)
            {
                Debug.LogWarning($"Cannot assign slot to player {pPlayer}: match is already full.", this);
                return;
            }

            if (lSlot == MatchPlayerSlot.TeamA)
                AssignPlayerSlot(pPlayer, MatchPlayerSlot.TeamA);
            else
                AssignPlayerSlot(pPlayer, MatchPlayerSlot.TeamB);
        }

        private void ClearPlayerSlot(PlayerID pPlayer)
        {
            if (_TeamAPlayer.HasValue && _TeamAPlayer.Value == pPlayer)
                _TeamAPlayer = null;

            if (_TeamBPlayer.HasValue && _TeamBPlayer.Value == pPlayer)
                _TeamBPlayer = null;
        }

        private MatchPlayerSlot ResolveNextFreeRemoteSlot()
        {
            if (!_ServerActsAsPlayer && !_TeamAPlayer.HasValue)
                return MatchPlayerSlot.TeamA;

            return !_TeamBPlayer.HasValue ? MatchPlayerSlot.TeamB : MatchPlayerSlot.None;
        }

        private void TryCachePlayersFromNetworkManager()
        {
            if (!isServer || networkManager == null || networkManager.players == null)
                return;

            foreach (PlayerID lPlayer in networkManager.players)
            {
                if (lPlayer != PlayerID.Server)
                    AssignRemotePlayerSlot(lPlayer);
            }
        }

        private static Team ResolveTeam(MatchPlayerSlot pSlot)
        {
            return pSlot == MatchPlayerSlot.TeamA
                ? Team.Player
                : pSlot == MatchPlayerSlot.TeamB
                    ? Team.Enemy
                    : Team.Neutral;
        }

        private static MatchPlayerSlot ResolveSlot(Team pTeam)
        {
            return pTeam == Team.Player
                ? MatchPlayerSlot.TeamA
                : pTeam == Team.Enemy
                    ? MatchPlayerSlot.TeamB
                    : MatchPlayerSlot.None;
        }

        #endregion
    }
}
