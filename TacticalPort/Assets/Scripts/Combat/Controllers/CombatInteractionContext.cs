#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Bootstrap;
using TacticalPort.Core;
using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEngine;
using TacticalPort.UI;
using TacticalPort.View;

namespace TacticalPort.Combat
{
    internal sealed class CombatInteractionContext
    {
        #region _____________________________/ VALUES

        private readonly CombatBootstrap _Bootstrap;
        private readonly BoardView _BoardView;
        private readonly Camera _MainCamera;

        #endregion

        #region _____________________________/ ACCESSORS

        public CombatBootstrap Bootstrap => _Bootstrap;
        public BoardView BoardView => _BoardView;
        public Camera MainCamera => _MainCamera;
        public HUDManager HudManager => _Bootstrap != null ? _Bootstrap.HudManager : null;
        public BoardCursorView BoardCursorView => _Bootstrap != null ? _Bootstrap.BoardCursorView : null;
        public bool IsPlacementPhaseActive => _Bootstrap != null && _Bootstrap.IsPlacementPhaseActive;

        #endregion

        #region _____________________________| INIT

        public CombatInteractionContext(CombatBootstrap pBootstrap, BoardView pBoardView, Camera pMainCamera)
        {
            _Bootstrap = pBootstrap;
            _BoardView = pBoardView;
            _MainCamera = pMainCamera;
        }

        #endregion

        #region _____________________________| HELPERS

        public bool CanRun() => _Bootstrap != null
            && _Bootstrap.IsBootstrapped
            && _BoardView != null
            && _MainCamera != null;

        public bool CanPlayerIssueCommands() => _Bootstrap != null
            && _Bootstrap.IsBootstrapped
            && _Bootstrap.CurrentTurn != null
            && _Bootstrap.BattleService != null
            && _Bootstrap.BattleService.Outcome == BattleOutcome.None
            && _Bootstrap.TryGetActiveUnit(out UnitRuntime lActiveUnit)
            && lActiveUnit.Team == Team.Player;

        public bool TryGetPlayerActiveUnit(string pEnemyTurnMessage, out UnitRuntime pActiveUnit)
        {
            pActiveUnit = null;

            if (_Bootstrap == null || !_Bootstrap.TryGetActiveUnit(out pActiveUnit))
            {
                SetStatus("No active unit is available.");
                return false;
            }

            if (pActiveUnit.Team == Team.Player)
                return true;

            SetStatus($"{pEnemyTurnMessage} during {pActiveUnit.Definition.DisplayName}'s turn.");
            return false;
        }

        public bool TryResolveAliveUnitAtCell(GridCoord pCell, out UnitRuntime pUnit)
        {
            pUnit = null;

            if (_Bootstrap?.BattleService == null)
                return false;

            foreach (UnitRuntime lUnit in _Bootstrap.BattleService.Units)
            {
                if (lUnit == null || !lUnit.IsAlive)
                    continue;

                foreach (GridCoord lOccupiedCell in lUnit.EnumerateOccupiedCells())
                {
                    if (lOccupiedCell != pCell)
                        continue;

                    pUnit = lUnit;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetBoardCellDefinition(GridCoord pCell, out CellDefinition pCellDefinition)
        {
            if (_BoardView != null)
                return _BoardView.TryGetCellDefinition(pCell, out pCellDefinition);

            pCellDefinition = null;
            return false;
        }

        public void SetStatus(string pMessage) => HudManager?.SetStatus(pMessage);

        public static string ResolveRangeLabel(SkillDefinition pSkill)
        {
            if (pSkill == null)
                return "0";

            return pSkill.RangeMin == pSkill.RangeMax
                ? pSkill.RangeMax.ToString()
                : $"{pSkill.RangeMin}-{pSkill.RangeMax}";
        }

        #endregion
    }
}
