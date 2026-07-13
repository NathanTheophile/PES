#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2025)
//  Engine : Unity
//  Note : MY_CONST, myPublic, m_MyProtected, _MyPrivate, lMyLocal, MyFunc(), pMyParam, onMyEvent, OnMyCallback, MyStruct
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using TacticalPort.Shared;

namespace TacticalPort.Core
{
    public sealed class TurnSystem : ITurnSystem
    {
        #region _____________________________/ VALUES

        private readonly List<UnitRuntime> _TurnOrder = new List<UnitRuntime>();
        private readonly Dictionary<UnitId, int> _LastInsertionIndexByAnchor = new Dictionary<UnitId, int>();
        private int _CurrentIndex = -1;

        #endregion

        #region _____________________________/ ACCESSORS

        public BattleTurnContext CurrentTurn { get; private set; }
        public int RoundIndex { get; private set; }

        #endregion

        #region _____________________________| SETUP

        public void Initialize(IEnumerable<UnitRuntime> pUnits)
        {
            _TurnOrder.Clear();
            _LastInsertionIndexByAnchor.Clear();
            BuildAlternatingTurnOrder(pUnits);

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

            UnitRuntime lActiveUnit = _TurnOrder[_CurrentIndex];
            lActiveUnit.BeginTurn();

            CurrentTurn = new BattleTurnContext(RoundIndex, _CurrentIndex + 1, lActiveUnit.Id);
            pTurnContext = CurrentTurn;
            return true;
        }

        public void CompleteCurrentTurn()
        {
            if (CurrentTurn == null)
                return;

            UnitId lActiveUnitId = CurrentTurn.UnitId;

            for (int lIndex = 0; lIndex < _TurnOrder.Count; lIndex++)
            {
                if (_TurnOrder[lIndex].Id != lActiveUnitId)
                    continue;

                _TurnOrder[lIndex].EndTurn();
                break;
            }

            CurrentTurn = null;
        }

        public void AddUnit(UnitRuntime pUnit, UnitId pAfterUnitId = default)
        {
            if (pUnit == null || !pUnit.IsAlive || !pUnit.Definition.ParticipatesInTurnOrder)
                return;

            if (CurrentTurn != null)
            {
                InsertAfterAnchorOrAppend(pUnit, pAfterUnitId);
                return;
            }

            InsertSorted(pUnit);
        }

        public void RemoveUnit(UnitId pUnitId)
        {
            bool lRemovedActiveUnit = CurrentTurn != null && CurrentTurn.UnitId == pUnitId;
            _LastInsertionIndexByAnchor.Remove(pUnitId);

            for (int lIndex = 0; lIndex < _TurnOrder.Count; lIndex++)
            {
                if (_TurnOrder[lIndex].Id != pUnitId)
                    continue;

                _TurnOrder.RemoveAt(lIndex);
                ReindexInsertionCursorsAfterRemove(lIndex);
                if (lIndex <= _CurrentIndex)
                    _CurrentIndex--;

                break;
            }

            if (lRemovedActiveUnit)
                CurrentTurn = null;
        }

        #endregion

        #region _____________________________| HELPERS

        private void BuildAlternatingTurnOrder(IEnumerable<UnitRuntime> pUnits)
        {
            if (pUnits == null)
                return;

            List<UnitRuntime> lTeamA = BuildTeamOrder(pUnits, Team.TeamA);
            List<UnitRuntime> lTeamB = BuildTeamOrder(pUnits, Team.TeamB);

            Team lStartingTeam = ResolveStartingTeam(lTeamA, lTeamB);
            if (lStartingTeam == Team.TeamB)
                AppendAlternating(lTeamB, lTeamA);
            else
                AppendAlternating(lTeamA, lTeamB);

            _TurnOrder.AddRange(
                pUnits
                    .Where(unit => unit != null && unit.IsAlive && unit.Definition.ParticipatesInTurnOrder && unit.Team == Team.Neutral)
                    .OrderByDescending(unit => unit.Definition.Initiative)
                    .ThenBy(unit => unit.Id.Value));
        }

        private static List<UnitRuntime> BuildTeamOrder(IEnumerable<UnitRuntime> pUnits, Team pTeam) =>
            pUnits
                .Where(unit => unit != null && unit.IsAlive && unit.Definition.ParticipatesInTurnOrder && unit.Team == pTeam)
                .OrderBy(unit => unit.Id.Value)
                .ToList();

