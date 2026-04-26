using System.Collections.Generic;
using TacticalPort.Bootstrap;
using TacticalPort.Core.Runtime;
using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TacticalPort.View
{
    public sealed class CombatPlayerController : MonoBehaviour
    {
        #region _____________________________| VALUES

        [SerializeField] private CombatBootstrap _Bootstrap;
        [SerializeField] private BoardView _BoardView;
        [SerializeField] private Camera _MainCamera;

        private readonly HashSet<GridCoord> _ReachableCells = new HashSet<GridCoord>();
        private bool _HasHoveredCell;
        private GridCoord _HoveredCell;
        private BattleUnitId _CachedActiveUnitId = BattleUnitId.None;
        private SkillDefinition _SelectedSkill;
        private SkillId _SelectedSkillId = SkillId.None;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            ValidateReferences();
        }

        private void Update()
        {
            if (!CanRun())
                return;

            Vector2 lMouseScreenPosition = ReadMouseScreenPosition();
            SyncInteractionState(lMouseScreenPosition);
            HandleSkillCancelShortcut();
            HandleEndTurnShortcut();
            HandleMouseClick();
        }

        #endregion

        #region _____________________________| INPUT

        private HUDManager HudManager => _Bootstrap != null ? _Bootstrap.HudManager : null;
        private BoardCursorView BoardCursorView => _Bootstrap != null ? _Bootstrap.BoardCursorView : null;

        public void OnEndTurnButtonClicked() => TryEndTurn();

        public void OnSkillButtonClicked(int pSkillSlotIndex) => TrySelectSkill(pSkillSlotIndex);

        public void OnCancelSkillButtonClicked() => CancelSkillSelection("Skill selection canceled.");

        private void HandleEndTurnShortcut()
        {
            if (Keyboard.current == null || !Keyboard.current.spaceKey.wasPressedThisFrame)
                return;

            TryEndTurn();
        }

        private void HandleSkillCancelShortcut()
        {
            if (!HasSelectedSkill())
                return;

            bool lEscapePressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            bool lRightClickPressed = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;

            if (!lEscapePressed && !lRightClickPressed)
                return;

            CancelSkillSelection("Skill selection canceled.");
        }

        private void HandleMouseClick()
        {
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
                return;

            if (IsPointerOverUi())
                return;

            if (HasSelectedSkill())
            {
                TryUseSelectedSkill();
                return;
            }

            TryMoveHoveredCell();
        }

        #endregion

        #region _____________________________| INTERACTION

        private void TryMoveHoveredCell()
        {
            if (!TryGetPlayerActiveUnit("Manual movement is disabled", out _))
                return;

            if (!_HasHoveredCell)
            {
                SetStatus("Point to a cell to move the active unit.");
                return;
            }

            if (!_ReachableCells.Contains(_HoveredCell))
            {
                SetStatus($"Cell {_HoveredCell} is not reachable this turn.");
                return;
            }

            _Bootstrap.MoveActiveUnit(_HoveredCell);
            RefreshInteractionState();
        }

        private void TrySelectSkill(int pSkillSlotIndex)
        {
            if (!TryGetPlayerActiveUnit("Skills are disabled", out BattleUnitRuntime lActiveUnit))
                return;

            if (pSkillSlotIndex < 0 || pSkillSlotIndex >= lActiveUnit.Skills.Count)
            {
                SetStatus("This skill slot is empty.");
                return;
            }

            SkillDefinition lSkill = lActiveUnit.Skills[pSkillSlotIndex];
            if (lSkill == null)
            {
                SetStatus("This skill slot is empty.");
                return;
            }

            SkillId lSkillId = new SkillId(lSkill.Id);
            if (_SelectedSkillId == lSkillId)
            {
                CancelSkillSelection("Skill selection canceled.");
                return;
            }

            if (lSkill.TargetType == SkillTargetType.Self)
            {
                BattleActionResult lSelfResult = _Bootstrap.UseActiveUnitSkill(lSkillId, SkillTarget.ForSelf(lActiveUnit.Id));
                if (lSelfResult.IsSuccess)
                    CancelSkillSelection(null);

                RefreshInteractionState();
                return;
            }

            _SelectedSkill = lSkill;
            _SelectedSkillId = lSkillId;
            RefreshInteractionState();
            SetStatus($"Selected skill: {lSkill.DisplayName}.");
        }

        private void TryUseSelectedSkill()
        {
            if (!TryGetPlayerActiveUnit("Skills are disabled", out BattleUnitRuntime lActiveUnit))
                return;

            if (!HasSelectedSkill())
            {
                SetStatus("No skill is selected.");
                return;
            }

            if (!TryBuildSelectedSkillTarget(lActiveUnit, out SkillTarget lTarget, out string lFailureReason))
            {
                SetStatus(lFailureReason);
                return;
            }

            BattleActionResult lValidation = _Bootstrap.ValidateActiveSkill(_SelectedSkillId, lTarget);
            if (!lValidation.IsSuccess)
            {
                SetStatus(lValidation.Message);
                return;
            }

            BattleActionResult lResult = _Bootstrap.UseActiveUnitSkill(_SelectedSkillId, lTarget);
            if (lResult.IsSuccess)
                CancelSkillSelection(null);

            RefreshInteractionState();
        }

        private void TryEndTurn()
        {
            if (!CanRun())
                return;

            if (!TryGetPlayerActiveUnit("Turn input is disabled", out _))
                return;

            CancelSkillSelection(null);
            _Bootstrap.EndActiveTurn();
            RefreshInteractionState();
        }

        private void UpdateHoveredCell(Vector2 pMouseScreenPosition)
        {
            if (IsPointerOverUi())
            {
                ClearHoveredCell();
                return;
            }

            if (!TryResolveHoveredCell(pMouseScreenPosition, out GridCoord lHoveredCell))
            {
                ClearHoveredCell();
                return;
            }

            if (_HasHoveredCell && _HoveredCell == lHoveredCell)
                return;

            _HasHoveredCell = true;
            _HoveredCell = lHoveredCell;
            _BoardView.SetHoveredCell(_HoveredCell);
            RefreshHoverCursor();
        }

        private bool TryResolveHoveredCell(Vector2 pMouseScreenPosition, out GridCoord pCoord)
        {
            pCoord = default;

            float lBoardZ = _BoardView.transform.position.z;
            float lCameraDistance = Mathf.Abs(lBoardZ - _MainCamera.transform.position.z);
            Vector3 lWorldPosition = _MainCamera.ScreenToWorldPoint(new Vector3(pMouseScreenPosition.x, pMouseScreenPosition.y, lCameraDistance));
            lWorldPosition.z = lBoardZ;

            return _BoardView.TryGetGridCoord(lWorldPosition, out pCoord);
        }

        private void ClearHoveredCell()
        {
            if (!_HasHoveredCell)
                return;

            _HasHoveredCell = false;
            _BoardView.ClearHoveredCell();
            RefreshHoverCursor();
        }

        #endregion

        #region _____________________________| SYNC

        private void SyncReachableCells()
        {
            HashSet<GridCoord> lLatestReachableCells = new HashSet<GridCoord>();

            if (HasSelectedSkill() && _Bootstrap.TryGetActiveUnit(out BattleUnitRuntime lActiveUnit))
            {
                foreach (GridCoord lCoord in EnumerateSkillHighlightCells(lActiveUnit, _SelectedSkill))
                    lLatestReachableCells.Add(lCoord);
            }
            else
            {
                foreach (GridCoord lCoord in _Bootstrap.GetReachableCellsForActiveUnit())
                    lLatestReachableCells.Add(lCoord);
            }

            if (_ReachableCells.SetEquals(lLatestReachableCells))
                return;

            _ReachableCells.Clear();

            foreach (GridCoord lCoord in lLatestReachableCells)
                _ReachableCells.Add(lCoord);

            _BoardView.SetReachableCells(_ReachableCells);
        }

        private void SyncPreviewCells(BattleUnitRuntime pActiveUnit)
        {
            if (_BoardView == null)
                return;

            if (pActiveUnit == null || !HasSelectedSkill() || !_HasHoveredCell)
            {
                _BoardView.SetPreviewCells(null);
                return;
            }

            if (!TryBuildPreviewSkillTarget(pActiveUnit, out SkillTarget lTarget))
            {
                _BoardView.SetPreviewCells(null);
                return;
            }

            BattleActionResult lValidation = _Bootstrap.ValidateActiveSkill(_SelectedSkillId, lTarget);
            if (!lValidation.IsSuccess)
            {
                _BoardView.SetPreviewCells(null);
                return;
            }

            _BoardView.SetPreviewCells(BuildSkillPreviewCells(pActiveUnit, _SelectedSkill, lTarget));
        }

        private void SyncEndTurnAvailability()
        {
            HudManager?.SetEndTurnAvailable(CanPlayerIssueCommands(), OnEndTurnButtonClicked);
        }

        private void SyncSkillAvailability()
        {
            HudManager?.SetSkillState(
                CanPlayerIssueCommands(),
                HasSelectedSkill(),
                OnSkillButtonClicked,
                OnCancelSkillButtonClicked);
        }

        private void SyncSkillMode()
        {
            HudManager?.SetSkillMode(ResolveSkillModeMessage());
        }

        private void SyncActiveUnitState()
        {
            if (!_Bootstrap.TryGetActiveUnit(out BattleUnitRuntime lActiveUnit))
            {
                _CachedActiveUnitId = BattleUnitId.None;
                CancelSkillSelection(null);
                return;
            }

            if (_CachedActiveUnitId != lActiveUnit.Id)
            {
                _CachedActiveUnitId = lActiveUnit.Id;
                CancelSkillSelection(null);
            }

            if (lActiveUnit.Team != BattleTeam.Player)
            {
                CancelSkillSelection(null);
                return;
            }

            if (!HasSelectedSkill())
                return;

            if (!lActiveUnit.TryGetSkill(_SelectedSkillId, out SkillDefinition lSkill) || lSkill == null)
            {
                CancelSkillSelection(null);
                return;
            }

            _SelectedSkill = lSkill;
        }

        #endregion

        #region _____________________________| HELPERS

        private void RefreshInteractionState() => SyncInteractionState(ReadMouseScreenPosition());

        private bool HasSelectedSkill() => _SelectedSkill != null && _SelectedSkillId.IsValid;

        private void CancelSkillSelection(string pStatusMessage)
        {
            _SelectedSkill = null;
            _SelectedSkillId = SkillId.None;
            _BoardView?.SetPreviewCells(null);
            RefreshHoverCursor();

            if (!string.IsNullOrWhiteSpace(pStatusMessage))
                SetStatus(pStatusMessage);
        }

        private void RefreshHoverCursor()
        {
            BoardCursorView lBoardCursorView = BoardCursorView;
            if (lBoardCursorView == null)
                return;

            if (!_HasHoveredCell)
            {
                lBoardCursorView.SetHover(default, false);
                lBoardCursorView.SetTarget(default, false);
                return;
            }

            bool lHasSelectedSkill = HasSelectedSkill();
            lBoardCursorView.SetHover(_HoveredCell, !lHasSelectedSkill);
            lBoardCursorView.SetTarget(_HoveredCell, lHasSelectedSkill);
        }

        private bool TryBuildSelectedSkillTarget(BattleUnitRuntime pActiveUnit, out SkillTarget pTarget, out string pFailureReason)
        {
            pTarget = null;
            pFailureReason = string.Empty;

            if (_SelectedSkill == null)
            {
                pFailureReason = "No skill is selected.";
                return false;
            }

            switch (_SelectedSkill.TargetType)
            {
                case SkillTargetType.Self:
                    pTarget = SkillTarget.ForSelf(pActiveUnit.Id);
                    return true;

                case SkillTargetType.Unit:
                    if (!_HasHoveredCell)
                    {
                        pFailureReason = "Point to a unit to use this skill.";
                        return false;
                    }

                    if (!TryResolveAliveUnitAtCell(_HoveredCell, out BattleUnitRuntime lHoveredUnit))
                    {
                        pFailureReason = "No valid unit is under the cursor.";
                        return false;
                    }

                    pTarget = SkillTarget.ForUnit(lHoveredUnit.Id);
                    return true;

                case SkillTargetType.Cell:
                    if (!_HasHoveredCell)
                    {
                        pFailureReason = "Point to a target cell to use this skill.";
                        return false;
                    }

                    pTarget = SkillTarget.ForCell(_HoveredCell);
                    return true;

                default:
                    pFailureReason = "This skill target type is not supported in the sample scene.";
                    return false;
            }
        }

        private IEnumerable<GridCoord> EnumerateSkillHighlightCells(BattleUnitRuntime pActiveUnit, SkillDefinition pSkill)
        {
            if (pActiveUnit == null || pSkill == null || _BoardView == null || _BoardView.Scenario == null)
                yield break;

            switch (pSkill.TargetType)
            {
                case SkillTargetType.Self:
                    yield return pActiveUnit.Position;
                    yield break;

                case SkillTargetType.Unit:
                case SkillTargetType.Cell:
                    foreach (BattleGridCellDefinition lCell in _BoardView.Scenario.EnumerateCells())
                    {
                        GridCoord lCoord = lCell.Coordinate.ToRuntime();
                        if (IsSkillCellInRange(pActiveUnit, pSkill, lCoord))
                            yield return lCoord;
                    }

                    yield break;
            }
        }

        private bool TryBuildPreviewSkillTarget(BattleUnitRuntime pActiveUnit, out SkillTarget pTarget)
        {
            pTarget = null;

            if (_SelectedSkill == null)
                return false;

            switch (_SelectedSkill.TargetType)
            {
                case SkillTargetType.Self:
                    pTarget = SkillTarget.ForSelf(pActiveUnit.Id);
                    return true;

                case SkillTargetType.Unit:
                    return TryResolveAliveUnitAtCell(_HoveredCell, out BattleUnitRuntime lHoveredUnit)
                        && (pTarget = SkillTarget.ForUnit(lHoveredUnit.Id)) != null;

                case SkillTargetType.Cell:
                    pTarget = SkillTarget.ForCell(_HoveredCell);
                    return true;

                default:
                    return false;
            }
        }

        private IReadOnlyCollection<GridCoord> BuildSkillPreviewCells(BattleUnitRuntime pActiveUnit, SkillDefinition pSkill, SkillTarget pTarget)
        {
            List<GridCoord> lCells = new List<GridCoord>();
            if (pActiveUnit == null || pSkill == null || pTarget == null || _BoardView == null)
                return lCells;

            GridCoord lOrigin = ResolveSkillPreviewOrigin(pActiveUnit, pTarget, _HoveredCell);
            int lSize = pSkill.AoeShape == SkillAoeShape.Single ? 0 : pSkill.AoeSize;

            for (int lOffsetY = -lSize; lOffsetY <= lSize; lOffsetY++)
            {
                for (int lOffsetX = -lSize; lOffsetX <= lSize; lOffsetX++)
                {
                    if (!GridLineOfSightUtility.IsInsideAreaShape(pSkill.AoeShape, lOffsetX, lOffsetY, lSize))
                        continue;

                    GridCoord lCandidate = new GridCoord(lOrigin.X + lOffsetX, lOrigin.Y + lOffsetY);
                    if (_BoardView.ContainsCell(lCandidate))
                        lCells.Add(lCandidate);
                }
            }

            return lCells;
        }

        private static GridCoord ResolveSkillPreviewOrigin(BattleUnitRuntime pActiveUnit, SkillTarget pTarget, GridCoord pHoveredCell)
        {
            switch (pTarget.TargetType)
            {
                case SkillTargetType.Self:
                    return pActiveUnit.Position;

                case SkillTargetType.Unit:
                    return pHoveredCell;

                case SkillTargetType.Cell:
                    return pTarget.Cell;

                default:
                    return pActiveUnit != null ? pActiveUnit.Position : default;
            }
        }

        private bool TryResolveAliveUnitAtCell(GridCoord pCell, out BattleUnitRuntime pUnit)
        {
            pUnit = null;

            if (_Bootstrap?.BattleService == null)
                return false;

            foreach (BattleUnitRuntime lUnit in _Bootstrap.BattleService.Units)
            {
                if (lUnit == null || !lUnit.IsAlive)
                    continue;

                if (lUnit.Position != pCell)
                    continue;

                pUnit = lUnit;
                return true;
            }

            return false;
        }

        private string ResolveSkillModeMessage()
        {
            if (!_Bootstrap.TryGetActiveUnit(out BattleUnitRuntime lActiveUnit))
                return "Mode: Waiting for an active unit.";

            if (lActiveUnit.Team != BattleTeam.Player)
                return $"Mode: Enemy turn for {lActiveUnit.Definition.DisplayName}.";

            if (!HasSelectedSkill())
                return "Mode: Move. Click a blue cell to move, or choose a skill.";

            string lTargetLabel = ResolveTargetLabel(_SelectedSkill);
            string lHoveredLabel = ResolveHoveredLabel();
            return $"Mode: {_SelectedSkill.DisplayName} [{lTargetLabel}] Range {ResolveRangeLabel(_SelectedSkill)}, AP {_SelectedSkill.ActionPointCost}. {lHoveredLabel} Right click or Esc to cancel.";
        }

        private string ResolveTargetLabel(SkillDefinition pSkill) => pSkill.TargetType switch
        {
            SkillTargetType.Unit => "Unit Target",
            SkillTargetType.Cell => "Cell Target",
            SkillTargetType.Self => "Self",
            _ => "Unknown"
        };

        private string ResolveHoveredLabel()
        {
            if (!_HasHoveredCell)
                return "Hover a valid target.";

            if (TryResolveAliveUnitAtCell(_HoveredCell, out BattleUnitRuntime lHoveredUnit))
                return $"Hover: {lHoveredUnit.Definition.DisplayName} {_HoveredCell}.";

            return $"Hover: cell {_HoveredCell}.";
        }

        private static string ResolveRangeLabel(SkillDefinition pSkill)
        {
            if (pSkill == null)
                return "0";

            return pSkill.RangeMin == pSkill.RangeMax
                ? pSkill.RangeMax.ToString()
                : $"{pSkill.RangeMin}-{pSkill.RangeMax}";
        }

        private bool IsSkillCellInRange(BattleUnitRuntime pActiveUnit, SkillDefinition pSkill, GridCoord pTargetCell)
        {
            if (pActiveUnit == null || pSkill == null)
                return false;

            int lDistance = pActiveUnit.Position.ManhattanDistanceTo(pTargetCell);
            int lRangeMin = pActiveUnit.GetSkillRangeMin(pSkill);
            int lRangeMax = pActiveUnit.GetSkillRangeMax(pSkill);
            if (lDistance < lRangeMin || lDistance > lRangeMax)
                return false;

            if (!GridLineOfSightUtility.MatchesAlignment(pActiveUnit.Position, pTargetCell, pSkill.TargetAlignment))
                return false;

            if (!TryGetBoardCellDefinition(pTargetCell, out BattleGridCellDefinition lCellDefinition) || !lCellDefinition.IsWalkable)
                return false;

            if (pSkill.RequiresLineOfSight && pActiveUnit.Position != pTargetCell && !HasLineOfSight(pActiveUnit.Position, pTargetCell))
                return false;

            return true;
        }

        private bool TryGetBoardCellDefinition(GridCoord pCell, out BattleGridCellDefinition pCellDefinition)
        {
            if (_BoardView != null)
                return _BoardView.TryGetCellDefinition(pCell, out pCellDefinition);

            pCellDefinition = null;
            return false;
        }

        private bool HasLineOfSight(GridCoord pOrigin, GridCoord pTarget)
        {
            if (_BoardView == null)
                return false;

            return GridLineOfSightUtility.HasLineOfSight(
                pOrigin,
                pTarget,
                pCell =>
                {
                    if (!TryGetBoardCellDefinition(pCell, out BattleGridCellDefinition lCell))
                        return true;

                    return lCell.BlocksLineOfSight || TryResolveAliveUnitAtCell(pCell, out _);
                });
        }

        private bool CanRun() => _Bootstrap != null
            && _Bootstrap.IsBootstrapped
            && _BoardView != null
            && _MainCamera != null;

        private void SyncInteractionState(Vector2 pMouseScreenPosition)
        {
            SyncActiveUnitState();
            SyncReachableCells();
            SyncEndTurnAvailability();
            SyncSkillAvailability();
            UpdateHoveredCell(pMouseScreenPosition);
            _Bootstrap.TryGetActiveUnit(out BattleUnitRuntime lActiveUnit);
            SyncPreviewCells(lActiveUnit);
            SyncSkillMode();
            RefreshHoverCursor();
        }

        private bool TryGetPlayerActiveUnit(string pEnemyTurnMessage, out BattleUnitRuntime pActiveUnit)
        {
            pActiveUnit = null;

            if (!_Bootstrap.TryGetActiveUnit(out pActiveUnit))
            {
                SetStatus("No active unit is available.");
                return false;
            }

            if (pActiveUnit.Team == BattleTeam.Player)
                return true;

            SetStatus($"{pEnemyTurnMessage} during {pActiveUnit.Definition.DisplayName}'s turn.");
            return false;
        }

        private bool CanPlayerIssueCommands() => _Bootstrap != null
            && _Bootstrap.IsBootstrapped
            && _Bootstrap.CurrentTurn != null
            && _Bootstrap.BattleService != null
            && _Bootstrap.BattleService.Outcome == BattleOutcome.None
            && _Bootstrap.TryGetActiveUnit(out BattleUnitRuntime lActiveUnit)
            && lActiveUnit.Team == BattleTeam.Player;

        private bool IsPointerOverUi() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        private void SetStatus(string pMessage) => HudManager?.SetStatus(pMessage);

        private void ValidateReferences()
        {
            LogMissingReference(_Bootstrap, nameof(_Bootstrap));
            LogMissingReference(_BoardView, nameof(_BoardView));
            LogMissingReference(_MainCamera, nameof(_MainCamera));
        }

        private void LogMissingReference(Object pReference, string pFieldName)
        {
            if (pReference == null)
                Debug.LogWarning($"CombatPlayerController is missing reference '{pFieldName}'.", this);
        }

        private static Vector2 ReadMouseScreenPosition()
        {
            return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        }

        #endregion
    }
}
