#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
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

        [SerializeField] private string _MainMenuSceneName = "S_MainMenu";
        [SerializeField] private bool _LoadMenuOnStart = true;
        [SerializeField] private bool _KeepAliveAcrossScenes = true;
        [SerializeField] private UgsPlayerIdentityService _PlayerIdentityService;
        [SerializeField] private UgsQuickMatchService _QuickMatchService;
        [Tooltip("Optional. LocalGameServerAllocator for local validation, UgsCloudCodeGameServerAllocator for EdgeGap allocation later.")]
        [SerializeField] private MonoBehaviour _GameServerAllocatorSource;
        [Tooltip("Optional. UgsCustomRelayLobbyService for private Custom Relay matches.")]
        [SerializeField] private MonoBehaviour _PartyLobbyServiceSource;
        [SerializeField] private QuickMatchFlowController _QuickMatchFlowController;
        [SerializeField] private MatchRuntimeContext _MatchRuntimeContext;
        [SerializeField] private MatchRuntimeSessionLifecycle _MatchRuntimeSessionLifecycle;

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

            SceneManager.LoadScene(_MainMenuSceneName);
        }

        private void CacheMissingReferences()
        {
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

        #endregion
    }
}
