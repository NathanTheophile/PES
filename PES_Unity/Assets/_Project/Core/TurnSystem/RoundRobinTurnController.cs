using System;
using System.Collections.Generic;
using PES.Core.Simulation;

namespace PES.Core.TurnSystem
{
    /// <summary>
    /// Contrôleur de tour minimal : ordre fixe et budget d'actions par tour.
    /// </summary>
    public sealed class RoundRobinTurnController
    {
        private readonly List<EntityId> _order;
        private readonly int _actionsPerTurn;

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
            _currentIndex = 0;
            RemainingActions = _actionsPerTurn;
            Round = 1;
        }

        public int Round { get; private set; }

        public EntityId CurrentActorId => _order[_currentIndex];

        public int RemainingActions { get; private set; }

        public bool TryConsumeAction(EntityId actorId)
        {
            if (!actorId.Equals(CurrentActorId) || RemainingActions <= 0)
            {
                return false;
            }

            RemainingActions--;
            return true;
        }

        public void EndTurn()
        {
            _currentIndex = (_currentIndex + 1) % _order.Count;
            if (_currentIndex == 0)
            {
                Round++;
            }

            RemainingActions = _actionsPerTurn;
        }
    }
}
