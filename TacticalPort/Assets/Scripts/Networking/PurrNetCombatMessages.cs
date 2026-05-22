#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Networking
#endregion

using PurrNet.Packing;

namespace TacticalPort.Networking
{
    public struct PurrNetCombatHandshakeMessage : IPackedAuto
    {
        public string PlayerId;
        public string MatchId;
        public string ManifestJson;
    }

    public struct PurrNetMatchManifestMessage : IPackedAuto
    {
        public string MatchId;
        public string ManifestJson;
    }

    public struct PurrNetCustomMatchJoinRequestMessage : IPackedAuto
    {
        public string PlayerId;
        public string MatchId;
        public string ManifestJson;
    }

    public struct PurrNetCombatSlotAssignmentMessage : IPackedAuto
    {
        public string PlayerId;
        public string MatchId;
        public int Slot;
        public string FailureReason;
    }

    public struct PurrNetCombatCommandMessage : IPackedAuto
    {
        public string PlayerId;
        public string MatchId;
        public CombatCommandPacket Command;
    }

    public struct PurrNetCombatCommandResultMessage : IPackedAuto
    {
        public string PlayerId;
        public string MatchId;
        public CombatCommandResultPacket Result;
    }

    public struct PurrNetCombatAcceptedCommandMessage : IPackedAuto
    {
        public string MatchId;
        public CombatCommandPacket Command;
        public int ServerChecksum;
    }
}
