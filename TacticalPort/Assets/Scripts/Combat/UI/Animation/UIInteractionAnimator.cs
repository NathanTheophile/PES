using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TacticalPort.UI
{
    public enum UIInteractionAnimationType
    {
        AnchoredPosition = 0,
        LocalPosition = 1,
        Rotation = 2,
        Scale = 3,
        Color = 4,
        Fade = 5,
        ShakePosition = 6,
        ShakeRotation = 7,
        ShakeScale = 8,
        PunchPosition = 9,
        PunchRotation = 10,
        PunchScale = 11
    }

    public enum UIInteractionAnimationStepMode
    {
        Append = 0,
        Join = 1
    }

    public enum UIInteractionAnimationValueMode
    {
        ExplicitValue = 0,
        OffsetFromInitial = 1,
        InitialValue = 2,
        OffsetFromCurrent = 3
    }

    [DisallowMultipleComponent]
    [AddComponentMenu("TacticalPort/UI/UI Interaction Animator")]
    public sealed class UIInteractionAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Header("Playback")]
        [SerializeField] private bool _UseUnscaledTime;
        [SerializeField] private bool _KillOnDisable = true;
        [SerializeField] private bool _ResetOnDisable;
        [SerializeField] private bool _IgnoreHoverWhileSelected = true;
        [SerializeField] private bool _ReturnToHoverWhenDeselectedAndPointerOver = true;

        [Header("Events")]
        [SerializeField] private UIInteractionAnimationEvent _HoverEnter = UIInteractionAnimationEvent.Default;
        [SerializeField] private UIInteractionAnimationEvent _HoverExit = UIInteractionAnimationEvent.Default;
        [SerializeField] private UIInteractionAnimationEvent _PointerDown = UIInteractionAnimationEvent.Default;
        [SerializeField] private UIInteractionAnimationEvent _PointerUp = UIInteractionAnimationEvent.Default;
        [SerializeField] private UIInteractionAnimationEvent _Click = UIInteractionAnimationEvent.Default;
        [SerializeField] private UIInteractionAnimationEvent _Selected = UIInteractionAnimationEvent.Default;
        [SerializeField] private UIInteractionAnimationEvent _Deselected = UIInteractionAnimationEvent.Default;

        private readonly Dictionary<RectTransform, UIInteractionRectState> _RectStates = new();
        private readonly Dictionary<Graphic, Color> _GraphicColors = new();
        private Sequence _CurrentSequence;
        private bool _IsPointerOver;
        private bool _IsSelected;

        private void OnDestroy() => Stop();

        private void OnDisable()
        {
            if (_KillOnDisable)
                Stop();

            if (_ResetOnDisable)
                ResetAllTargets();
        }

        public void OnPointerEnter(PointerEventData pEventData)
        {
            _IsPointerOver = true;
            if (!_IsSelected || !_IgnoreHoverWhileSelected)
                PlayHoverEnter();
        }

        public void OnPointerExit(PointerEventData pEventData)
        {
            _IsPointerOver = false;
            if (!_IsSelected || !_IgnoreHoverWhileSelected)
                PlayHoverExit();
        }

        public void OnPointerDown(PointerEventData pEventData) => PlayPointerDown();
        public void OnPointerUp(PointerEventData pEventData) => PlayPointerUp();
        public void OnPointerClick(PointerEventData pEventData) => PlayClick();

        public void PlayHoverEnter() => Play(_HoverEnter);
        public void PlayHoverExit() => Play(_HoverExit);
        public void PlayPointerDown() => Play(_PointerDown);
        public void PlayPointerUp() => Play(_PointerUp);
        public void PlayClick() => Play(_Click);
        public void PlaySelected() => Play(_Selected);
        public void PlayDeselected() => Play(_Deselected);

        public void SetSelected(bool pIsSelected)
        {
            if (_IsSelected == pIsSelected)
                return;

            _IsSelected = pIsSelected;
            if (_IsSelected)
            {
                Play(_Selected);
                return;
            }

            if (_ReturnToHoverWhenDeselectedAndPointerOver && _IsPointerOver && Play(_HoverEnter))
                return;

            if (!Play(_Deselected))
                Play(_HoverExit);
        }

        public void Stop()
        {
            if (_CurrentSequence == null)
                return;

            _CurrentSequence.Kill();
            _CurrentSequence = null;
        }

        public void ResetAllTargets()
        {
            CaptureInitialStates();

            foreach (KeyValuePair<RectTransform, UIInteractionRectState> lState in _RectStates)
                ApplyInitialState(lState.Key, lState.Value);

            foreach (KeyValuePair<Graphic, Color> lState in _GraphicColors)
            {
                if (lState.Key != null)
                    lState.Key.color = lState.Value;
            }
        }

        private bool Play(UIInteractionAnimationEvent pAnimationEvent)
        {
            if (pAnimationEvent == null || !pAnimationEvent.Enabled)
                return false;

            if (pAnimationEvent.Tracks == null || pAnimationEvent.Tracks.Length == 0)
                return false;

            CaptureInitialStates();

            if (pAnimationEvent.StopCurrentBeforePlay)
                Stop();

            if (pAnimationEvent.ResetTargetsBeforePlay)
                ResetTargets(pAnimationEvent.Tracks);

            Sequence lSequence = BuildSequence(pAnimationEvent);
            if (lSequence == null)
                return false;

            _CurrentSequence = lSequence;
            lSequence.OnKill(() =>
            {
                if (_CurrentSequence == lSequence)
                    _CurrentSequence = null;
            });

            lSequence.Play();
            return true;
        }

        private Sequence BuildSequence(UIInteractionAnimationEvent pAnimationEvent)
        {
            if (pAnimationEvent.Tracks == null || pAnimationEvent.Tracks.Length == 0)
                return null;

            Sequence lSequence = DOTween.Sequence()
                .SetAutoKill(true)
                .SetUpdate(_UseUnscaledTime)
                .Pause();

            bool lHasTween = false;
            for (int lIndex = 0; lIndex < pAnimationEvent.Tracks.Length; lIndex++)
            {
                UIInteractionAnimationTrack lTrack = pAnimationEvent.Tracks[lIndex];
                Tween lTween = BuildTween(lTrack);
                if (lTween == null)
                    continue;

                lTween.SetDelay(lTrack.Delay);

                if (!lHasTween || lTrack.StepMode == UIInteractionAnimationStepMode.Append)
                    lSequence.Append(lTween);
                else
                    lSequence.Join(lTween);

                lHasTween = true;
            }

            if (lHasTween)
                return lSequence;

            lSequence.Kill();
            return null;
        }

        private Tween BuildTween(UIInteractionAnimationTrack pTrack)
        {
            RectTransform lRectTransform = ResolveRectTransform(pTrack);
            Graphic lGraphic = ResolveGraphic(pTrack, lRectTransform);
            float lDuration = Mathf.Max(0.01f, pTrack.Duration);

            Tween lTween = pTrack.AnimationType switch
            {
                UIInteractionAnimationType.AnchoredPosition when lRectTransform != null => DOTween.To(
                    () => lRectTransform.anchoredPosition,
                    pValue => lRectTransform.anchoredPosition = pValue,
                    ResolveVector2Target(lRectTransform, pTrack, lRectTransform.anchoredPosition),
                    lDuration),

                UIInteractionAnimationType.LocalPosition when lRectTransform != null => DOTween.To(
                    () => lRectTransform.localPosition,
                    pValue => lRectTransform.localPosition = pValue,
                    ResolveVector3Target(lRectTransform, pTrack, lRectTransform.localPosition, lState => lState.LocalPosition),
                    lDuration),

                UIInteractionAnimationType.Rotation when lRectTransform != null => lRectTransform.DOLocalRotate(
                    ResolveVector3Target(lRectTransform, pTrack, lRectTransform.localEulerAngles, lState => lState.LocalEulerAngles),
                    lDuration,
                    RotateMode.Fast),

                UIInteractionAnimationType.Scale when lRectTransform != null => DOTween.To(
                    () => lRectTransform.localScale,
                    pValue => lRectTransform.localScale = pValue,
                    ResolveVector3Target(lRectTransform, pTrack, lRectTransform.localScale, lState => lState.LocalScale),
                    lDuration),

                UIInteractionAnimationType.Color when lGraphic != null => DOTween.To(
                    () => lGraphic.color,
                    pValue => lGraphic.color = pValue,
                    ResolveColorTarget(lGraphic, pTrack),
                    lDuration),

                UIInteractionAnimationType.Fade when lGraphic != null => DOTween.To(
                    () => lGraphic.color,
                    pValue => lGraphic.color = pValue,
                    ResolveFadeTarget(lGraphic, pTrack),
                    lDuration),

                UIInteractionAnimationType.ShakePosition when lRectTransform != null => DOTween.Shake(
                    () => lRectTransform.anchoredPosition,
                    pValue => lRectTransform.anchoredPosition = pValue,
                    lDuration,
                    (Vector2)pTrack.VectorValue,
                    pTrack.Vibrato,
                    pTrack.Randomness,
                    pTrack.FadeOut),

                UIInteractionAnimationType.ShakeRotation when lRectTransform != null => lRectTransform.DOShakeRotation(
                    lDuration,
                    pTrack.VectorValue,
                    pTrack.Vibrato,
                    pTrack.Randomness,
                    pTrack.FadeOut),

                UIInteractionAnimationType.ShakeScale when lRectTransform != null => lRectTransform.DOShakeScale(
                    lDuration,
                    pTrack.VectorValue,
                    pTrack.Vibrato,
                    pTrack.Randomness,
                    pTrack.FadeOut),

                UIInteractionAnimationType.PunchPosition when lRectTransform != null => DOTween.Punch(
                    () => lRectTransform.anchoredPosition,
                    pValue => lRectTransform.anchoredPosition = pValue,
                    (Vector2)pTrack.VectorValue,
                    lDuration,
                    pTrack.Vibrato,
                    pTrack.Elasticity),

                UIInteractionAnimationType.PunchRotation when lRectTransform != null => DOTween.Punch(
                    () => lRectTransform.localEulerAngles,
                    pValue => lRectTransform.localEulerAngles = pValue,
                    pTrack.VectorValue,
                    lDuration,
                    pTrack.Vibrato,
                    pTrack.Elasticity),

                UIInteractionAnimationType.PunchScale when lRectTransform != null => DOTween.Punch(
                    () => lRectTransform.localScale,
                    pValue => lRectTransform.localScale = pValue,
                    pTrack.VectorValue,
                    lDuration,
                    pTrack.Vibrato,
                    pTrack.Elasticity),

                _ => null
            };

            if (lTween == null)
                return null;

            lTween.SetEase(pTrack.Ease);

            if (pTrack.Loops > 1)
                lTween.SetLoops(pTrack.Loops, pTrack.LoopType);

            return lTween;
        }

        private void CaptureInitialStates()
        {
            CaptureInitialStates(_HoverEnter);
            CaptureInitialStates(_HoverExit);
            CaptureInitialStates(_PointerDown);
            CaptureInitialStates(_PointerUp);
            CaptureInitialStates(_Click);
            CaptureInitialStates(_Selected);
            CaptureInitialStates(_Deselected);
        }

        private void CaptureInitialStates(UIInteractionAnimationEvent pAnimationEvent)
        {
            if (pAnimationEvent?.Tracks == null)
                return;

            for (int lIndex = 0; lIndex < pAnimationEvent.Tracks.Length; lIndex++)
            {
                UIInteractionAnimationTrack lTrack = pAnimationEvent.Tracks[lIndex];
                RectTransform lRectTransform = ResolveRectTransform(lTrack);
                if (lRectTransform != null && !_RectStates.ContainsKey(lRectTransform))
                    _RectStates.Add(lRectTransform, UIInteractionRectState.From(lRectTransform));

                Graphic lGraphic = ResolveGraphic(lTrack, lRectTransform);
                if (lGraphic != null && !_GraphicColors.ContainsKey(lGraphic))
                    _GraphicColors.Add(lGraphic, lGraphic.color);
            }
        }

        private void ResetTargets(UIInteractionAnimationTrack[] pTracks)
        {
            if (pTracks == null)
                return;

            for (int lIndex = 0; lIndex < pTracks.Length; lIndex++)
            {
                RectTransform lRectTransform = ResolveRectTransform(pTracks[lIndex]);
                if (lRectTransform != null && _RectStates.TryGetValue(lRectTransform, out UIInteractionRectState lRectState))
                    ApplyInitialState(lRectTransform, lRectState);

                Graphic lGraphic = ResolveGraphic(pTracks[lIndex], lRectTransform);
                if (lGraphic != null && _GraphicColors.TryGetValue(lGraphic, out Color lColor))
                    lGraphic.color = lColor;
            }
        }

        private static void ApplyInitialState(RectTransform pRectTransform, UIInteractionRectState pState)
        {
            if (pRectTransform == null)
                return;

            pRectTransform.anchoredPosition = pState.AnchoredPosition;
            pRectTransform.localPosition = pState.LocalPosition;
            pRectTransform.localEulerAngles = pState.LocalEulerAngles;
            pRectTransform.localScale = pState.LocalScale;
        }

        private static RectTransform ResolveRectTransform(UIInteractionAnimationTrack pTrack)
        {
            if (pTrack == null)
                return null;

            if (pTrack.TargetRectTransform != null)
                return pTrack.TargetRectTransform;

            return pTrack.TargetGraphic != null ? pTrack.TargetGraphic.rectTransform : null;
        }

        private static Graphic ResolveGraphic(UIInteractionAnimationTrack pTrack, RectTransform pRectTransform)
        {
            if (pTrack == null)
                return null;

            if (pTrack.TargetGraphic != null)
                return pTrack.TargetGraphic;

            return pRectTransform != null ? pRectTransform.GetComponent<Graphic>() : null;
        }

        private Vector2 ResolveVector2Target(RectTransform pRectTransform, UIInteractionAnimationTrack pTrack, Vector2 pCurrentValue)
        {
            UIInteractionRectState lState = GetRectState(pRectTransform);
            return pTrack.ValueMode switch
            {
                UIInteractionAnimationValueMode.InitialValue => lState.AnchoredPosition,
                UIInteractionAnimationValueMode.OffsetFromInitial => lState.AnchoredPosition + (Vector2)pTrack.VectorValue,
                UIInteractionAnimationValueMode.OffsetFromCurrent => pCurrentValue + (Vector2)pTrack.VectorValue,
                _ => (Vector2)pTrack.VectorValue
            };
        }

        private Vector3 ResolveVector3Target(RectTransform pRectTransform, UIInteractionAnimationTrack pTrack, Vector3 pCurrentValue, System.Func<UIInteractionRectState, Vector3> pInitialValueSelector)
        {
            UIInteractionRectState lState = GetRectState(pRectTransform);
            Vector3 lInitialValue = pInitialValueSelector(lState);

            return pTrack.ValueMode switch
            {
                UIInteractionAnimationValueMode.InitialValue => lInitialValue,
                UIInteractionAnimationValueMode.OffsetFromInitial => lInitialValue + pTrack.VectorValue,
                UIInteractionAnimationValueMode.OffsetFromCurrent => pCurrentValue + pTrack.VectorValue,
                _ => pTrack.VectorValue
            };
        }

        private Color ResolveColorTarget(Graphic pGraphic, UIInteractionAnimationTrack pTrack)
        {
            Color lInitialColor = GetGraphicColor(pGraphic);
            return pTrack.ValueMode switch
            {
                UIInteractionAnimationValueMode.InitialValue => lInitialColor,
                UIInteractionAnimationValueMode.OffsetFromInitial => lInitialColor + pTrack.ColorValue,
                UIInteractionAnimationValueMode.OffsetFromCurrent => pGraphic.color + pTrack.ColorValue,
                _ => pTrack.ColorValue
            };
        }

        private Color ResolveFadeTarget(Graphic pGraphic, UIInteractionAnimationTrack pTrack)
        {
            Color lColor = pGraphic.color;
            float lInitialAlpha = GetGraphicColor(pGraphic).a;
            lColor.a = pTrack.ValueMode switch
            {
                UIInteractionAnimationValueMode.InitialValue => lInitialAlpha,
                UIInteractionAnimationValueMode.OffsetFromInitial => lInitialAlpha + pTrack.FloatValue,
                UIInteractionAnimationValueMode.OffsetFromCurrent => lColor.a + pTrack.FloatValue,
                _ => pTrack.FloatValue
            };

            lColor.a = Mathf.Clamp01(lColor.a);
            return lColor;
        }

        private UIInteractionRectState GetRectState(RectTransform pRectTransform)
        {
            if (pRectTransform == null)
                return default;

            if (_RectStates.TryGetValue(pRectTransform, out UIInteractionRectState lState))
                return lState;

            lState = UIInteractionRectState.From(pRectTransform);
            _RectStates.Add(pRectTransform, lState);
            return lState;
        }

        private Color GetGraphicColor(Graphic pGraphic)
        {
            if (pGraphic == null)
                return Color.white;

            if (_GraphicColors.TryGetValue(pGraphic, out Color lColor))
                return lColor;

            _GraphicColors.Add(pGraphic, pGraphic.color);
            return pGraphic.color;
        }

        private readonly struct UIInteractionRectState
        {
            public readonly Vector2 AnchoredPosition;
            public readonly Vector3 LocalPosition;
            public readonly Vector3 LocalEulerAngles;
            public readonly Vector3 LocalScale;

            private UIInteractionRectState(RectTransform pRectTransform)
            {
                AnchoredPosition = pRectTransform.anchoredPosition;
                LocalPosition = pRectTransform.localPosition;
                LocalEulerAngles = pRectTransform.localEulerAngles;
                LocalScale = pRectTransform.localScale;
            }

            public static UIInteractionRectState From(RectTransform pRectTransform) => new(pRectTransform);
        }
    }

    [System.Serializable]
    public sealed class UIInteractionAnimationEvent
    {
        public static UIInteractionAnimationEvent Default => new();

        public bool Enabled = true;
        public bool StopCurrentBeforePlay = true;
        public bool ResetTargetsBeforePlay;
        public UIInteractionAnimationTrack[] Tracks = new UIInteractionAnimationTrack[0];
    }

    [System.Serializable]
    public sealed class UIInteractionAnimationTrack
    {
        [Header("Target")]
        public RectTransform TargetRectTransform;
        public Graphic TargetGraphic;

        [Header("Playback")]
        public UIInteractionAnimationType AnimationType = UIInteractionAnimationType.Scale;
        public UIInteractionAnimationStepMode StepMode = UIInteractionAnimationStepMode.Append;
        public UIInteractionAnimationValueMode ValueMode = UIInteractionAnimationValueMode.OffsetFromInitial;
        public Ease Ease = Ease.OutQuad;
        [Min(0f)] public float Delay;
        [Min(0.01f)] public float Duration = 0.2f;
        [Tooltip("1 or lower plays once. Infinite loops are intentionally unsupported inside composed UI sequences.")]
        public int Loops;
        public LoopType LoopType = LoopType.Yoyo;

        [Header("Values")]
        public Vector3 VectorValue = Vector3.one * 0.1f;
        public Color ColorValue = Color.white;
        [Range(0f, 1f)] public float FloatValue = 1f;

        [Header("Shake / Punch")]
        [Min(0)] public int Vibrato = 10;
        [Range(0f, 180f)] public float Randomness = 90f;
        [Range(0f, 1f)] public float Elasticity = 1f;
        public bool FadeOut = true;
    }
}
