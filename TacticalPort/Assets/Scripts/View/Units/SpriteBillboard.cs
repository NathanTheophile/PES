#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using UnityEngine;

namespace TacticalPort.View
{
    [DisallowMultipleComponent]
    public sealed class SpriteBillboard : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private Camera _TargetCamera;
        [SerializeField] private bool _MatchCameraForward = true;
        [SerializeField] private bool _LockRoll = true;
        [SerializeField] private Vector3 _LocalEulerOffset = Vector3.zero;

        #endregion

        #region _____________________________| UNITY

        private void LateUpdate()
        {
            Camera lCamera = ResolveCamera();
            if (lCamera == null)
                return;

            ApplyBillboard(lCamera);
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
                return;

            Camera lCamera = ResolveCamera();
            if (lCamera != null)
                ApplyBillboard(lCamera);
        }

        #endregion

        #region _____________________________| CONFIGURE

        public void SetTargetCamera(Camera pCamera)
        {
            _TargetCamera = pCamera;
            if (_TargetCamera != null)
                ApplyBillboard(_TargetCamera);
        }

        #endregion

        #region _____________________________| HELPERS

        private Camera ResolveCamera()
        {
            if (_TargetCamera != null)
                return _TargetCamera;

            _TargetCamera = Camera.main;
            return _TargetCamera;
        }

        private void ApplyBillboard(Camera pCamera)
        {
            Vector3 lForward = _MatchCameraForward
                ? pCamera.transform.forward
                : transform.position - pCamera.transform.position;

            if (_LockRoll)
                lForward.y = 0f;

            if (lForward.sqrMagnitude <= 0.0001f)
                return;

            Quaternion lRotation = Quaternion.LookRotation(lForward.normalized, Vector3.up);
            transform.rotation = lRotation * Quaternion.Euler(_LocalEulerOffset);
        }

        #endregion
    }
}
