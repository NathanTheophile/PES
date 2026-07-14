#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Matchmaking
#endregion

using System;
using System.Threading;
using System.Threading.Tasks;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.Matchmaking
{
    public sealed class LocalQuickMatchService : MonoBehaviour, IQuickMatchService
    {
        #region _____________________________/ VALUES

        [SerializeField] private string _LocalPlayerId = "local-player";
        [SerializeField] private string _OpponentPlayerId = "local-opponent";
        [SerializeField] private string _OpponentTeamPresetId = string.Empty;
        [SerializeField] private string _MapId = "alpha-1";

        private MatchTicketSnapshot _CurrentTicket;

        #endregion

        #region _____________________________| TICKET

        public Task<MatchTicketSnapshot> CreateTicketAsync(QuickMatchRequest pRequest, CancellationToken pCancellationToken)
        {
            pCancellationToken.ThrowIfCancellationRequested();
            string lTicketId = Guid.NewGuid().ToString("N");
            _CurrentTicket = new MatchTicketSnapshot
            {
                TicketId = lTicketId,
                Status = MatchTicketStatus.Searching,
                Manifest = BuildManifest(lTicketId, ResolveLocalPlayerId(pRequest), pRequest?.TeamPresetId)
            };

            return Task.FromResult(_CurrentTicket);
        }

        public Task<MatchTicketSnapshot> PollTicketAsync(string pTicketId, CancellationToken pCancellationToken)
        {
            pCancellationToken.ThrowIfCancellationRequested();
            if (_CurrentTicket == null || _CurrentTicket.TicketId != pTicketId)
            {
                return Task.FromResult(new MatchTicketSnapshot
                {
                    TicketId = pTicketId,
                    Status = MatchTicketStatus.Failed,
                    FailureReason = "Local ticket not found."
                });
            }

            _CurrentTicket.Status = MatchTicketStatus.Found;
            return Task.FromResult(_CurrentTicket);
        }

        public Task CancelTicketAsync(string pTicketId, CancellationToken pCancellationToken)
        {
            pCancellationToken.ThrowIfCancellationRequested();
            if (_CurrentTicket != null && _CurrentTicket.TicketId == pTicketId)
                _CurrentTicket.Status = MatchTicketStatus.Cancelled;

            return Task.CompletedTask;
        }

        private string ResolveLocalPlayerId(QuickMatchRequest pRequest) =>
            pRequest != null && pRequest.Player.IsValid ? pRequest.Player.PlayerId : _LocalPlayerId;

        private MatchManifest BuildManifest(string pMatchId, string pLocalPlayerId, string pTeamPresetId) =>
            new MatchManifest
            {
                MatchId = pMatchId,
                MapId = _MapId,
                Players =
                {
                    new MatchPlayerAssignment
                    {
                        PlayerId = pLocalPlayerId,
                        Slot = MatchPlayerSlot.TeamA,
                        TeamPresetId = pTeamPresetId ?? string.Empty
                    },
                    new MatchPlayerAssignment
                    {
                        PlayerId = _OpponentPlayerId,
                        Slot = MatchPlayerSlot.TeamB,
                        TeamPresetId = _OpponentTeamPresetId
                    }
                }
            };

        #endregion
    }
}
