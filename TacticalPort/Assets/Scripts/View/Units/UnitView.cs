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
using UnityEngine.Serialization;

namespace TacticalPort.View
{
    public sealed class UnitView : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [Tooltip("Optional 3D model root. If empty, this UnitView transform is used as the model root.")]
        [SerializeField] private Transform _ModelRoot;
        [Tooltip("Optional Animator driven by the spawned or prebuilt 3D model.")]
        [SerializeField] private Animator _Animator;
        [Tooltip("Renderers tinted from the UnitDefinition tint and defeated tint. If empty, they are cached from the model root at runtime.")]
        [SerializeField] private Renderer[] _TintRenderers = System.Array.Empty<Renderer>();
        [Tooltip("Material color property used for 3D model tinting. URP Lit uses _BaseColor.")]
        [SerializeField] private string _TintColorProperty = "_BaseColor";
        [Tooltip("When enabled, UnitView instantiates UnitDefinition.ModelPrefab under the model root when binding.")]
        [SerializeField] private bool _InstantiateDefinitionModelPrefab = true;
        [Tooltip("Legacy 2D fallback while old prefabs are migrated to 3D models.")]
        [FormerlySerializedAs("_SpriteRenderer")]
        [SerializeField] private SpriteRenderer _LegacySpriteRenderer;
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
        private GameObject _RuntimeModelInstance;
        private MaterialPropertyBlock _TintPropertyBlock;
        private int _TintColorPropertyId;

        #endregion

        #region _____________________________/ ACCESSORS

        public UnitId UnitId => _Runtime != null ? _Runtime.Id : UnitId.None;
        public Sprite BodySprite => _LegacySpriteRenderer != null ? _LegacySpriteRenderer.sprite : null;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            CacheMissingReferences();
            CacheVisualReferences();
        }

        private void OnValidate()
        {
            CacheVisualReferences();
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
            RebuildRuntimeModelIfNeeded();

            if (_Runtime != null)
                _Runtime.ValueChanged += HandleValueChanged;
            Refresh();
        }

        private void OnDestroy()
        {
            if (_Runtime != null)
                _Runtime.ValueChanged -= HandleValueChanged;

            DestroyRuntimeModel();
        }

        #endregion

        #region _____________________________| DISPLAY

        public void Refresh()
        {
            if (_Runtime == null || _BoardView == null)
                return;

            transform.position = ResolveWorldPosition();
            gameObject.name = $"UnitView_{_Runtime.Id}_{_Runtime.Definition.DisplayName}";
            ApplyVisualState();
        }

        #endregion

        #region _____________________________| MODEL

        private void RebuildRuntimeModelIfNeeded()
        {
            if (!_InstantiateDefinitionModelPrefab || _Definition == null || _Definition.ModelPrefab == null)
                return;

            DestroyRuntimeModel();

            Transform lModelRoot = ResolveModelRoot();
            _RuntimeModelInstance = Instantiate(_Definition.ModelPrefab, lModelRoot);
            _RuntimeModelInstance.transform.localPosition = Vector3.zero;
            _RuntimeModelInstance.transform.localRotation = Quaternion.identity;
            _RuntimeModelInstance.transform.localScale = Vector3.one;

            if (_Animator == null)
                _Animator = _RuntimeModelInstance.GetComponentInChildren<Animator>(true);

            CacheTintRenderers(lModelRoot);
        }

        private void DestroyRuntimeModel()
        {
            if (_RuntimeModelInstance == null)
                return;

            if (Application.isPlaying)
                Destroy(_RuntimeModelInstance);
            else
                DestroyImmediate(_RuntimeModelInstance);

            _RuntimeModelInstance = null;
        }

        private Transform ResolveModelRoot() => _ModelRoot != null ? _ModelRoot : transform;

        private void ApplyVisualState()
        {
            Color lTint = _Runtime != null && _Runtime.IsAlive ? ResolveAliveTint() : _DefeatedTint;
            ApplyLegacySpriteState(lTint);
            ApplyRendererTint(lTint);
        }

        private void ApplyLegacySpriteState(Color pTint)
        {
            if (_LegacySpriteRenderer == null)
                return;

            _LegacySpriteRenderer.enabled = true;
            _LegacySpriteRenderer.color = pTint;
            _LegacySpriteRenderer.sortingOrder = ResolveSortingOrder(_BodySortingOrderOffset);
            _LegacySpriteRenderer.transform.localScale = _BaseSpriteLocalScale;
        }

        private void ApplyRendererTint(Color pTint)
        {
            if (_TintRenderers == null || _TintRenderers.Length == 0)
                CacheTintRenderers(ResolveModelRoot());

            if (_TintRenderers == null || _TintRenderers.Length == 0)
                return;

            _TintPropertyBlock ??= new MaterialPropertyBlock();
            _TintColorPropertyId = ResolveTintColorPropertyId();

            for (int lIndex = 0; lIndex < _TintRenderers.Length; lIndex++)
            {
                Renderer lRenderer = _TintRenderers[lIndex];
                if (lRenderer == null)
                    continue;

                lRenderer.GetPropertyBlock(_TintPropertyBlock);
                _TintPropertyBlock.SetColor(_TintColorPropertyId, pTint);
                _TintPropertyBlock.SetColor("_Color", pTint);
                lRenderer.SetPropertyBlock(_TintPropertyBlock);
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

        private void CacheVisualReferences()
        {
            if (_LegacySpriteRenderer != null && _LegacySpriteRenderer.transform != null)
            {
                _BaseSpriteLocalScale = _LegacySpriteRenderer.transform.localScale;
                _BaseSpriteColor = _LegacySpriteRenderer.color;
                _HasBaseSpriteColor = true;
            }

            _TintColorPropertyId = ResolveTintColorPropertyId();
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
                .Join(DOTween.To(
                    () => lRectTransform.anchoredPosition,
                    pPosition => lRectTransform.anchoredPosition = pPosition,
                    lEndPosition,
                    lLifetime).SetEase(Ease.OutCubic))
                .Join(DOTween.To(
                    () => pPopup.color.a,
                    alpha => pPopup.color = new Color(lStartColor.r, lStartColor.g, lStartColor.b, alpha),
                    0f,
                    lLifetime));

            if (lHalfPunchDuration > 0f)
            {
                lSequence
                    .Insert(0f, DOTween.To(
                        () => lRectTransform.localScale,
                        pScale => lRectTransform.localScale = pScale,
                        lPeakScale,
                        lHalfPunchDuration).SetEase(Ease.OutCubic))
                    .Insert(lHalfPunchDuration, DOTween.To(
                        () => lRectTransform.localScale,
                        pScale => lRectTransform.localScale = pScale,
                        lBaseScale,
                        lHalfPunchDuration).SetEase(Ease.OutCubic));
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

        private void CacheTintRenderers(Transform pRoot)
        {
            if (pRoot == null)
                return;

            Renderer[] lRenderers = pRoot.GetComponentsInChildren<Renderer>(true);
            if (lRenderers == null || lRenderers.Length == 0)
                return;

            _TintRenderers = lRenderers;
        }

        private int ResolveTintColorPropertyId() =>
            Shader.PropertyToID(string.IsNullOrWhiteSpace(_TintColorProperty) ? "_BaseColor" : _TintColorProperty);

        private void CacheMissingReferences()
        {
            if (_FeedbackRoot != null && _ValuePopupPrefab == null)
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
