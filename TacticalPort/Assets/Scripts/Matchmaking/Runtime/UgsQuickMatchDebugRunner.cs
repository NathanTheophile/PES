#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
//  Matchmaking
#endregion

using UnityEngine;

namespace TacticalPort.Matchmaking
{
    [DisallowMultipleComponent]
    public sealed class UgsQuickMatchDebugRunner : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private QuickMatchFlowController _FlowController;
        [SerializeField] private bool _RunOnStart;

        #endregion

        #region _____________________________| UNITY

        private void Awake() => CacheMissingReferences();

        private void Start()
        {
            if (_RunOnStart)
                RunQuickMatchDebug();
        }

        private void OnValidate() => CacheMissingReferences();

        #endregion

        #region _____________________________| DEBUG RUN

        [ContextMenu("Run UGS Quick Match Debug")]
        public void RunQuickMatchDebug()
        {
            if (!ValidateReferences())
                return;

            _FlowController.StartQuickMatch();
        }

        [ContextMenu("Cancel UGS Quick Match Debug")]
        public void CancelQuickMatchDebug()
        {
            if (!ValidateReferences())
                return;

            _FlowController.CancelQuickMatch();
        }

        #endregion

        #region _____________________________| HELPERS

        private void CacheMissingReferences()
        {
            if (_FlowController == null)
                _FlowController = GetComponent<QuickMatchFlowController>() ?? GetComponentInParent<QuickMatchFlowController>();
        }

        private bool ValidateReferences()
        {
            CacheMissingReferences();

            if (_FlowController != null)
                return true;

            Debug.LogWarning($"{nameof(UgsQuickMatchDebugRunner)} is missing a {nameof(QuickMatchFlowController)} reference.", this);
            return false;
        }

        #endregion
    }
}
