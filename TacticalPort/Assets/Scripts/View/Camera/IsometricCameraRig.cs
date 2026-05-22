#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Data;
using TacticalPort.Shared;
using TacticalPort.View;
using UnityEngine;

namespace TacticalPort.View.Cameras
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class IsometricCameraRig : MonoBehaviour
    {
        #region _____________________________/ VALUES

        private const float TWO_TO_ONE_PITCH = 30f;
        private const float TWO_TO_ONE_YAW = 45f;

        [Header("References")]
        [SerializeField] private UnityEngine.Camera _Camera;
        [SerializeField] private BoardView _BoardView;
        [SerializeField] private Transform _FollowTarget;

        [Header("Isometric View")]
        [SerializeField] private bool _LockTwoToOneProjection = true;
        [SerializeField] private Vector3 _PivotOffset = Vector3.zero;
        [SerializeField, Range(20f, 80f)] private float _Pitch = TWO_TO_ONE_PITCH;
        [SerializeField, Range(-180f, 180f)] private float _Yaw = TWO_TO_ONE_YAW;
        [SerializeField, Min(1f)] private float _Distance = 18f;

        [Header("Framing")]
        [SerializeField] private bool _AutoFrameBoard = true;
        [SerializeField, Min(1f)] private float _MinOrthographicSize = 5f;
        [SerializeField, Min(0f)] private float _BoardPadding = 2f;
        [SerializeField, Min(0.1f)] private float _BoardCellExtent = 1f;

        [Header("Runtime Follow")]
        [SerializeField] private bool _FollowTargetAtRuntime;
        [SerializeField, Min(0f)] private float _FollowSharpness = 12f;

        private bool _HasAppliedInitialFrame;
        private Vector3 _CurrentPivot;

        #endregion

        #region _____________________________/ ACCESSORS

        public UnityEngine.Camera Camera => _Camera;
        public Vector3 CurrentPivot => _CurrentPivot;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            CacheMissingReferences();
            ApplyProjectionPreset();
            ConfigureCamera();
        }

        private void Start()
        {
            ApplyInitialFrameIfReady();
        }

        private void LateUpdate()
        {
            if (!_HasAppliedInitialFrame)
                ApplyInitialFrameIfReady();

            if (_FollowTargetAtRuntime && _FollowTarget != null)
                SetPivot(Vector3.Lerp(_CurrentPivot, _FollowTarget.position + _PivotOffset, ResolveFollowAlpha()));
        }

        private void OnValidate()
        {
            _Distance = Mathf.Max(1f, _Distance);
            _MinOrthographicSize = Mathf.Max(1f, _MinOrthographicSize);
            _BoardCellExtent = Mathf.Max(0.1f, _BoardCellExtent);
            _FollowSharpness = Mathf.Max(0f, _FollowSharpness);
            CacheMissingReferences();
            ApplyProjectionPreset();
            ConfigureCamera();

            if (!Application.isPlaying)
                TryFrameBoard();
        }

        #endregion

        #region _____________________________| CAMERA

        public void FrameBoard()
        {
            if (TryFrameBoard())
                _HasAppliedInitialFrame = true;
        }

        public void SetFollowTarget(Transform pTarget)
        {
            _FollowTarget = pTarget;
        }

        public void SetPivot(Vector3 pPivot)
        {
            _CurrentPivot = pPivot;
            transform.position = _CurrentPivot - transform.forward * _Distance;
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

        private void ApplyInitialFrameIfReady()
        {
            ApplyProjectionPreset();
            ConfigureCamera();

            if (_AutoFrameBoard)
            {
                if (TryFrameBoard())
                {
                    _HasAppliedInitialFrame = true;
                    return;
                }

                if (_BoardView != null && _BoardView.Scenario == null)
                    return;
            }

            SetPivot(_FollowTarget != null ? _FollowTarget.position + _PivotOffset : _PivotOffset);
            _HasAppliedInitialFrame = true;
        }

        private bool TryFrameBoard()
        {
            ApplyProjectionPreset();
            ConfigureCamera();
            if (_Camera == null || _BoardView == null || _BoardView.Scenario == null)
                return false;

            if (!TryCalculateBoardBounds(out Bounds lBounds))
                return false;

            Vector3 lPivot = lBounds.center + _PivotOffset;
            SetPivot(lPivot);
            _Camera.orthographicSize = Mathf.Max(_MinOrthographicSize, ResolveOrthographicSize(lBounds));
            return true;
        }

        private bool TryCalculateBoardBounds(out Bounds pBounds)
        {
            pBounds = default;

            bool lHasBounds = false;
            foreach (CellDefinition lCell in _BoardView.Scenario.EnumerateCells())
            {
                if (lCell == null || !lCell.IsWalkable)
                    continue;

                GridCoord lCoord = lCell.Coordinate.ToRuntime();
                Vector3 lCellPosition = _BoardView.GetWorldPosition(lCoord);
                Bounds lCellBounds = new Bounds(lCellPosition, Vector3.one * _BoardCellExtent);

                if (!lHasBounds)
                {
                    pBounds = lCellBounds;
                    lHasBounds = true;
                    continue;
                }

                pBounds.Encapsulate(lCellBounds);
            }

            return lHasBounds;
        }

        private float ResolveOrthographicSize(Bounds pBounds)
        {
            Vector3[] lCorners =
            {
                new Vector3(pBounds.min.x, pBounds.min.y, pBounds.min.z),
                new Vector3(pBounds.min.x, pBounds.min.y, pBounds.max.z),
                new Vector3(pBounds.min.x, pBounds.max.y, pBounds.min.z),
                new Vector3(pBounds.min.x, pBounds.max.y, pBounds.max.z),
                new Vector3(pBounds.max.x, pBounds.min.y, pBounds.min.z),
                new Vector3(pBounds.max.x, pBounds.min.y, pBounds.max.z),
                new Vector3(pBounds.max.x, pBounds.max.y, pBounds.min.z),
                new Vector3(pBounds.max.x, pBounds.max.y, pBounds.max.z)
            };

            Matrix4x4 lWorldToCamera = transform.worldToLocalMatrix;
            float lMinX = float.PositiveInfinity;
            float lMinY = float.PositiveInfinity;
            float lMaxX = float.NegativeInfinity;
            float lMaxY = float.NegativeInfinity;

            for (int lIndex = 0; lIndex < lCorners.Length; lIndex++)
            {
                Vector3 lCameraPoint = lWorldToCamera.MultiplyPoint3x4(lCorners[lIndex]);
                lMinX = Mathf.Min(lMinX, lCameraPoint.x);
                lMinY = Mathf.Min(lMinY, lCameraPoint.y);
                lMaxX = Mathf.Max(lMaxX, lCameraPoint.x);
                lMaxY = Mathf.Max(lMaxY, lCameraPoint.y);
            }

            float lHalfHeight = (lMaxY - lMinY) * 0.5f;
            float lHalfWidth = (lMaxX - lMinX) * 0.5f;
            float lAspect = _Camera != null && _Camera.aspect > 0f ? _Camera.aspect : 16f / 9f;
            return Mathf.Max(lHalfHeight, lHalfWidth / lAspect) + _BoardPadding;
        }

        private void ApplyProjectionPreset()
        {
            if (!_LockTwoToOneProjection)
                return;

            _Pitch = TWO_TO_ONE_PITCH;
            _Yaw = TWO_TO_ONE_YAW;
        }

        private float ResolveFollowAlpha()
        {
            if (_FollowSharpness <= 0f)
                return 1f;

            return 1f - Mathf.Exp(-_FollowSharpness * Time.deltaTime);
        }

        #endregion
    }
}
