using System;
using System.Collections.Generic;

namespace PES.Core.TurnSystem
{
    /// <summary>
    /// Construit un ordre d'initiative individuel (acteur par acteur), trié par rapidité décroissante.
    /// Le teamId n'influence pas l'ordre : deux acteurs de la même équipe peuvent jouer à la suite.
    /// </summary>
    public static class InitiativeOrderService
    {
        public static BattleActorDefinition[] BuildIndividualTurnOrder(IReadOnlyList<BattleActorDefinition> actorDefinitions)
        {
            if (actorDefinitions == null || actorDefinitions.Count == 0)
            {
                throw new ArgumentException("Actor definitions must contain at least one actor.", nameof(actorDefinitions));
            }

            var orderedDefinitions = new BattleActorDefinition[actorDefinitions.Count];
            for (var i = 0; i < actorDefinitions.Count; i++)
            {
                orderedDefinitions[i] = actorDefinitions[i];
            }

            Array.Sort(orderedDefinitions, static (left, right) =>
            {
                var rapidityCompare = right.Rapidity.CompareTo(left.Rapidity);
                if (rapidityCompare != 0)
                {
                    return rapidityCompare;
                }

                return left.ActorId.Value.CompareTo(right.ActorId.Value);
            });

            return orderedDefinitions;
        }
    }
}
