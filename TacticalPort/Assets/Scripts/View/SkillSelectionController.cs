#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Core.Runtime;
using TacticalPort.Data;
using TacticalPort.Shared;

namespace TacticalPort.View
{
    internal sealed class SkillSelectionController
    {
        #region _____________________________/ VALUES

        private readonly CombatInteractionContext _Context;
        private UnitId _CachedActiveUnitId = UnitId.None;
        private SkillDefinition _SelectedSkill;
        private SkillId _SelectedSkillId = SkillId.None;

        #endregion

        #region _____________________________/ ACCESSORS

        public bool HasSelectedSkill => _SelectedSkill != null && _SelectedSkillId.IsValid;
        public SkillDefinition SelectedSkill => _SelectedSkill;
        public SkillId SelectedSkillId => _SelectedSkillId;

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

            SkillDefinition lSkill = lActiveUnit.Skills[pSkillSlotIndex];
            if (lSkill == null)
            {
                _Context.SetStatus("This skill slot is empty.");
                return false;
            }

            SkillId lSkillId = new SkillId(lSkill.Id);
            if (_SelectedSkillId == lSkillId)
            {
                Cancel("Skill selection canceled.");
                return true;
            }

            _SelectedSkill = lSkill;
            _SelectedSkillId = lSkillId;
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

            if (lActiveUnit.Team != Team.Player)
            {
                Cancel(null);
                return;
            }

            if (!HasSelectedSkill)
                return;

            if (!lActiveUnit.TryGetSkill(_SelectedSkillId, out SkillDefinition lSkill) || lSkill == null)
            {
                Cancel(null);
                return;
            }

            _SelectedSkill = lSkill;
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

            if (lActiveUnit.Team != Team.Player)
                return $"Mode: Enemy turn for {lActiveUnit.Definition.DisplayName}.";

            if (!HasSelectedSkill)
                return "Mode: Move. Click a blue cell to move, or choose a skill.";

            string lHoveredLabel = ResolveHoveredLabel(pHoverController);
            return $"Mode: {_SelectedSkill.DisplayName} Range {CombatInteractionContext.ResolveRangeLabel(_SelectedSkill)}, AP {_SelectedSkill.ActionPointCost}. {lHoveredLabel} Right click or Esc to cancel.";
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

        #endregion
    }
}
