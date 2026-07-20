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

        [Tooltip("Optional 3D model root. If empty, this UnitView transform is used as the model root.")]
        [SerializeField] private Transform _ModelRoot;
        [Tooltip("Optional Animator driven by the spawned or prebuilt 3D model.")]
        [SerializeField] private Animator _Animator;
        [Tooltip("Local rotation applied to the instantiated 3D model. The shared actor billboard expects character models to face the camera after a 180-degree yaw.")]
        [SerializeField] private Vector3 _ModelRotationEuler = new Vector3(0f, 180f, 0f);
        [Tooltip("Renderers tinted from the UnitDefinition tint and defeated tint. If empty, they are cached from the model root at runtime.")]
        [SerializeField] private Renderer[] _TintRenderers = System.Array.Empty<Renderer>();
        [Tooltip("Material color property used for 3D model tinting. URP Lit uses _BaseColor.")]
        [SerializeField] private string _TintColorProperty = "_BaseColor";
        [Tooltip("When enabled, UnitView instantiates UnitDefinition.ModelPrefab under the model root when binding.")]
        [SerializeField] private bool _InstantiateDefinitionModelPrefab = true;
        [Tooltip("Optional 2D fallback used when the unit does not provide a 3D model.")]
        [SerializeField] private SpriteRenderer _SpriteFallbackRenderer;
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

        #endregion

        #region _____________________________/ ACCESSORS

        public UnitId UnitId => _Runtime != null ? _Runtime.Id : UnitId.None;
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
            gameObject.name = $"UnitView_{_Runtime.Id}_{_Runtime.Definition.Id}";
            ApplyVisualState();
        }

        #endregion

        #region _____________________________| MODEL

        private void RebuildRuntimeModelIfNeeded()
        {
            DestroyRuntimeModel();

            if (!_InstantiateDefinitionModelPrefab || _Definition == null || _Definition.ModelPrefab == null)
                return;

            Transform lModelRoot = ResolveModelRoot();
            _RuntimeModelInstance = Instantiate(_Definition.ModelPrefab, lModelRoot);
            _RuntimeModelInstance.transform.localPosition = Vector3.zero;
            _RuntimeModelInstance.transform.localRotation = Quaternion.Euler(_ModelRotationEuler);
            _RuntimeModelInstance.transform.localScale = Vector3.one;

            if (_Animator == null)
                _Animator = _RuntimeModelInstance.GetComponentInChildren<Animator>(true);

            CacheTintRenderers(_RuntimeModelInstance.transform);
        }

        private void DestroyRuntimeModel()
        {
            if (_RuntimeModelInstance == null)
                return;

            if (_Animator != null && _Animator.transform.IsChildOf(_RuntimeModelInstance.transform))
                _Animator = null;

            _TintRenderers = System.Array.Empty<Renderer>();

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
            ApplySpriteFallbackState(lTint);
            ApplyRendererTint(lTint);
        }

        private void ApplySpriteFallbackState(Color pTint)
        {
            if (_SpriteFallbackRenderer == null)
                return;

            _SpriteFallbackRenderer.enabled = _RuntimeModelInstance == null;
            _SpriteFallbackRenderer.color = pTint;
            _SpriteFallbackRenderer.sortingOrder = ResolveSortingOrder(_BodySortingOrderOffset);
            _SpriteFallbackRenderer.transform.localScale = _BaseSpriteLocalScale;
        }

        private void ApplyRendererTint(Color pTint)
        {
            if (_TintRenderers == null || _TintRenderers.Length == 0)
                CacheTintRenderers(ResolveModelRoot());

            if (_TintRenderers == null || _TintRenderers.Length == 0)
                return;

            _TintPropertyBlock ??= new MaterialPropertyBlock();

            for (int lIndex = 0; lIndex < _TintRenderers.Length; lIndex++)
            {
                Renderer lRenderer = _TintRenderers[lIndex];
                if (lRenderer == null)
                    continue;

                Material[] lMaterials = lRenderer.sharedMaterials;
                for (int lMaterialIndex = 0; lMaterialIndex < lMaterials.Length; lMaterialIndex++)
                {
                    Material lMaterial = lMaterials[lMaterialIndex];
                    string lColorProperty = ResolveMaterialColorProperty(lMaterial);
                    if (lMaterial == null || string.IsNullOrEmpty(lColorProperty))
                        continue;

                    int lColorPropertyId = Shader.PropertyToID(lColorProperty);
                    Color lBaseColor = lMaterial.GetColor(lColorPropertyId);
                    lRenderer.GetPropertyBlock(_TintPropertyBlock, lMaterialIndex);
                    _TintPropertyBlock.SetColor(lColorPropertyId, lBaseColor * pTint);
                    lRenderer.SetPropertyBlock(_TintPropertyBlock, lMaterialIndex);
                }
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
            if (_SpriteFallbackRenderer != null && _SpriteFallbackRenderer.transform != null)
            {
                _BaseSpriteLocalScale = _SpriteFallbackRenderer.transform.localScale;
                _BaseSpriteColor = _SpriteFallbackRenderer.color;
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

        private string ResolveMaterialColorProperty(Material pMaterial)
        {
            if (pMaterial == null)
                return null;

            if (!string.IsNullOrWhiteSpace(_TintColorProperty) && pMaterial.HasProperty(_TintColorProperty))
                return _TintColorProperty;

            if (pMaterial.HasProperty("_BaseColor"))
                return "_BaseColor";

            return pMaterial.HasProperty("_Color") ? "_Color" : null;
        }

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
