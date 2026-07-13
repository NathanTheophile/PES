#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback
//  Menu UI
#endregion

using TacticalPort.Data;
using UnityEngine;
using UnityEngine.UI;

namespace TacticalPort.UI
{
    public sealed class DeckbuildingUnitPreviewView : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private RawImage _RenderTarget;
        [SerializeField] private Image _FallbackPortrait;
        [SerializeField] private Image _Background;
        [SerializeField] private Camera _PreviewCamera;
        [SerializeField] private Transform _ModelRoot;
        [SerializeField] private Vector3 _ModelRotationEuler = new Vector3(0f, 180f, 0f);
        [SerializeField, Min(1)] private int _TextureSize = 512;
        [SerializeField, Min(1f)] private float _FramingPadding = 1.25f;

        private RenderTexture _RenderTexture;
        private GameObject _RuntimeModel;
        private UnitDefinition _BoundUnit;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            if (_Background == null)
                _Background = GetComponent<Image>();

            SetTransparentBackground();
            SetFallback(null, false);
            SetRenderVisible(false);
            EnsureRenderTexture();
        }

        private void OnEnable()
        {
            EnsureRenderTexture();
        }

        private void OnDestroy()
        {
            ClearModel();
            ReleaseRenderTexture();
        }

        #endregion

        #region _____________________________| API

        public void Bind(UnitDefinition pUnit)
        {
            if (_BoundUnit == pUnit)
                return;

            _BoundUnit = pUnit;
            ClearModel();
            EnsureRenderTexture();

            Sprite lFallbackSprite = pUnit != null ? pUnit.DisplaySprite : null;
            bool lHasModel = pUnit != null && pUnit.ModelPrefab != null && _ModelRoot != null && _PreviewCamera != null;

            SetFallback(lFallbackSprite, !lHasModel);
            SetRenderVisible(lHasModel);

            if (!lHasModel)
                return;

            _RuntimeModel = Instantiate(pUnit.ModelPrefab, _ModelRoot);
            _RuntimeModel.transform.localPosition = Vector3.zero;
            _RuntimeModel.transform.localRotation = Quaternion.Euler(_ModelRotationEuler);
            _RuntimeModel.transform.localScale = Vector3.one;

            FrameModel();
        }

        #endregion

        #region _____________________________| HELPERS

        private void EnsureRenderTexture()
        {
            if (_RenderTarget == null || _PreviewCamera == null || _RenderTexture != null)
                return;

            _RenderTexture = new RenderTexture(_TextureSize, _TextureSize, 16, RenderTextureFormat.ARGB32)
            {
                name = $"{name}_RenderTexture",
                antiAliasing = 4
            };

            _RenderTarget.texture = _RenderTexture;
            _PreviewCamera.targetTexture = _RenderTexture;
            _PreviewCamera.clearFlags = CameraClearFlags.SolidColor;
            _PreviewCamera.backgroundColor = Color.clear;
        }

        private void SetTransparentBackground()
        {
            if (_Background == null)
                return;

            Color lColor = _Background.color;
            lColor.a = 0f;
            _Background.color = lColor;
        }

        private void ReleaseRenderTexture()
        {
            if (_PreviewCamera != null)
                _PreviewCamera.targetTexture = null;
            if (_RenderTarget != null)
                _RenderTarget.texture = null;
            if (_RenderTexture != null)
                _RenderTexture.Release();

            _RenderTexture = null;
        }

        private void ClearModel()
        {
            if (_RuntimeModel == null)
                return;

            Destroy(_RuntimeModel);
            _RuntimeModel = null;
        }

        private void FrameModel()
        {
            if (_RuntimeModel == null || _PreviewCamera == null)
                return;

            if (!TryGetModelBounds(_RuntimeModel, out Bounds lBounds))
                return;

            Vector3 lCenter = lBounds.center;
            float lHeight = Mathf.Max(0.1f, lBounds.size.y);
            float lWidth = Mathf.Max(0.1f, lBounds.size.x);
            float lDepth = Mathf.Max(0.1f, lBounds.size.z);
            float lSize = Mathf.Max(lHeight, lWidth, lDepth);

            _PreviewCamera.orthographic = true;
            _PreviewCamera.orthographicSize = lSize * 0.5f * _FramingPadding;
            _PreviewCamera.transform.position = lCenter + new Vector3(0f, lHeight * 0.15f, -lSize * 2.25f);
            _PreviewCamera.transform.rotation = Quaternion.LookRotation(lCenter - _PreviewCamera.transform.position, Vector3.up);
            _PreviewCamera.nearClipPlane = 0.01f;
            _PreviewCamera.farClipPlane = Mathf.Max(10f, lSize * 6f);
        }

        private static bool TryGetModelBounds(GameObject pModel, out Bounds pBounds)
        {
            Renderer[] lRenderers = pModel.GetComponentsInChildren<Renderer>(true);
            pBounds = default;

            bool lHasBounds = false;
            for (int lIndex = 0; lIndex < lRenderers.Length; lIndex++)
            {
                Renderer lRenderer = lRenderers[lIndex];
                if (lRenderer == null)
                    continue;

                if (!lHasBounds)
                {
                    pBounds = lRenderer.bounds;
                    lHasBounds = true;
                    continue;
                }

                pBounds.Encapsulate(lRenderer.bounds);
            }

            return lHasBounds;
        }

        private void SetFallback(Sprite pSprite, bool pVisible)
        {
            if (_FallbackPortrait == null)
                return;

            _FallbackPortrait.sprite = pSprite;
            _FallbackPortrait.preserveAspect = true;
            _FallbackPortrait.enabled = pVisible && pSprite != null;
        }

        private void SetRenderVisible(bool pVisible)
        {
            if (_RenderTarget != null)
                _RenderTarget.enabled = pVisible;
            if (_PreviewCamera != null)
                _PreviewCamera.enabled = pVisible;
        }

        #endregion
    }
}
