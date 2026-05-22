#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.App;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace TacticalPort.UI
{
    public sealed class MainMenuScreenController : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private string _TeamSelectionSceneName = "S_TeamSelection";
        [Tooltip("Message displayed by the persistent loading screen when opening the standalone team selection scene.")]
        [SerializeField] private string _LoadingMessage = "Loading team selection...";
        [SerializeField] private Button _TeamButton;
        [SerializeField] private Button _CustomMatchButton;
        [FormerlySerializedAs("_ScreenRouter")]
        [SerializeField] private MenuScreenRouter _MenuRouter;
        [Tooltip("Optional persistent transition service used only when falling back to standalone scene navigation.")]
        [FormerlySerializedAs("_SceneTransitionController")]
        [SerializeField] private MonoBehaviour _SceneTransitionServiceSource;
        [SerializeField] private bool _UseEmbeddedTeamSelection;
        [SerializeField, HideInInspector] private Button _PlayTestButton;

        private ISceneTransitionService _SceneTransitionService;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            Button lTeamButton = _TeamButton != null ? _TeamButton : _PlayTestButton;

            if (lTeamButton == null)
                Debug.LogWarning($"{nameof(MainMenuScreenController)} is missing a Team button reference.", this);
            else
            {
                lTeamButton.onClick.RemoveListener(OpenTeamSelection);
                lTeamButton.onClick.AddListener(OpenTeamSelection);
            }

            if (_CustomMatchButton != null)
            {
                _CustomMatchButton.onClick.RemoveListener(OpenCustomMatchPopup);
                _CustomMatchButton.onClick.AddListener(OpenCustomMatchPopup);
            }
        }

        #endregion

        #region _____________________________| HELPERS

        public void OpenTeamSelection()
        {
            if (_UseEmbeddedTeamSelection && ResolveMenuRouter())
            {
                _MenuRouter.ShowTeamSelectionPopup();
                return;
            }

            if (string.IsNullOrWhiteSpace(_TeamSelectionSceneName))
                return;

            if (ResolveSceneTransitionService())
            {
                _SceneTransitionService.LoadScene(_TeamSelectionSceneName, LoadSceneMode.Single, _LoadingMessage);
                return;
            }

            SceneManager.LoadScene(_TeamSelectionSceneName);
        }

        public void OpenCustomMatchPopup()
        {
            if (ResolveMenuRouter())
                _MenuRouter.ShowCustomMatchPopup();
        }

        public void OpenCustomMatchPanel() => OpenCustomMatchPopup();

        private bool ResolveMenuRouter()
        {
            if (_MenuRouter == null)
                _MenuRouter = GetComponentInParent<MenuScreenRouter>();

            if (_MenuRouter == null)
                _MenuRouter = FindAnyObjectByType<MenuScreenRouter>();

            return _MenuRouter != null;
        }

        private bool ResolveSceneTransitionService()
        {
            _SceneTransitionService ??= _SceneTransitionServiceSource as ISceneTransitionService;
            if (_SceneTransitionService != null)
                return true;

            MonoBehaviour[] lBehaviours = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
            for (int lIndex = 0; lIndex < lBehaviours.Length; lIndex++)
            {
                if (lBehaviours[lIndex] is ISceneTransitionService lService)
                {
                    _SceneTransitionServiceSource = lBehaviours[lIndex];
                    _SceneTransitionService = lService;
                    return true;
                }
            }

            return false;
        }

        #endregion
    }
}
