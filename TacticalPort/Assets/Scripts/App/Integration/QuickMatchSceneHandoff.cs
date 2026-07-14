#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Matchmaking
#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using TacticalPort.Bootstrap;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TacticalPort.Matchmaking
{
    [Serializable]
    public sealed class MatchMapSceneCatalog
    {
        [Serializable]
        public sealed class Entry
        {
            [SerializeField] private string _MapId = string.Empty;
            [SerializeField] private string _SceneName = string.Empty;

            public Entry(string pMapId, string pSceneName)
            {
                _MapId = pMapId;
                _SceneName = pSceneName;
            }

            public string MapId => _MapId?.Trim() ?? string.Empty;
            public string SceneName => _SceneName?.Trim() ?? string.Empty;
        }

        [SerializeField] private List<Entry> _Entries = new List<Entry>
        {
            new Entry("alpha-1", "S_Map_Alpha_1"),
            new Entry("poutch", "S_Map_Alpha_1"),
            new Entry("custom-direct", "S_Map_Alpha_1"),
            new Entry("custom-relay", "S_Map_Alpha_1")
        };

        public MatchMapSceneCatalog() { }

        public MatchMapSceneCatalog(IEnumerable<Entry> pEntries) =>
            _Entries = pEntries != null ? new List<Entry>(pEntries) : new List<Entry>();

        public IReadOnlyList<Entry> Entries => _Entries;

        public bool TryResolveScene(string pMapId, out string pSceneName, out string pFailure)
        {
            pSceneName = string.Empty;
            if (!TryValidate(out pFailure))
                return false;

            string lMapId = pMapId?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(lMapId))
            {
                pFailure = "Match manifest does not define a map id.";
                return false;
            }

            for (int lIndex = 0; lIndex < _Entries.Count; lIndex++)
            {
                Entry lEntry = _Entries[lIndex];
                if (string.Equals(lEntry.MapId, lMapId, StringComparison.OrdinalIgnoreCase))
                {
                    pSceneName = lEntry.SceneName;
                    pFailure = string.Empty;
                    return true;
                }
            }

            pFailure = $"No combat scene is registered for map id '{lMapId}'.";
            return false;
        }

        public bool TryValidate(out string pFailure)
        {
            if (_Entries == null || _Entries.Count == 0)
            {
                pFailure = "Match map scene catalog is empty.";
                return false;
            }

            HashSet<string> lMapIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int lIndex = 0; lIndex < _Entries.Count; lIndex++)
            {
                Entry lEntry = _Entries[lIndex];
                if (lEntry == null || string.IsNullOrWhiteSpace(lEntry.MapId) || string.IsNullOrWhiteSpace(lEntry.SceneName))
                {
                    pFailure = $"Match map scene catalog entry #{lIndex + 1} requires a map id and scene name.";
                    return false;
                }

                if (!lMapIds.Add(lEntry.MapId))
                {
                    pFailure = $"Match map scene catalog contains duplicate map id '{lEntry.MapId}'.";
                    return false;
                }
            }

            pFailure = string.Empty;
            return true;
        }
    }

    public sealed class QuickMatchSceneHandoff : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private MatchRuntimeContext _MatchContext;
        [Tooltip("Optional transition service used to display a loading screen before combat.")]
        [SerializeField] private SceneTransitionController _SceneTransitionController;
        [SerializeField] private MatchMapSceneCatalog _MapSceneCatalog = new MatchMapSceneCatalog();
        [SerializeField] private string _LoadingMessage = "Loading combat...";
        [SerializeField] private bool _LoadOnMatchFound = true;
        [SerializeField, Min(0f)] private float _LoadDelaySeconds = 1f;
        [SerializeField] private bool _LogEvents = true;

        private Coroutine _LoadRoutine;
        private bool _HasRequestedLoad;
        private string _ResolvedCombatSceneName = string.Empty;

        #endregion

        #region _____________________________| UNITY

        private void Awake() => CacheMissingReferences();

        private void OnValidate() => CacheMissingReferences();

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
            _ResolvedCombatSceneName = string.Empty;
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

            if (_MapSceneCatalog == null)
            {
                Debug.LogError($"{nameof(QuickMatchSceneHandoff)} cannot load combat: map scene catalog is missing.", this);
                return;
            }

            if (!_MapSceneCatalog.TryResolveScene(_MatchContext.Manifest.MapId, out _ResolvedCombatSceneName, out string lFailure))
            {
                Debug.LogError($"{nameof(QuickMatchSceneHandoff)} cannot load combat: {lFailure}", this);
                return;
            }

            if (SceneManager.GetActiveScene().name == _ResolvedCombatSceneName)
                return;

            _HasRequestedLoad = true;
            _LoadRoutine = StartCoroutine(LoadCombatSceneRoutine());
        }

        private IEnumerator LoadCombatSceneRoutine()
        {
            if (_LogEvents)
                Debug.Log($"[QuickMatch Handoff] Match found. Loading {_ResolvedCombatSceneName}. MapId={_MatchContext.Manifest.MapId}, MatchId={_MatchContext.MatchId}", this);

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
        }

        private void LoadCombatScene()
        {
            if (string.IsNullOrWhiteSpace(_ResolvedCombatSceneName))
                return;

            if (_SceneTransitionController != null)
            {
                _SceneTransitionController.LoadScene(_ResolvedCombatSceneName, LoadSceneMode.Single, _LoadingMessage);
                return;
            }

            if (SceneTransitionController.TryLoadScene(_ResolvedCombatSceneName, LoadSceneMode.Single, _LoadingMessage))
                return;

            SceneManager.LoadScene(_ResolvedCombatSceneName, LoadSceneMode.Single);
        }

        #endregion
    }
}
