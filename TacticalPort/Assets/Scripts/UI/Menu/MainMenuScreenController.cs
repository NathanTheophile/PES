#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.App;
using UnityEngine;
using UnityEngine.SceneManagement;
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
        [Tooltip("Menu router on this object or a parent. Assign explicitly if the router lives elsewhere in the prefab hierarchy.")]
        [SerializeField] private MenuScreenRouter _MenuRouter;
        [Tooltip("Optional persistent transition service used only when falling back to standalone scene navigation.")]
        [SerializeField] private MonoBehaviour _SceneTransitionServiceSource;
        [SerializeField] private bool _UseEmbeddedTeamSelection;
        [SerializeField, HideInInspector] private Button _PlayTestButton;

        private ISceneTransitionService _SceneTransitionService;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            CacheLocalReferences();
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

        private void OnValidate() => CacheLocalReferences();

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

        public void ConfigureSceneTransitionService(MonoBehaviour pServiceSource)
        {
            _SceneTransitionServiceSource = pServiceSource;
            _SceneTransitionService = pServiceSource as ISceneTransitionService;
        }

        private bool ResolveMenuRouter()
        {
            CacheLocalReferences();
            if (_MenuRouter == null)
                Debug.LogWarning($"{nameof(MainMenuScreenController)} requires a {nameof(MenuScreenRouter)} on its parent hierarchy or in the serialized field.", this);
            return _MenuRouter != null;
        }

        private void CacheLocalReferences()
        {
            if (_MenuRouter == null)
                _MenuRouter = GetComponentInParent<MenuScreenRouter>(true);
        }

        private bool ResolveSceneTransitionService()
        {
            _SceneTransitionService ??= _SceneTransitionServiceSource as ISceneTransitionService;
            return _SceneTransitionService != null;
        }

        #endregion
    }
}
