#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Bootstrap
#endregion

using TacticalPort.Matchmaking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TacticalPort.Bootstrap
{
    public sealed class RuntimeServicesBootstrap : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [Header("Scene Flow")]
        [Tooltip("Frontend scene loaded after the runtime services are configured.")]
        [SerializeField] private string _MainMenuSceneName = "S_MainMenu";
        [Tooltip("When enabled, the bootstrap scene automatically loads the frontend scene during Start.")]
        [SerializeField] private bool _LoadMenuOnStart = true;
        [Tooltip("Keeps this runtime service root alive when moving between frontend and combat scenes.")]
        [SerializeField] private bool _KeepAliveAcrossScenes = true;
        [Tooltip("Optional persistent transition service used to load scenes through a loading screen.")]
        [SerializeField] private SceneTransitionController _SceneTransitionController;
        [Tooltip("Message displayed while the frontend scene is loading from bootstrap.")]
        [SerializeField] private string _InitialLoadMessage = "Loading menu...";

        [Header("Core Services")]
        [Tooltip("UGS identity service used before matchmaking and custom lobby flows.")]
        [SerializeField] private UgsPlayerIdentityService _PlayerIdentityService;
        [Tooltip("UGS Matchmaker service used by the quick match flow.")]
        [SerializeField] private UgsQuickMatchService _QuickMatchService;
        [Tooltip("Quick match orchestrator configured from this bootstrap at runtime.")]
        [SerializeField] private QuickMatchFlowController _QuickMatchFlowController;
        [Tooltip("Shared match context passed from frontend matchmaking to networking and combat.")]
        [SerializeField] private MatchRuntimeContext _MatchRuntimeContext;
        [Tooltip("Session cleanup owner for match end, main menu return, and runtime teardown.")]
        [SerializeField] private MatchRuntimeSessionLifecycle _MatchRuntimeSessionLifecycle;

        [Header("Optional Match Services")]
        [Tooltip("Optional. Source implementing IGameServerAllocator and optionally IGameServerAllocationReleaser.")]
        [SerializeField] private MonoBehaviour _GameServerAllocatorSource;
        [Tooltip("Optional. UgsCustomRelayLobbyService for private Custom Relay matches.")]
        [SerializeField] private MonoBehaviour _PartyLobbyServiceSource;

        private static RuntimeServicesBootstrap _Instance;
        private bool _IsDuplicate;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            CacheMissingReferences();

            if (_Instance != null && _Instance != this)
            {
                _IsDuplicate = true;
                Destroy(gameObject);
                return;
            }

            _Instance = this;
            ConfigureQuickMatchFlow();

            if (_KeepAliveAcrossScenes)
                DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            if (!_IsDuplicate && _LoadMenuOnStart)
                LoadMainMenu();
        }

        private void OnValidate() => CacheMissingReferences();

        #endregion

        #region _____________________________| HELPERS

        private void ConfigureQuickMatchFlow()
        {
            if (!ValidateReferences())
                return;

            _QuickMatchFlowController.ConfigureServices(_PlayerIdentityService, _QuickMatchService, _GameServerAllocatorSource, _MatchRuntimeContext);
            if (_GameServerAllocatorSource != null)
                _MatchRuntimeSessionLifecycle?.ConfigureGameServerAllocator(_GameServerAllocatorSource);

            _MatchRuntimeSessionLifecycle?.ConfigurePartyLobbyService(_PartyLobbyServiceSource);
        }

        private void LoadMainMenu()
        {
            if (string.IsNullOrWhiteSpace(_MainMenuSceneName))
                return;

            if (SceneManager.GetActiveScene().name == _MainMenuSceneName)
                return;

            LoadScene(_MainMenuSceneName, _InitialLoadMessage);
        }

        private void CacheMissingReferences()
        {
            if (_SceneTransitionController == null)
                _SceneTransitionController = GetComponentInChildren<SceneTransitionController>(true);

            if (_PlayerIdentityService == null)
                _PlayerIdentityService = GetComponent<UgsPlayerIdentityService>();

            if (_QuickMatchService == null)
                _QuickMatchService = GetComponent<UgsQuickMatchService>();

            if (_QuickMatchFlowController == null)
                _QuickMatchFlowController = GetComponent<QuickMatchFlowController>();

            if (_MatchRuntimeContext == null)
                _MatchRuntimeContext = GetComponent<MatchRuntimeContext>();

            if (_MatchRuntimeSessionLifecycle == null)
                _MatchRuntimeSessionLifecycle = GetComponent<MatchRuntimeSessionLifecycle>();

            if (_GameServerAllocatorSource == null)
                _GameServerAllocatorSource = FindGameServerAllocatorSource();

            if (_PartyLobbyServiceSource == null)
                _PartyLobbyServiceSource = FindPartyLobbyServiceSource();
        }

        private bool ValidateReferences()
        {
            bool lIsValid = true;
            lIsValid &= LogMissingReference(_PlayerIdentityService, nameof(_PlayerIdentityService));
            lIsValid &= LogMissingReference(_QuickMatchService, nameof(_QuickMatchService));
            lIsValid &= LogMissingReference(_QuickMatchFlowController, nameof(_QuickMatchFlowController));
            lIsValid &= LogMissingReference(_MatchRuntimeContext, nameof(_MatchRuntimeContext));
            return lIsValid;
        }

        private MonoBehaviour FindGameServerAllocatorSource()
        {
            MonoBehaviour[] lBehaviours = GetComponents<MonoBehaviour>();
            for (int lIndex = 0; lIndex < lBehaviours.Length; lIndex++)
            {
                if (lBehaviours[lIndex] is IGameServerAllocator)
                    return lBehaviours[lIndex];
            }

            return null;
        }

        private MonoBehaviour FindPartyLobbyServiceSource()
        {
            MonoBehaviour[] lBehaviours = GetComponents<MonoBehaviour>();
            for (int lIndex = 0; lIndex < lBehaviours.Length; lIndex++)
            {
                if (lBehaviours[lIndex] is IPartyLobbyService)
                    return lBehaviours[lIndex];
            }

            return null;
        }

        private bool LogMissingReference(Object pReference, string pFieldName)
        {
            if (pReference != null)
                return true;

            Debug.LogWarning($"{nameof(RuntimeServicesBootstrap)} is missing reference '{pFieldName}'.", this);
            return false;
        }

        private void LoadScene(string pSceneName, string pMessage)
        {
            if (_SceneTransitionController != null)
            {
                _SceneTransitionController.LoadScene(pSceneName, LoadSceneMode.Single, pMessage);
                return;
            }

            if (SceneTransitionController.TryLoadScene(pSceneName, LoadSceneMode.Single, pMessage))
                return;

            SceneManager.LoadScene(pSceneName, LoadSceneMode.Single);
        }

        #endregion
    }
}
