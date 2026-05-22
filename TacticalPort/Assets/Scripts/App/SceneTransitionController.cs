#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Bootstrap
#endregion

using System.Collections;
using TacticalPort.App;
using TacticalPort.UI;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TacticalPort.Bootstrap
{
    public sealed class SceneTransitionController : MonoBehaviour, ISceneTransitionService
    {
        #region _____________________________/ VALUES

        private const int FallbackSortingOrder = 32767;

        private static SceneTransitionController _Current;

        private enum LoadingFadeOrder
        {
            BackgroundThenContent,
            ContentThenBackground
        }

        [Header("View")]
        [Tooltip("Optional loading screen view. If missing, a minimal fallback UI is created at runtime.")]
        [SerializeField] private LoadingScreenView _LoadingScreenView;
        [Tooltip("Creates a basic fullscreen loading screen when no view is assigned.")]
        [SerializeField] private bool _CreateFallbackViewWhenMissing = true;

        [Header("Persistence")]
        [Tooltip("Keeps this transition service alive when it is placed as a scene root.")]
        [SerializeField] private bool _KeepAliveAcrossScenes = true;

        [Header("Defaults")]
        [Tooltip("Default message shown when callers do not provide a transition message.")]
        [SerializeField] private string _DefaultMessage = "Loading...";
        [Tooltip("Minimum visible time so very fast loads do not flash the screen.")]
        [SerializeField, Min(0f)] private float _MinimumVisibleSeconds = 0.15f;
        [Tooltip("Hides the loading screen as soon as the scene load has completed.")]
        [SerializeField] private bool _HideWhenLoaded = true;

        [Header("Fade")]
        [Tooltip("Fades the loading screen to black before loading the next scene.")]
        [SerializeField] private bool _FadeToBlack = true;
        [Tooltip("Duration of the background fade to black before the scene load starts.")]
        [SerializeField, Min(0f)] private float _FadeInSeconds = 0.2f;
        [Tooltip("Duration of the loading content fade after the background is fully visible.")]
        [SerializeField, Min(0f)] private float _ContentFadeInSeconds = 0.12f;
        [Tooltip("Duration of the loading content fade before the background fades out.")]
        [SerializeField, Min(0f)] private float _ContentFadeOutSeconds = 0.16f;
        [Tooltip("Duration of the background fade back from black after the scene load has completed.")]
        [SerializeField, Min(0f)] private float _FadeOutSeconds = 0.2f;

        [Header("Debug")]
        [Tooltip("Logs scene transition requests and completion.")]
        [SerializeField] private bool _LogEvents = true;

        private Coroutine _LoadRoutine;

        #endregion

        #region _____________________________/ ACCESSORS

        public static SceneTransitionController Current => _Current;
        public bool IsLoading => _LoadRoutine != null;
        public bool IsTransitioning => IsLoading;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            if (_Current != null && _Current != this)
            {
                Destroy(gameObject);
                return;
            }

            _Current = this;

            if (_KeepAliveAcrossScenes)
                DontDestroyOnLoad(transform.root.gameObject);

            CacheMissingReferences();
            _LoadingScreenView?.HideImmediate();
        }

        private void OnDestroy()
        {
            if (_Current == this)
                _Current = null;
        }

        private void OnValidate() => CacheMissingReferences();

        #endregion

        #region _____________________________| LOAD

        public static bool TryLoadScene(string pSceneName, LoadSceneMode pLoadSceneMode = LoadSceneMode.Single, string pMessage = null)
        {
            if (_Current == null || !_Current.isActiveAndEnabled)
                return false;

            _Current.LoadScene(pSceneName, pLoadSceneMode, pMessage);
            return true;
        }

        public void LoadScene(string pSceneName, LoadSceneMode pLoadSceneMode = LoadSceneMode.Single, string pMessage = null)
        {
            if (string.IsNullOrWhiteSpace(pSceneName))
            {
                Debug.LogWarning($"{nameof(SceneTransitionController)} cannot load an empty scene name.", this);
                return;
            }

            if (_LoadRoutine != null)
            {
                if (_LogEvents)
                    Debug.LogWarning($"{nameof(SceneTransitionController)} ignored scene load request while another transition is running. Requested={pSceneName}", this);

                return;
            }

            _LoadRoutine = StartCoroutine(LoadSceneRoutine(pSceneName, pLoadSceneMode, pMessage));
        }

        private IEnumerator LoadSceneRoutine(string pSceneName, LoadSceneMode pLoadSceneMode, string pMessage)
        {
            string lMessage = string.IsNullOrWhiteSpace(pMessage) ? _DefaultMessage : pMessage;
            float lStartTime = Time.unscaledTime;

            CacheMissingReferences();
            yield return ShowLoadingScreenRoutine(lMessage);

            if (_LogEvents)
                Debug.Log($"[Scene Transition] Loading scene '{pSceneName}' ({pLoadSceneMode}).", this);

            AsyncOperation lOperation = SceneManager.LoadSceneAsync(pSceneName, pLoadSceneMode);
            if (lOperation == null)
            {
                Debug.LogWarning($"{nameof(SceneTransitionController)} failed to start loading scene '{pSceneName}'.", this);
                yield return HideLoadingScreenIfNeeded();
                _LoadRoutine = null;
                yield break;
            }

            while (!lOperation.isDone)
            {
                float lProgress = Mathf.Clamp01(lOperation.progress / 0.9f);
                _LoadingScreenView?.SetProgress(lProgress);
                yield return null;
            }

            _LoadingScreenView?.SetProgress(1f);

            while (Time.unscaledTime - lStartTime < _MinimumVisibleSeconds)
                yield return null;

            yield return HideLoadingScreenIfNeeded();

            if (_LogEvents)
                Debug.Log($"[Scene Transition] Scene '{pSceneName}' loaded.", this);

            _LoadRoutine = null;
        }

        private IEnumerator HideLoadingScreenIfNeeded()
        {
            if (_HideWhenLoaded)
                yield return HideLoadingScreenRoutine();
        }

        private IEnumerator HideLoadingScreenRoutine()
        {
            if (_LoadingScreenView == null)
                yield break;

            if (!_FadeToBlack)
            {
                _LoadingScreenView.HideImmediate();
                yield break;
            }

            yield return FadeLoadingScreenLayers(1f, 0f, _FadeOutSeconds, 1f, 0f, _ContentFadeOutSeconds, LoadingFadeOrder.ContentThenBackground);
            _LoadingScreenView.HideImmediate();
        }

        private IEnumerator ShowLoadingScreenRoutine(string pMessage)
        {
            if (_LoadingScreenView == null)
                yield break;

            if (!_FadeToBlack)
            {
                _LoadingScreenView.Show(pMessage, 0f);
                yield break;
            }

            _LoadingScreenView.Show(pMessage, 0f, 0f, 0f);
            yield return FadeLoadingScreenLayers(0f, 1f, _FadeInSeconds, 0f, 1f, _ContentFadeInSeconds, LoadingFadeOrder.BackgroundThenContent);
        }

        private IEnumerator FadeLoadingScreenLayers(
            float pBackgroundStartAlpha,
            float pBackgroundTargetAlpha,
            float pBackgroundDuration,
            float pContentStartAlpha,
            float pContentTargetAlpha,
            float pContentDuration,
            LoadingFadeOrder pOrder)
        {
            if (pOrder == LoadingFadeOrder.BackgroundThenContent)
            {
                yield return FadeLoadingLayer(pBackgroundStartAlpha, pBackgroundTargetAlpha, pBackgroundDuration, true);
                yield return FadeLoadingLayer(pContentStartAlpha, pContentTargetAlpha, pContentDuration, false);
                yield break;
            }

            yield return FadeLoadingLayer(pContentStartAlpha, pContentTargetAlpha, pContentDuration, false);
            yield return FadeLoadingLayer(pBackgroundStartAlpha, pBackgroundTargetAlpha, pBackgroundDuration, true);
        }

        private IEnumerator FadeLoadingLayer(float pStartAlpha, float pTargetAlpha, float pDuration, bool pIsBackground)
        {
            if (_LoadingScreenView == null)
                yield break;

            if (pDuration <= 0f)
            {
                SetLoadingLayerAlpha(pIsBackground, pTargetAlpha);
                yield break;
            }

            float lElapsed = 0f;

            while (lElapsed < pDuration)
            {
                lElapsed += Time.unscaledDeltaTime;
                float lRatio = Mathf.Clamp01(lElapsed / pDuration);
                SetLoadingLayerAlpha(pIsBackground, Mathf.Lerp(pStartAlpha, pTargetAlpha, lRatio));
                yield return null;
            }

            SetLoadingLayerAlpha(pIsBackground, pTargetAlpha);
        }

        private void SetLoadingLayerAlpha(bool pIsBackground, float pAlpha)
        {
            if (pIsBackground)
                _LoadingScreenView.SetBackgroundAlpha(pAlpha);
            else
                _LoadingScreenView.SetContentAlpha(pAlpha);
        }

        #endregion

        #region _____________________________| HELPERS

        private void CacheMissingReferences()
        {
            if (_LoadingScreenView == null)
                _LoadingScreenView = GetComponentInChildren<LoadingScreenView>(true);

            if (_LoadingScreenView == null && _CreateFallbackViewWhenMissing && Application.isPlaying)
                _LoadingScreenView = CreateFallbackLoadingScreen();
        }

        private LoadingScreenView CreateFallbackLoadingScreen()
        {
            GameObject lRoot = new GameObject("Screen_Loading", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup), typeof(Image), typeof(LoadingScreenView));
            lRoot.transform.SetParent(transform, false);

            RectTransform lRootRect = lRoot.GetComponent<RectTransform>();
            Stretch(lRootRect);

            Canvas lCanvas = lRoot.GetComponent<Canvas>();
            lCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            lCanvas.sortingOrder = FallbackSortingOrder;

            CanvasScaler lCanvasScaler = lRoot.GetComponent<CanvasScaler>();
            lCanvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            lCanvasScaler.referenceResolution = new Vector2(1920f, 1080f);
            lCanvasScaler.matchWidthOrHeight = 0.5f;

            Image lBackground = lRoot.GetComponent<Image>();
            lBackground.color = Color.black;

            TextMeshProUGUI lMessageText = CreateText("Txt_LoadingMessage", lRoot.transform, 0f, 34f, FontStyles.Bold);
            TextMeshProUGUI lProgressText = CreateText("Txt_LoadingProgress", lRoot.transform, -48f, 18f, FontStyles.Normal);

            LoadingScreenView lView = lRoot.GetComponent<LoadingScreenView>();
            lView.ConfigureGeneratedView(lRoot, lRoot.GetComponent<CanvasGroup>(), lMessageText, lProgressText, null);
            return lView;
        }

        private static TextMeshProUGUI CreateText(string pName, Transform pParent, float pAnchoredY, float pFontSize, FontStyles pFontStyle)
        {
            GameObject lTextObject = new GameObject(pName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            lTextObject.transform.SetParent(pParent, false);

            RectTransform lRectTransform = lTextObject.GetComponent<RectTransform>();
            lRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            lRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            lRectTransform.pivot = new Vector2(0.5f, 0.5f);
            lRectTransform.anchoredPosition = new Vector2(0f, pAnchoredY);
            lRectTransform.sizeDelta = new Vector2(720f, 64f);

            TextMeshProUGUI lText = lTextObject.GetComponent<TextMeshProUGUI>();
            lText.alignment = TextAlignmentOptions.Center;
            lText.color = Color.white;
            lText.fontSize = pFontSize;
            lText.fontStyle = pFontStyle;
            lText.raycastTarget = false;
            return lText;
        }

        private static void Stretch(RectTransform pRectTransform)
        {
            if (pRectTransform == null)
                return;

            pRectTransform.anchorMin = Vector2.zero;
            pRectTransform.anchorMax = Vector2.one;
            pRectTransform.offsetMin = Vector2.zero;
            pRectTransform.offsetMax = Vector2.zero;
            pRectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        #endregion
    }
}
