#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.Data;
using TacticalPort.Shared;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TacticalPort.View
{
    public sealed class TeamSelectionController : MonoBehaviour
    {
        #region _____________________________/ TYPES

        [System.Serializable]
        private sealed class CharacterSlotView
        {
            public RectTransform Root;
            public Image Visual;
            public TMP_Text Name;
            public Button SwitchButton;
        }

        [System.Serializable]
        private sealed class CharacterGridItemView
        {
            public RectTransform Root;
            public Image Icon;
            public TMP_Text Name;
            public Button Button;
        }

        #endregion

        #region _____________________________/ VALUES

        [SerializeField] private List<UnitDefinition> _AvailableUnits = new List<UnitDefinition>();
        [SerializeField] private string _CombatSceneName = "S_Poutch";

        private readonly List<CharacterSlotView> _Slots = new List<CharacterSlotView>(3);
        private readonly List<CharacterGridItemView> _GridItems = new List<CharacterGridItemView>();
        private readonly List<UnitDefinition> _SelectedUnits = new List<UnitDefinition>(3);

        private RectTransform _GridRoot;
        private GameObject _GridPanel;
        private Button _LaunchButton;
        private int _PendingSlotIndex = -1;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            CacheReferences();
            InitializeSelection();
            RefreshSlotViews();
            BuildGrid();
            SetGridVisible(false);
            HookButtons();
        }

        #endregion

        #region _____________________________| SETUP

        private void CacheReferences()
        {
            _Slots.Clear();

            Transform lSlotsRoot = transform.Find("HBox_Characters");
            if (lSlotsRoot != null)
            {
                for (int lIndex = 0; lIndex < lSlotsRoot.childCount; lIndex++)
                {
                    Transform lSlotTransform = lSlotsRoot.GetChild(lIndex);
                    _Slots.Add(new CharacterSlotView
                    {
                        Root = lSlotTransform as RectTransform,
                        Visual = lSlotTransform.Find("Panel_Visual")?.GetComponent<Image>(),
                        Name = lSlotTransform.Find("Txt_Name")?.GetComponent<TMP_Text>(),
                        SwitchButton = lSlotTransform.Find("Btn_Switch")?.GetComponent<Button>()
                    });
                }
            }

            _GridRoot = transform.Find("Grid_Characters") as RectTransform;
            _GridPanel = _GridRoot != null ? _GridRoot.gameObject : null;
            _LaunchButton = transform.Find("Btn_Play")?.GetComponent<Button>();
        }

        private void InitializeSelection()
        {
            _SelectedUnits.Clear();
            List<UnitDefinition> lAvailableUnits = GetAvailableUnits();
            if (lAvailableUnits.Count == 0)
                return;

            List<UnitDefinition> lPool = new List<UnitDefinition>();
            lPool.AddRange(lAvailableUnits);

            for (int lIndex = 0; lIndex < _Slots.Count; lIndex++)
            {
                UnitDefinition lSelectedUnit = lPool.Count > lIndex
                    ? PopRandomUnit(lPool)
                    : lAvailableUnits[lIndex % lAvailableUnits.Count];
                _SelectedUnits.Add(lSelectedUnit);
            }

            TeamSelectionState.SetSelectedUnits(_SelectedUnits);
        }

        private void BuildGrid()
        {
            if (_GridRoot == null)
                return;

            List<UnitDefinition> lAvailableUnits = GetAvailableUnits();
            _GridItems.Clear();
            CharacterGridItemView lTemplate = CreateGridItemView(_GridRoot.childCount > 0 ? _GridRoot.GetChild(0) as RectTransform : null);
            if (lTemplate == null)
                return;

            for (int lIndex = _GridRoot.childCount; lIndex < lAvailableUnits.Count; lIndex++)
            {
                RectTransform lClone = Instantiate(lTemplate.Root, _GridRoot);
                lClone.name = $"GridElement_Character ({lIndex})";
            }

            for (int lIndex = 0; lIndex < _GridRoot.childCount; lIndex++)
            {
                CharacterGridItemView lItem = CreateGridItemView(_GridRoot.GetChild(lIndex) as RectTransform);
                if (lItem == null)
                    continue;

                bool lIsActive = lIndex < lAvailableUnits.Count;
                lItem.Root.gameObject.SetActive(lIsActive);
                if (!lIsActive)
                    continue;

                UnitDefinition lDefinition = lAvailableUnits[lIndex];
                BindGridItem(lItem, lDefinition, lIndex);
                _GridItems.Add(lItem);
            }
        }

        private void HookButtons()
        {
            for (int lIndex = 0; lIndex < _Slots.Count; lIndex++)
            {
                int lSlotIndex = lIndex;
                Button lSwitchButton = _Slots[lIndex].SwitchButton;
                if (lSwitchButton == null)
                    continue;

                lSwitchButton.onClick.RemoveAllListeners();
                lSwitchButton.onClick.AddListener(() => OpenGridForSlot(lSlotIndex));
            }

            if (_LaunchButton != null)
            {
                _LaunchButton.onClick.RemoveAllListeners();
                _LaunchButton.onClick.AddListener(LaunchCombatScene);
            }
        }

        #endregion

        #region _____________________________| FLOW

        private void RefreshSlotViews()
        {
            for (int lIndex = 0; lIndex < _Slots.Count; lIndex++)
            {
                UnitDefinition lDefinition = lIndex < _SelectedUnits.Count ? _SelectedUnits[lIndex] : null;
                BindSlot(_Slots[lIndex], lDefinition);
            }
        }

        private void OpenGridForSlot(int pSlotIndex)
        {
            _PendingSlotIndex = pSlotIndex;
            SetGridVisible(true);
        }

        private void SelectGridCharacter(int pUnitIndex)
        {
            List<UnitDefinition> lAvailableUnits = GetAvailableUnits();
            if (_PendingSlotIndex < 0 || _PendingSlotIndex >= _Slots.Count || pUnitIndex < 0 || pUnitIndex >= lAvailableUnits.Count)
                return;

            _SelectedUnits[_PendingSlotIndex] = lAvailableUnits[pUnitIndex];
            TeamSelectionState.SetSelectedUnits(_SelectedUnits);
            RefreshSlotViews();
            _PendingSlotIndex = -1;
            SetGridVisible(false);
        }

        private void LaunchCombatScene()
        {
            TeamSelectionState.SetSelectedUnits(_SelectedUnits);
            if (!string.IsNullOrWhiteSpace(_CombatSceneName))
                SceneManager.LoadScene(_CombatSceneName);
        }

        private void SetGridVisible(bool pVisible)
        {
            if (_GridPanel != null)
                _GridPanel.SetActive(pVisible);
        }

        #endregion

        #region _____________________________| BINDING

        private void BindSlot(CharacterSlotView pSlot, UnitDefinition pDefinition)
        {
            if (pSlot == null)
                return;

            if (pSlot.Name != null)
                pSlot.Name.text = pDefinition != null ? pDefinition.DisplayName : "Empty";

            if (pSlot.Visual != null)
            {
                pSlot.Visual.sprite = ResolvePreviewSprite(pDefinition);
                pSlot.Visual.preserveAspect = true;
                pSlot.Visual.enabled = pDefinition != null && pSlot.Visual.sprite != null;
                if (pSlot.Visual.enabled)
                    pSlot.Visual.color = pDefinition.Tint;
            }
        }

        private void BindGridItem(CharacterGridItemView pItem, UnitDefinition pDefinition, int pIndex)
        {
            if (pItem == null)
                return;

            if (pItem.Icon != null)
            {
                pItem.Icon.sprite = pDefinition != null ? pDefinition.Portrait : null;
                pItem.Icon.preserveAspect = true;
                pItem.Icon.enabled = pDefinition != null && pItem.Icon.sprite != null;
            }

            if (pItem.Name != null)
                pItem.Name.text = pDefinition != null ? pDefinition.DisplayName : "Empty";

            if (pItem.Button == null)
                pItem.Button = pItem.Root.GetComponent<Button>();

            if (pItem.Button != null)
            {
                pItem.Button.onClick.RemoveAllListeners();
                pItem.Button.onClick.AddListener(() => SelectGridCharacter(pIndex));
            }
        }

        #endregion

        #region _____________________________| HELPERS

        private static CharacterGridItemView CreateGridItemView(RectTransform pRoot) =>
            pRoot != null
                ? new CharacterGridItemView
            {
                Root = pRoot,
                Icon = pRoot.Find("Image")?.GetComponent<Image>(),
                Name = pRoot.Find("Text (TMP)")?.GetComponent<TMP_Text>(),
                Button = pRoot.GetComponent<Button>() ?? pRoot.gameObject.AddComponent<Button>()
            }
                : null;

        private static Sprite ResolvePreviewSprite(UnitDefinition pDefinition)
        {
            if (pDefinition == null)
                return null;

            if (pDefinition.UnitViewPrefab != null)
            {
                SpriteRenderer lRenderer = pDefinition.UnitViewPrefab.GetComponentInChildren<SpriteRenderer>(true);
                if (lRenderer != null && lRenderer.sprite != null)
                    return lRenderer.sprite;
            }

            return pDefinition.Portrait;
        }

        private static UnitDefinition PopRandomUnit(List<UnitDefinition> pPool)
        {
            if (pPool == null || pPool.Count == 0)
                return null;

            int lIndex = Random.Range(0, pPool.Count);
            UnitDefinition lDefinition = pPool[lIndex];
            pPool.RemoveAt(lIndex);
            return lDefinition;
        }

        private List<UnitDefinition> GetAvailableUnits()
        {
            List<UnitDefinition> lUnits = new List<UnitDefinition>(_AvailableUnits.Count);
            for (int lIndex = 0; lIndex < _AvailableUnits.Count; lIndex++)
            {
                if (_AvailableUnits[lIndex] != null)
                    lUnits.Add(_AvailableUnits[lIndex]);
            }

            return lUnits;
        }

        #endregion
    }
}
