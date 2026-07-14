#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Matchmaking UI
#endregion

using TacticalPort.Matchmaking;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TacticalPort.UI
{
    public sealed class QuickMatchPanelView : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [Tooltip("Injected by RuntimeServicesBootstrap when S_MainMenu is loaded. May be assigned directly for isolated scene tests.")]
        [SerializeField] private QuickMatchFlowController _FlowController;
        [SerializeField] private Button _QuickMatchButton;
        [SerializeField] private Button _CancelQuickMatchButton;
        [SerializeField] private TMP_Text _StatusText;
        [SerializeField] private TMP_Text _ErrorText;
        [SerializeField] private GameObject _SearchingPanel;

        #endregion

        #region _____________________________| UNITY

        private void Awake() => CacheMissingSceneReferences();

        private void OnEnable()
        {
            HookButtons();
            BindFlowController();
        }

        private void OnDisable()
        {
            UnhookButtons();

            if (_FlowController != null)
                _FlowController.StateChanged -= HandleStateChanged;
        }

        #endregion

        #region _____________________________| FLOW

        public void ConfigureFlowController(QuickMatchFlowController pFlowController)
        {
            if (_FlowController != null)
                _FlowController.StateChanged -= HandleStateChanged;

            _FlowController = pFlowController;

            if (isActiveAndEnabled)
                BindFlowController();
        }

        private void HandleStartQuickMatchClicked()
        {
            if (!EnsureFlowController())
                return;

            SetError(string.Empty);
            _FlowController.StartQuickMatch();
            RefreshQuickMatchState(_FlowController.LastSnapshot);
        }

        private void HandleCancelQuickMatchClicked()
        {
            if (!EnsureFlowController())
                return;

            _FlowController.CancelQuickMatch();
            RefreshQuickMatchState(_FlowController.LastSnapshot);
        }

        private void BindFlowController()
        {
            if (!EnsureFlowController())
                return;

            _FlowController.StateChanged -= HandleStateChanged;
            _FlowController.StateChanged += HandleStateChanged;
            RefreshQuickMatchState(_FlowController.LastSnapshot);
        }

        private void HandleStateChanged(QuickMatchFlowSnapshot pSnapshot) => RefreshQuickMatchState(pSnapshot);

        #endregion

        #region _____________________________| DISPLAY

        private void RefreshQuickMatchState(QuickMatchFlowSnapshot pSnapshot)
        {
            QuickMatchFlowStatus lStatus = pSnapshot != null ? pSnapshot.Status : QuickMatchFlowStatus.Idle;
            bool lIsSearching = IsSearchingStatus(lStatus);

            if (_SearchingPanel != null)
                _SearchingPanel.SetActive(lIsSearching);

            if (_QuickMatchButton != null)
                _QuickMatchButton.interactable = !lIsSearching;

            if (_CancelQuickMatchButton != null)
                _CancelQuickMatchButton.interactable = lIsSearching;

            SetStatus(BuildStatusText(pSnapshot));
            SetError(lStatus == QuickMatchFlowStatus.Failed ? pSnapshot?.Message : string.Empty);
        }

        private void ShowMissingBootstrapState()
        {
            if (_SearchingPanel != null)
                _SearchingPanel.SetActive(false);

            if (_QuickMatchButton != null)
                _QuickMatchButton.interactable = false;

            if (_CancelQuickMatchButton != null)
                _CancelQuickMatchButton.interactable = false;

            SetStatus("Matchmaking unavailable.");
            SetError("Runtime services not found. Start from S_Bootstrap so UGS can initialize.");
        }

        private static string BuildStatusText(QuickMatchFlowSnapshot pSnapshot)
        {
            if (pSnapshot == null)
                return "Ready for quick match.";

            return pSnapshot.Status switch
            {
                QuickMatchFlowStatus.Idle => "Ready for quick match.",
                QuickMatchFlowStatus.SigningIn => "Signing in.",
                QuickMatchFlowStatus.CreatingTicket => "Creating quick match ticket.",
                QuickMatchFlowStatus.Searching => "Searching for an opponent.",
                QuickMatchFlowStatus.Found => $"Match found. MatchId: {ResolveMatchId(pSnapshot)}. {ResolveTrustLabel(pSnapshot)}.",
                QuickMatchFlowStatus.Failed => "Quick match failed.",
                QuickMatchFlowStatus.Cancelled => "Quick match cancelled.",
                _ => pSnapshot.Message ?? string.Empty
            };
        }

        private static string ResolveMatchId(QuickMatchFlowSnapshot pSnapshot) =>
            !string.IsNullOrWhiteSpace(pSnapshot?.Ticket?.Manifest?.MatchId)
                ? pSnapshot.Ticket.Manifest.MatchId
                : "unknown";

        private static string ResolveTrustLabel(QuickMatchFlowSnapshot pSnapshot) =>
            !string.IsNullOrWhiteSpace(pSnapshot?.Ticket?.TrustLabel)
                ? pSnapshot.Ticket.TrustLabel
                : "Trust unknown";

        private static bool IsSearchingStatus(QuickMatchFlowStatus pStatus) =>
            pStatus == QuickMatchFlowStatus.SigningIn ||
            pStatus == QuickMatchFlowStatus.CreatingTicket ||
            pStatus == QuickMatchFlowStatus.Searching;

        private void SetStatus(string pText)
        {
            if (_StatusText != null)
                _StatusText.text = pText ?? string.Empty;
        }

        private void SetError(string pText)
        {
            if (_ErrorText != null)
                _ErrorText.text = pText ?? string.Empty;
        }

        #endregion

        #region _____________________________| HELPERS

        private void HookButtons()
        {
            if (_QuickMatchButton != null)
            {
                _QuickMatchButton.onClick.RemoveListener(HandleStartQuickMatchClicked);
                _QuickMatchButton.onClick.AddListener(HandleStartQuickMatchClicked);
            }

            if (_CancelQuickMatchButton != null)
            {
                _CancelQuickMatchButton.onClick.RemoveListener(HandleCancelQuickMatchClicked);
                _CancelQuickMatchButton.onClick.AddListener(HandleCancelQuickMatchClicked);
            }
        }

        private void UnhookButtons()
        {
            if (_QuickMatchButton != null)
                _QuickMatchButton.onClick.RemoveListener(HandleStartQuickMatchClicked);

            if (_CancelQuickMatchButton != null)
                _CancelQuickMatchButton.onClick.RemoveListener(HandleCancelQuickMatchClicked);
        }

        private bool EnsureFlowController()
        {
            if (_FlowController != null)
                return true;

            ShowMissingBootstrapState();
            return false;
        }

        private void CacheMissingSceneReferences()
        {
            if (_QuickMatchButton == null)
                _QuickMatchButton = FindComponentByObjectName<Button>("Button_QuickMatch", "Btn_Quickmatch", "Btn_QuickMatch");

            if (_CancelQuickMatchButton == null)
                _CancelQuickMatchButton = FindComponentByObjectName<Button>("Button_CancelQuickMatch", "Btn_CancelQuickmatch", "Btn_CancelQuickMatch");

            if (_StatusText == null)
                _StatusText = FindComponentByObjectName<TMP_Text>("Text_MatchmakingStatus", "Txt_MatchmakingStatus");

            if (_ErrorText == null)
                _ErrorText = FindComponentByObjectName<TMP_Text>("Text_MatchmakingError", "Txt_MatchmakingError");

            if (_SearchingPanel == null)
                _SearchingPanel = FindGameObjectByName("Panel_QuickMatchStatus", "Panel_MatchmakingSearching");
        }

        private T FindComponentByObjectName<T>(params string[] pNames) where T : Component
        {
            GameObject lGameObject = FindGameObjectByName(pNames);
            return lGameObject != null ? lGameObject.GetComponent<T>() : null;
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
