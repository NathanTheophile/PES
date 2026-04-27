using System.Collections.Generic;
using TacticalPort.Core.Factories;
using TacticalPort.Core.Interfaces;
using TacticalPort.Core.Runtime;
using TacticalPort.Data;
using TacticalPort.Shared;
using TacticalPort.View;
using UnityEngine;

namespace TacticalPort.Bootstrap
{
    public sealed class CombatBootstrap : MonoBehaviour
    {
        #region _____________________________| VALUES

        [SerializeField] private BattleScenarioDefinition _Scenario;
        [SerializeField] private CombatSceneReferences _SceneReferences;
        [SerializeField] private bool _BootstrapOnStart = true;
        [SerializeField] private bool _StartFirstTurnOnBootstrap = true;
        [SerializeField] private bool _AutoAdvanceTurns = true;
        [SerializeField] private bool _RefreshPresentationEachFrame = true;
        [SerializeField, Min(0f)] private float _EnemyTurnDelaySeconds = 0.15f;

        private readonly Dictionary<UnitId, UnitView> _UnitViews = new Dictionary<UnitId, UnitView>();
        private IBattleService _BattleService;
        private EnemyTurnController _EnemyTurnController;
        private bool _IsBootstrapped;

        #endregion

        #region _____________________________| ACCESSORS

        public IBattleService BattleService => _BattleService;
        public bool IsBootstrapped => _IsBootstrapped;
        public BattleTurnContext CurrentTurn => _BattleService != null ? _BattleService.CurrentTurn : null;
        public UnitRuntime ActiveUnit => _BattleService != null ? _BattleService.ActiveUnit : null;
        public HUDManager HudManager => _SceneReferences != null ? _SceneReferences.HudManager : null;
        public BoardCursorView BoardCursorView => _SceneReferences != null ? _SceneReferences.BoardCursorView : null;

        #endregion

        private void Start()
        {
            if (_BootstrapOnStart)
                BootstrapCombat();
        }

        private void Update()
        {
            _EnemyTurnController?.Tick(Time.deltaTime);

            if (_RefreshPresentationEachFrame && _IsBootstrapped)
                RefreshPresentation();
        }

        public void BootstrapCombat()
        {
            if (_SceneReferences == null)
            {
                Debug.LogError("CombatBootstrap requires scene references.", this);
                return;
            }

            BattleScenarioDefinition lActiveScenario = ResolveScenario();
            if (lActiveScenario == null)
            {
                Debug.LogError("Combat bootstrap requires a scenario definition.", this);
                return;
            }

            if (!lActiveScenario.TryValidate(out string lFailureReason))
            {
                Debug.LogError($"Failed to bootstrap combat: {lFailureReason}", this);
                _SceneReferences?.HudManager?.SetStatus(lFailureReason);

                return;
            }

            _BattleService = BattleRuntimeCompositionRoot.CreateDefaultBattleService();
            _BattleService.Initialize(lActiveScenario);
            _EnemyTurnController = new EnemyTurnController(this, _EnemyTurnDelaySeconds);

            if (_StartFirstTurnOnBootstrap)
                TryAdvanceBattle();

            if (_SceneReferences != null)
            {
                _SceneReferences.BoardView?.Configure(lActiveScenario);
                _SceneReferences.HudManager?.Bind(_BattleService);
                _SceneReferences.HudManager?.SetStatus(ResolveBootstrapStatus(lActiveScenario));
            }

            RebuildUnitViews(lActiveScenario);
            _IsBootstrapped = true;
            RefreshPresentation();
        }

        public bool TryAdvanceBattle()
        {
            if (_BattleService == null || _BattleService.Outcome != BattleOutcome.None || _BattleService.CurrentTurn != null)
                return false;

            if (!_BattleService.TryStartNextTurn(out BattleTurnContext lTurnContext))
            {
                RefreshPresentation();
                return false;
            }

            if (_SceneReferences != null)
                _SceneReferences.HudManager?.SetStatus($"Turn started for {ResolveUnitLabel(lTurnContext.UnitId)}.");

            RefreshPresentation();
            return true;
        }

