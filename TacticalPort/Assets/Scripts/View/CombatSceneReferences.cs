using TacticalPort.Data;
using UnityEngine;

namespace TacticalPort.View
{
    public sealed class CombatSceneReferences : MonoBehaviour
    {
        #region _____________________________| VALUES

        [SerializeField] private BoardView _BoardView;
        [SerializeField] private SceneBoardAuthoring _SceneBoardAuthoring;
        [SerializeField] private TilemapBoardAuthoring _TilemapBoardAuthoring;
        [SerializeField] private BoardCursorView _BoardCursorView;
        [SerializeField] private HUDManager _HudManager;
        [SerializeField] private Transform _UnitRoot;
        [SerializeField] private UnitView _DefaultUnitViewPrefab;
        [SerializeField] private BattleScenarioDefinition _ScenarioOverride;

        #endregion

        #region _____________________________| ACCESSORS

        public BoardView BoardView => _BoardView;
        public BoardCursorView BoardCursorView => _BoardCursorView;
        public HUDManager HudManager => _HudManager;
        public Transform UnitRoot => _UnitRoot != null ? _UnitRoot : transform;
        public UnitView DefaultUnitViewPrefab => _DefaultUnitViewPrefab;
        public BattleScenarioDefinition ScenarioOverride => ResolveScenarioOverride();

        #endregion

        private void Awake() => CacheMissingReferences();

        private void OnValidate() => CacheMissingReferences();

        private BattleScenarioDefinition ResolveScenarioOverride()
        {
            if (_TilemapBoardAuthoring != null)
            {
                BattleScenarioDefinition lTilemapScenario = _TilemapBoardAuthoring.BuildScenario();
                if (lTilemapScenario != null)
                    return lTilemapScenario;
            }

            if (_SceneBoardAuthoring != null)
            {
                BattleScenarioDefinition lBoardScenario = _SceneBoardAuthoring.BuildScenario();
                if (lBoardScenario != null)
                    return lBoardScenario;
            }

            return _ScenarioOverride;
        }

        private void CacheMissingReferences()
        {
            if (_TilemapBoardAuthoring == null && _BoardView != null)
                _TilemapBoardAuthoring = _BoardView.GetComponent<TilemapBoardAuthoring>();

            if (_SceneBoardAuthoring == null && _BoardView != null)
                _SceneBoardAuthoring = _BoardView.GetComponent<SceneBoardAuthoring>();

            _TilemapBoardAuthoring ??= GetComponentInChildren<TilemapBoardAuthoring>(true);
            _SceneBoardAuthoring ??= GetComponentInChildren<SceneBoardAuthoring>(true);
        }
    }
}
