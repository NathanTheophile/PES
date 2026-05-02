#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Data;
using UnityEngine;
using TacticalPort.UI;
using TacticalPort.View;

namespace TacticalPort.Combat
{
    public sealed class CombatSceneReferences : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private BoardView _BoardView;
        [SerializeField] private BoardAuthoring3D _Board3DAuthoring;
        [SerializeField] private BoardCursorView _BoardCursorView;
        [SerializeField] private HUDManager _HudManager;
        [SerializeField] private Transform _UnitRoot;
        [SerializeField] private UnitView _DefaultUnitViewPrefab;
        [SerializeField] private BattleScenarioDefinition _ScenarioOverride;

        #endregion

        #region _____________________________/ ACCESSORS

        public BoardView BoardView => _BoardView;
        public BoardAuthoring3D Board3DAuthoring => _Board3DAuthoring;
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

        public BattleScenarioDefinition ResolveScenario(BattleScenarioDefinition pFallbackScenario = null)
        {
            if (_ScenarioOverride != null)
                return _ScenarioOverride;

            return _Board3DAuthoring != null && _Board3DAuthoring.HasTiles
                ? _Board3DAuthoring.BuildScenario()
                : pFallbackScenario;
        }

        #endregion

        #region _____________________________| HELPERS

        private void CacheMissingReferences()
        {
            if (_Board3DAuthoring == null && _BoardView != null)
                _Board3DAuthoring = _BoardView.GetComponent<BoardAuthoring3D>();

            _Board3DAuthoring ??= GetComponentInChildren<BoardAuthoring3D>(true);
        }

        #endregion
    }
}
