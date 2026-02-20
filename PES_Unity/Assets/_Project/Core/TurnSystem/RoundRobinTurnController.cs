using System;
using System.Collections.Generic;
using PES.Core.Simulation;

namespace PES.Core.TurnSystem
{
    /// <summary>
    /// Contrôleur de tour round-robin avec budget d'actions et gestion d'acteurs actifs/inactifs.
    /// </summary>
    public sealed class RoundRobinTurnController
    {
        private readonly List<EntityId> _order;
        private readonly int _actionsPerTurn;
        private readonly HashSet<EntityId> _inactiveActors;

        private int _currentIndex;

        public RoundRobinTurnController(IReadOnlyList<EntityId> turnOrder, int actionsPerTurn = 1)
        {
            if (turnOrder == null || turnOrder.Count == 0)
            {
                throw new ArgumentException("Turn order must contain at least one actor.", nameof(turnOrder));
            }

            if (actionsPerTurn <= 0)
            {
                throw new ArgumentException("Actions per turn must be > 0.", nameof(actionsPerTurn));
            }

            _order = new List<EntityId>(turnOrder);
            _actionsPerTurn = actionsPerTurn;
            _inactiveActors = new HashSet<EntityId>();
            _currentIndex = 0;
            Round = 1;

            EnsureCurrentActorIsActive();
            RemainingActions = HasAnyActiveActor() ? _actionsPerTurn : 0;
        }

        public int Round { get; private set; }

        public EntityId CurrentActorId => _order[_currentIndex];

        public int RemainingActions { get; private set; }

        public bool IsActorActive(EntityId actorId)
        {
            return !_inactiveActors.Contains(actorId);
        }

        public bool SetActorActive(EntityId actorId, bool isActive)
        {
            if (!_order.Contains(actorId))
            {
                return false;
            }

            if (isActive)
            {
                _inactiveActors.Remove(actorId);
            }
            else
            {
                _inactiveActors.Add(actorId);
            }

            EnsureCurrentActorIsActive();
            if (!HasAnyActiveActor())
            {
                RemainingActions = 0;
            }
            else if (RemainingActions <= 0)
            {
                RemainingActions = _actionsPerTurn;
            }

            return true;
        }

        public bool TryConsumeAction(EntityId actorId)
        {
            if (!actorId.Equals(CurrentActorId) || RemainingActions <= 0 || !IsActorActive(actorId))
            {
                return false;
            }

            RemainingActions--;
            return true;
        }

        public void EndTurn()
        {
            if (!HasAnyActiveActor())
            {
                RemainingActions = 0;
                return;
            }

            AdvanceToNextActiveActor();
            RemainingActions = _actionsPerTurn;
        }

        private bool HasAnyActiveActor()
        {
            return _inactiveActors.Count < _order.Count;
        }

        private void EnsureCurrentActorIsActive()
        {
            if (!HasAnyActiveActor() || IsActorActive(CurrentActorId))
            {
                return;
            }

            AdvanceToNextActiveActor();
        }

        private void AdvanceToNextActiveActor()
        {
            var startIndex = _currentIndex;

            do
            {
                _currentIndex = (_currentIndex + 1) % _order.Count;
                if (_currentIndex == 0)
                {
                    Round++;
                }

                if (IsActorActive(CurrentActorId))
                {
                    return;
                }
            }
            while (_currentIndex != startIndex);
        }
    }
}
