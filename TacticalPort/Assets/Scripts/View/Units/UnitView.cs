#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using DG.Tweening;
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
        [SerializeField] private Vector2 _PopupTravelOffset = new Vector2(0f, 80f);
        [SerializeField, Min(0.01f)] private float _PopupScalePunchDurationPercent = 0.2f;
        [SerializeField, Min(0f)] private float _PopupSpawnScale = 0.85f;
        [SerializeField, Min(0f)] private float _PopupPeakScale = 1.25f;

        private UnitRuntime _Runtime;
        private UnitDefinition _Definition;
        private BoardView _BoardView;
        private Vector3 _BaseSpriteLocalScale = Vector3.one;
        private Color _BaseSpriteColor;
        private bool _HasBaseSpriteColor;

        #endregion

        #region _____________________________/ ACCESSORS

        public UnitId UnitId => _Runtime != null ? _Runtime.Id : UnitId.None;
        public Sprite BodySprite => _SpriteRenderer != null ? _SpriteRenderer.sprite : null;

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
            AnimateValuePopup(lPopup);
        }

        private void AnimateValuePopup(TMP_Text pPopup)
        {
            if (pPopup == null)
                return;

            RectTransform lRectTransform = pPopup.rectTransform;
            Vector2 lEndPosition = lRectTransform.anchoredPosition + _PopupTravelOffset;
            Vector3 lBaseScale = lRectTransform.localScale;
            Vector3 lSpawnScale = lBaseScale * _PopupSpawnScale;
            Vector3 lPeakScale = lBaseScale * _PopupPeakScale;
            Color lStartColor = pPopup.color;
            float lLifetime = Mathf.Max(0.1f, _PopupLifetimeSeconds);
            float lPunchDuration = lLifetime * Mathf.Clamp01(_PopupScalePunchDurationPercent);
            float lHalfPunchDuration = lPunchDuration * 0.5f;

            lRectTransform.localScale = lSpawnScale;

            Sequence lSequence = DOTween.Sequence()
                .SetTarget(pPopup)
                .SetLink(pPopup.gameObject)
                .Join(lRectTransform.DOAnchorPos(lEndPosition, lLifetime).SetEase(Ease.OutCubic))
                .Join(DOTween.To(
                    () => pPopup.color.a,
                    alpha => pPopup.color = new Color(lStartColor.r, lStartColor.g, lStartColor.b, alpha),
                    0f,
                    lLifetime));

            if (lHalfPunchDuration > 0f)
            {
                lSequence
                    .Insert(0f, lRectTransform.DOScale(lPeakScale, lHalfPunchDuration).SetEase(Ease.OutCubic))
                    .Insert(lHalfPunchDuration, lRectTransform.DOScale(lBaseScale, lHalfPunchDuration).SetEase(Ease.OutCubic));
            }
            else
            {
                lRectTransform.localScale = lBaseScale;
            }

            lSequence.OnComplete(() =>
            {
                if (pPopup != null)
                    Destroy(pPopup.gameObject);
            });
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
