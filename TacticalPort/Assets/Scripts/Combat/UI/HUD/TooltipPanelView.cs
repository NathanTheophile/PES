#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback
#endregion

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TacticalPort.UI
{
    [DisallowMultipleComponent]
    [AddComponentMenu("TacticalPort/UI/Tooltip Panel View")]
    public sealed class TooltipPanelView : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private RectTransform _LayoutRoot;
        [SerializeField] private TMP_Text _DescriptionText;

        #endregion

        #region _____________________________| UNITY

        private void OnEnable() => RefreshLayout();

        #endregion

        #region _____________________________| API

        public void Show()
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
                return;
            }

            RefreshLayout();
        }

        public void Hide() => gameObject.SetActive(false);

        public void SetDescription(string pText)
        {
            if (_DescriptionText == null)
            {
                Debug.LogError($"[{nameof(TooltipPanelView)}] Missing description text on '{name}'.", this);
                return;
            }

            _DescriptionText.text = pText ?? string.Empty;
            if (isActiveAndEnabled)
                RefreshLayout();
        }

        public void RefreshLayout()
        {
            if (_LayoutRoot == null)
            {
                Debug.LogError($"[{nameof(TooltipPanelView)}] Missing layout root on '{name}'.", this);
                return;
            }

            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_LayoutRoot);
        }

        #endregion
    }
}
