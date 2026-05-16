#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Core;
using TacticalPort.Shared;
using UnityEngine;
using TacticalPort.UI;
using TacticalPort.View;

namespace TacticalPort.Combat
{
    internal sealed class CombatHoverController
    {
        #region _____________________________/ VALUES

        private readonly CombatInteractionContext _Context;
        private bool _HasHoveredCell;
        private GridCoord _HoveredCell;

        #endregion

        #region _____________________________/ ACCESSORS

        public bool HasHoveredCell => _HasHoveredCell;
        public GridCoord HoveredCell => _HoveredCell;

        #endregion

        #region _____________________________| INIT

        public CombatHoverController(CombatInteractionContext pContext)
        {
            _Context = pContext;
        }

        #endregion

        #region _____________________________| HOVER

        public void UpdateHoveredCell(Vector2 pMouseScreenPosition, bool pIsPointerOverUi)
        {
            if (pIsPointerOverUi)
            {
                ClearHoveredCell();
                return;
            }

            if (!TryResolveHoveredCell(pMouseScreenPosition, out GridCoord lHoveredCell))
            {
                ClearHoveredCell();
                return;
            }

            _Context.HudManager?.SetMapHoveredUnit(ResolveVisibleHoveredUnit(lHoveredCell));

            if (_HasHoveredCell && _HoveredCell == lHoveredCell)
                return;

            _HasHoveredCell = true;
            _HoveredCell = lHoveredCell;
            _Context.BoardView.SetHoveredCell(_HoveredCell);
            RefreshCursor(false, null, false);
        }

        public void RefreshCursor(
            bool pIsPlacementPhaseActive,
            PlacementInteractionController pPlacementInteraction,
            bool pHasSelectedSkill)
        {
            BoardCursorView lBoardCursorView = _Context.BoardCursorView;
            if (lBoardCursorView == null)
                return;

            if (!_HasHoveredCell)
            {
                lBoardCursorView.SetHover(default, false);
                lBoardCursorView.SetTarget(default, false);
                if (pIsPlacementPhaseActive)
                {
                    UnitRuntime lSelectedPlacementUnit = null;
                    bool lHasSelectedPlacementUnit = pPlacementInteraction != null
                        && pPlacementInteraction.TryGetSelectedUnit(out lSelectedPlacementUnit);
                    lBoardCursorView.SetSelection(
                        lHasSelectedPlacementUnit ? lSelectedPlacementUnit.Position : default,
                        lHasSelectedPlacementUnit);
                }
                return;
            }

            if (pIsPlacementPhaseActive)
            {
                UnitRuntime lSelectedUnit = null;
                bool lHasSelectedPlacementUnit = pPlacementInteraction != null
                    && pPlacementInteraction.TryGetSelectedUnit(out lSelectedUnit);
                lBoardCursorView.SetHover(_HoveredCell, true);
                lBoardCursorView.SetTarget(_HoveredCell, lHasSelectedPlacementUnit);
                lBoardCursorView.SetSelection(
                    lHasSelectedPlacementUnit ? lSelectedUnit.Position : default,
                    lHasSelectedPlacementUnit);
                return;
            }

            lBoardCursorView.SetHover(_HoveredCell, !pHasSelectedSkill);
            lBoardCursorView.SetTarget(_HoveredCell, pHasSelectedSkill);
        }

        #endregion

        #region _____________________________| HELPERS

        private bool TryResolveHoveredCell(Vector2 pMouseScreenPosition, out GridCoord pCoord)
        {
            pCoord = default;

            if (_Context.BoardView == null || _Context.MainCamera == null)
                return false;

            Ray lRay = _Context.MainCamera.ScreenPointToRay(pMouseScreenPosition);
            if (_Context.BoardView.TryGetGridCoord(lRay, out pCoord))
                return true;

            float lBoardZ = _Context.BoardView.transform.position.z;
            float lCameraDistance = Mathf.Abs(lBoardZ - _Context.MainCamera.transform.position.z);
            Vector3 lWorldPosition = _Context.MainCamera.ScreenToWorldPoint(new Vector3(pMouseScreenPosition.x, pMouseScreenPosition.y, lCameraDistance));
            lWorldPosition.z = lBoardZ;

            return _Context.BoardView.TryGetGridCoord(lWorldPosition, out pCoord);
        }

        private UnitRuntime ResolveVisibleHoveredUnit(GridCoord pCell)
        {
            if (!_Context.TryResolveAliveUnitAtCell(pCell, out UnitRuntime lHoveredUnit))
                return null;

            return _Context.IsPlacementPhaseActive
                   && _Context.Bootstrap.GetLocalTeamRelation(lHoveredUnit) == CombatTeamRelation.Opponent
                ? null
                : lHoveredUnit;
        }

        private void ClearHoveredCell()
        {
            _Context.HudManager?.SetMapHoveredUnit(null);

            if (!_HasHoveredCell)
                return;

            _HasHoveredCell = false;
            _Context.BoardView.ClearHoveredCell();
            RefreshCursor(false, null, false);
        }

        #endregion
    }
}
