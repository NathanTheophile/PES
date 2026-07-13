#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback
//  Menu UI
#endregion

using System;
using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TacticalPort.UI
{
    public sealed class MainMenuTeamDeckbuildingController : MonoBehaviour
    {
        #region _____________________________/ TYPES

        [Serializable]
        public sealed class MainMenuTeamSlotView
        {
            public Button Button;
            public Transform ModelAnchor;
            public TMP_Text NameLabel;
            public Image FallbackPortrait;

            [NonSerialized] public UnitDefinition BoundUnit;
            [NonSerialized] public GameObject RuntimeModel;
        }

        #endregion

        #region _____________________________/ VALUES

        private const int TeamSize = 3;

        [Header("Data")]
        [SerializeField] private List<UnitDefinition> _AvailableUnits = new List<UnitDefinition>();

        [Header("Team Slots")]
        [SerializeField] private List<MainMenuTeamSlotView> _TeamSlots = new List<MainMenuTeamSlotView>(TeamSize);

        [Header("Deckbuilding")]
        [SerializeField] private GameObject _DeckbuildingPanel;
        [SerializeField] private DeckbuildingPanelController _DeckbuildingPanelController;

        private readonly List<UnitDefinition> _ResolvedTeam = new List<UnitDefinition>(TeamSize);
        private bool _HasHookedButtons;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            ResolveMissingReferences();
            HookButtons();
            SubscribeDeckbuildingEvents();
            SetDeckbuildingVisible(false);
            RefreshTeamSlots();
        }

        private void OnEnable() => RefreshTeamSlots();

        private void OnDestroy()
        {
            UnsubscribeDeckbuildingEvents();
            ClearSlotModels();
        }

        #endregion

        #region _____________________________| REFRESH

        public void RefreshTeamSlots()
        {
            ResolveTeamSelection();

            for (int lIndex = 0; lIndex < _TeamSlots.Count; lIndex++)
            {
                UnitDefinition lUnit = lIndex < _ResolvedTeam.Count ? _ResolvedTeam[lIndex] : null;
                BindSlot(_TeamSlots[lIndex], lUnit, lIndex);
            }
        }

        private void BindSlot(MainMenuTeamSlotView pSlot, UnitDefinition pUnit, int pSlotIndex)
        {
            if (pSlot == null)
                return;

            TMP_Text lNameLabel = pSlot.NameLabel != null
                ? pSlot.NameLabel
                : pSlot.Button != null
                    ? pSlot.Button.GetComponentInChildren<TMP_Text>(true)
                    : null;

            if (lNameLabel != null)
                lNameLabel.text = pUnit != null ? pUnit.DisplayName : $"Character {pSlotIndex + 1}";

            if (pSlot.FallbackPortrait != null)
            {
                Sprite lSprite = ResolveFallbackSprite(pUnit);
                pSlot.FallbackPortrait.sprite = lSprite;
                pSlot.FallbackPortrait.enabled = lSprite != null && pUnit?.ModelPrefab == null;
                pSlot.FallbackPortrait.preserveAspect = true;
            }

            if (pSlot.Button != null)
                pSlot.Button.interactable = pUnit != null;

            RefreshSlotModel(pSlot, pUnit);
            pSlot.BoundUnit = pUnit;
        }

        #endregion

        #region _____________________________| BUTTONS

        private void HookButtons()
        {
            if (_HasHookedButtons)
                return;

            for (int lIndex = 0; lIndex < _TeamSlots.Count; lIndex++)
            {
                int lSlotIndex = lIndex;
                Button lButton = _TeamSlots[lIndex]?.Button;
                if (lButton == null)
                    continue;

                lButton.onClick.RemoveAllListeners();
                lButton.onClick.AddListener(() => OpenDeckbuilding(lSlotIndex));
            }

            _HasHookedButtons = true;
        }

        private void OpenDeckbuilding(int pSlotIndex)
        {
            ResolveTeamSelection();

            if (_DeckbuildingPanelController == null)
                ResolveMissingReferences();

            if (_DeckbuildingPanelController == null)
                return;

            _DeckbuildingPanelController.ShowForTeamSlot(pSlotIndex);
        }

        #endregion

        #region _____________________________| DECKBUILDING EVENTS

        private void SubscribeDeckbuildingEvents()
        {
            if (_DeckbuildingPanelController == null)
                return;

            _DeckbuildingPanelController.TeamSelectionChanged -= RefreshTeamSlots;
            _DeckbuildingPanelController.TeamSelectionChanged += RefreshTeamSlots;
            _DeckbuildingPanelController.Closed -= RefreshTeamSlots;
            _DeckbuildingPanelController.Closed += RefreshTeamSlots;
        }

        private void UnsubscribeDeckbuildingEvents()
        {
            if (_DeckbuildingPanelController == null)
                return;

            _DeckbuildingPanelController.TeamSelectionChanged -= RefreshTeamSlots;
            _DeckbuildingPanelController.Closed -= RefreshTeamSlots;
        }

        private void SetDeckbuildingVisible(bool pVisible)
        {
            if (_DeckbuildingPanel != null && _DeckbuildingPanel.activeSelf != pVisible)
                _DeckbuildingPanel.SetActive(pVisible);
        }

        #endregion

        #region _____________________________| TEAM STATE

        private void ResolveTeamSelection()
        {
            List<UnitDefinition> lAvailableUnits = GetAvailableUnits();
            _ResolvedTeam.Clear();

            if (lAvailableUnits.Count == 0)
                return;

            bool lHasSavedSelection = TryLoadCurrentSelection(lAvailableUnits, _ResolvedTeam);
            if (!lHasSavedSelection)
                FillDefaultSelection(lAvailableUnits, _ResolvedTeam);

            FillMissingSlots(lAvailableUnits, _ResolvedTeam);
            TeamSelectionState.SetSelectedUnits(_ResolvedTeam);

            if (!lHasSavedSelection)
                TeamSelectionState.SaveSelectedUnits();
        }

        private bool TryLoadCurrentSelection(IReadOnlyList<UnitDefinition> pAvailableUnits, List<UnitDefinition> pTarget)
        {
            if (!TeamSelectionState.HasSelection && !TeamSelectionState.LoadSavedUnitIds())
                return false;

            if (TeamSelectionState.SelectedUnits.Count > 0)
            {
                for (int lIndex = 0; lIndex < TeamSelectionState.SelectedUnits.Count; lIndex++)
                {
                    UnitDefinition lUnit = TeamSelectionState.SelectedUnits[lIndex];
                    if (lUnit != null && ContainsUnit(pAvailableUnits, lUnit) && !pTarget.Contains(lUnit))
                        pTarget.Add(lUnit);
                }
            }

            if (pTarget.Count == 0)
                TryResolveSelection(TeamSelectionState.SelectedUnitIds, pAvailableUnits, pTarget);

            return pTarget.Count > 0;
        }

        private static void TryResolveSelection(IReadOnlyList<string> pUnitIds, IReadOnlyList<UnitDefinition> pAvailableUnits, List<UnitDefinition> pTarget)
        {
            if (pUnitIds == null || pAvailableUnits == null)
                return;

            for (int lIdIndex = 0; lIdIndex < pUnitIds.Count; lIdIndex++)
            {
                UnitDefinition lUnit = FindAvailableUnitById(pAvailableUnits, pUnitIds[lIdIndex]);
                if (lUnit != null && !pTarget.Contains(lUnit))
                    pTarget.Add(lUnit);
            }
        }

        private static UnitDefinition FindAvailableUnitById(IReadOnlyList<UnitDefinition> pAvailableUnits, string pId)
        {
            if (string.IsNullOrWhiteSpace(pId))
                return null;

            for (int lIndex = 0; lIndex < pAvailableUnits.Count; lIndex++)
            {
                UnitDefinition lUnit = pAvailableUnits[lIndex];
                if (lUnit != null && string.Equals(lUnit.Id, pId, StringComparison.Ordinal))
                    return lUnit;
            }

            return null;
        }

        private void FillDefaultSelection(IReadOnlyList<UnitDefinition> pAvailableUnits, List<UnitDefinition> pTarget)
        {
            int lCount = Mathf.Min(TeamSize, pAvailableUnits.Count);
            for (int lIndex = 0; lIndex < lCount; lIndex++)
            {
                UnitDefinition lUnit = pAvailableUnits[lIndex];
                if (lUnit != null && !pTarget.Contains(lUnit))
                    pTarget.Add(lUnit);
            }
        }

        private static void FillMissingSlots(IReadOnlyList<UnitDefinition> pAvailableUnits, List<UnitDefinition> pTarget)
        {
            for (int lIndex = 0; pTarget.Count < TeamSize && lIndex < pAvailableUnits.Count; lIndex++)
            {
                UnitDefinition lUnit = pAvailableUnits[lIndex];
                if (lUnit != null && !pTarget.Contains(lUnit))
                    pTarget.Add(lUnit);
            }

            if (pTarget.Count > TeamSize)
                pTarget.RemoveRange(TeamSize, pTarget.Count - TeamSize);
        }

        private List<UnitDefinition> GetAvailableUnits()
        {
            List<UnitDefinition> lUnits = new List<UnitDefinition>(_AvailableUnits.Count);
            for (int lIndex = 0; lIndex < _AvailableUnits.Count; lIndex++)
            {
                UnitDefinition lUnit = _AvailableUnits[lIndex];
                if (lUnit != null && !lUnits.Contains(lUnit))
                    lUnits.Add(lUnit);
            }

            return lUnits;
        }

        private static bool ContainsUnit(IReadOnlyList<UnitDefinition> pUnits, UnitDefinition pUnit)
        {
            if (pUnits == null || pUnit == null)
                return false;

            for (int lIndex = 0; lIndex < pUnits.Count; lIndex++)
            {
                if (pUnits[lIndex] == pUnit)
                    return true;
            }

            return false;
        }

        #endregion

        #region _____________________________| MODELS

        private void RefreshSlotModel(MainMenuTeamSlotView pSlot, UnitDefinition pUnit)
        {
            if (pSlot.ModelAnchor == null)
                return;

            if (pSlot.RuntimeModel != null && pSlot.BoundUnit == pUnit)
                return;

            DestroySlotModel(pSlot);

            if (pUnit?.ModelPrefab == null)
                return;

            pSlot.RuntimeModel = Instantiate(pUnit.ModelPrefab, pSlot.ModelAnchor);
            pSlot.RuntimeModel.name = $"Model_{pUnit.DisplayName}";
            Transform lModelTransform = pSlot.RuntimeModel.transform;
            lModelTransform.localPosition = Vector3.zero;
            lModelTransform.localRotation = Quaternion.identity;
            lModelTransform.localScale = Vector3.one;
        }

        private void ClearSlotModels()
        {
            for (int lIndex = 0; lIndex < _TeamSlots.Count; lIndex++)
                DestroySlotModel(_TeamSlots[lIndex]);
        }

        private static void DestroySlotModel(MainMenuTeamSlotView pSlot)
        {
            if (pSlot == null || pSlot.RuntimeModel == null)
                return;

            if (Application.isPlaying)
                Destroy(pSlot.RuntimeModel);
            else
                DestroyImmediate(pSlot.RuntimeModel);

            pSlot.RuntimeModel = null;
        }

        private static Sprite ResolveFallbackSprite(UnitDefinition pUnit)
        {
            if (pUnit == null)
                return null;

            if (pUnit.PreviewSprite != null)
                return pUnit.PreviewSprite;

            return pUnit.Portrait != null ? pUnit.Portrait : pUnit.DisplaySprite;
        }

        #endregion

        #region _____________________________| REFERENCES

        private void ResolveMissingReferences()
        {
            if (_DeckbuildingPanelController == null && _DeckbuildingPanel != null)
                _DeckbuildingPanelController = _DeckbuildingPanel.GetComponentInChildren<DeckbuildingPanelController>(true);

            if (_DeckbuildingPanel == null && _DeckbuildingPanelController != null)
                _DeckbuildingPanel = _DeckbuildingPanelController.gameObject;
        }

        #endregion
    }
}
