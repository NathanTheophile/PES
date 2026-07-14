#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Core;
using TacticalPort.Data;
using TacticalPort.Shared;
using TacticalPort.Combat;
using TacticalPort.View;

namespace TacticalPort.UI
{
    internal sealed class SkillSelectionController
    {
        #region _____________________________/ VALUES

        private readonly CombatInteractionContext _Context;
        private UnitId _CachedActiveUnitId = UnitId.None;
        private SkillDefinition _SelectedSkill;
        private SkillId _SelectedSkillId = SkillId.None;
        private int _SelectedSkillSlotIndex = -1;

        #endregion

        #region _____________________________/ ACCESSORS

        public bool HasSelectedSkill => _SelectedSkill != null && _SelectedSkillId.IsValid;
        public SkillDefinition SelectedSkill => _SelectedSkill;
        public SkillId SelectedSkillId => _SelectedSkillId;
        public int SelectedSkillSlotIndex => HasSelectedSkill ? _SelectedSkillSlotIndex : -1;

        #endregion

        #region _____________________________| INIT

        public SkillSelectionController(CombatInteractionContext pContext)
        {
            _Context = pContext;
        }

        #endregion

        #region _____________________________| INTERACTION

        public bool TrySelectSkill(int pSkillSlotIndex)
        {
            if (!_Context.TryGetPlayerActiveUnit("Skills are disabled", out UnitRuntime lActiveUnit))
                return false;

            if (pSkillSlotIndex < 0 || pSkillSlotIndex >= lActiveUnit.Skills.Count)
            {
                _Context.SetStatus("This skill slot is empty.");
                return false;
            }

            SkillDefinition lBaseSkill = lActiveUnit.Skills[pSkillSlotIndex];
            if (lBaseSkill == null)
            {
                _Context.SetStatus("This skill slot is empty.");
                return false;
            }

            SkillDefinition lSkill = lActiveUnit.ResolveEffectiveSkill(lBaseSkill);
            SkillId lSkillId = new SkillId(lBaseSkill.Id);
            if (_SelectedSkillId == lSkillId)
            {
                Cancel("Skill selection canceled.");
                return true;
            }

            _SelectedSkill = lSkill;
            _SelectedSkillId = lSkillId;
            _SelectedSkillSlotIndex = pSkillSlotIndex;
            _Context.SetStatus($"Selected skill: {lSkill.DisplayName}.");
            return true;
        }

        public bool TryUseSelectedSkill(bool pHasHoveredCell, GridCoord pHoveredCell)
        {
            if (!_Context.TryGetPlayerActiveUnit("Skills are disabled", out _))
                return false;

            if (!HasSelectedSkill)
            {
                _Context.SetStatus("No skill is selected.");
                return false;
            }

            if (!TryBuildSelectedSkillTarget(pHasHoveredCell, pHoveredCell, out SkillTarget lTarget, out string lFailureReason))
            {
                _Context.SetStatus(lFailureReason);
                return false;
            }

            BattleActionResult lValidation = _Context.Bootstrap.ValidateActiveSkill(_SelectedSkillId, lTarget);
            if (!lValidation.IsSuccess)
            {
                _Context.SetStatus(lValidation.Message);
                return false;
            }

            BattleActionResult lResult = _Context.Bootstrap.UseActiveUnitSkill(_SelectedSkillId, lTarget);
            if (lResult.IsSuccess)
                Cancel(null);

            return true;
        }

        public void Cancel(string pStatusMessage)
        {
            _SelectedSkill = null;
            _SelectedSkillId = SkillId.None;
            _SelectedSkillSlotIndex = -1;
            _Context.BoardView?.SetPreviewCells(null);

            if (!string.IsNullOrWhiteSpace(pStatusMessage))
                _Context.SetStatus(pStatusMessage);
        }

        #endregion

        #region _____________________________| SYNC

