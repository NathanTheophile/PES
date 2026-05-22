#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Networking
#endregion

using PurrNet;
using TacticalPort.Matchmaking;
using TacticalPort.Shared;

namespace TacticalPort.Networking
{
    internal sealed class CombatPlayerSlotRegistry
    {
        #region _____________________________/ VALUES

        private PlayerID? _TeamAPlayer;
        private PlayerID? _TeamBPlayer;

        #endregion

        #region _____________________________/ ACCESSORS

        public string LocalPlayerId { get; private set; } = string.Empty;
        public MatchPlayerSlot LocalPlayerSlot { get; private set; } = MatchPlayerSlot.None;

        #endregion

        #region _____________________________| CONFIGURE

        public void Reset(MatchManifest pManifest, PlayerIdentity pLocalPlayer)
        {
            LocalPlayerId = pLocalPlayer.PlayerId ?? string.Empty;
            LocalPlayerSlot = ResolveLocalSlotFromManifest(pManifest);
            ClearRemotePlayers();
        }

        public void SetLocalSlot(MatchPlayerSlot pSlot) => LocalPlayerSlot = pSlot;

        #endregion

        #region _____________________________| REMOTE PLAYERS

        public bool TryAssign(PlayerID pPlayer, MatchPlayerSlot pSlot, out string pFailure)
        {
            pFailure = string.Empty;

            if (pSlot == MatchPlayerSlot.None)
            {
                pFailure = "Cannot assign player to slot None.";
                return false;
            }

            if (TryGetAssignedPlayer(pSlot, out PlayerID lAssignedPlayer))
            {
                if (lAssignedPlayer == pPlayer)
                    return true;

                pFailure = $"Slot {pSlot} is already assigned to another player.";
                return false;
            }

            Clear(pPlayer, out _);
            SetAssignedPlayer(pSlot, pPlayer);
            return true;
        }

        public bool Clear(PlayerID pPlayer, out MatchPlayerSlot pClearedSlot)
        {
            if (_TeamAPlayer.HasValue && _TeamAPlayer.Value == pPlayer)
            {
                _TeamAPlayer = null;
                pClearedSlot = MatchPlayerSlot.TeamA;
                return true;
            }

            if (_TeamBPlayer.HasValue && _TeamBPlayer.Value == pPlayer)
            {
                _TeamBPlayer = null;
                pClearedSlot = MatchPlayerSlot.TeamB;
                return true;
            }

            pClearedSlot = MatchPlayerSlot.None;
            return false;
        }

        private void ClearRemotePlayers()
        {
            _TeamAPlayer = null;
            _TeamBPlayer = null;
        }

        private bool TryGetAssignedPlayer(MatchPlayerSlot pSlot, out PlayerID pPlayer)
        {
            PlayerID? lPlayer = pSlot == MatchPlayerSlot.TeamA
                ? _TeamAPlayer
                : pSlot == MatchPlayerSlot.TeamB
                    ? _TeamBPlayer
                    : null;

            if (lPlayer.HasValue)
            {
                pPlayer = lPlayer.Value;
                return true;
            }

            pPlayer = default;
            return false;
        }

        private void SetAssignedPlayer(MatchPlayerSlot pSlot, PlayerID pPlayer)
        {
            if (pSlot == MatchPlayerSlot.TeamA)
                _TeamAPlayer = pPlayer;
            else if (pSlot == MatchPlayerSlot.TeamB)
                _TeamBPlayer = pPlayer;
        }

        #endregion

        #region _____________________________| RESOLVE

        public bool TryResolveLocalControlledSlot(
            bool pHasOnlineMatchContext,
            bool pIsOnlineServerAuthority,
            bool pIsSpawned,
            bool pServerActsAsPlayer,
            bool pIsServerOnly,
            MatchPlayerSlot pOfflineControlledSlot,
            MatchPlayerSlot pServerPlayerSlot,
            out MatchPlayerSlot pSlot)
        {
            if (pHasOnlineMatchContext)
            {
                if (pIsOnlineServerAuthority)
                {
                    pSlot = pServerActsAsPlayer ? ResolveServerPlayerSlot(pServerPlayerSlot) : MatchPlayerSlot.None;
                    return pSlot != MatchPlayerSlot.None;
                }

                pSlot = LocalPlayerSlot;
                return pSlot != MatchPlayerSlot.None;
            }

            if (!pIsSpawned)
            {
                pSlot = pOfflineControlledSlot;
                return pSlot != MatchPlayerSlot.None;
            }

            pSlot = LocalPlayerSlot;
            return pSlot != MatchPlayerSlot.None;
        }

        public bool TryResolveControlledTeam(
            PlayerID pPlayer,
            bool pServerActsAsPlayer,
            bool pIsServerOnly,
            MatchPlayerSlot pServerPlayerSlot,
            out Team pTeam)
        {
            if (TryResolvePlayerSlot(pPlayer, pServerActsAsPlayer, pIsServerOnly, pServerPlayerSlot, out MatchPlayerSlot lSlot))
            {
                pTeam = CombatTeamUtility.ToTeam(lSlot);
                return pTeam != Team.Neutral;
            }

            pTeam = Team.Neutral;
            return false;
        }

        public bool TryResolvePlayerSlot(
            PlayerID pPlayer,
            bool pServerActsAsPlayer,
            bool pIsServerOnly,
            MatchPlayerSlot pServerPlayerSlot,
            out MatchPlayerSlot pSlot)
        {
            if (pPlayer == PlayerID.Server)
            {
                pSlot = pServerActsAsPlayer ? ResolveServerPlayerSlot(pServerPlayerSlot) : MatchPlayerSlot.None;
                return pSlot != MatchPlayerSlot.None;
            }

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

        public string ResolvePlayerIdForSlot(MatchManifest pManifest, MatchPlayerSlot pSlot) =>
            pManifest != null && pManifest.TryGetPlayerId(pSlot, out string lPlayerId)
                ? lPlayerId
                : string.Empty;

        public string ResolvePlayerIdForPlayer(
            MatchManifest pManifest,
            PlayerID pPlayer,
            bool pServerActsAsPlayer,
            bool pIsServerOnly,
            MatchPlayerSlot pServerPlayerSlot)
        {
            return TryResolvePlayerSlot(pPlayer, pServerActsAsPlayer, pIsServerOnly, pServerPlayerSlot, out MatchPlayerSlot lSlot)
                ? ResolvePlayerIdForSlot(pManifest, lSlot)
                : string.Empty;
        }

        private MatchPlayerSlot ResolveLocalSlotFromManifest(MatchManifest pManifest) =>
            pManifest != null && pManifest.TryGetSlot(LocalPlayerId, out MatchPlayerSlot lSlot)
                ? lSlot
                : MatchPlayerSlot.None;

        private MatchPlayerSlot ResolveServerPlayerSlot(MatchPlayerSlot pServerPlayerSlot) =>
            LocalPlayerSlot != MatchPlayerSlot.None ? LocalPlayerSlot : pServerPlayerSlot;

        #endregion
    }
}
