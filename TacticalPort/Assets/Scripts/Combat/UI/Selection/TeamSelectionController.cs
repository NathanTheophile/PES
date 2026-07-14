#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using TacticalPort.App;
using TacticalPort.Data;
using TacticalPort.Shared;
using TacticalPort.State;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TacticalPort.UI
{
    public sealed class TeamSelectionController : MonoBehaviour, ITeamSelectionPopupView
    {
        #region _____________________________/ TYPES

        [System.Serializable]
        private sealed class CharacterSlotView
        {
            public RectTransform Root = null;
            public Image Visual = null;
            public TMP_Text Name = null;
            public Button SwitchButton = null;
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
        [SerializeField] private string _MainMenuSceneName = "S_MainMenu";
        [Tooltip("Message displayed by the persistent loading screen when the standalone scene returns to frontend.")]
        [SerializeField] private string _LoadingMessage = "Returning to menu...";
        [SerializeField] private List<CharacterSlotView> _Slots = new List<CharacterSlotView>(3);
        [SerializeField] private RectTransform _GridRoot;
        [SerializeField] private RectTransform _GridItemPrefab;
        [SerializeField] private GameObject _GridPanel;
        [SerializeField] private Button _SaveButton;
        [SerializeField, HideInInspector] private Button _LaunchButton;
        [SerializeField] private TMP_Text _SaveButtonLabel;
        [SerializeField] private MonoBehaviour _EmbeddedMenuRouterSource;
        [Tooltip("Optional persistent transition service used only by the standalone scene return path.")]
        [SerializeField] private MonoBehaviour _SceneTransitionServiceSource;
        [SerializeField] private bool _UseEmbeddedReturn;

        private readonly List<UnitDefinition> _SelectedUnits = new List<UnitDefinition>(3);

        private int _PendingSlotIndex = -1;
        private ISceneTransitionService _SceneTransitionService;
        private IMenuPopupRouter _EmbeddedMenuRouter;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            LogMissingReferences();
            InitializeSelection();
            RefreshSlotViews();
            BuildGrid();
            SetGridVisible(false);
            HookButtons();
            RefreshSaveButtonLabel();
        }

        #endregion

        #region _____________________________| SETUP

        public void ConfigureSceneTransitionService(MonoBehaviour pServiceSource)
        {
            _SceneTransitionServiceSource = pServiceSource;
            _SceneTransitionService = pServiceSource as ISceneTransitionService;
        }

        private void InitializeSelection()
        {
            _SelectedUnits.Clear();
            List<UnitDefinition> lAvailableUnits = GetAvailableUnits();
            if (lAvailableUnits.Count == 0)
                return;

            if (TryLoadCurrentSelection(lAvailableUnits, _SelectedUnits))
            {
                FillMissingSlots(lAvailableUnits, _SelectedUnits);
                TeamSelectionState.SetSelectedUnits(_SelectedUnits);
                return;
            }

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
            if (_GridRoot == null || _GridItemPrefab == null)
                return;

            ClearGrid();
            List<UnitDefinition> lAvailableUnits = GetAvailableUnits();

            for (int lIndex = 0; lIndex < lAvailableUnits.Count; lIndex++)
            {
                RectTransform lInstance = Instantiate(_GridItemPrefab, _GridRoot);
                lInstance.name = $"GridElement_Character_{lIndex + 1}";
                lInstance.gameObject.SetActive(true);

                CharacterGridItemView lItem = CreateGridItemView(lInstance);
                if (lItem == null)
                    continue;

                BindGridItem(lItem, lAvailableUnits[lIndex], lIndex);
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

            Button lSaveButton = ResolveSaveButton();
            if (lSaveButton != null)
            {
                lSaveButton.onClick.RemoveAllListeners();
                lSaveButton.onClick.AddListener(SaveSelectionAndReturnToMenu);
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

        private void SaveSelectionAndReturnToMenu()
        {
            TeamSelectionState.SetSelectedUnits(_SelectedUnits);
            TeamSelectionState.SaveSelectedUnits();

            if (_UseEmbeddedReturn && ResolveEmbeddedMenuRouter())
            {
                _EmbeddedMenuRouter.ShowMainMenu();
                return;
            }

            if (string.IsNullOrWhiteSpace(_MainMenuSceneName))
                return;

            if (ResolveSceneTransitionService())
            {
                _SceneTransitionService.LoadScene(_MainMenuSceneName, LoadSceneMode.Single, _LoadingMessage);
                return;
            }

            SceneManager.LoadScene(_MainMenuSceneName);
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
                pItem.Icon.sprite = pDefinition != null ? pDefinition.DisplaySprite : null;
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

        public void RefreshSelectionView()
        {
            InitializeSelection();
            RefreshSlotViews();
            SetGridVisible(false);
            RefreshSaveButtonLabel();
        }

        private void ClearGrid()
        {
            for (int lIndex = _GridRoot.childCount - 1; lIndex >= 0; lIndex--)
            {
                Transform lChild = _GridRoot.GetChild(lIndex);
                if (lChild == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(lChild.gameObject);
                else
                    DestroyImmediate(lChild.gameObject);
            }
        }

        private static CharacterGridItemView CreateGridItemView(RectTransform pRoot) =>
            pRoot != null
                ? new CharacterGridItemView
            {
                Root = pRoot,
                Icon = FindChildComponent<Image>(pRoot, "Img_Icon", "Image"),
                Name = FindChildComponent<TMP_Text>(pRoot, "Txt_Name", "Text (TMP)"),
                Button = pRoot.GetComponent<Button>()
            }
                : null;

        private static T FindChildComponent<T>(Transform pRoot, params string[] pNames) where T : Component
        {
            if (pRoot == null)
                return null;

            for (int lIndex = 0; lIndex < pNames.Length; lIndex++)
            {
                Transform lChild = pRoot.Find(pNames[lIndex]);
                if (lChild != null && lChild.TryGetComponent(out T lComponent))
                    return lComponent;
            }

            return null;
        }

        private static Sprite ResolvePreviewSprite(UnitDefinition pDefinition) => pDefinition != null ? pDefinition.DisplaySprite : null;

        private bool TryLoadCurrentSelection(IReadOnlyList<UnitDefinition> pAvailableUnits, List<UnitDefinition> pTarget)
        {
            if (!TeamSelectionState.HasSelection && !TeamSelectionState.LoadSavedUnitIds())
                return false;

            if (TeamSelectionState.SelectedUnits.Count > 0)
            {
                List<string> lUnitIds = new List<string>(TeamSelectionState.SelectedUnits.Count);
                for (int lIndex = 0; lIndex < TeamSelectionState.SelectedUnits.Count; lIndex++)
                {
                    UnitDefinition lUnit = TeamSelectionState.SelectedUnits[lIndex];
                    if (lUnit != null)
                        lUnitIds.Add(lUnit.Id);
                }

                return TryResolveSelection(lUnitIds, pAvailableUnits, pTarget);
            }

            return TryResolveSelection(TeamSelectionState.SelectedUnitIds, pAvailableUnits, pTarget);
        }

        private static bool TryResolveSelection(IReadOnlyList<string> pUnitIds, IReadOnlyList<UnitDefinition> pAvailableUnits, List<UnitDefinition> pTarget)
        {
            pTarget.Clear();
            if (pUnitIds == null || pAvailableUnits == null)
                return false;

            for (int lIndex = 0; lIndex < pUnitIds.Count; lIndex++)
            {
                UnitDefinition lUnit = FindAvailableUnitById(pAvailableUnits, pUnitIds[lIndex]);
                if (lUnit != null)
                    pTarget.Add(lUnit);
            }

            return pTarget.Count > 0;
        }

        private static UnitDefinition FindAvailableUnitById(IReadOnlyList<UnitDefinition> pAvailableUnits, string pId)
        {
            if (string.IsNullOrWhiteSpace(pId))
                return null;

            for (int lIndex = 0; lIndex < pAvailableUnits.Count; lIndex++)
            {
                UnitDefinition lUnit = pAvailableUnits[lIndex];
                if (lUnit != null && string.Equals(lUnit.Id, pId, System.StringComparison.Ordinal))
                    return lUnit;
            }

            return null;
        }

        private void FillMissingSlots(IReadOnlyList<UnitDefinition> pAvailableUnits, List<UnitDefinition> pTarget)
        {
            if (pAvailableUnits == null || pAvailableUnits.Count == 0 || pTarget == null)
                return;

            int lSlotCount = _Slots != null ? _Slots.Count : 0;
            while (pTarget.Count < lSlotCount)
                pTarget.Add(pAvailableUnits[pTarget.Count % pAvailableUnits.Count]);

            if (pTarget.Count > lSlotCount)
                pTarget.RemoveRange(lSlotCount, pTarget.Count - lSlotCount);
        }

        private void RefreshSaveButtonLabel()
        {
            Button lSaveButton = ResolveSaveButton();
            TMP_Text lLabel = _SaveButtonLabel != null
                ? _SaveButtonLabel
                : lSaveButton != null
                    ? lSaveButton.GetComponentInChildren<TMP_Text>(true)
                    : null;

            if (lLabel != null)
                lLabel.text = "SAVE";
        }

        private Button ResolveSaveButton() => _SaveButton != null ? _SaveButton : _LaunchButton;

        private bool ResolveEmbeddedMenuRouter()
        {
            _EmbeddedMenuRouter ??= _EmbeddedMenuRouterSource as IMenuPopupRouter;
            if (_EmbeddedMenuRouter != null)
                return true;

            MonoBehaviour[] lBehaviours = GetComponentsInParent<MonoBehaviour>(true);
            for (int lIndex = 0; lIndex < lBehaviours.Length; lIndex++)
            {
                if (lBehaviours[lIndex] is IMenuPopupRouter lRouter)
                {
                    _EmbeddedMenuRouterSource = lBehaviours[lIndex];
                    _EmbeddedMenuRouter = lRouter;
                    return true;
                }
            }

            Debug.LogWarning($"{nameof(TeamSelectionController)} uses embedded return but has no {nameof(IMenuPopupRouter)} reference.", this);
            return false;
        }

        private bool ResolveSceneTransitionService()
        {
            _SceneTransitionService ??= _SceneTransitionServiceSource as ISceneTransitionService;
            return _SceneTransitionService != null;
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

        private void LogMissingReferences()
        {
            if (_Slots == null || _Slots.Count == 0)
                Debug.LogWarning($"{nameof(TeamSelectionController)} has no configured character slots.", this);

            LogMissingReference(_GridRoot, nameof(_GridRoot));
            LogMissingReference(_GridItemPrefab, nameof(_GridItemPrefab));
            LogMissingReference(_GridPanel, nameof(_GridPanel));
            LogMissingReference(ResolveSaveButton(), nameof(_SaveButton));
        }

        private void LogMissingReference(Object pReference, string pFieldName)
        {
            if (pReference == null)
                Debug.LogWarning($"{nameof(TeamSelectionController)} is missing reference '{pFieldName}'.", this);
        }

        #endregion
    }
}
