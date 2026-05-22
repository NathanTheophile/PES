#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Matchmaking
#endregion

using System.Threading;
using System.Threading.Tasks;

namespace TacticalPort.Matchmaking
{
    public interface IPlayerIdentityService
    {
        Task<PlayerIdentity> SignInAsync(CancellationToken pCancellationToken);
    }

    public interface IPartyLobbyService
    {
        Task<PartyLobbySnapshot> CreateLobbyAsync(PartyLobbyRequest pRequest, CancellationToken pCancellationToken);
        Task<PartyLobbySnapshot> JoinLobbyAsync(string pJoinCode, string pTeamPresetId, CancellationToken pCancellationToken);
        Task<PartyLobbySnapshot> SetReadyAsync(string pLobbyId, bool pIsReady, CancellationToken pCancellationToken);
        Task LeaveLobbyAsync(string pLobbyId, CancellationToken pCancellationToken);
    }

    public interface IQuickMatchService
    {
        Task<MatchTicketSnapshot> CreateTicketAsync(QuickMatchRequest pRequest, CancellationToken pCancellationToken);
        Task<MatchTicketSnapshot> PollTicketAsync(string pTicketId, CancellationToken pCancellationToken);
        Task CancelTicketAsync(string pTicketId, CancellationToken pCancellationToken);
    }

    public interface IGameServerAllocationReleaser
    {
        Task ReleaseServerAsync(MatchServerEndpoint pEndpoint, CancellationToken pCancellationToken);
    }

    public interface IGameServerAllocator : IGameServerAllocationReleaser
    {
        Task<MatchServerEndpoint> AllocateServerAsync(MatchAllocationRequest pRequest, CancellationToken pCancellationToken);
    }
}
