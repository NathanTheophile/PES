#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TacticalPort.View.Cameras
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class IsometricCameraRig : MonoBehaviour
    {
        #region _____________________________/ VALUES

        private const float TWO_TO_ONE_PITCH = 30f;
        private const float TWO_TO_ONE_YAW = 45f;

        private enum OrbitDragButton
        {
            Left,
            Right,
            Middle
        }

        [Header("References")]
        [SerializeField] private UnityEngine.Camera _Camera;
        [SerializeField] private Transform _FollowTarget;

        [Header("Isometric View")]
        [SerializeField] private bool _LockTwoToOneProjection = true;
        [SerializeField] private Vector3 _PivotOffset = Vector3.zero;
        [SerializeField, Range(20f, 80f)] private float _Pitch = TWO_TO_ONE_PITCH;
        [SerializeField, Range(-180f, 180f)] private float _Yaw = TWO_TO_ONE_YAW;
        [SerializeField, Min(1f)] private float _Distance = 18f;

        [Header("Zoom")]
        [SerializeField, Min(0.1f)] private float _OrthographicSize = 8f;
        [SerializeField, Min(0.1f)] private float _MinOrthographicSize = 4f;
        [SerializeField, Min(0.1f)] private float _MaxOrthographicSize = 18f;
        [SerializeField, Range(2, 12)] private int _ZoomStepCount = 4;
        [SerializeField] private bool _EnableScrollZoom = true;
        [SerializeField] private bool _BlockScrollZoomOverUi = true;
        [SerializeField] private bool _EnableZoomInertia = true;
        [SerializeField, Min(0f)] private float _ZoomInertiaDamping = 10f;
        [SerializeField, Min(0f)] private float _MinZoomInertiaDelta = 0.01f;

        [Header("Runtime Follow")]
        [SerializeField] private bool _FollowTargetAtRuntime;
        [SerializeField, Min(0f)] private float _FollowSharpness = 12f;

        [Header("Runtime Orbit")]
        [SerializeField] private bool _EnableOrbitDrag = true;
        [SerializeField] private OrbitDragButton _OrbitDragButton = OrbitDragButton.Middle;
        [SerializeField] private bool _BlockOrbitDragOverUi = true;
        [SerializeField] private bool _InvertOrbitDrag;
        [SerializeField, Min(0f)] private float _OrbitYawDegreesPerPixel = 0.25f;
        [SerializeField] private bool _EnableOrbitInertia = true;
        [SerializeField, Min(0f)] private float _OrbitInertiaDamping = 8f;
        [SerializeField, Min(0f)] private float _MinOrbitInertiaVelocity = 0.05f;
        [SerializeField] private bool _AllowOrbitPitchDrag;
        [SerializeField, Min(0f)] private float _OrbitPitchDegreesPerPixel = 0.15f;
        [SerializeField, Range(10f, 85f)] private float _MinOrbitPitch = 20f;
        [SerializeField, Range(10f, 85f)] private float _MaxOrbitPitch = 60f;

        private bool _IsOrbitDragging;
        private int _TargetZoomStepIndex;
        private float _TargetOrthographicSize;
        private float _OrbitYawVelocity;
        private Vector2 _LastOrbitPointerPosition;
        private Vector3 _CurrentPivot;

        #endregion

        #region _____________________________/ ACCESSORS

        public UnityEngine.Camera Camera => _Camera;
        public Vector3 CurrentPivot => _CurrentPivot;
        public float OrthographicSize => _OrthographicSize;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            CacheMissingReferences();
            _CurrentPivot = ResolveDefaultPivot();
            InitializeZoomStep();
            ApplyCameraState();
        }

        private void Start()
        {
            SetPivot(ResolveDefaultPivot());
        }

        private void LateUpdate()
        {
            HandleScrollZoom();
            ApplyZoomInertia();
            HandleOrbitDrag();

            if (_FollowTargetAtRuntime && _FollowTarget != null)
                SetPivot(Vector3.Lerp(_CurrentPivot, _FollowTarget.position + _PivotOffset, ResolveFollowAlpha()));
        }

        private void OnValidate()
        {
            _Distance = Mathf.Max(1f, _Distance);
            _OrthographicSize = Mathf.Max(0.1f, _OrthographicSize);
            _MinOrthographicSize = Mathf.Max(0.1f, _MinOrthographicSize);
            _MaxOrthographicSize = Mathf.Max(_MinOrthographicSize, _MaxOrthographicSize);
            _ZoomStepCount = Mathf.Clamp(_ZoomStepCount, 2, 12);
            _OrthographicSize = Mathf.Clamp(_OrthographicSize, _MinOrthographicSize, _MaxOrthographicSize);
            _ZoomInertiaDamping = Mathf.Max(0f, _ZoomInertiaDamping);
            _MinZoomInertiaDelta = Mathf.Max(0f, _MinZoomInertiaDelta);
            _FollowSharpness = Mathf.Max(0f, _FollowSharpness);
            _OrbitYawDegreesPerPixel = Mathf.Max(0f, _OrbitYawDegreesPerPixel);
            _OrbitInertiaDamping = Mathf.Max(0f, _OrbitInertiaDamping);
            _MinOrbitInertiaVelocity = Mathf.Max(0f, _MinOrbitInertiaVelocity);
            _OrbitPitchDegreesPerPixel = Mathf.Max(0f, _OrbitPitchDegreesPerPixel);
            if (_MinOrbitPitch > _MaxOrbitPitch)
                _MaxOrbitPitch = _MinOrbitPitch;
            CacheMissingReferences();
            _CurrentPivot = ResolveDefaultPivot();
            InitializeZoomStep();
            ApplyCameraState();
        }

        #endregion

        #region _____________________________| CAMERA

        public void SetFollowTarget(Transform pTarget)
        {
            _FollowTarget = pTarget;
        }

        public void SetPivot(Vector3 pPivot)
        {
            _CurrentPivot = pPivot;
            ApplyCameraTransform();
        }

        public void SetOrbit(float pYaw, float pPitch)
        {
            _Yaw = NormalizeYaw(pYaw);
            _Pitch = Mathf.Clamp(pPitch, _MinOrbitPitch, _MaxOrbitPitch);
            ApplyCameraState();
        }

        public void SetOrthographicSize(float pOrthographicSize)
        {
            _TargetZoomStepIndex = ResolveClosestZoomStepIndex(pOrthographicSize);
            _TargetOrthographicSize = ResolveZoomStepSize(_TargetZoomStepIndex);
            ApplyOrthographicSize(_TargetOrthographicSize);
        }

        private void ApplyOrthographicSize(float pOrthographicSize)
        {
            _OrthographicSize = Mathf.Clamp(pOrthographicSize, _MinOrthographicSize, _MaxOrthographicSize);
            ApplyCameraSize();
        }

        #endregion

        #region _____________________________| HELPERS

        private void CacheMissingReferences()
        {
            _Camera ??= GetComponent<UnityEngine.Camera>();
        }

        private void ConfigureCamera()
        {
            if (_Camera == null)
                return;

            _Camera.orthographic = true;
            transform.rotation = Quaternion.Euler(_Pitch, _Yaw, 0f);
        }

        private void ApplyCameraState()
        {
            ApplyProjectionPreset();
            ConfigureCamera();
            ApplyCameraSize();
            ApplyCameraTransform();
        }

        private void ApplyCameraSize()
        {
            if (_Camera == null)
                return;

            _Camera.orthographicSize = Mathf.Clamp(_OrthographicSize, _MinOrthographicSize, _MaxOrthographicSize);
        }

        private void ApplyCameraTransform()
        {
            transform.position = _CurrentPivot - transform.forward * _Distance;
        }

        private void HandleScrollZoom()
        {
            if (!_EnableScrollZoom || Mouse.current == null)
                return;

            if (_BlockScrollZoomOverUi && IsPointerOverUi())
                return;

            float lScrollSteps = ResolveScrollSteps(Mouse.current.scroll.ReadValue().y);
            if (Mathf.Abs(lScrollSteps) <= Mathf.Epsilon)
                return;

            int lStepDirection = lScrollSteps > 0f ? -1 : 1;
            _TargetZoomStepIndex = Mathf.Clamp(_TargetZoomStepIndex + lStepDirection, 0, _ZoomStepCount - 1);
            _TargetOrthographicSize = ResolveZoomStepSize(_TargetZoomStepIndex);

            if (!_EnableZoomInertia)
                ApplyOrthographicSize(_TargetOrthographicSize);
        }

        private void ApplyZoomInertia()
        {
            if (!_EnableZoomInertia)
                return;

            float lDeltaTime = Time.unscaledDeltaTime;
            if (lDeltaTime <= 0f)
                return;

            float lDelta = _TargetOrthographicSize - _OrthographicSize;
            if (Mathf.Abs(lDelta) <= _MinZoomInertiaDelta)
            {
                ApplyOrthographicSize(_TargetOrthographicSize);
                return;
            }

            float lAlpha = _ZoomInertiaDamping <= 0f
                ? 1f
                : 1f - Mathf.Exp(-_ZoomInertiaDamping * lDeltaTime);
            ApplyOrthographicSize(Mathf.Lerp(_OrthographicSize, _TargetOrthographicSize, lAlpha));
        }

        private void HandleOrbitDrag()
        {
            if (!_EnableOrbitDrag || Mouse.current == null)
            {
                _IsOrbitDragging = false;
                _OrbitYawVelocity = 0f;
                return;
            }

            if (WasOrbitButtonPressedThisFrame())
            {
                _OrbitYawVelocity = 0f;
                _IsOrbitDragging = !_BlockOrbitDragOverUi || !IsPointerOverUi();
                _LastOrbitPointerPosition = Mouse.current.position.ReadValue();
                return;
            }

            if (!_IsOrbitDragging)
            {
                ApplyOrbitInertia();
                return;
            }

            if (!IsOrbitButtonPressed())
            {
                _IsOrbitDragging = false;
                ApplyOrbitInertia();
                return;
            }

            Vector2 lPointerPosition = Mouse.current.position.ReadValue();
            Vector2 lDelta = lPointerPosition - _LastOrbitPointerPosition;
            _LastOrbitPointerPosition = lPointerPosition;

            if (lDelta.sqrMagnitude <= Mathf.Epsilon)
            {
                _OrbitYawVelocity = 0f;
                return;
            }

            ApplyOrbitDragDelta(lDelta);
        }

        private void ApplyOrbitDragDelta(Vector2 pDelta)
        {
            float lDirection = _InvertOrbitDrag ? -1f : 1f;
            float lYawDelta = pDelta.x * _OrbitYawDegreesPerPixel * lDirection;
            _Yaw = NormalizeYaw(_Yaw + lYawDelta);
            _OrbitYawVelocity = ResolveOrbitVelocity(lYawDelta);

            if (_AllowOrbitPitchDrag && !_LockTwoToOneProjection)
                _Pitch = Mathf.Clamp(_Pitch - pDelta.y * _OrbitPitchDegreesPerPixel * lDirection, _MinOrbitPitch, _MaxOrbitPitch);

            ApplyCameraState();
        }

        private void ApplyOrbitInertia()
        {
            if (!_EnableOrbitInertia || Mathf.Abs(_OrbitYawVelocity) <= _MinOrbitInertiaVelocity)
            {
                _OrbitYawVelocity = 0f;
                return;
            }

            float lDeltaTime = Time.unscaledDeltaTime;
            if (lDeltaTime <= 0f)
                return;

            _Yaw = NormalizeYaw(_Yaw + _OrbitYawVelocity * lDeltaTime);
            _OrbitYawVelocity *= Mathf.Exp(-_OrbitInertiaDamping * lDeltaTime);

            ApplyCameraState();
        }

        private static float ResolveOrbitVelocity(float pYawDelta)
        {
            float lDeltaTime = Time.unscaledDeltaTime;
            return lDeltaTime > 0f ? pYawDelta / lDeltaTime : 0f;
        }

        private void InitializeZoomStep()
        {
            _TargetZoomStepIndex = ResolveClosestZoomStepIndex(_OrthographicSize);
            _TargetOrthographicSize = ResolveZoomStepSize(_TargetZoomStepIndex);
            _OrthographicSize = _TargetOrthographicSize;
        }

        private int ResolveClosestZoomStepIndex(float pOrthographicSize)
        {
            if (_ZoomStepCount <= 1 || Mathf.Approximately(_MaxOrthographicSize, _MinOrthographicSize))
                return 0;

            float lNormalizedSize = Mathf.InverseLerp(_MinOrthographicSize, _MaxOrthographicSize, pOrthographicSize);
            return Mathf.Clamp(Mathf.RoundToInt(lNormalizedSize * (_ZoomStepCount - 1)), 0, _ZoomStepCount - 1);
        }

        private float ResolveZoomStepSize(int pStepIndex)
        {
            if (_ZoomStepCount <= 1)
                return _MinOrthographicSize;

            float lStepRatio = (float)Mathf.Clamp(pStepIndex, 0, _ZoomStepCount - 1) / (_ZoomStepCount - 1);
            return Mathf.Lerp(_MinOrthographicSize, _MaxOrthographicSize, lStepRatio);
        }

        private static float ResolveScrollSteps(float pScrollDelta)
        {
            return Mathf.Abs(pScrollDelta) >= 10f ? pScrollDelta / 120f : pScrollDelta;
        }

        private void ApplyProjectionPreset()
        {
            if (!_LockTwoToOneProjection)
                return;

            _Pitch = TWO_TO_ONE_PITCH;
        }

        private float ResolveFollowAlpha()
        {
            if (_FollowSharpness <= 0f)
                return 1f;

            return 1f - Mathf.Exp(-_FollowSharpness * Time.deltaTime);
        }

        private Vector3 ResolveDefaultPivot()
        {
            return _FollowTarget != null ? _FollowTarget.position + _PivotOffset : _PivotOffset;
        }

        private bool WasOrbitButtonPressedThisFrame()
        {
            return _OrbitDragButton switch
            {
                OrbitDragButton.Left => Mouse.current.leftButton.wasPressedThisFrame,
                OrbitDragButton.Right => Mouse.current.rightButton.wasPressedThisFrame,
                OrbitDragButton.Middle => Mouse.current.middleButton.wasPressedThisFrame,
                _ => false
            };
        }

        private bool IsOrbitButtonPressed()
        {
            return _OrbitDragButton switch
            {
                OrbitDragButton.Left => Mouse.current.leftButton.isPressed,
                OrbitDragButton.Right => Mouse.current.rightButton.isPressed,
                OrbitDragButton.Middle => Mouse.current.middleButton.isPressed,
                _ => false
            };
        }

        private bool IsPointerOverUi()
        {
            return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        }

        private static float NormalizeYaw(float pYaw)
        {
            pYaw %= 360f;
            if (pYaw > 180f)
                pYaw -= 360f;
            else if (pYaw < -180f)
                pYaw += 360f;

            return pYaw;
        }

        #endregion
    }
}