        public void SyncActiveUnitState()
        {
            if (_Context.IsPlacementPhaseActive)
            {
                _CachedActiveUnitId = UnitId.None;
                Cancel(null);
                return;
            }

            if (_Context.Bootstrap == null || !_Context.Bootstrap.TryGetActiveUnit(out UnitRuntime lActiveUnit))
            {
                _CachedActiveUnitId = UnitId.None;
                Cancel(null);
                return;
            }

            if (_CachedActiveUnitId != lActiveUnit.Id)
            {
                _CachedActiveUnitId = lActiveUnit.Id;
                Cancel(null);
            }

            if (!_Context.Bootstrap.CanLocalPlayerControlUnit(lActiveUnit))
            {
                Cancel(null);
                return;
            }

            if (!HasSelectedSkill)
                return;

            if (!lActiveUnit.TryGetSkill(_SelectedSkillId, out SkillDefinition lBaseSkill) || lBaseSkill == null)
            {
                Cancel(null);
                return;
            }

            _SelectedSkill = lActiveUnit.ResolveEffectiveSkill(lBaseSkill);
            _SelectedSkillSlotIndex = ResolveSkillSlotIndex(lActiveUnit, _SelectedSkillId);
        }

        public string ResolveSkillModeMessage(
            bool pIsPlacementPhaseActive,
            PlacementInteractionController pPlacementInteraction,
            CombatHoverController pHoverController)
        {
            if (pIsPlacementPhaseActive)
                return pPlacementInteraction.ResolveModeMessage();

            if (_Context.Bootstrap == null || !_Context.Bootstrap.TryGetActiveUnit(out UnitRuntime lActiveUnit))
                return "Mode: Waiting for an active unit.";

            if (!_Context.Bootstrap.CanLocalPlayerControlUnit(lActiveUnit))
                return $"Mode: Waiting during {lActiveUnit.Definition.DisplayName}'s turn.";

            if (!HasSelectedSkill)
                return "Mode: Move. Click a blue cell to move, or choose a skill.";

            string lHoveredLabel = ResolveHoveredLabel(pHoverController);
            return $"Mode: {_SelectedSkill.DisplayName} Range {CombatInteractionContext.ResolveRangeLabel(_SelectedSkill)}, Energy {_SelectedSkill.EnergyCost}. {lHoveredLabel} Right click or Esc to cancel.";
        }

        #endregion

        #region _____________________________| HELPERS

        private bool TryBuildSelectedSkillTarget(
            bool pHasHoveredCell,
            GridCoord pHoveredCell,
            out SkillTarget pTarget,
            out string pFailureReason)
        {
            pTarget = null;
            pFailureReason = string.Empty;

            if (_SelectedSkill == null)
            {
                pFailureReason = "No skill is selected.";
                return false;
            }

            if (!pHasHoveredCell)
            {
                pFailureReason = "Point to a target cell to use this skill.";
                return false;
            }

            pTarget = SkillTarget.ForCell(pHoveredCell);
            return true;
        }

        private string ResolveHoveredLabel(CombatHoverController pHoverController)
        {
            if (pHoverController == null || !pHoverController.HasHoveredCell)
                return "Hover a valid target.";

            GridCoord lHoveredCell = pHoverController.HoveredCell;
            if (_Context.TryResolveAliveUnitAtCell(lHoveredCell, out UnitRuntime lHoveredUnit))
                return $"Hover: {lHoveredUnit.Definition.DisplayName} {lHoveredCell}.";

            return $"Hover: cell {lHoveredCell}.";
        }

        private static int ResolveSkillSlotIndex(UnitRuntime pUnit, SkillId pSkillId)
        {
            if (pUnit?.Skills == null)
                return -1;

            for (int lIndex = 0; lIndex < pUnit.Skills.Count; lIndex++)
            {
                SkillDefinition lSkill = pUnit.Skills[lIndex];
                if (lSkill != null && new SkillId(lSkill.Id) == pSkillId)
                    return lIndex;
            }

            return -1;
        }

        #endregion
    }
}
