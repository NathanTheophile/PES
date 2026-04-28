#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using TacticalPort.Core.Runtime;
using TacticalPort.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TacticalPort.View
{
    public sealed class TimelineElementView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        #region _____________________________/ VALUES

        [SerializeField] private Image _PortraitImage;
        [SerializeField] private Slider _HealthSlider;
        [SerializeField] private Image _HealthFillImage;
        [SerializeField] private Button _Button;
        [SerializeField] private Color _PlayerHealthColor = new Color(0.2f, 0.85f, 0.35f, 1f);
        [SerializeField] private Color _EnemyHealthColor = new Color(0.9f, 0.18f, 0.16f, 1f);
        [SerializeField] private Color _NeutralHealthColor = Color.white;

        private UnitRuntime _Unit;
        private Action<UnitRuntime> _OnHoverEnter;
        private Action<UnitRuntime> _OnHoverExit;
        private Vector3 _BaseScale = Vector3.one;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            _BaseScale = transform.localScale;
            CacheMissingReferences();
            DisableClickBehaviour();
        }

        #endregion

        #region _____________________________| BINDING

        public void Bind(UnitRuntime pUnit, Action<UnitRuntime> pOnHoverEnter, Action<UnitRuntime> pOnHoverExit)
        {
            _Unit = pUnit;
            _OnHoverEnter = pOnHoverEnter;
            _OnHoverExit = pOnHoverExit;

            CacheMissingReferences();
            DisableClickBehaviour();
            Refresh();
        }

        public void Bind(UnitRuntime pUnit) => Bind(pUnit, null, null);

        public void Clear()
        {
            _Unit = null;
            _OnHoverEnter = null;
            _OnHoverExit = null;
            SetHighlighted(false);
        }

        public UnitId UnitId => _Unit != null ? _Unit.Id : UnitId.None;

        public void Refresh()
        {
            if (_Unit == null)
                return;

            if (_PortraitImage != null)
            {
                _PortraitImage.sprite = _Unit.Definition.Portrait;
                _PortraitImage.enabled = _Unit.Definition.Portrait != null;
                _PortraitImage.preserveAspect = true;
            }

            if (_HealthSlider != null)
            {
                _HealthSlider.minValue = 0;
                _HealthSlider.maxValue = Mathf.Max(1, _Unit.Definition.MaxHealth);
                _HealthSlider.value = Mathf.Clamp(_Unit.CurrentHealth, 0, _Unit.Definition.MaxHealth);
                _HealthSlider.interactable = false;
            }

            if (_HealthFillImage != null)
                _HealthFillImage.color = ResolveHealthColor(_Unit.Team);
        }

        public void SetHighlighted(bool pValue)
        {
            transform.localScale = pValue ? _BaseScale * 1.08f : _BaseScale;
        }

        #endregion

        #region _____________________________| POINTER

        public void OnPointerEnter(PointerEventData pEventData)
        {
            if (_Unit != null && _Unit.IsAlive)
                _OnHoverEnter?.Invoke(_Unit);
        }

        public void OnPointerExit(PointerEventData pEventData)
        {
            if (_Unit != null)
                _OnHoverExit?.Invoke(_Unit);
        }

        public void OnPointerClick(PointerEventData pEventData)
        {
            // Timeline entries are hover-only feedback; clicks intentionally do nothing.
        }

        #endregion

        #region _____________________________| HELPERS

        private void CacheMissingReferences()
        {
            _PortraitImage ??= FindChildImage("Img_Portrait");
            _HealthSlider ??= FindChildSlider("Slider_HP");
            _HealthFillImage ??= FindSliderFillImage(_HealthSlider);
            _Button ??= GetComponent<Button>();
        }

        private void DisableClickBehaviour()
        {
            if (_Button == null)
                return;

            _Button.onClick.RemoveAllListeners();
            _Button.navigation = new Navigation { mode = Navigation.Mode.None };
        }

        private Image FindChildImage(string pName)
        {
            Image[] lImages = GetComponentsInChildren<Image>(true);
            for (int lIndex = 0; lIndex < lImages.Length; lIndex++)
            {
                if (lImages[lIndex] != null && lImages[lIndex].gameObject.name == pName)
                    return lImages[lIndex];
            }

            return lImages.Length > 0 ? lImages[0] : null;
        }

        private Slider FindChildSlider(string pName)
        {
            Slider[] lSliders = GetComponentsInChildren<Slider>(true);
            for (int lIndex = 0; lIndex < lSliders.Length; lIndex++)
            {
                if (lSliders[lIndex] != null && lSliders[lIndex].gameObject.name == pName)
                    return lSliders[lIndex];
            }

            return lSliders.Length > 0 ? lSliders[0] : null;
        }

        private static Image FindSliderFillImage(Slider pSlider)
        {
            if (pSlider == null || pSlider.fillRect == null)
                return null;

            return pSlider.fillRect.GetComponent<Image>();
        }

        private Color ResolveHealthColor(Team pTeam)
        {
            return pTeam switch
            {
                Team.Player => _PlayerHealthColor,
                Team.Enemy => _EnemyHealthColor,
                _ => _NeutralHealthColor
            };
        }

        #endregion
    }
}
