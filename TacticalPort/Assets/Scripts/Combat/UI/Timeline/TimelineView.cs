#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System.Collections.Generic;
using System;
using TacticalPort.Core;
using TacticalPort.Shared;
using UnityEngine;

namespace TacticalPort.UI
{
    public sealed class TimelineView : MonoBehaviour
    {
        #region _____________________________/ VALUES

        [SerializeField] private RectTransform _ContentRoot;
        [SerializeField] private TimelineElementView _ElementPrefab;
        [SerializeField] private bool _ShowDefeatedUnits;

        private readonly List<TimelineElementView> _RuntimeElements = new List<TimelineElementView>();
        private readonly List<UnitRuntime> _DisplayedUnits = new List<UnitRuntime>();
        private readonly List<UnitRuntime> _SortedUnitsBuffer = new List<UnitRuntime>();
        private IBattleService _BattleService;
        private Action<UnitRuntime> _OnUnitHoverEnter;
        private Action<UnitRuntime> _OnUnitHoverExit;
        private MatchPlayerSlot _LocalPlayerSlot = MatchPlayerSlot.TeamA;

        #endregion

        #region _____________________________| UNITY

        private void Awake()
        {
            CacheMissingReferences();
            Refresh();
        }

        private void OnDestroy()
        {
            UnsubscribeFromBattleService();
            ClearRuntimeElements();
        }

        private void OnValidate() => CacheMissingReferences();

        #endregion

        #region _____________________________| BINDING

        public void Bind(IBattleService pBattleService)
        {
            if (_BattleService == pBattleService)
            {
                Refresh();
                return;
            }

            UnsubscribeFromBattleService();
            _BattleService = pBattleService;
            SubscribeToBattleService();
            Rebuild();
        }

        public void SetElementPrefab(TimelineElementView pElementPrefab)
        {
            if (_ElementPrefab == pElementPrefab)
                return;

            _ElementPrefab = pElementPrefab;
            Rebuild();
        }

        public void SetHoverCallbacks(Action<UnitRuntime> pOnUnitHoverEnter, Action<UnitRuntime> pOnUnitHoverExit)
        {
            _OnUnitHoverEnter = pOnUnitHoverEnter;
            _OnUnitHoverExit = pOnUnitHoverExit;
            Rebuild();
        }

        public void SetLocalPlayerSlot(MatchPlayerSlot pSlot)
        {
            if (pSlot == MatchPlayerSlot.None || _LocalPlayerSlot == pSlot)
                return;

            _LocalPlayerSlot = pSlot;
            for (int lIndex = 0; lIndex < _RuntimeElements.Count; lIndex++)
                _RuntimeElements[lIndex]?.SetLocalPlayerSlot(_LocalPlayerSlot);
        }

        #endregion

        #region _____________________________| DISPLAY

        public void Rebuild()
        {
            CacheMissingReferences();
            ClearRuntimeElements();
            _DisplayedUnits.Clear();

            if (_BattleService == null || _ContentRoot == null || _ElementPrefab == null)
                return;

            BuildSortedUnitsBuffer();
            SetHierarchyTemplateVisible(false);

            for (int lIndex = 0; lIndex < _SortedUnitsBuffer.Count; lIndex++)
            {
                UnitRuntime lUnit = _SortedUnitsBuffer[lIndex];
                TimelineElementView lElement = Instantiate(_ElementPrefab, _ContentRoot);
                lElement.gameObject.SetActive(true);
                lElement.Bind(lUnit, _OnUnitHoverEnter, _OnUnitHoverExit);
                lElement.SetLocalPlayerSlot(_LocalPlayerSlot);

                _RuntimeElements.Add(lElement);
                _DisplayedUnits.Add(lUnit);
            }

            Refresh();
        }

