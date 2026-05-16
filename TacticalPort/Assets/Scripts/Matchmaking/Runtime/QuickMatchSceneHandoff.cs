#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Matchmaking
#endregion

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TacticalPort.Matchmaking
{
    public sealed class QuickMatchSceneHandoff : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private MatchRuntimeContext _MatchContext;
        [SerializeField] private string _CombatSceneName = "S_Poutch";
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
            TryRequestCombatLoad();
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
                TryRequestCombatLoad();
                return;
            }

            _HasRequestedLoad = false;
            if (_LoadRoutine != null)
            {
                StopCoroutine(_LoadRoutine);
                _LoadRoutine = null;
            }
        }

        private void TryRequestCombatLoad()
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

            SceneManager.LoadScene(_CombatSceneName, LoadSceneMode.Single);
        }

        #endregion

        #region _____________________________| HELPERS

        private void CacheMissingReferences()
        {
            if (_MatchContext == null)
                _MatchContext = GetComponent<MatchRuntimeContext>();
        }

        #endregion
    }
}
