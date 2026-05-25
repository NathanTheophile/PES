#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Bootstrap;
using TacticalPort.Core;
using TacticalPort.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TacticalPort.UI;
using TacticalPort.View;

namespace TacticalPort.Combat
{
    public sealed class CombatPlayerController : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private CombatBootstrap _Bootstrap;
        [SerializeField] private BoardView _BoardView;
        [SerializeField] private Camera _MainCamera;

        private CombatInteractionContext _Context;
        private PlacementInteractionController _PlacementInteraction;
        private SkillSelectionController _SkillSelection;
        private CombatHoverController _HoverController;
        private CombatPreviewController _PreviewController;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            LogMissingReferences();
            EnsureControllers();
        }

        private void Update()
        {
            EnsureControllers();
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

        public void OnEndTurnButtonClicked()
        {
            EnsureControllers();
            HandlePrimaryAction();
        }

        public void OnSkillButtonClicked(int pSkillSlotIndex)
        {
            EnsureControllers();
            SelectSkillIfPossible(pSkillSlotIndex);
        }

        public void OnCancelSkillButtonClicked()
        {
            EnsureControllers();
            CancelSkillSelection("Skill selection canceled.");
        }

        private void HandleEndTurnShortcut()
        {
            if (Keyboard.current == null || !Keyboard.current.spaceKey.wasPressedThisFrame)
                return;

            HandlePrimaryAction();
        }

        private void HandleSkillCancelShortcut()
        {
            if (!_SkillSelection.HasSelectedSkill)
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

            if (_SkillSelection.HasSelectedSkill)
            {
                UseSelectedSkillIfPossible();
                return;
            }

            if (IsPlacementPhaseActive())
            {
                HandlePlacementClickIfPossible();
                return;
            }

            MoveHoveredCellIfPossible();
        }

        #endregion

        #region _____________________________| INTERACTION

        private void MoveHoveredCellIfPossible()
        {
            if (!_Context.TryGetPlayerActiveUnit("Manual movement is disabled", out _))
                return;

            if (!_HoverController.HasHoveredCell)
            {
                SetStatus("Point to a cell to move the active unit.");
                return;
            }

            GridCoord lHoveredCell = _HoverController.HoveredCell;
            if (!_PreviewController.IsReachable(lHoveredCell))
            {
                SetStatus($"Cell {lHoveredCell} is not reachable this turn.");
                return;
            }

            _Bootstrap.MoveActiveUnit(lHoveredCell);
            RefreshInteractionState();
        }

        private void HandlePlacementClickIfPossible()
        {
            if (_PlacementInteraction.TryHandleClick(_HoverController.HasHoveredCell, _HoverController.HoveredCell))
                RefreshInteractionState();
        }

        private void SelectSkillIfPossible(int pSkillSlotIndex)
        {
            if (_SkillSelection.TrySelectSkill(pSkillSlotIndex))
                RefreshInteractionState();
        }

        private void UseSelectedSkillIfPossible()
        {
            if (_SkillSelection.TryUseSelectedSkill(_HoverController.HasHoveredCell, _HoverController.HoveredCell))
                RefreshInteractionState();
        }

        private void HandlePrimaryAction()
        {
            if (!CanRun())
                return;

            if (IsPlacementPhaseActive())
            {
                _PlacementInteraction.ClearSelection();
                CancelSkillSelection(null);
                _PreviewController.ClearInteractionPreviews();
                _Bootstrap.StartCombatFromPlacement();
                RefreshInteractionState();
                return;
            }

            EndTurnIfPossible();
        }

        private void EndTurnIfPossible()
        {
            if (!_Context.TryGetPlayerActiveUnit("Turn input is disabled", out _))
                return;

            CancelSkillSelection(null);
            _Bootstrap.EndActiveTurn();
            RefreshInteractionState();
        }

        #endregion

        #region _____________________________| SYNC

        private void SyncInteractionState(Vector2 pMouseScreenPosition)
        {
            _SkillSelection.SyncActiveUnitState();
            _PreviewController.SyncReachableCells(IsPlacementPhaseActive(), _SkillSelection, _PlacementInteraction);
            SyncEndTurnAvailability();
            SyncSkillAvailability();
            _HoverController.UpdateHoveredCell(pMouseScreenPosition, IsPointerOverUi());
            _PreviewController.SyncPreviewCells(IsPlacementPhaseActive(), _SkillSelection, _HoverController);
            SyncSkillMode();
            RefreshHoverCursor();
        }

        private void SyncEndTurnAvailability()
        {
            if (IsPlacementPhaseActive())
            {
                _Context.HudManager?.SetEndTurnLabel("Ready");
                _Context.HudManager?.SetEndTurnAvailable(CanReadyPlacement(), OnEndTurnButtonClicked);
                return;
            }

            _Context.HudManager?.SetEndTurnLabel("End Turn");
            _Context.HudManager?.SetEndTurnAvailable(_Context.CanPlayerIssueCommands(), OnEndTurnButtonClicked);
        }

        private void SyncSkillAvailability()
        {
            _Context.HudManager?.SetSkillState(
                _Context.CanPlayerIssueCommands(),
                _SkillSelection.HasSelectedSkill,
                _SkillSelection.SelectedSkillSlotIndex,
                _Context.Bootstrap.CanLocalPlayerControlUnit,
                OnSkillButtonClicked,
                OnCancelSkillButtonClicked);
        }

        private void SyncSkillMode()
        {
            _Context.HudManager?.SetSkillMode(
                _SkillSelection.ResolveSkillModeMessage(IsPlacementPhaseActive(), _PlacementInteraction, _HoverController));
        }

        #endregion

        #region _____________________________| HELPERS

        private void RefreshInteractionState() => SyncInteractionState(ReadMouseScreenPosition());

        private void CancelSkillSelection(string pStatusMessage)
        {
            _SkillSelection.Cancel(pStatusMessage);
            RefreshHoverCursor();
        }

        private void RefreshHoverCursor()
        {
            _HoverController?.RefreshCursor(
                IsPlacementPhaseActive(),
                _PlacementInteraction,
                _SkillSelection != null && _SkillSelection.HasSelectedSkill);
        }

        private void EnsureControllers()
        {
            if (_Context != null)
                return;

            _Context = new CombatInteractionContext(_Bootstrap, _BoardView, _MainCamera);
            _PlacementInteraction = new PlacementInteractionController(_Context);
            _SkillSelection = new SkillSelectionController(_Context);
            _HoverController = new CombatHoverController(_Context);
            _PreviewController = new CombatPreviewController(_Context);
        }

        private bool CanRun() => _Context != null && _Context.CanRun();

        private bool IsPlacementPhaseActive() => _Context != null && _Context.IsPlacementPhaseActive;

        private bool CanReadyPlacement() =>
            _Bootstrap != null
            && _Bootstrap.TryGetLocalPlayerSlot(out MatchPlayerSlot lSlot)
            && _Bootstrap.TryValidatePlacementReadyForSlot(lSlot, out _);

        private bool IsPointerOverUi() => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

        private void SetStatus(string pMessage) => _Context.SetStatus(pMessage);

        private void LogMissingReferences()
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