        private void AppendAlternating(IReadOnlyList<UnitRuntime> pFirstTeam, IReadOnlyList<UnitRuntime> pSecondTeam)
        {
            int lMaxCount = Math.Max(pFirstTeam.Count, pSecondTeam.Count);
            for (int lIndex = 0; lIndex < lMaxCount; lIndex++)
            {
                if (lIndex < pFirstTeam.Count)
                    _TurnOrder.Add(pFirstTeam[lIndex]);

                if (lIndex < pSecondTeam.Count)
                    _TurnOrder.Add(pSecondTeam[lIndex]);
            }
        }

        private static Team ResolveStartingTeam(IReadOnlyList<UnitRuntime> pTeamA, IReadOnlyList<UnitRuntime> pTeamB)
        {
            if (pTeamA.Count == 0)
                return Team.TeamB;

            if (pTeamB.Count == 0)
                return Team.TeamA;

            int lTeamAInitiative = SumInitiative(pTeamA);
            int lTeamBInitiative = SumInitiative(pTeamB);
            return lTeamBInitiative > lTeamAInitiative ? Team.TeamB : Team.TeamA;
        }

        private static int SumInitiative(IReadOnlyList<UnitRuntime> pUnits)
        {
            int lTotal = 0;
            for (int lIndex = 0; lIndex < pUnits.Count; lIndex++)
                lTotal += pUnits[lIndex].Definition.Initiative;

            return lTotal;
        }

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

        private void InsertSorted(UnitRuntime pUnit)
        {
            int lInsertIndex = _TurnOrder.Count;

            for (int lIndex = 0; lIndex < _TurnOrder.Count; lIndex++)
            {
                UnitRuntime lExistingUnit = _TurnOrder[lIndex];
                if (lExistingUnit.Definition.Initiative > pUnit.Definition.Initiative)
                    continue;

                if (lExistingUnit.Definition.Initiative == pUnit.Definition.Initiative && lExistingUnit.Id.Value < pUnit.Id.Value)
                    continue;

                lInsertIndex = lIndex;
                break;
            }

            _TurnOrder.Insert(lInsertIndex, pUnit);
            ReindexInsertionCursorsAfterInsert(lInsertIndex);
        }

        private void InsertAfterAnchorOrAppend(UnitRuntime pUnit, UnitId pAfterUnitId)
        {
            int lInsertIndex = _TurnOrder.Count;
            if (pAfterUnitId.IsValid)
            {
                int lAnchorIndex = FindUnitIndex(pAfterUnitId);
                if (lAnchorIndex >= 0)
                {
                    lInsertIndex = lAnchorIndex + 1;
                    if (_LastInsertionIndexByAnchor.TryGetValue(pAfterUnitId, out int lLastInsertIndex) && lLastInsertIndex >= lAnchorIndex)
                        lInsertIndex = Math.Min(_TurnOrder.Count, lLastInsertIndex + 1);
                }
            }

            _TurnOrder.Insert(lInsertIndex, pUnit);
            ReindexInsertionCursorsAfterInsert(lInsertIndex);

            if (pAfterUnitId.IsValid)
                _LastInsertionIndexByAnchor[pAfterUnitId] = lInsertIndex;

            if (lInsertIndex <= _CurrentIndex)
                _CurrentIndex++;
        }

        private int FindUnitIndex(UnitId pUnitId)
        {
            for (int lIndex = 0; lIndex < _TurnOrder.Count; lIndex++)
            {
                if (_TurnOrder[lIndex].Id == pUnitId)
                    return lIndex;
            }

            return -1;
        }

        private void ReindexInsertionCursorsAfterInsert(int pInsertedIndex)
        {
            List<UnitId> lKeys = new List<UnitId>(_LastInsertionIndexByAnchor.Keys);
            for (int lIndex = 0; lIndex < lKeys.Count; lIndex++)
            {
                UnitId lKey = lKeys[lIndex];
                if (_LastInsertionIndexByAnchor[lKey] >= pInsertedIndex)
                    _LastInsertionIndexByAnchor[lKey]++;
            }
        }

        private void ReindexInsertionCursorsAfterRemove(int pRemovedIndex)
        {
            List<UnitId> lKeys = new List<UnitId>(_LastInsertionIndexByAnchor.Keys);
            for (int lIndex = 0; lIndex < lKeys.Count; lIndex++)
            {
                UnitId lKey = lKeys[lIndex];
                int lStoredIndex = _LastInsertionIndexByAnchor[lKey];
                if (lStoredIndex == pRemovedIndex)
                    _LastInsertionIndexByAnchor.Remove(lKey);
                else if (lStoredIndex > pRemovedIndex)
                    _LastInsertionIndexByAnchor[lKey] = lStoredIndex - 1;
            }
        }

        #endregion
    }
}
