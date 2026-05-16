#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using TacticalPort.Core;
using TacticalPort.Shared;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TacticalPort.UI
{
    public sealed class TimelineElementView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
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
        private MatchPlayerSlot _LocalPlayerSlot = MatchPlayerSlot.TeamA;
        private Vector3 _BaseScale = Vector3.one;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            _BaseScale = transform.localScale;
            ValidateReferences();
            DisableClickBehaviour();
        }

        #endregion

        #region _____________________________| BINDING

        public void Bind(UnitRuntime pUnit, Action<UnitRuntime> pOnHoverEnter, Action<UnitRuntime> pOnHoverExit)
        {
            _Unit = pUnit;
            _OnHoverEnter = pOnHoverEnter;
            _OnHoverExit = pOnHoverExit;

            DisableClickBehaviour();
            Refresh();
        }

        public void Bind(UnitRuntime pUnit) => Bind(pUnit, null, null);

        public void SetLocalPlayerSlot(MatchPlayerSlot pSlot)
        {
            if (pSlot == MatchPlayerSlot.None || _LocalPlayerSlot == pSlot)
                return;

            _LocalPlayerSlot = pSlot;
            Refresh();
        }

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

        #endregion

        #region _____________________________| HELPERS

        private void DisableClickBehaviour()
        {
            if (_Button == null)
                return;

            _Button.onClick.RemoveAllListeners();
            _Button.navigation = new Navigation { mode = Navigation.Mode.None };
        }

        private void ValidateReferences()
        {
            LogMissingReference(_PortraitImage, nameof(_PortraitImage));
            LogMissingReference(_HealthSlider, nameof(_HealthSlider));
            LogMissingReference(_HealthFillImage, nameof(_HealthFillImage));
            LogMissingReference(_Button, nameof(_Button));
        }

        private void LogMissingReference(UnityEngine.Object pReference, string pFieldName)
        {
            if (pReference == null)
                Debug.LogWarning($"{nameof(TimelineElementView)} is missing reference '{pFieldName}'.", this);
        }

        private Color ResolveHealthColor(Team pTeam)
        {
            return pTeam switch
            {
                Team.TeamA => CombatTeamUtility.ResolveRelation(pTeam, _LocalPlayerSlot) == CombatTeamRelation.Own ? _PlayerHealthColor : _EnemyHealthColor,
                Team.TeamB => CombatTeamUtility.ResolveRelation(pTeam, _LocalPlayerSlot) == CombatTeamRelation.Own ? _PlayerHealthColor : _EnemyHealthColor,
                _ => _NeutralHealthColor
            };
        }

        #endregion
    }
}