        public bool TryGetActiveUnit(out UnitRuntime pUnit)
        {
            pUnit = ActiveUnit;
            return pUnit != null;
        }

        public IReadOnlyCollection<GridCoord> GetReachableCellsForActiveUnit()
        {
            if (_BattleService == null || !_BattleService.TryGetActiveUnit(out UnitRuntime lUnit))
                return System.Array.Empty<GridCoord>();

            return _BattleService.GetReachableCells(lUnit.Id);
        }

        public BattleActionResult ValidateActiveSkill(SkillId pSkillId, SkillTarget pTarget)
        {
            if (_BattleService == null || !_BattleService.TryGetActiveUnit(out UnitRuntime lUnit))
                return BattleActionResult.Failed(BattleActionType.Skill, "No active unit is available.");

            return _BattleService.ValidateSkill(lUnit.Id, pSkillId, pTarget);
        }

        public BattleActionResult MoveActiveUnit(GridCoord pDestination)
        {
            if (_BattleService == null || !_BattleService.TryGetActiveUnit(out UnitRuntime lUnit))
                return BattleActionResult.Failed(BattleActionType.Move, "No active unit is available.");

            BattleActionResult lResult = _BattleService.MoveUnit(lUnit.Id, pDestination);
            ApplyActionFeedback(lResult);
            return lResult;
        }

        public BattleActionResult UseActiveUnitSkill(SkillId pSkillId, SkillTarget pTarget)
        {
            if (_BattleService == null || !_BattleService.TryGetActiveUnit(out UnitRuntime lUnit))
                return BattleActionResult.Failed(BattleActionType.Skill, "No active unit is available.");

            BattleActionResult lResult = _BattleService.UseSkill(lUnit.Id, pSkillId, pTarget);
            ApplyActionFeedback(lResult);
            return lResult;
        }

        public BattleActionResult EndActiveTurn(string pStatusOverride = null)
        {
            if (_BattleService == null || !_BattleService.TryGetActiveUnit(out UnitRuntime lUnit))
                return BattleActionResult.Failed(BattleActionType.EndTurn, "No active unit is available.");

            BattleActionResult lResult = _BattleService.EndTurn(lUnit.Id);
            ApplyActionFeedback(lResult);

            if (lResult.IsSuccess && _AutoAdvanceTurns)
                TryAdvanceBattle();

            ApplyStatusOverride(pStatusOverride);
            return lResult;
        }

        public void RefreshPresentation()
        {
            if (!_IsBootstrapped || _BattleService == null)
                return;

            SyncRuntimeUnitViews();
            _SceneReferences?.BoardView?.SetOccupiedCells(_BattleService.Units, _BattleService.ActiveUnit != null ? _BattleService.ActiveUnit.Id : UnitId.None);
            _SceneReferences?.BoardView?.SetGlyphCells(_BattleService.GetActiveGlyphs());

            foreach (UnitView lUnitView in _UnitViews.Values)
            {
                if (lUnitView == null)
                    continue;

                lUnitView.Refresh();
            }

            RefreshSelectionCursor();
            _SceneReferences?.HudManager?.Refresh();
        }

        private BattleScenarioDefinition ResolveScenario()
        {
            if (_SceneReferences != null && _SceneReferences.ScenarioOverride != null)
                return _SceneReferences.ScenarioOverride;
            return _Scenario;
        }

        private void RebuildUnitViews(BattleScenarioDefinition pActiveScenario)
        {
            ClearUnitViews();

            for (int lIndex = 0; lIndex < pActiveScenario.Units.Count; lIndex++)
            {
                UnitId lUnitId = new UnitId(lIndex + 1);
                if (!_BattleService.TryGetUnit(lUnitId, out var lRuntimeUnit))
                    continue;

                if (!pActiveScenario.TryGetSpawn(lUnitId, out UnitSpawnDefinition lSpawn) || lSpawn == null)
                    continue;

                UnitView lPrefab = ResolvePrefab(lSpawn.Unit);
                if (lPrefab == null)
                {
                    string lUnitLabel = lSpawn.Unit != null ? lSpawn.Unit.DisplayName : lUnitId.ToString();
                    Debug.LogError($"Cannot create UnitView for '{lUnitLabel}': no UnitView prefab is assigned on the unit definition or CombatSceneReferences.", this);
                    continue;
                }

                Transform lParent = _SceneReferences != null ? _SceneReferences.UnitRoot : transform;
                UnitView lInstance = Instantiate(lPrefab, lParent);
                lInstance.Bind(lRuntimeUnit, lSpawn.Unit, _SceneReferences != null ? _SceneReferences.BoardView : null);
                _UnitViews[lUnitId] = lInstance;
            }
        }

