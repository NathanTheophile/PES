#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Matchmaking
#endregion

using System.Threading;
using System.Threading.Tasks;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.Matchmaking
{
    public sealed class LocalLobbyService : MonoBehaviour, IPartyLobbyService
    {
        #region _____________________________/ VALUES

        [SerializeField] private string _LocalPlayerId = "local-player";
        [SerializeField] private string _JoinCode = "LOCAL";

        private PartyLobbySnapshot _CurrentLobby;

        #endregion

        #region _____________________________| LOBBY

        public Task<PartyLobbySnapshot> CreateLobbyAsync(PartyLobbyRequest pRequest, CancellationToken pCancellationToken)
        {
            pCancellationToken.ThrowIfCancellationRequested();
            _CurrentLobby = CreateSnapshot("local-lobby", pRequest?.TeamPresetId, MatchPlayerSlot.TeamA);
            return Task.FromResult(_CurrentLobby);
        }

        public Task<PartyLobbySnapshot> JoinLobbyAsync(string pJoinCode, string pTeamPresetId, CancellationToken pCancellationToken)
        {
            pCancellationToken.ThrowIfCancellationRequested();
            _CurrentLobby = CreateSnapshot("local-lobby", pTeamPresetId, MatchPlayerSlot.TeamB);
            _CurrentLobby.JoinCode = string.IsNullOrWhiteSpace(pJoinCode) ? _JoinCode : pJoinCode;
            return Task.FromResult(_CurrentLobby);
        }

        public Task<PartyLobbySnapshot> SetReadyAsync(string pLobbyId, bool pIsReady, CancellationToken pCancellationToken)
        {
            pCancellationToken.ThrowIfCancellationRequested();
            _CurrentLobby ??= CreateSnapshot(string.IsNullOrWhiteSpace(pLobbyId) ? "local-lobby" : pLobbyId, string.Empty, MatchPlayerSlot.TeamA);
            return Task.FromResult(_CurrentLobby);
        }

        public Task LeaveLobbyAsync(string pLobbyId, CancellationToken pCancellationToken)
        {
            pCancellationToken.ThrowIfCancellationRequested();
            _CurrentLobby = null;
            return Task.CompletedTask;
        }

        private PartyLobbySnapshot CreateSnapshot(string pLobbyId, string pTeamPresetId, MatchPlayerSlot pSlot) =>
            new PartyLobbySnapshot
            {
                LobbyId = pLobbyId,
                JoinCode = _JoinCode,
                Players =
                {
                    new MatchPlayerAssignment
                    {
                        PlayerId = _LocalPlayerId,
                        Slot = pSlot,
                        TeamPresetId = pTeamPresetId ?? string.Empty
                    }
                }
            };

        #endregion
    }
}
