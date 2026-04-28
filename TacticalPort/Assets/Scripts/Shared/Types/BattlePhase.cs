#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Shared
#endregion

namespace TacticalPort.Shared
{
    public enum BattlePhase
    {
        Setup = 0,
        Placement = 1,
        AwaitingAction = 2,
        ResolvingAction = 3,
        TurnEnd = 4,
        Completed = 5
    }
}
