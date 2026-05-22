#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Matchmaking
#endregion

using System.Collections;
using TacticalPort.Bootstrap;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TacticalPort.Matchmaking
{
    public sealed class QuickMatchSceneHandoff : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private MatchRuntimeContext _MatchContext;
        [Tooltip("Optional transition service used to display a loading screen before combat.")]
        [SerializeField] private SceneTransitionController _SceneTransitionController;
        [SerializeField] private string _CombatSceneName = "S_Poutch";
        [SerializeField] private string _LoadingMessage = "Loading combat...";
        [SerializeField] private bool _LoadOnMatchFound = true;
        [SerializeField, Min(0f)] private float _LoadDelaySeconds = 1f;
        [SerializeField] private bool _LogEvents = true;

        private Coroutine _LoadRoutine;
        private bool _HasRequestedLoad;

        #endregion

        #region _____________________________| UNITY

        private void Awake() => CacheMissingReferences();

        private void OnEnable()
        {
            CacheMissingReferences();

            if (_MatchContext == null)
                return;

            _MatchContext.Changed -= HandleMatchContextChanged;
            _MatchContext.Changed += HandleMatchContextChanged;
            RequestCombatLoadIfReady();
        }

        private void OnDisable()
        {
            if (_MatchContext != null)
                _MatchContext.Changed -= HandleMatchContextChanged;

            if (_LoadRoutine != null)
            {
                StopCoroutine(_LoadRoutine);
                _LoadRoutine = null;
            }
        }

        #endregion

        #region _____________________________| HANDOFF

        private void HandleMatchContextChanged(MatchRuntimeContext pContext)
        {
            if (_MatchContext == null || _MatchContext.HasMatch)
            {
                RequestCombatLoadIfReady();
                return;
            }

            _HasRequestedLoad = false;
            if (_LoadRoutine != null)
            {
                StopCoroutine(_LoadRoutine);
                _LoadRoutine = null;
            }
        }

        private void RequestCombatLoadIfReady()
        {
            if (!_LoadOnMatchFound || _HasRequestedLoad || _MatchContext == null || !_MatchContext.HasMatch)
                return;

            if (!_MatchContext.Manifest.HasAllPlayerCompositions)
                return;

            if (string.IsNullOrWhiteSpace(_CombatSceneName))
            {
                Debug.LogWarning($"{nameof(QuickMatchSceneHandoff)} cannot load combat: scene name is empty.", this);
                return;
            }

            if (SceneManager.GetActiveScene().name == _CombatSceneName)
                return;

            _HasRequestedLoad = true;
            _LoadRoutine = StartCoroutine(LoadCombatSceneRoutine());
        }

        private IEnumerator LoadCombatSceneRoutine()
        {
            if (_LogEvents)
                Debug.Log($"[QuickMatch Handoff] Match found. Loading {_CombatSceneName}. MatchId={_MatchContext.MatchId}", this);

            if (_LoadDelaySeconds > 0f)
                yield return new WaitForSeconds(_LoadDelaySeconds);

            LoadCombatScene();
        }

        #endregion

        #region _____________________________| HELPERS

        private void CacheMissingReferences()
        {
            if (_MatchContext == null)
                _MatchContext = GetComponent<MatchRuntimeContext>();

            if (_SceneTransitionController == null)
                _SceneTransitionController = GetComponentInParent<SceneTransitionController>(true);

            if (_SceneTransitionController == null)
                _SceneTransitionController = FindAnyObjectByType<SceneTransitionController>();
        }

        private void LoadCombatScene()
        {
            if (_SceneTransitionController != null)
            {
                _SceneTransitionController.LoadScene(_CombatSceneName, LoadSceneMode.Single, _LoadingMessage);
                return;
            }

            if (SceneTransitionController.TryLoadScene(_CombatSceneName, LoadSceneMode.Single, _LoadingMessage))
                return;

            SceneManager.LoadScene(_CombatSceneName, LoadSceneMode.Single);
        }

        #endregion
    }
}
