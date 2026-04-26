using System.Collections.Generic;
using System.Linq;
using TacticalPort.Core.Interfaces;
using TacticalPort.Core.Runtime;
using TacticalPort.Shared;

namespace TacticalPort.Core.Services
{
    public sealed class TurnSystem : ITurnSystem
    {
        #region _____________________________| VALUES

        private readonly List<BattleUnitRuntime> _TurnOrder = new List<BattleUnitRuntime>();
        private int _CurrentIndex = -1;

        #endregion

        #region _____________________________| ACCESSORS

        public BattleTurnContext CurrentTurn { get; private set; }
        public int RoundIndex { get; private set; }

        #endregion

        #region _____________________________| SETUP

        public void Initialize(IEnumerable<BattleUnitRuntime> pUnits)
        {
            _TurnOrder.Clear();
            _TurnOrder.AddRange(
                pUnits
                    .Where(unit => unit != null && unit.IsAlive)
                    .OrderByDescending(unit => unit.Definition.Initiative)
                    .ThenBy(unit => unit.Id.Value));

            _CurrentIndex = -1;
            RoundIndex = 0;
            CurrentTurn = null;
        }

        #endregion

        #region _____________________________| TURNS

        public bool TryStartNextTurn(out BattleTurnContext pTurnContext)
        {
            if (CurrentTurn != null)
            {
                pTurnContext = CurrentTurn;
                return false;
            }

            RemoveDefeatedUnits();
            if (_TurnOrder.Count == 0)
            {
                pTurnContext = null;
                return false;
            }

            _CurrentIndex++;

            if (_CurrentIndex >= _TurnOrder.Count)
            {
                _CurrentIndex = 0;
                RoundIndex++;
            }
            else if (RoundIndex == 0)
            {
                RoundIndex = 1;
            }

            BattleUnitRuntime lActiveUnit = _TurnOrder[_CurrentIndex];
            lActiveUnit.BeginTurn();

            CurrentTurn = new BattleTurnContext(RoundIndex, _CurrentIndex + 1, lActiveUnit.Id);
            pTurnContext = CurrentTurn;
            return true;
        }

        public void CompleteCurrentTurn()
        {
            if (CurrentTurn == null)
                return;

            BattleUnitId lActiveUnitId = CurrentTurn.UnitId;

            for (int lIndex = 0; lIndex < _TurnOrder.Count; lIndex++)
            {
                if (_TurnOrder[lIndex].Id != lActiveUnitId)
                    continue;

                _TurnOrder[lIndex].EndTurn();
                break;
            }

            CurrentTurn = null;
        }

        public void AddUnit(BattleUnitRuntime pUnit)
        {
            if (pUnit == null || !pUnit.IsAlive)
                return;

            if (CurrentTurn != null)
            {
                _TurnOrder.Add(pUnit);
                return;
            }

            InsertSorted(pUnit);
        }

        public void RemoveUnit(BattleUnitId pUnitId)
        {
            bool lRemovedActiveUnit = CurrentTurn != null && CurrentTurn.UnitId == pUnitId;

            for (int lIndex = 0; lIndex < _TurnOrder.Count; lIndex++)
            {
                if (_TurnOrder[lIndex].Id != pUnitId)
                    continue;

                _TurnOrder.RemoveAt(lIndex);
                if (lIndex <= _CurrentIndex)
                    _CurrentIndex--;

                break;
            }

            if (lRemovedActiveUnit)
                CurrentTurn = null;
        }

        #endregion

        #region _____________________________| HELPERS

        private void RemoveDefeatedUnits()
        {
            for (int lIndex = _TurnOrder.Count - 1; lIndex >= 0; lIndex--)
            {
                if (_TurnOrder[lIndex].IsAlive)
                    continue;

                _TurnOrder.RemoveAt(lIndex);
                if (lIndex <= _CurrentIndex)
                    _CurrentIndex--;
            }
        }

        private void InsertSorted(BattleUnitRuntime pUnit)
        {
            int lInsertIndex = _TurnOrder.Count;

            for (int lIndex = 0; lIndex < _TurnOrder.Count; lIndex++)
            {
                BattleUnitRuntime lExistingUnit = _TurnOrder[lIndex];
                if (lExistingUnit.Definition.Initiative > pUnit.Definition.Initiative)
                    continue;

                if (lExistingUnit.Definition.Initiative == pUnit.Definition.Initiative && lExistingUnit.Id.Value < pUnit.Id.Value)
                    continue;

                lInsertIndex = lIndex;
                break;
            }

            _TurnOrder.Insert(lInsertIndex, pUnit);
        }

        #endregion
    }
}
