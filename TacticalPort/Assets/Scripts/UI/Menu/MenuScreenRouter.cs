#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Menu screen routing
#endregion

using TacticalPort.App;
using UnityEngine;
using UnityEngine.Serialization;

namespace TacticalPort.UI
{
    public sealed class MenuScreenRouter : MonoBehaviour, IMenuPopupRouter
    {
        #region _____________________________/ VALUES

        [SerializeField] private GameObject _MainMenuScreen;
        [FormerlySerializedAs("_TeamSelectionScreen")]
        [SerializeField] private GameObject _TeamSelectionPopup;
        [FormerlySerializedAs("_CustomMatchPanel")]
        [SerializeField] private GameObject _CustomMatchPopup;
        [FormerlySerializedAs("_NetworkDebugPanel")]
        [SerializeField] private GameObject _NetworkDebugPopup;
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
                HideCustomMatchPopup();

            if (_HideNetworkDebugOnAwake)
                HideNetworkDebugPopup();
        }

        #endregion

        #region _____________________________| FLOW

        public void ShowMainMenu()
        {
            SetActive(_MainMenuScreen, true);
            HideAllPopups();
        }

        public void ShowTeamSelectionPopup()
        {
            SetActive(_MainMenuScreen, true);
            HideCustomMatchPopup();
            HideNetworkDebugPopup();
            SetActive(_TeamSelectionPopup, true);

            ITeamSelectionPopupView lTeamSelection = ResolveTeamSelectionPopupView();
            lTeamSelection?.RefreshSelectionView();
        }

        public void HideTeamSelectionPopup() => SetActive(_TeamSelectionPopup, false);

        public void ShowCustomMatchPopup()
        {
            SetActive(_MainMenuScreen, true);
            HideTeamSelectionPopup();
            HideNetworkDebugPopup();
            SetActive(_CustomMatchPopup, true);

            CustomMatchPopupView lCustomMatch = _CustomMatchPopup != null
                ? _CustomMatchPopup.GetComponentInChildren<CustomMatchPopupView>(true)
                : null;
            lCustomMatch?.ShowPopup();
        }

        public void HideCustomMatchPopup()
        {
            CustomMatchPopupView lCustomMatch = _CustomMatchPopup != null
                ? _CustomMatchPopup.GetComponentInChildren<CustomMatchPopupView>(true)
                : null;

            if (lCustomMatch != null)
                lCustomMatch.HidePopup();
            else
                SetActive(_CustomMatchPopup, false);
        }

        public void ShowNetworkDebugPopup()
        {
            SetActive(_MainMenuScreen, true);
            HideTeamSelectionPopup();
            HideCustomMatchPopup();
            SetActive(_NetworkDebugPopup, true);
        }

        public void HideNetworkDebugPopup() => SetActive(_NetworkDebugPopup, false);

        public void HideAllPopups()
        {
            HideTeamSelectionPopup();
            HideCustomMatchPopup();
            HideNetworkDebugPopup();
        }

        public void ShowTeamSelection() => ShowTeamSelectionPopup();
        public void ShowCustomMatchPanel() => ShowCustomMatchPopup();
        public void HideCustomMatchPanel() => HideCustomMatchPopup();

        #endregion

        #region _____________________________| HELPERS

        private void CacheMissingSceneReferences()
        {
            if (_MainMenuScreen == null)
                _MainMenuScreen = FindGameObjectByName("Screen_MainMenu", "UI_Menu_Main");

            if (_TeamSelectionPopup == null)
                _TeamSelectionPopup = FindGameObjectByName("Popup_TeamSelection", "UI_Popup_TeamSelection", "Screen_TeamSelection", "UI_Menu_TeamSelection");

            if (_CustomMatchPopup == null)
                _CustomMatchPopup = FindGameObjectByName("Popup_CustomMatch", "UI_Popup_CustomMatch", "Panel_CustomMatch", "UI_CustomMatchView", "Panel_CustomMatchView");

            if (_NetworkDebugPopup == null)
                _NetworkDebugPopup = FindGameObjectByName("Popup_NetworkDebug", "Panel_NetworkDebug", "Panel_Debug");
        }

        private static void SetActive(GameObject pGameObject, bool pActive)
        {
            if (pGameObject != null && pGameObject.activeSelf != pActive)
                pGameObject.SetActive(pActive);
        }

        private ITeamSelectionPopupView ResolveTeamSelectionPopupView()
        {
            if (_TeamSelectionPopup == null)
                return null;

            MonoBehaviour[] lBehaviours = _TeamSelectionPopup.GetComponentsInChildren<MonoBehaviour>(true);
            for (int lIndex = 0; lIndex < lBehaviours.Length; lIndex++)
            {
                if (lBehaviours[lIndex] is ITeamSelectionPopupView lView)
                    return lView;
            }

            return null;
        }

        private GameObject FindGameObjectByName(params string[] pNames)
        {
            Transform[] lTransforms = transform.root != null
                ? transform.root.GetComponentsInChildren<Transform>(true)
                : GetComponentsInChildren<Transform>(true);
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
