#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Data;
using UnityEngine;

namespace TacticalPort.View
{
    public sealed class CombatSceneReferences : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private BoardView _BoardView;
        [SerializeField] private BoardAuthoring _TilemapBoardAuthoring;
        [SerializeField] private BoardCursorView _BoardCursorView;
        [SerializeField] private HUDManager _HudManager;
        [SerializeField] private Transform _UnitRoot;
        [SerializeField] private UnitView _DefaultUnitViewPrefab;
        [SerializeField] private BattleScenarioDefinition _ScenarioOverride;

        #endregion

        #region _____________________________/ ACCESSORS

        public BoardView BoardView => _BoardView;
        public BoardAuthoring BoardAuthoring => _TilemapBoardAuthoring;
        public BoardCursorView BoardCursorView => _BoardCursorView;
        public HUDManager HudManager => _HudManager;
        public Transform UnitRoot => _UnitRoot != null ? _UnitRoot : transform;
        public UnitView DefaultUnitViewPrefab => _DefaultUnitViewPrefab;
        public BattleScenarioDefinition ScenarioOverride => _ScenarioOverride;

        #endregion

        #region _____________________________| UNITY

        private void Awake() => CacheMissingReferences();

        private void OnValidate() => CacheMissingReferences();

        #endregion

        #region _____________________________| RESOLVE

        public BattleScenarioDefinition ResolveScenario(BattleScenarioDefinition pFallbackScenario = null) =>
            _ScenarioOverride != null
                ? _ScenarioOverride
                : _TilemapBoardAuthoring != null
                    ? _TilemapBoardAuthoring.BuildScenario()
                    : pFallbackScenario;

        #endregion

        #region _____________________________| HELPERS

        private void CacheMissingReferences()
        {
            if (_TilemapBoardAuthoring == null && _BoardView != null)
                _TilemapBoardAuthoring = _BoardView.GetComponent<BoardAuthoring>();

            _TilemapBoardAuthoring ??= GetComponentInChildren<BoardAuthoring>(true);
        }

        #endregion
    }
}
