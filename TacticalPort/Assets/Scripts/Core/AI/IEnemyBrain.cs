#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

namespace TacticalPort.Core.AI
{
    public interface IEnemyBrain
    {
        EnemyAiAction ChooseAction(EnemyAiContext context);
    }
}
