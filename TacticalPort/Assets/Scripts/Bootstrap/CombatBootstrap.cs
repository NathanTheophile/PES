#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

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
        #region _____________________________/ VALUES

        [SerializeField] private BattleScenarioDefinition _Scenario;
        [SerializeField] private CombatSceneReferences _SceneReferences;
        [SerializeField] private bool _BootstrapOnStart = true;
        [SerializeField] private bool _UsePlacementPhase = true;
        [SerializeField] private bool _StartFirstTurnOnBootstrap = true;
        [SerializeField] private bool _AutoAdvanceTurns = true;
        [SerializeField] private bool _RefreshPresentationEachFrame = true;
        [SerializeField, Min(0f)] private float _EnemyTurnDelaySeconds = 0.15f;

        private readonly Dictionary<UnitId, UnitView> _UnitViews = new Dictionary<UnitId, UnitView>();
        private IBattleService _BattleService;
        private EnemyTurnController _EnemyTurnController;
        private bool _IsBootstrapped;

        #endregion

        #region _____________________________/ ACCESSORS

        public IBattleService BattleService => _BattleService;
        public bool IsBootstrapped => _IsBootstrapped;
        public BattleTurnContext CurrentTurn => _BattleService != null ? _BattleService.CurrentTurn : null;
        public UnitRuntime ActiveUnit => _BattleService != null ? _BattleService.ActiveUnit : null;
        public HUDManager HudManager => _SceneReferences != null ? _SceneReferences.HudManager : null;
        public BoardCursorView BoardCursorView => _SceneReferences != null ? _SceneReferences.BoardCursorView : null;
        public bool IsPlacementPhaseActive => _BattleService != null && _BattleService.Phase == BattlePhase.Placement;

        #endregion

        #region _____________________________| UNITY

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

        #endregion

        #region _____________________________| BOOTSTRAP

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

            if (_SceneReferences != null)
            {
                _SceneReferences.BoardView?.Configure(lActiveScenario);
                _SceneReferences.HudManager?.Bind(_BattleService);
            }

            RebuildUnitViews(lActiveScenario);
            _IsBootstrapped = true;

            if (_UsePlacementPhase)
            {
                _BattleService.EnterPlacementPhase();
                _SceneReferences.HudManager?.SetStatus("Placement phase: select a player unit, choose an empty spawn cell, then press Ready.");
            }
            else if (_StartFirstTurnOnBootstrap)
            {
                TryAdvanceBattle();
            }
            else
            {
                _SceneReferences.HudManager?.SetStatus(ResolveBootstrapStatus(lActiveScenario));
            }

            RefreshPresentation();
        }

        #endregion

        #region _____________________________| ACTIONS

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

        public bool StartCombatFromPlacement()
        {
            if (!IsPlacementPhaseActive)
                return false;

            _SceneReferences?.HudManager?.SetStatus("Combat started.");
            return TryAdvanceBattle();
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

        public BattleActionResult RepositionUnitDuringPlacement(UnitId pUnitId, GridCoord pDestination)
        {
            if (!IsPlacementPhaseActive)
                return BattleActionResult.Failed(BattleActionType.Placement, "Placement phase is not active.");

            if (_BattleService == null || !_BattleService.TryGetUnit(pUnitId, out UnitRuntime lUnit) || lUnit == null || !lUnit.IsAlive)
                return BattleActionResult.Failed(BattleActionType.Placement, "Selected unit is not available.");

            if (lUnit.Team != Team.Player)
                return BattleActionResult.Failed(BattleActionType.Placement, "Only player units can be placed.");

            if (_SceneReferences?.BoardView == null || !_SceneReferences.BoardView.IsSpawnerCell(pDestination))
                return BattleActionResult.Failed(BattleActionType.Placement, "Choose an empty spawn cell.");

            foreach (UnitRuntime lRuntimeUnit in _BattleService.Units)
            {
                if (lRuntimeUnit == null || !lRuntimeUnit.IsAlive || lRuntimeUnit.Id == pUnitId)
                    continue;

                foreach (GridCoord lOccupiedCell in lRuntimeUnit.EnumerateOccupiedCells())
                {
                    if (lOccupiedCell == pDestination)
                        return BattleActionResult.Failed(BattleActionType.Placement, "This spawn cell is already occupied.");
                }
            }

            BattleActionResult lResult = _BattleService.RepositionUnitDuringPlacement(pUnitId, pDestination);
            ApplyActionFeedback(lResult);
            return lResult;
        }

        #endregion

        #region _____________________________| DISPLAY

        public void RefreshPresentation()
        {
            if (!_IsBootstrapped || _BattleService == null)
                return;

            SyncRuntimeUnitViews();
            _SceneReferences?.BoardView?.SetSpawnerCellsVisible(IsPlacementPhaseActive);
            _SceneReferences?.BoardView?.SetOccupiedCells(_BattleService.Units, _BattleService.ActiveUnit != null ? _BattleService.ActiveUnit.Id : UnitId.None);
            _SceneReferences?.BoardView?.SetGlyphCells(_BattleService.GetActiveGlyphs());
            _SceneReferences?.BoardView?.SetHazardCells(_BattleService.GetTelegraphedHazards());

            foreach (UnitView lUnitView in _UnitViews.Values)
            {
                if (lUnitView == null)
                    continue;

                lUnitView.Refresh();
            }

            RefreshSelectionCursor();
            _SceneReferences?.HudManager?.Refresh();
        }

        #endregion

        #region _____________________________| HELPERS

        private BattleScenarioDefinition ResolveScenario() =>
            _SceneReferences != null ? _SceneReferences.ResolveScenario(_Scenario) : _Scenario;

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

        private UnitView ResolvePrefab(UnitDefinition pUnitDefinition) =>
            pUnitDefinition != null && pUnitDefinition.UnitViewPrefab != null
                ? pUnitDefinition.UnitViewPrefab
                : _SceneReferences != null && _SceneReferences.DefaultUnitViewPrefab != null
                    ? _SceneReferences.DefaultUnitViewPrefab
                    : null;

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

        private string ResolveUnitLabel(UnitId pUnitId) =>
            _BattleService != null && _BattleService.TryGetUnit(pUnitId, out UnitRuntime lUnit)
                ? lUnit.Definition.DisplayName
                : pUnitId.ToString();

        private string ResolveBootstrapStatus(BattleScenarioDefinition pScenario)
        {
            string lScenarioName = pScenario != null ? pScenario.DisplayName : "Unknown";

            if (_BattleService != null && _BattleService.TryGetActiveUnit(out UnitRuntime lUnit))
                return $"Scenario '{lScenarioName}' initialized. Active turn: {lUnit.Definition.DisplayName}.";

            return $"Scenario '{lScenarioName}' initialized.";
        }

        #endregion
    }
}
