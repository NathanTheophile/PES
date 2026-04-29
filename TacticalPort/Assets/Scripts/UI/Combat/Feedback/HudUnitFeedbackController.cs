#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using TacticalPort.Core;
using TacticalPort.Shared;

namespace TacticalPort.UI
{
    internal sealed class HudUnitFeedbackController
    {
        #region _____________________________/ VALUES

        private readonly TimelineView _TimelineView;
        private readonly TimelineElementView _TimelineElementPrefab;
        private readonly UnitInfoBoxView _ActiveUnitInfobox;
        private readonly UnitInfoBoxView _HoverUnitInfobox;
        private readonly EndCombatView _EndCombatView;
        private IBattleService _BattleService;
        private UnitRuntime _TimelineHoveredUnit;
        private UnitRuntime _MapHoveredUnit;
        private bool _HasConfiguredTimelineCallbacks;

        #endregion

        #region _____________________________| INIT

        public HudUnitFeedbackController(
            TimelineView pTimelineView,
            TimelineElementView pTimelineElementPrefab,
            UnitInfoBoxView pActiveUnitInfobox,
            UnitInfoBoxView pHoverUnitInfobox,
            EndCombatView pEndCombatView)
        {
            _TimelineView = pTimelineView;
            _TimelineElementPrefab = pTimelineElementPrefab;
            _ActiveUnitInfobox = pActiveUnitInfobox;
            _HoverUnitInfobox = pHoverUnitInfobox;
            _EndCombatView = pEndCombatView;
        }

        #endregion

        #region _____________________________| BINDING

        public void Bind(IBattleService pBattleService)
        {
            _BattleService = pBattleService;

            if (_TimelineView != null)
            {
                if (_TimelineElementPrefab != null)
                    _TimelineView.SetElementPrefab(_TimelineElementPrefab);

                if (!_HasConfiguredTimelineCallbacks)
                {
                    _TimelineView.SetHoverCallbacks(SetTimelineHoveredUnit, ClearTimelineHoveredUnit);
                    _HasConfiguredTimelineCallbacks = true;
                }

                _TimelineView.Bind(_BattleService);
            }

            _HoverUnitInfobox?.Hide();
            _EndCombatView?.Hide();
        }

        public void SetMapHoveredUnit(UnitRuntime pUnit)
        {
            if (_MapHoveredUnit == pUnit)
                return;

            _MapHoveredUnit = pUnit;
            RefreshHoverInfobox();
        }

        #endregion

        #region _____________________________| DISPLAY

        public void Refresh(UnitRuntime pActiveUnit)
        {
            if (_ActiveUnitInfobox != null)
            {
                if (pActiveUnit != null && pActiveUnit.IsAlive)
                    _ActiveUnitInfobox.Show(pActiveUnit);
                else
                    _ActiveUnitInfobox.Hide();
            }

            _TimelineView?.Refresh();
            RefreshHoverInfobox();
            RefreshEndCombat();
        }

        #endregion

        #region _____________________________| HELPERS

        private void SetTimelineHoveredUnit(UnitRuntime pUnit)
        {
            _TimelineHoveredUnit = pUnit;
            RefreshHoverInfobox();
        }

        private void ClearTimelineHoveredUnit(UnitRuntime pUnit)
        {
            if (_TimelineHoveredUnit == pUnit)
                _TimelineHoveredUnit = null;

            RefreshHoverInfobox();
        }

        private void RefreshHoverInfobox()
        {
            if (_HoverUnitInfobox == null)
                return;

            UnitRuntime lHoveredUnit = ResolveHoverUnit();
            if (lHoveredUnit != null && lHoveredUnit.IsAlive)
            {
                _HoverUnitInfobox.Show(lHoveredUnit);
                return;
            }

            _HoverUnitInfobox.Hide();
        }

        private UnitRuntime ResolveHoverUnit()
        {
            if (_TimelineHoveredUnit != null && _TimelineHoveredUnit.IsAlive)
                return _TimelineHoveredUnit;

            if (_MapHoveredUnit != null && _MapHoveredUnit.IsAlive)
                return _MapHoveredUnit;

            return null;
        }

        private void RefreshEndCombat()
        {
            if (_EndCombatView == null || _BattleService == null)
                return;

            if (_BattleService.Outcome == BattleOutcome.None)
            {
                _EndCombatView.Hide();
                return;
            }

            _EndCombatView.Show(_BattleService.Outcome);
        }

        #endregion
    }
}
