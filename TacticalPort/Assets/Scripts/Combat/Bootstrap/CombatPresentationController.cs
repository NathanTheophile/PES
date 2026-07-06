#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Bootstrap
#endregion

using System.Collections.Generic;
using TacticalPort.Combat;
using TacticalPort.Core;
using TacticalPort.Data;
using TacticalPort.Shared;
using TacticalPort.View;
using UnityEngine;

namespace TacticalPort.Bootstrap
{
    internal interface ICombatPresentationContext
    {
        IBattleService BattleService { get; }
        CombatSceneReferences SceneReferences { get; }
        Transform UnitViewParent { get; }
        bool IsBootstrapped { get; }
        bool IsPlacementPhaseActive { get; }

        bool TryGetLocalPlayerSlot(out MatchPlayerSlot pSlot);
        bool CanLocalPlayerControlUnit(UnitRuntime pUnit);
        bool TryGetPlacementSlotForTeam(Team pTeam, out MatchPlayerSlot pSlot);
        CombatTeamRelation GetLocalTeamRelation(UnitRuntime pUnit);
        UnitView ResolveUnitViewPrefab(UnitDefinition pUnitDefinition);
        void LogMissingUnitViewPrefab(string pUnitLabel);
    }

    internal sealed class CombatPresentationController
    {
        #region _____________________________/ VALUES

        private readonly ICombatPresentationContext _Context;
        private readonly Dictionary<UnitId, UnitView> _UnitViews = new Dictionary<UnitId, UnitView>();
        private readonly List<UnitRuntime> _VisibleUnitsScratch = new List<UnitRuntime>();

        #endregion

        #region _____________________________| INIT

        public CombatPresentationController(ICombatPresentationContext pContext)
        {
            _Context = pContext;
        }

        #endregion

        #region _____________________________| DISPLAY

        public void RefreshPresentation()
        {
            if (!_Context.IsBootstrapped || _Context.BattleService == null)
                return;

            SyncRuntimeUnitViews();
            BoardView lBoardView = _Context.SceneReferences != null ? _Context.SceneReferences.BoardView : null;
            lBoardView?.BeginCellStateUpdate();
            try
            {
                if (_Context.TryGetLocalPlayerSlot(out MatchPlayerSlot lLocalPlayerSlot))
                {
                    lBoardView?.SetLocalPlayerSlot(lLocalPlayerSlot);
                    _Context.SceneReferences?.HudManager?.SetLocalPlayerSlot(lLocalPlayerSlot);
                }

                lBoardView?.SetSpawnerCellsVisible(_Context.IsPlacementPhaseActive);
                lBoardView?.SetOccupiedCells(ResolveVisibleUnitsForPresentation(), _Context.BattleService.ActiveUnit != null ? _Context.BattleService.ActiveUnit.Id : UnitId.None);
                lBoardView?.SetGlyphCells(_Context.BattleService.GetActiveGlyphs());
                lBoardView?.SetHazardCells(_Context.BattleService.GetTelegraphedHazards());
            }
            finally
            {
                lBoardView?.EndCellStateUpdate();
            }

            foreach (UnitView lUnitView in _UnitViews.Values)
            {
                if (lUnitView == null)
                    continue;

                if (_Context.BattleService.TryGetUnit(lUnitView.UnitId, out UnitRuntime lRuntimeUnit))
                    lUnitView.gameObject.SetActive(ShouldDisplayUnit(lRuntimeUnit));

                if (!lUnitView.gameObject.activeSelf)
                    continue;

                lUnitView.Refresh();
            }

            RefreshSelectionCursor();
            _Context.SceneReferences?.HudManager?.Refresh();
        }

        public void RebuildUnitViews(BattleScenarioDefinition pActiveScenario)
        {
            ClearUnitViews();
            if (pActiveScenario == null || _Context.BattleService == null)
                return;

            for (int lIndex = 0; lIndex < pActiveScenario.Units.Count; lIndex++)
            {
                UnitId lUnitId = new UnitId(lIndex + 1);
                if (!_Context.BattleService.TryGetUnit(lUnitId, out var lRuntimeUnit))
                    continue;

                if (!pActiveScenario.TryGetSpawn(lUnitId, out UnitSpawnDefinition lSpawn) || lSpawn == null)
                    continue;

                UnitView lPrefab = _Context.ResolveUnitViewPrefab(lSpawn.Unit);
                if (lPrefab == null)
                {
                    string lUnitLabel = lSpawn.Unit != null ? lSpawn.Unit.DisplayName : lUnitId.ToString();
                    _Context.LogMissingUnitViewPrefab(lUnitLabel);
                    continue;
                }

                UnitView lInstance = Object.Instantiate(lPrefab, _Context.UnitViewParent);
                lInstance.Bind(lRuntimeUnit, lSpawn.Unit, _Context.SceneReferences != null ? _Context.SceneReferences.BoardView : null);
                _UnitViews[lUnitId] = lInstance;
            }
        }

        #endregion

        #region _____________________________| HELPERS

        private IReadOnlyCollection<UnitRuntime> ResolveVisibleUnitsForPresentation()
        {
            _VisibleUnitsScratch.Clear();
            if (_Context.BattleService?.Units == null)
                return _VisibleUnitsScratch;

            foreach (UnitRuntime lUnit in _Context.BattleService.Units)
            {
                if (ShouldDisplayUnit(lUnit))
                    _VisibleUnitsScratch.Add(lUnit);
            }

            return _VisibleUnitsScratch;
        }

        private bool ShouldDisplayUnit(UnitRuntime pUnit)
        {
            if (pUnit == null)
                return false;

            return !_Context.IsPlacementPhaseActive || _Context.GetLocalTeamRelation(pUnit) != CombatTeamRelation.Opponent;
        }

        private void RefreshSelectionCursor()
        {
            BoardCursorView lBoardCursorView = _Context.SceneReferences != null ? _Context.SceneReferences.BoardCursorView : null;
            if (lBoardCursorView == null)
                return;

            bool lCanShowSelection =
                _Context.IsBootstrapped
                && _Context.BattleService != null
                && _Context.BattleService.Outcome == BattleOutcome.None
                && _Context.BattleService.CurrentTurn != null
                && _Context.BattleService.ActiveUnit != null;

            if (!lCanShowSelection)
            {
                lBoardCursorView.SetSelection(default, false);
                return;
            }

            lBoardCursorView.SetSelection(_Context.BattleService.ActiveUnit.Position, true);
        }

        private void ClearUnitViews()
        {
            foreach (UnitView lUnitView in _UnitViews.Values)
            {
                if (lUnitView == null)
                    continue;

                if (Application.isPlaying)
                    Object.Destroy(lUnitView.gameObject);
                else
                    Object.DestroyImmediate(lUnitView.gameObject);
            }

            _UnitViews.Clear();
        }

        private void SyncRuntimeUnitViews()
        {
            if (_Context.BattleService == null)
                return;

            foreach (UnitRuntime lRuntimeUnit in _Context.BattleService.Units)
            {
                if (lRuntimeUnit == null || _UnitViews.ContainsKey(lRuntimeUnit.Id))
                    continue;

                UnitView lPrefab = _Context.ResolveUnitViewPrefab(lRuntimeUnit.Definition);
                if (lPrefab == null)
                    continue;

                UnitView lInstance = Object.Instantiate(lPrefab, _Context.UnitViewParent);
                lInstance.Bind(lRuntimeUnit, lRuntimeUnit.Definition, _Context.SceneReferences != null ? _Context.SceneReferences.BoardView : null);
                _UnitViews[lRuntimeUnit.Id] = lInstance;
            }
        }

        #endregion
    }
}
