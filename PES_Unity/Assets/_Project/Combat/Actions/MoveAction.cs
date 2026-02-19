// Utilité : ce script implémente la première commande métier de déplacement
// avec validation d'origine, trajectoire, occupation, obstacles et coût de mouvement.
using System;
using System.Collections.Generic;
using PES.Core.Random;
using PES.Core.Simulation;
using PES.Grid.Grid3D;
using PES.Grid.Pathfinding;

namespace PES.Combat.Actions
{
    /// <summary>
    /// Commande représentant une tentative de déplacement d'une coordonnée grille à une autre.
    /// </summary>
    public readonly struct MoveAction : IActionCommand
    {
        // Budget maximal de coût de mouvement pour une action.
        private const int MaxMovementCostPerAction = 3;

        // Budget de variation verticale autorisée entre deux cases consécutives.
        private const int MaxVerticalStepPerTile = 1;

        /// <summary>
        /// Construit une commande de déplacement pour un acteur donné.
        /// </summary>
        public MoveAction(EntityId actorId, GridCoord3 origin, GridCoord3 destination)
        {
            ActorId = actorId;
            Origin = origin;
            Destination = destination;
        }

        /// <summary>Entité qui tente de se déplacer.</summary>
        public EntityId ActorId { get; }

        /// <summary>Origine attendue, utilisée pour détecter les commandes obsolètes.</summary>
        public GridCoord3 Origin { get; }

        /// <summary>Destination demandée.</summary>
        public GridCoord3 Destination { get; }

        /// <summary>
        /// Résout le déplacement contre l'état de combat courant.
        /// </summary>
        public ActionResolution Resolve(BattleState state, IRngService rngService)
        {
            var originPosition = ToPosition(Origin);
            var destinationPosition = ToPosition(Destination);

            // Garde 1 : l'acteur doit exister à l'origine annoncée.
            if (!state.TryGetEntityPosition(ActorId, out var actorCurrentPosition) || !actorCurrentPosition.Equals(originPosition))
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"MoveActionRejected: invalid origin for {ActorId} ({Origin} -> {Destination})");
            }

            // Garde 2 : la destination ne doit pas être bloquée ni occupée par une autre entité.
            if (state.IsPositionBlocked(destinationPosition))
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"MoveActionRejected: destination blocked for {ActorId} ({Destination})");
            }

            if (state.IsPositionOccupied(destinationPosition, ActorId))
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"MoveActionRejected: destination occupied for {ActorId} ({Destination})");
            }

            // Construction de la carte des cellules bloquées terrain.
            var blockedCells = BuildBlockedCellSet(state, ActorId, originPosition, destinationPosition);
            var pathService = new PathfindingService();
            if (!pathService.TryComputePath(Origin, Destination, blockedCells, out var path))
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"MoveActionRejected: blocked path for {ActorId} ({Origin} -> {Destination})");
            }

            // Validation du coût de mouvement + contraintes de dénivelé par pas.
            var movementCost = ComputeMovementCost(state, path);
            if (movementCost > MaxMovementCostPerAction)
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"MoveActionRejected: movement cost exceeded for {ActorId} ({movementCost}/{MaxMovementCostPerAction})");
            }

            // Mutation d'état uniquement après validation complète.
            state.SetEntityPosition(ActorId, destinationPosition);
            return new ActionResolution(true, ActionResolutionCode.Succeeded, $"MoveActionResolved: {ActorId} {Origin} -> {Destination} [cost:{movementCost}]");
        }

        private static int ComputeMovementCost(BattleState state, IReadOnlyList<GridCoord3> path)
        {
            var totalCost = 0;
            for (var i = 1; i < path.Count; i++)
            {
                var previous = path[i - 1];
                var current = path[i];

                var verticalStep = Math.Abs(current.Z - previous.Z);
                if (verticalStep > MaxVerticalStepPerTile)
                {
                    return int.MaxValue;
                }

                var cellCost = state.GetMovementCost(ToPosition(current));
                var stepCost = cellCost + verticalStep;
                totalCost += stepCost;
            }

            return totalCost;
        }

        private static HashSet<GridCoord3> BuildBlockedCellSet(BattleState state, EntityId actorId, Position3 origin, Position3 destination)
        {
            var blocked = new HashSet<GridCoord3>();

            foreach (var blockedPosition in state.GetBlockedPositions())
            {
                blocked.Add(new GridCoord3(blockedPosition.X, blockedPosition.Y, blockedPosition.Z));
            }

            foreach (var pair in state.GetEntityPositions())
            {
                if (pair.Key.Equals(actorId))
                {
                    continue;
                }

                var position = pair.Value;
                if (position.Equals(origin) || position.Equals(destination))
                {
                    continue;
                }

                blocked.Add(new GridCoord3(position.X, position.Y, position.Z));
            }

            return blocked;
        }

        // Aide de conversion entre type coordonnée de grille et type stockage d'état.
        private static Position3 ToPosition(GridCoord3 coord)
        {
            return new Position3(coord.X, coord.Y, coord.Z);
        }
    }
}