        private UnitView ResolvePrefab(UnitDefinition pUnitDefinition)
        {
            if (pUnitDefinition != null && pUnitDefinition.UnitViewPrefab != null)
                return pUnitDefinition.UnitViewPrefab;

            if (_SceneReferences != null && _SceneReferences.DefaultUnitViewPrefab != null)
                return _SceneReferences.DefaultUnitViewPrefab;

            return null;
        }

        private void RefreshSelectionCursor()
        {
            BoardCursorView lBoardCursorView = BoardCursorView;
            if (lBoardCursorView == null)
                return;

            bool lCanShowSelection =
                _IsBootstrapped
                && _BattleService != null
                && _BattleService.Outcome == BattleOutcome.None
                && _BattleService.CurrentTurn != null
                && _BattleService.ActiveUnit != null;

            if (!lCanShowSelection)
            {
                lBoardCursorView.SetSelection(default, false);
                return;
            }

            lBoardCursorView.SetSelection(_BattleService.ActiveUnit.Position, true);
        }

        private void ClearUnitViews()
        {
            foreach (UnitView lUnitView in _UnitViews.Values)
            {
                if (lUnitView == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(lUnitView.gameObject);
                else
                    DestroyImmediate(lUnitView.gameObject);
            }

            _UnitViews.Clear();
        }

        private void SyncRuntimeUnitViews()
        {
            if (_BattleService == null)
                return;

            foreach (UnitRuntime lRuntimeUnit in _BattleService.Units)
            {
                if (lRuntimeUnit == null || _UnitViews.ContainsKey(lRuntimeUnit.Id))
                    continue;

                UnitView lPrefab = ResolvePrefab(lRuntimeUnit.Definition);
                if (lPrefab == null)
                    continue;

                Transform lParent = _SceneReferences != null ? _SceneReferences.UnitRoot : transform;
                UnitView lInstance = Instantiate(lPrefab, lParent);
                lInstance.Bind(lRuntimeUnit, lRuntimeUnit.Definition, _SceneReferences != null ? _SceneReferences.BoardView : null);
                _UnitViews[lRuntimeUnit.Id] = lInstance;
            }
        }

        private void ApplyActionFeedback(BattleActionResult pResult)
        {
            if (_SceneReferences != null && pResult != null)
                _SceneReferences.HudManager?.SetStatus(pResult.Message);

            RefreshPresentation();
        }

        private void ApplyStatusOverride(string pStatusOverride)
        {
            if (_SceneReferences == null || string.IsNullOrWhiteSpace(pStatusOverride))
                return;

            if (_BattleService != null && _BattleService.CurrentTurn != null)
            {
                _SceneReferences.HudManager?.SetStatus($"{pStatusOverride} Next turn: {ResolveUnitLabel(_BattleService.CurrentTurn.UnitId)}.");
                return;
            }

            _SceneReferences.HudManager?.SetStatus(pStatusOverride);
        }

        private string ResolveUnitLabel(UnitId pUnitId)
        {
            if (_BattleService != null && _BattleService.TryGetUnit(pUnitId, out UnitRuntime lUnit))
                return lUnit.Definition.DisplayName;

            return pUnitId.ToString();
        }

        private string ResolveBootstrapStatus(BattleScenarioDefinition pScenario)
        {
            string lScenarioName = pScenario != null ? pScenario.DisplayName : "Unknown";

            if (_BattleService != null && _BattleService.TryGetActiveUnit(out UnitRuntime lUnit))
                return $"Scenario '{lScenarioName}' initialized. Active turn: {lUnit.Definition.DisplayName}.";

            return $"Scenario '{lScenarioName}' initialized.";
        }
    }
}
