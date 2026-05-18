#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TacticalPort.UI
{
    public sealed class MainMenuController : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private string _TeamSelectionSceneName = "S_TeamSelection";
        [SerializeField] private Button _TeamButton;
        [SerializeField] private Button _CustomMatchButton;
        [SerializeField] private FrontendScreenRouter _ScreenRouter;
        [SerializeField] private bool _UseEmbeddedTeamSelection;
        [SerializeField, HideInInspector] private Button _PlayTestButton;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            Button lTeamButton = _TeamButton != null ? _TeamButton : _PlayTestButton;

            if (lTeamButton == null)
                Debug.LogWarning($"{nameof(MainMenuController)} is missing a Team button reference.", this);
            else
            {
                lTeamButton.onClick.RemoveListener(OpenTeamSelection);
                lTeamButton.onClick.AddListener(OpenTeamSelection);
            }

            if (_CustomMatchButton != null)
            {
                _CustomMatchButton.onClick.RemoveListener(OpenCustomMatchPanel);
                _CustomMatchButton.onClick.AddListener(OpenCustomMatchPanel);
            }
        }

        #endregion

        #region _____________________________| HELPERS

        public void OpenTeamSelection()
        {
            if (_UseEmbeddedTeamSelection && ResolveScreenRouter())
            {
                _ScreenRouter.ShowTeamSelection();
                return;
            }

            if (!string.IsNullOrWhiteSpace(_TeamSelectionSceneName))
                SceneManager.LoadScene(_TeamSelectionSceneName);
        }

        public void OpenCustomMatchPanel()
        {
            if (ResolveScreenRouter())
                _ScreenRouter.ShowCustomMatchPanel();
        }

        private bool ResolveScreenRouter()
        {
            if (_ScreenRouter == null)
                _ScreenRouter = GetComponentInParent<FrontendScreenRouter>();

            if (_ScreenRouter == null)
                _ScreenRouter = FindAnyObjectByType<FrontendScreenRouter>();

            return _ScreenRouter != null;
        }

        #endregion
    }
}
