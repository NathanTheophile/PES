#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using System.Collections.Generic;
using TacticalPort.Core;
using TacticalPort.Data;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.UI
{
    internal sealed class HudSkillBarController
    {
        #region _____________________________/ VALUES

        private readonly RectTransform _Root;
        private readonly SkillButtonView _SkillButtonPrefab;
        private readonly List<SkillButtonView> _RuntimeSkillButtons = new List<SkillButtonView>();
        private readonly List<SkillDefinition> _DisplayedSkills = new List<SkillDefinition>();
        private IBattleService _BattleService;
        private UnitId _DisplayedSkillUnitId = UnitId.None;
        private bool _CanUseSkills;
        private int _SelectedSkillSlotIndex = -1;
        private Func<UnitRuntime, bool> _CanDisplaySkills;
        private Action<int> _OnSkillButtonClicked;

        #endregion

        #region _____________________________| INIT

        public HudSkillBarController(RectTransform pRoot, SkillButtonView pSkillButtonPrefab)
        {
            _Root = pRoot;
            _SkillButtonPrefab = pSkillButtonPrefab;
        }

        #endregion

        #region _____________________________| BINDING

        public void Bind(IBattleService pBattleService)
        {
            _BattleService = pBattleService;
        }

        public void SetState(
            bool pCanUseSkills,
            int pSelectedSkillSlotIndex,
            Func<UnitRuntime, bool> pCanDisplaySkills,
            Action<int> pOnSkillButtonClicked)
        {
            if (_CanUseSkills == pCanUseSkills
                && _SelectedSkillSlotIndex == pSelectedSkillSlotIndex
                && _CanDisplaySkills == pCanDisplaySkills
                && _OnSkillButtonClicked == pOnSkillButtonClicked)
                return;

            _CanUseSkills = pCanUseSkills;
            _SelectedSkillSlotIndex = pSelectedSkillSlotIndex;
            _CanDisplaySkills = pCanDisplaySkills;
            _OnSkillButtonClicked = pOnSkillButtonClicked;
            Refresh();
        }

        #endregion

        #region _____________________________| DISPLAY

        public void Refresh()
        {
            if (!TryResolveDisplayedSkills(out UnitRuntime lActiveUnit, out IReadOnlyList<SkillDefinition> lSkills))
            {
                SetVisible(false);
                Clear();
                _DisplayedSkillUnitId = UnitId.None;
                _DisplayedSkills.Clear();
                return;
            }

            SetVisible(true);

            if (ShouldRebuildButtons(lActiveUnit, lSkills))
                RebuildButtons(lActiveUnit, lSkills);

            RefreshButtons();
        }

        public void Clear()
        {
            for (int lIndex = 0; lIndex < _RuntimeSkillButtons.Count; lIndex++)
            {
                SkillButtonView lButtonView = _RuntimeSkillButtons[lIndex];
                if (lButtonView == null)
                    continue;

                if (Application.isPlaying)
                    UnityEngine.Object.Destroy(lButtonView.gameObject);
                else
                    UnityEngine.Object.DestroyImmediate(lButtonView.gameObject);
            }

            _RuntimeSkillButtons.Clear();
        }

        #endregion

        #region _____________________________| HELPERS

        private bool TryResolveDisplayedSkills(out UnitRuntime pUnit, out IReadOnlyList<SkillDefinition> pSkills)
        {
            pUnit = null;
            pSkills = null;

            if (_BattleService == null || !_BattleService.TryGetActiveUnit(out pUnit) || pUnit == null)
                return false;

            bool lCanDisplayUnitSkills = _CanDisplaySkills != null
                ? _CanDisplaySkills(pUnit)
                : pUnit.Team == Team.TeamA;
            if (!lCanDisplayUnitSkills || pUnit.Skills == null || pUnit.Skills.Count == 0)
                return false;

            pSkills = pUnit.Skills;
            return true;
        }

        private bool ShouldRebuildButtons(UnitRuntime pUnit, IReadOnlyList<SkillDefinition> pSkills)
        {
            if (_DisplayedSkillUnitId != pUnit.Id)
                return true;

            if (_DisplayedSkills.Count != pSkills.Count || _RuntimeSkillButtons.Count != pSkills.Count)
                return true;

            for (int lIndex = 0; lIndex < pSkills.Count; lIndex++)
            {
                SkillDefinition lDisplaySkill = pSkills[lIndex];
                if (_RuntimeSkillButtons[lIndex] == null || _DisplayedSkills[lIndex] != lDisplaySkill)
                    return true;
            }

            return false;
        }

        private void RebuildButtons(UnitRuntime pUnit, IReadOnlyList<SkillDefinition> pSkills)
        {
            Clear();
            _DisplayedSkills.Clear();
            _DisplayedSkillUnitId = pUnit.Id;

            if (_Root == null || _SkillButtonPrefab == null)
                return;

            for (int lIndex = 0; lIndex < pSkills.Count; lIndex++)
            {
                SkillDefinition lSkill = pSkills[lIndex];
                int lCapturedIndex = lIndex;
                SkillButtonView lButtonView = UnityEngine.Object.Instantiate(_SkillButtonPrefab, _Root);
                lButtonView.Bind(lSkill, () => _OnSkillButtonClicked?.Invoke(lCapturedIndex));
                lButtonView.SetInteractable(_CanUseSkills);
                lButtonView.SetSelected(lIndex == _SelectedSkillSlotIndex);
                _RuntimeSkillButtons.Add(lButtonView);
                _DisplayedSkills.Add(lSkill);
            }
        }

        private void RefreshButtons()
        {
            UnitRuntime lActiveUnit = null;
            if (_BattleService != null)
                _BattleService.TryGetActiveUnit(out lActiveUnit);

            for (int lIndex = 0; lIndex < _RuntimeSkillButtons.Count; lIndex++)
            {
                SkillButtonView lButtonView = _RuntimeSkillButtons[lIndex];
                if (lButtonView == null)
                    continue;

                SkillDefinition lSkill = lActiveUnit != null && lIndex >= 0 && lIndex < lActiveUnit.Skills.Count
                    ? lActiveUnit.Skills[lIndex]
                    : null;

                if (lIndex >= 0 && lIndex < _DisplayedSkills.Count && _DisplayedSkills[lIndex] != lSkill)
                {
                    int lCapturedIndex = lIndex;
                    lButtonView.Bind(lSkill, () => _OnSkillButtonClicked?.Invoke(lCapturedIndex));
                    _DisplayedSkills[lIndex] = lSkill;
                }

                lButtonView.SetInteractable(IsSkillInteractable(lActiveUnit, lSkill));
                lButtonView.SetSelected(lIndex == _SelectedSkillSlotIndex);
            }
        }

        private bool IsSkillInteractable(UnitRuntime pUnit, SkillDefinition pSkill)
        {
            if (!_CanUseSkills || pUnit == null || pSkill == null)
                return false;

            if (!pUnit.CanSpendEnergy(pSkill.EnergyCost))
                return false;

            if (pUnit.GetRemainingCooldown(pSkill) > 0)
                return false;

            return pSkill.UsePerTurn <= 0 || pUnit.GetSkillUsesThisTurn(pSkill) < pSkill.UsePerTurn;
        }

        private void SetVisible(bool pIsVisible)
        {
            if (_Root != null && _Root.gameObject.activeSelf != pIsVisible)
                _Root.gameObject.SetActive(pIsVisible);
        }

        #endregion
    }
}