        public void Refresh()
        {
            CacheMissingReferences();

            if (_BattleService == null)
            {
                ClearRuntimeElements();
                _DisplayedUnits.Clear();
                return;
            }

            BuildSortedUnitsBuffer();
            if (ShouldRebuild())
            {
                Rebuild();
                return;
            }

            UnitId lActiveUnitId = _BattleService.CurrentTurn != null
                ? _BattleService.CurrentTurn.UnitId
                : UnitId.None;

            for (int lIndex = 0; lIndex < _RuntimeElements.Count; lIndex++)
            {
                TimelineElementView lElement = _RuntimeElements[lIndex];
                if (lElement == null)
                    continue;

                lElement.Refresh();
                lElement.SetHighlighted(lElement.UnitId == lActiveUnitId);
            }
        }

        #endregion

        #region _____________________________| HELPERS

        private void CacheMissingReferences()
        {
            _ContentRoot ??= transform as RectTransform;
            _ElementPrefab ??= GetComponentInChildren<TimelineElementView>(true);
        }

        private void SetHierarchyTemplateVisible(bool pIsVisible)
        {
            if (_ElementPrefab == null || _ElementPrefab.transform == transform || !_ElementPrefab.transform.IsChildOf(transform))
                return;

            _ElementPrefab.gameObject.SetActive(pIsVisible);
        }

        private void BuildSortedUnitsBuffer()
        {
            _SortedUnitsBuffer.Clear();

            if (_BattleService?.TurnOrder == null)
                return;

            for (int lIndex = 0; lIndex < _BattleService.TurnOrder.Count; lIndex++)
            {
                UnitRuntime lUnit = _BattleService.TurnOrder[lIndex];
                if (lUnit == null)
                    continue;

                if (!_ShowDefeatedUnits && !lUnit.IsAlive)
                    continue;

                _SortedUnitsBuffer.Add(lUnit);
            }

            if (!_ShowDefeatedUnits || _BattleService.Units == null)
                return;

            foreach (UnitRuntime lUnit in _BattleService.Units)
            {
                if (lUnit != null && !lUnit.IsAlive && !_SortedUnitsBuffer.Contains(lUnit))
                    _SortedUnitsBuffer.Add(lUnit);
            }
        }

        private bool ShouldRebuild()
        {
            if (_RuntimeElements.Count != _SortedUnitsBuffer.Count || _DisplayedUnits.Count != _SortedUnitsBuffer.Count)
                return true;

            for (int lIndex = 0; lIndex < _SortedUnitsBuffer.Count; lIndex++)
            {
                if (_RuntimeElements[lIndex] == null || _DisplayedUnits[lIndex] != _SortedUnitsBuffer[lIndex])
                    return true;
            }

            return false;
        }

        private void SubscribeToBattleService()
        {
            if (_BattleService == null)
                return;

            _BattleService.TurnStarted += HandleBattleTimelineChanged;
            _BattleService.TurnEnded += HandleBattleTimelineChanged;
            _BattleService.SkillUsed += HandleBattleTimelineChanged;
            _BattleService.HazardsResolved += HandleBattleTimelineChanged;
        }

        private void UnsubscribeFromBattleService()
        {
            if (_BattleService == null)
                return;

            _BattleService.TurnStarted -= HandleBattleTimelineChanged;
            _BattleService.TurnEnded -= HandleBattleTimelineChanged;
            _BattleService.SkillUsed -= HandleBattleTimelineChanged;
            _BattleService.HazardsResolved -= HandleBattleTimelineChanged;
        }

        private void HandleBattleTimelineChanged(BattleTurnContext pTurnContext) => Refresh();
        private void HandleBattleTimelineChanged(UnitId pUnitId) => Refresh();
        private void HandleBattleTimelineChanged(BattleActionResult pResult) => Refresh();

        private void ClearRuntimeElements()
        {
            for (int lIndex = 0; lIndex < _RuntimeElements.Count; lIndex++)
            {
                TimelineElementView lElement = _RuntimeElements[lIndex];
                if (lElement == null)
                    continue;

                lElement.Clear();

                if (Application.isPlaying)
                    Destroy(lElement.gameObject);
                else
                    DestroyImmediate(lElement.gameObject);
            }

            _RuntimeElements.Clear();
        }

        #endregion
    }
}
