#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  App Contracts
#endregion

using UnityEngine.SceneManagement;

namespace TacticalPort.App
{
    public interface ISceneTransitionService
    {
        bool IsTransitioning { get; }
        void LoadScene(string pSceneName, LoadSceneMode pMode = LoadSceneMode.Single, string pMessage = "");
    }
}
