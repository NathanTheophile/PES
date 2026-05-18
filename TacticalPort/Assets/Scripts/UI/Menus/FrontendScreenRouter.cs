#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Frontend screen routing
#endregion

using UnityEngine;

namespace TacticalPort.UI
{
    public sealed class FrontendScreenRouter : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private GameObject _MainMenuScreen;
        [SerializeField] private GameObject _TeamSelectionScreen;
        [SerializeField] private GameObject _CustomMatchPanel;
        [SerializeField] private GameObject _NetworkDebugPanel;
        [SerializeField] private bool _ShowMainMenuOnAwake = true;
        [SerializeField] private bool _HideCustomMatchOnAwake = true;
        [SerializeField] private bool _HideNetworkDebugOnAwake = true;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            CacheMissingSceneReferences();

            if (_ShowMainMenuOnAwake)
                ShowMainMenu();
            else if (_HideCustomMatchOnAwake)
                HideCustomMatchPanel();

            if (_HideNetworkDebugOnAwake && _NetworkDebugPanel != null)
                _NetworkDebugPanel.SetActive(false);
        }

        #endregion

        #region _____________________________| FLOW

        public void ShowMainMenu()
        {
            SetActive(_MainMenuScreen, true);
            SetActive(_TeamSelectionScreen, false);
            HideCustomMatchPanel();
        }

        public void ShowTeamSelection()
        {
            SetActive(_MainMenuScreen, false);
            SetActive(_TeamSelectionScreen, true);
            HideCustomMatchPanel();

            TeamSelectionController lTeamSelection = _TeamSelectionScreen != null
                ? _TeamSelectionScreen.GetComponentInChildren<TeamSelectionController>(true)
                : null;
            lTeamSelection?.RefreshSelectionView();
        }

        public void ShowCustomMatchPanel()
        {
            SetActive(_MainMenuScreen, true);
            SetActive(_TeamSelectionScreen, false);
            SetActive(_CustomMatchPanel, true);

            MainMenuCustomMatchView lCustomMatch = _CustomMatchPanel != null
                ? _CustomMatchPanel.GetComponentInChildren<MainMenuCustomMatchView>(true)
                : null;
            lCustomMatch?.ShowPanel();
        }

        public void HideCustomMatchPanel()
        {
            MainMenuCustomMatchView lCustomMatch = _CustomMatchPanel != null
                ? _CustomMatchPanel.GetComponentInChildren<MainMenuCustomMatchView>(true)
                : null;

            if (lCustomMatch != null)
                lCustomMatch.HidePanel();
            else
                SetActive(_CustomMatchPanel, false);
        }

        #endregion

        #region _____________________________| HELPERS

        private void CacheMissingSceneReferences()
        {
            if (_MainMenuScreen == null)
                _MainMenuScreen = FindGameObjectByName("Screen_MainMenu", "UI_Menu_Main");

            if (_TeamSelectionScreen == null)
                _TeamSelectionScreen = FindGameObjectByName("Screen_TeamSelection", "UI_Menu_TeamSelection");

            if (_CustomMatchPanel == null)
                _CustomMatchPanel = FindGameObjectByName("Panel_CustomMatch", "UI_CustomMatchView", "Panel_CustomMatchView");

            if (_NetworkDebugPanel == null)
                _NetworkDebugPanel = FindGameObjectByName("Panel_NetworkDebug", "Panel_Debug");
        }

        private static void SetActive(GameObject pGameObject, bool pActive)
        {
            if (pGameObject != null && pGameObject.activeSelf != pActive)
                pGameObject.SetActive(pActive);
        }

        private static GameObject FindGameObjectByName(params string[] pNames)
        {
            Transform[] lTransforms = FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (int lNameIndex = 0; lNameIndex < pNames.Length; lNameIndex++)
            {
                for (int lIndex = 0; lIndex < lTransforms.Length; lIndex++)
                {
                    Transform lTransform = lTransforms[lIndex];
                    if (lTransform != null && lTransform.name == pNames[lNameIndex])
                        return lTransform.gameObject;
                }
            }

            return null;
        }

        #endregion
    }
}
