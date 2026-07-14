#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Custom Match UI
#endregion

using System;
using System.Threading;
using TacticalPort.Matchmaking;
using TacticalPort.Shared;
using TacticalPort.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TacticalPort.UI
{
    public sealed class CustomMatchPopupView : MonoBehaviour
    {
        private enum DirectIpConnectionMode
        {
            CustomHostOrJoin = 0,
            DedicatedServerTest = 1
        }

        private enum DedicatedServerTestSlotSource
        {
            Inspector = 0,
            AutoFromClonePath = 1,
            CommandLineOrEnvironment = 2
        }

        #region _____________________________/ VALUES

        [Tooltip("Injected by RuntimeServicesBootstrap when S_MainMenu is loaded. May be assigned directly for isolated scene tests.")]
        [SerializeField] private MonoBehaviour _PlayerIdentityServiceSource;
        [Tooltip("Injected by RuntimeServicesBootstrap when S_MainMenu is loaded. Required only for Relay lobby mode.")]
        [SerializeField] private MonoBehaviour _PartyLobbyServiceSource;
        [Tooltip("Injected by RuntimeServicesBootstrap when S_MainMenu is loaded. May be assigned directly for isolated scene tests.")]
        [SerializeField] private MatchRuntimeContext _MatchContext;
        [SerializeField] private Button _OpenButton;
        [SerializeField] private Button _HostButton;
        [SerializeField] private Button _JoinButton;
        [SerializeField] private Button _CancelButton;
        [SerializeField] private GameObject _PopupRoot;
        [SerializeField] private TMP_InputField _JoinCodeInput;
        [SerializeField] private TMP_InputField _AddressInput;
        [SerializeField] private TMP_InputField _PortInput;
        [SerializeField] private TMP_Text _StatusText;
        [SerializeField] private TMP_Text _ErrorText;
        [SerializeField] private string _DefaultAddress = "127.0.0.1";
        [SerializeField, Min(1)] private int _DefaultPort = 7777;
        [SerializeField] private bool _UseRelayLobbyByDefault = true;
        [SerializeField] private bool _ShowDirectIpDebugControls;
        [Tooltip("CustomHostOrJoin is for player-hosted local tests. DedicatedServerTest is for an already running EdgeGap/local dedicated server.")]
        [SerializeField] private DirectIpConnectionMode _DirectIpMode = DirectIpConnectionMode.CustomHostOrJoin;
        [Tooltip("AutoFromClonePath maps ParrelSync clone 0 to TeamA and clone 1 to TeamB. CommandLineOrEnvironment reads -matchSlot TeamA/TeamB or TACTICALPORT_MATCH_SLOT.")]
        [SerializeField] private DedicatedServerTestSlotSource _DedicatedServerTestSlotSource = DedicatedServerTestSlotSource.AutoFromClonePath;
        [Tooltip("Used only when Direct Ip Mode is DedicatedServerTest. Set clone 0 to TeamA and clone 1 to TeamB.")]
        [SerializeField] private MatchPlayerSlot _DedicatedServerTestSlot = MatchPlayerSlot.TeamA;
        [SerializeField] private bool _HidePopupOnAwake = true;

        private IPlayerIdentityService _PlayerIdentityService;
        private IPartyLobbyService _PartyLobbyService;
        private CancellationTokenSource _Cancellation;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            CacheMissingSceneReferences();
            RefreshInputVisibility();

            if (_HidePopupOnAwake)
                SetPopupVisible(false);
        }

        private void OnEnable()
        {
            HookButtons();
            if (ResolveServices())
                RefreshIdleState();

            RefreshInputVisibility();
        }

        private void OnDisable()
        {
            UnhookButtons();
            CancelPendingOperation();
        }

        #endregion

        #region _____________________________| ACTIONS

        public void ConfigureServices(
            MonoBehaviour pPlayerIdentityServiceSource,
            MonoBehaviour pPartyLobbyServiceSource,
            MatchRuntimeContext pMatchContext)
        {
            _PlayerIdentityServiceSource = pPlayerIdentityServiceSource;
            _PartyLobbyServiceSource = pPartyLobbyServiceSource;
            _MatchContext = pMatchContext;

            if (isActiveAndEnabled && ResolveServices())
                RefreshIdleState();
        }

        private void HandleHostCustomMatchClicked() => HandleCustomMatchAsync(true);
        private void HandleJoinCustomMatchClicked() => HandleCustomMatchAsync(false);

        public void ShowPopup()
        {
            SetPopupVisible(true);

            if (ResolveServices())
                RefreshIdleState();
        }

        public void HidePopup() => SetPopupVisible(false);

        public void ShowPanel() => ShowPopup();
        public void HidePanel() => HidePopup();

        private void HandleOpenCustomMatchClicked() => ShowPopup();

        private async void HandleCancelCustomMatchClicked()
        {
            CancelPendingOperation();
            string lLobbyId = _MatchContext?.LobbyId ?? string.Empty;
            MatchConnectionMode lMode = _MatchContext != null ? _MatchContext.ConnectionMode : MatchConnectionMode.Offline;

            if (IsRelayMode(lMode) && ResolvePartyLobbyService(false))
                await LeaveLobbyAsync(lLobbyId);

            _MatchContext?.Clear();
            RefreshIdleState();
            HidePopup();
        }

        private async void HandleCustomMatchAsync(bool pHost)
        {
            if (!ResolveServices())
                return;

            CancelPendingOperation();
            _Cancellation = new CancellationTokenSource();
            SetInteractable(false);
            SetError(string.Empty);
            SetStatus(pHost ? "Starting custom host." : "Joining custom match.");

            try
            {
                PlayerIdentity lIdentity = await _PlayerIdentityService.SignInAsync(_Cancellation.Token);

                if (_UseRelayLobbyByDefault)
                    await StartRelayCustomMatchAsync(pHost, lIdentity, _Cancellation.Token);
                else
                    StartDirectCustomMatch(pHost, lIdentity);
            }
            catch (OperationCanceledException)
            {
                SetStatus("Custom match cancelled.");
            }
            catch (Exception pException)
            {
                SetError(pException.Message);
                SetStatus("Custom match failed.");
            }
            finally
            {
                SetInteractable(true);
                _Cancellation?.Dispose();
                _Cancellation = null;
            }
        }

        #endregion

        #region _____________________________| HELPERS

        private bool ResolveServices()
        {
            _PlayerIdentityService = _PlayerIdentityServiceSource as IPlayerIdentityService;

            bool lIsValid = true;
            lIsValid &= LogMissingService(_PlayerIdentityService, "Player identity service");
            lIsValid &= LogMissingService(_MatchContext, nameof(MatchRuntimeContext));
            lIsValid &= !_UseRelayLobbyByDefault || ResolvePartyLobbyService(true);
            return lIsValid;
        }

        private bool ResolvePartyLobbyService(bool pLogMissing)
        {
            _PartyLobbyService = _PartyLobbyServiceSource as IPartyLobbyService;
            return !pLogMissing || LogMissingService(_PartyLobbyService, "Custom Relay lobby service");
        }

        private async System.Threading.Tasks.Task StartRelayCustomMatchAsync(bool pHost, PlayerIdentity pIdentity, CancellationToken pCancellationToken)
        {
            if (!ResolvePartyLobbyService(true))
                return;

            if (pHost)
            {
                PartyLobbySnapshot lLobby = await _PartyLobbyService.CreateLobbyAsync(
                    new PartyLobbyRequest { MaxPlayers = 2, TeamPresetId = TeamPresetState.ActivePresetId },
                    pCancellationToken);
                _MatchContext.SetCustomRelayHostSession(pIdentity, lLobby);
                SetStatus($"Custom lobby created. Code: {lLobby.JoinCode}. Waiting for player.");
                return;
            }

            string lJoinCode = ReadJoinCode();
            if (string.IsNullOrWhiteSpace(lJoinCode))
                throw new InvalidOperationException("Custom lobby join code is empty.");

            PartyLobbySnapshot lJoinedLobby = await _PartyLobbyService.JoinLobbyAsync(lJoinCode, TeamPresetState.ActivePresetId, pCancellationToken);
            _MatchContext.SetCustomRelayJoinSession(pIdentity, lJoinedLobby);
            SetStatus($"Joining custom lobby {lJoinedLobby.JoinCode}.");
        }

        private void StartDirectCustomMatch(bool pHost, PlayerIdentity pIdentity)
        {
            MatchServerEndpoint lEndpoint = BuildEndpoint();

            if (_DirectIpMode == DirectIpConnectionMode.DedicatedServerTest)
            {
                MatchPlayerSlot lSlot = ResolveDedicatedServerTestSlot();
                _MatchContext.SetDedicatedServerTestSession(pIdentity, lEndpoint, lSlot);
                SetStatus($"Joining dedicated server test as {lSlot} at {lEndpoint.IpAddress}:{lEndpoint.Port}.");
                return;
            }

            if (pHost)
            {
                _MatchContext.SetCustomHostSession(pIdentity, lEndpoint);
                SetStatus($"Hosting custom unranked match on {lEndpoint.IpAddress}:{lEndpoint.Port}. Waiting for player.");
            }
            else
            {
                _MatchContext.SetCustomJoinSession(pIdentity, lEndpoint);
                SetStatus($"Joining custom unranked match at {lEndpoint.IpAddress}:{lEndpoint.Port}.");
            }
        }

        private MatchServerEndpoint BuildEndpoint()
        {
            string lAddress = _AddressInput != null && !string.IsNullOrWhiteSpace(_AddressInput.text)
                ? _AddressInput.text.Trim()
                : _DefaultAddress;

            int lPort = _DefaultPort;
            if (_PortInput != null && int.TryParse(_PortInput.text, out int lParsedPort))
                lPort = Mathf.Clamp(lParsedPort, 1, ushort.MaxValue);

            return new MatchServerEndpoint
            {
                IpAddress = lAddress,
                Port = (ushort)lPort,
                AllocationId = $"custom-direct-{lPort}"
            };
        }

        private string ReadJoinCode()
        {
            if (_JoinCodeInput != null && !string.IsNullOrWhiteSpace(_JoinCodeInput.text))
                return _JoinCodeInput.text.Trim();

            return _AddressInput != null ? _AddressInput.text.Trim() : string.Empty;
        }

        private void RefreshIdleState()
        {
            RefreshInputVisibility();

            if (!_UseRelayLobbyByDefault && _AddressInput != null && string.IsNullOrWhiteSpace(_AddressInput.text))
                _AddressInput.text = _DefaultAddress;

            if (!_UseRelayLobbyByDefault && _PortInput != null && string.IsNullOrWhiteSpace(_PortInput.text))
                _PortInput.text = _DefaultPort.ToString();

            SetStatus(GetIdleStatusText());
            SetError(string.Empty);
        }

        private string GetIdleStatusText()
        {
            if (_UseRelayLobbyByDefault)
                return "Custom Relay ready.";

            return _DirectIpMode == DirectIpConnectionMode.DedicatedServerTest
                ? $"Dedicated server direct test ready. Slot: {ResolveDedicatedServerTestSlot()}."
                : "Custom Direct IP ready.";
        }

        private MatchPlayerSlot ResolveDedicatedServerTestSlot()
        {
            if (_DedicatedServerTestSlotSource == DedicatedServerTestSlotSource.CommandLineOrEnvironment
                && TryReadSlotFromCommandLineOrEnvironment(out MatchPlayerSlot lConfiguredSlot))
                return lConfiguredSlot;

            if (_DedicatedServerTestSlotSource == DedicatedServerTestSlotSource.AutoFromClonePath
                && TryReadCloneIndex(out int lCloneIndex))
                return lCloneIndex % 2 == 0 ? MatchPlayerSlot.TeamA : MatchPlayerSlot.TeamB;

            return _DedicatedServerTestSlot == MatchPlayerSlot.TeamB ? MatchPlayerSlot.TeamB : MatchPlayerSlot.TeamA;
        }

        private static bool TryReadSlotFromCommandLineOrEnvironment(out MatchPlayerSlot pSlot)
        {
            string lEnvironmentSlot = Environment.GetEnvironmentVariable("TACTICALPORT_MATCH_SLOT");
            if (TryParseSlot(lEnvironmentSlot, out pSlot))
                return true;

            string[] lArguments = Environment.GetCommandLineArgs();
            for (int lIndex = 0; lIndex < lArguments.Length - 1; lIndex++)
            {
                if (string.Equals(lArguments[lIndex], "-matchSlot", StringComparison.OrdinalIgnoreCase)
                    && TryParseSlot(lArguments[lIndex + 1], out pSlot))
                    return true;
            }

            pSlot = MatchPlayerSlot.None;
            return false;
        }

        private static bool TryParseSlot(string pValue, out MatchPlayerSlot pSlot)
        {
            if (string.Equals(pValue, "TeamA", StringComparison.OrdinalIgnoreCase)
                || string.Equals(pValue, "A", StringComparison.OrdinalIgnoreCase)
                || string.Equals(pValue, "0", StringComparison.OrdinalIgnoreCase))
            {
                pSlot = MatchPlayerSlot.TeamA;
                return true;
            }

            if (string.Equals(pValue, "TeamB", StringComparison.OrdinalIgnoreCase)
                || string.Equals(pValue, "B", StringComparison.OrdinalIgnoreCase)
                || string.Equals(pValue, "1", StringComparison.OrdinalIgnoreCase))
            {
                pSlot = MatchPlayerSlot.TeamB;
                return true;
            }

            pSlot = MatchPlayerSlot.None;
            return false;
        }

        private static bool TryReadCloneIndex(out int pCloneIndex)
        {
            pCloneIndex = -1;
            string lPath = Application.dataPath;
            int lCloneIndex = lPath.LastIndexOf("clone", StringComparison.OrdinalIgnoreCase);
            if (lCloneIndex < 0)
                return false;

            int lDigitStart = -1;
            for (int lIndex = lCloneIndex + "clone".Length; lIndex < lPath.Length; lIndex++)
            {
                if (char.IsDigit(lPath[lIndex]))
                {
                    lDigitStart = lIndex;
                    break;
                }
            }

            if (lDigitStart < 0)
                return false;

            int lDigitEnd = lDigitStart;
            while (lDigitEnd < lPath.Length && char.IsDigit(lPath[lDigitEnd]))
                lDigitEnd++;

            return int.TryParse(lPath.Substring(lDigitStart, lDigitEnd - lDigitStart), out pCloneIndex);
        }

        private void HookButtons()
        {
            if (_OpenButton != null)
            {
                _OpenButton.onClick.RemoveListener(HandleOpenCustomMatchClicked);
                _OpenButton.onClick.AddListener(HandleOpenCustomMatchClicked);
            }

            if (_HostButton != null)
            {
                _HostButton.onClick.RemoveListener(HandleHostCustomMatchClicked);
                _HostButton.onClick.AddListener(HandleHostCustomMatchClicked);
            }

            if (_JoinButton != null)
            {
                _JoinButton.onClick.RemoveListener(HandleJoinCustomMatchClicked);
                _JoinButton.onClick.AddListener(HandleJoinCustomMatchClicked);
            }

            if (_CancelButton != null)
            {
                _CancelButton.onClick.RemoveListener(HandleCancelCustomMatchClicked);
                _CancelButton.onClick.AddListener(HandleCancelCustomMatchClicked);
            }
        }

        private void UnhookButtons()
        {
            if (_OpenButton != null)
                _OpenButton.onClick.RemoveListener(HandleOpenCustomMatchClicked);

            if (_HostButton != null)
                _HostButton.onClick.RemoveListener(HandleHostCustomMatchClicked);

            if (_JoinButton != null)
                _JoinButton.onClick.RemoveListener(HandleJoinCustomMatchClicked);

            if (_CancelButton != null)
                _CancelButton.onClick.RemoveListener(HandleCancelCustomMatchClicked);
        }

        private void SetInteractable(bool pInteractable)
        {
            if (_HostButton != null)
                _HostButton.interactable = pInteractable;

            if (_JoinButton != null)
                _JoinButton.interactable = pInteractable;
        }

        private async System.Threading.Tasks.Task LeaveLobbyAsync(string pLobbyId)
        {
            if (string.IsNullOrWhiteSpace(pLobbyId))
                return;

            try
            {
                await _PartyLobbyService.LeaveLobbyAsync(pLobbyId, CancellationToken.None);
            }
            catch (Exception pException)
            {
                SetError(pException.Message);
            }
        }

        private void CancelPendingOperation()
        {
            if (_Cancellation == null)
                return;

            _Cancellation.Cancel();
            _Cancellation.Dispose();
            _Cancellation = null;
        }

        private bool LogMissingService(object pService, string pLabel)
        {
            if (pService != null)
                return true;

            SetError($"{pLabel} not found. Start from S_Bootstrap.");
            return false;
        }

        private void RefreshInputVisibility()
        {
            bool lShowDirectIp = !_UseRelayLobbyByDefault || _ShowDirectIpDebugControls;

            if (_JoinCodeInput != null)
                _JoinCodeInput.gameObject.SetActive(_UseRelayLobbyByDefault);

            if (_AddressInput != null)
                _AddressInput.gameObject.SetActive(lShowDirectIp);

            if (_PortInput != null)
                _PortInput.gameObject.SetActive(lShowDirectIp);
        }

        private static bool IsRelayMode(MatchConnectionMode pMode) =>
            pMode is MatchConnectionMode.CustomRelayHost or MatchConnectionMode.CustomRelayJoin;

        private void SetPopupVisible(bool pVisible)
        {
            GameObject lPopupRoot = _PopupRoot != null ? _PopupRoot : gameObject;
            if (lPopupRoot != null && lPopupRoot.activeSelf != pVisible)
                lPopupRoot.SetActive(pVisible);
        }

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

        private void CacheMissingSceneReferences()
        {
            if (_OpenButton == null)
                _OpenButton = FindComponentByObjectName<Button>("Button_CustomMatch", "Button Custom Match", "Btn_CustomMatch");

            if (_HostButton == null)
                _HostButton = FindComponentByObjectName<Button>("Button_CustomHost", "Button_HostCustom", "Btn_CustomHost");

            if (_JoinButton == null)
                _JoinButton = FindComponentByObjectName<Button>("Button_CustomJoin", "Button_JoinCustom", "Btn_CustomJoin");

            if (_CancelButton == null)
                _CancelButton = FindComponentByObjectName<Button>("Button_CancelCustomMatch", "Button_CancelCustom", "Btn_CancelCustomMatch");

            if (_AddressInput == null)
                _AddressInput = FindComponentByObjectName<TMP_InputField>("Input_CustomAddress", "Input_CustomAdress", "Input_CustomMatchAddress");

            if (_JoinCodeInput == null)
                _JoinCodeInput = FindComponentByObjectName<TMP_InputField>("Input_CustomJoinCode", "Input_CustomLobbyCode", "Input_LobbyCode");

            if (_PortInput == null)
                _PortInput = FindComponentByObjectName<TMP_InputField>("Input_CustomPort", "Input_CustomMatchPort");

            if (_StatusText == null)
                _StatusText = FindComponentByObjectName<TMP_Text>("Text_CustomMatchStatus", "Txt_CustomMatchStatus");

            if (_ErrorText == null)
                _ErrorText = FindComponentByObjectName<TMP_Text>("Text_CustomMatchError", "Txt_CustomMatchError");

            if (_PopupRoot == null)
                _PopupRoot = FindGameObjectByName("Popup_CustomMatch", "UI_Popup_CustomMatch", "Panel_CustomMatch", "Panel_CustomMatchView", "UI_CustomMatchView");
        }

        private T FindComponentByObjectName<T>(params string[] pNames) where T : Component
        {
            GameObject lGameObject = FindGameObjectByName(pNames);
            return lGameObject != null ? lGameObject.GetComponent<T>() : null;
        }

        private GameObject FindGameObjectByName(params string[] pNames)
        {
            Transform[] lTransforms = GetComponentsInChildren<Transform>(true);
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
