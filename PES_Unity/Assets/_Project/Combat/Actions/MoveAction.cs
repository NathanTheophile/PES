// Utilité : ce script implémente la première commande métier de déplacement
// avec validation minimale d'origine, de distance et de dénivelé.
using System;
using PES.Core.Random;
using PES.Core.Simulation;
using PES.Grid.Grid3D;

namespace PES.Combat.Actions
{
    /// <summary>
    /// Commande représentant une tentative de déplacement d'une coordonnée grille à une autre.
    /// </summary>
    public readonly struct MoveAction : IActionCommand
    {
        // Budget de déplacement horizontal (distance Manhattan XY) pour ce premier slice.
        private const int MaxDistancePerAction = 3;

        // Budget de variation verticale autorisée sur une action.
        private const int MaxVerticalStep = 1;

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
            // Garde 1 : l'entité doit exister et être exactement à l'origine annoncée.
            if (!state.TryMoveEntity(ActorId, ToPosition(Origin), ToPosition(Destination)))
            {
                return new ActionResolution(false, $"MoveActionRejected: invalid origin for {ActorId} ({Origin} -> {Destination})");
            }

            // Calcul des coûts horizontal et vertical en espace grille.
            var horizontalDistance = Math.Abs(Destination.X - Origin.X) + Math.Abs(Destination.Y - Origin.Y);
            var verticalDelta = Math.Abs(Destination.Z - Origin.Z);

            // Rejet si le budget est dépassé + rollback de la mutation temporaire.
            if (horizontalDistance > MaxDistancePerAction || verticalDelta > MaxVerticalStep)
            {
                state.SetEntityPosition(ActorId, ToPosition(Origin));
                return new ActionResolution(false, $"MoveActionRejected: out of bounds for {ActorId} ({Origin} -> {Destination})");
            }

            // Action acceptée.
            return new ActionResolution(true, $"MoveActionResolved: {ActorId} {Origin} -> {Destination}");
        }

        // Aide de conversion entre type coordonnée de grille et type stockage d'état.
        private static Position3 ToPosition(GridCoord3 coord)
        {
            return new Position3(coord.X, coord.Y, coord.Z);
        }
    }
}
