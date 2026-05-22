#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  App Contracts
#endregion

using TacticalPort.Matchmaking;
using TacticalPort.Shared;

namespace TacticalPort.App
{
    public interface IMatchCombatNetworkBridge
    {
        void ConfigureServerPlayerMode(bool pServerActsAsPlayer, MatchPlayerSlot pServerPlayerSlot);
        void InitializeMatch(MatchManifest pManifest, PlayerIdentity pLocalPlayer);
    }
}
