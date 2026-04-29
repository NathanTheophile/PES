#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections;
using TacticalPort.Core;
using TacticalPort.Data;
using TacticalPort.Shared;
using TMPro;
using UnityEngine;

namespace TacticalPort.View
{
    public sealed class UnitView : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private SpriteRenderer _SpriteRenderer;
        [SerializeField] private Color _DefeatedTint;
        [SerializeField] private int _BaseSortingOrder = 1000;
        [SerializeField] private int _BodySortingOrderOffset = 20;
        [SerializeField] private RectTransform _FeedbackRoot;
        [SerializeField] private TMP_Text _ValuePopupPrefab;
        [SerializeField] private Color _DamagePopupColor;
        [SerializeField] private Color _HealPopupColor;
        [SerializeField] private float _PopupLifetimeSeconds = 2f;

        private UnitRuntime _Runtime;
        private UnitDefinition _Definition;
        private BoardView _BoardView;
        private Vector3 _BaseSpriteLocalScale = Vector3.one;
        private Color _BaseSpriteColor;
        private bool _HasBaseSpriteColor;

        #endregion

        #region _____________________________/ ACCESSORS

        public UnitId UnitId => _Runtime != null ? _Runtime.Id : UnitId.None;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            ValidateReferences();
            CacheBaseSpriteScale();
        }

        private void OnValidate()
        {
            CacheBaseSpriteScale();
        }

        #endregion

        #region _____________________________| BINDING

        public void Bind(UnitRuntime pRuntime, UnitDefinition pDefinition, BoardView pBoardView)
        {
            if (_Runtime != null)
                _Runtime.ValueChanged -= HandleValueChanged;

            _Runtime = pRuntime;
            _Definition = pDefinition;
            _BoardView = pBoardView;

            if (_Runtime != null)
                _Runtime.ValueChanged += HandleValueChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_Runtime != null)
                _Runtime.ValueChanged -= HandleValueChanged;
        }

        #endregion

        #region _____________________________| DISPLAY

        public void Refresh()
        {
            if (_Runtime == null || _BoardView == null)
                return;

            transform.position = ResolveWorldPosition();
            gameObject.name = $"UnitView_{_Runtime.Id}_{_Runtime.Definition.DisplayName}";

            if (_SpriteRenderer != null)
            {
                _SpriteRenderer.enabled = true;
                _SpriteRenderer.color = _Runtime.IsAlive ? ResolveAliveTint() : _DefeatedTint;
                _SpriteRenderer.sortingOrder = ResolveSortingOrder(_BodySortingOrderOffset);
                _SpriteRenderer.transform.localScale = _BaseSpriteLocalScale;
            }
        }

        #endregion

        #region _____________________________| HELPERS

        private int ResolveSortingOrder(int pOffset) =>
            _Runtime != null ? _BaseSortingOrder - ((_Runtime.Position.X + _Runtime.Position.Y) * 10) + pOffset : pOffset;

        private Vector3 ResolveWorldPosition()
        {
            if (_Runtime == null || _BoardView == null)
                return transform.position;

            Vector3 lAccumulated = Vector3.zero;
            int lCellCount = 0;
            foreach (GridCoord lCell in _Runtime.EnumerateOccupiedCells())
            {
                lAccumulated += _BoardView.GetWorldPosition(lCell);
                lCellCount++;
            }

            if (lCellCount <= 0)
                return _BoardView.GetWorldPosition(_Runtime.Position);

            return lAccumulated / lCellCount;
        }

        private void CacheBaseSpriteScale()
        {
            if (_SpriteRenderer != null && _SpriteRenderer.transform != null)
            {
                _BaseSpriteLocalScale = _SpriteRenderer.transform.localScale;
                _BaseSpriteColor = _SpriteRenderer.color;
                _HasBaseSpriteColor = true;
            }
        }

        private Color ResolveAliveTint() =>
            _Definition != null
                ? _Definition.Tint
                : _HasBaseSpriteColor
                    ? _BaseSpriteColor
                    : default;

        private void HandleValueChanged(int pAmount, bool pIsHeal)
        {
            if (pAmount <= 0 || _FeedbackRoot == null || _ValuePopupPrefab == null)
                return;

            TMP_Text lPopup = Instantiate(_ValuePopupPrefab, _FeedbackRoot);
            lPopup.text = pIsHeal ? $"+ {pAmount}" : $"- {pAmount}";
            lPopup.color = pIsHeal ? _HealPopupColor : _DamagePopupColor;
            StartCoroutine(DestroyPopupAfterDelay(lPopup.gameObject));
        }

        private IEnumerator DestroyPopupAfterDelay(GameObject pPopup)
        {
            yield return new WaitForSeconds(Mathf.Max(0.1f, _PopupLifetimeSeconds));

            if (pPopup != null)
                Destroy(pPopup);
        }

        private void ValidateReferences()
        {
            LogMissingReference(_SpriteRenderer, nameof(_SpriteRenderer));
            LogMissingReference(_FeedbackRoot, nameof(_FeedbackRoot));
            LogMissingReference(_ValuePopupPrefab, nameof(_ValuePopupPrefab));
        }

        private void LogMissingReference(Object pReference, string pFieldName)
        {
            if (pReference == null)
                Debug.LogWarning($"{nameof(UnitView)} is missing reference '{pFieldName}'.", this);
        }

        #endregion
    }
}
