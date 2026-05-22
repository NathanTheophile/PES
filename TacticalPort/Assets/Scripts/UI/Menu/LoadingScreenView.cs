#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  UI
#endregion

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TacticalPort.UI
{
    public sealed class LoadingScreenView : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [Header("Root")]
        [Tooltip("Root object toggled when the loading screen is shown or hidden.")]
        [SerializeField] private GameObject _Root;
        [Tooltip("CanvasGroup used to block input and control visibility.")]
        [SerializeField] private CanvasGroup _CanvasGroup;

        [Header("Visual Layers")]
        [Tooltip("Fullscreen background image faded before the loading content appears.")]
        [SerializeField] private Image _BackgroundImage;
        [Tooltip("Optional CanvasGroup for all loading content. If missing, child UI graphics are faded directly.")]
        [SerializeField] private CanvasGroup _ContentCanvasGroup;

        [Header("Content")]
        [Tooltip("Main loading message label.")]
        [SerializeField] private TMP_Text _MessageText;
        [Tooltip("Optional percent label.")]
        [SerializeField] private TMP_Text _ProgressText;
        [Tooltip("Optional progress fill image.")]
        [SerializeField] private Image _ProgressFillImage;
        [Tooltip("Shows the loading progress as a percentage when a progress label is assigned.")]
        [SerializeField] private bool _ShowProgressPercent = true;

        private Graphic[] _ContentGraphics;
        private float[] _ContentBaseAlphas;

        #endregion

        #region _____________________________| UNITY

        private void Awake() => CacheMissingReferences();

        private void OnValidate() => CacheMissingReferences();

        #endregion

        #region _____________________________| DISPLAY

        public void Show(string pMessage, float pProgress) => Show(pMessage, pProgress, 1f, 1f);

        public void Show(string pMessage, float pProgress, float pAlpha) => Show(pMessage, pProgress, pAlpha, pAlpha);

        public void Show(string pMessage, float pProgress, float pBackgroundAlpha, float pContentAlpha)
        {
            CacheMissingReferences();
            SetRootActive(true);
            SetCanvasInput(true);
            SetLayerAlphas(pBackgroundAlpha, pContentAlpha);
            SetMessage(pMessage);
            SetProgress(pProgress);
        }

        public void SetProgress(float pProgress)
        {
            float lProgress = Mathf.Clamp01(pProgress);

            if (_ProgressFillImage != null)
                _ProgressFillImage.fillAmount = lProgress;

            if (_ProgressText != null)
                _ProgressText.text = _ShowProgressPercent ? $"{Mathf.RoundToInt(lProgress * 100f)}%" : string.Empty;
        }

        public void HideImmediate()
        {
            CacheMissingReferences();
            SetLayerAlphas(0f, 0f);
            SetCanvasInput(false);
            SetRootActive(false);
        }

        public void SetAlpha(float pAlpha) => SetLayerAlphas(pAlpha, pAlpha);

        public void SetBackgroundAlpha(float pAlpha)
        {
            CacheMissingReferences();
            SetBackgroundAlphaInternal(pAlpha);
        }

        public void SetContentAlpha(float pAlpha)
        {
            CacheMissingReferences();
            SetContentAlphaInternal(pAlpha);
        }

        public void SetLayerAlphas(float pBackgroundAlpha, float pContentAlpha)
        {
            CacheMissingReferences();
            SetBackgroundAlphaInternal(pBackgroundAlpha);
            SetContentAlphaInternal(pContentAlpha);
        }

        public void ConfigureGeneratedView(GameObject pRoot, CanvasGroup pCanvasGroup, TMP_Text pMessageText, TMP_Text pProgressText, Image pProgressFillImage)
        {
            _Root = pRoot;
            _CanvasGroup = pCanvasGroup;
            _MessageText = pMessageText;
            _ProgressText = pProgressText;
            _ProgressFillImage = pProgressFillImage;
            _BackgroundImage = pRoot != null ? pRoot.GetComponent<Image>() : null;
            _ContentCanvasGroup = null;
            _ContentGraphics = null;
            _ContentBaseAlphas = null;
        }

        #endregion

        #region _____________________________| HELPERS

        private void CacheMissingReferences()
        {
            if (_Root == null)
                _Root = gameObject;

            if (_CanvasGroup == null)
                _CanvasGroup = GetComponent<CanvasGroup>();

            if (_BackgroundImage == null)
                _BackgroundImage = GetComponent<Image>();

            CacheContentGraphics();
        }

        private void SetMessage(string pMessage)
        {
            if (_MessageText != null)
                _MessageText.text = string.IsNullOrWhiteSpace(pMessage) ? "Loading..." : pMessage;
        }

        private void SetRootActive(bool pIsActive)
        {
            if (_Root != null && _Root.activeSelf != pIsActive)
                _Root.SetActive(pIsActive);
        }

        private void SetCanvasVisible(bool pIsVisible)
        {
            SetLayerAlphas(pIsVisible ? 1f : 0f, pIsVisible ? 1f : 0f);
            SetCanvasInput(pIsVisible);
        }

        private void SetCanvasInput(bool pBlocksInput)
        {
            if (_CanvasGroup == null)
                return;

            _CanvasGroup.alpha = 1f;
            _CanvasGroup.interactable = pBlocksInput;
            _CanvasGroup.blocksRaycasts = pBlocksInput;
        }

        private void SetBackgroundAlphaInternal(float pAlpha)
        {
            if (_BackgroundImage == null)
                return;

            Color lColor = _BackgroundImage.color;
            lColor.a = Mathf.Clamp01(pAlpha);
            _BackgroundImage.color = lColor;
        }

        private void SetContentAlphaInternal(float pAlpha)
        {
            float lAlpha = Mathf.Clamp01(pAlpha);

            if (_ContentCanvasGroup != null)
            {
                _ContentCanvasGroup.alpha = lAlpha;
                return;
            }

            if (_ContentGraphics == null || _ContentBaseAlphas == null)
                return;

            for (int lIndex = 0; lIndex < _ContentGraphics.Length; lIndex++)
            {
                Graphic lGraphic = _ContentGraphics[lIndex];
                if (lGraphic == null)
                    continue;

                Color lColor = lGraphic.color;
                lColor.a = _ContentBaseAlphas[lIndex] * lAlpha;
                lGraphic.color = lColor;
            }
        }

        private void CacheContentGraphics()
        {
            Graphic[] lGraphics = GetComponentsInChildren<Graphic>(true);
            int lContentCount = 0;

            for (int lIndex = 0; lIndex < lGraphics.Length; lIndex++)
            {
                if (lGraphics[lIndex] != null && lGraphics[lIndex] != _BackgroundImage)
                    lContentCount++;
            }

            if (_ContentGraphics != null && _ContentGraphics.Length == lContentCount)
                return;

            _ContentGraphics = new Graphic[lContentCount];
            _ContentBaseAlphas = new float[lContentCount];

            int lContentIndex = 0;
            for (int lIndex = 0; lIndex < lGraphics.Length; lIndex++)
            {
                Graphic lGraphic = lGraphics[lIndex];
                if (lGraphic == null || lGraphic == _BackgroundImage)
                    continue;

                _ContentGraphics[lContentIndex] = lGraphic;
                _ContentBaseAlphas[lContentIndex] = lGraphic.color.a;
                lContentIndex++;
            }
        }

        #endregion
    }
}
