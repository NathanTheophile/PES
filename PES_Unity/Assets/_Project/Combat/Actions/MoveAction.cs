// Utilité : ce script implémente la première commande métier de déplacement
// avec validation d'origine, trajectoire, occupation, obstacles et coût de mouvement.
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
        // Politique par défaut du sprint : 1 action = 3 points de coût max, 1 niveau de hauteur max par pas.
        private static readonly MoveActionPolicy DefaultPolicy = new(maxMovementCostPerAction: 3, maxVerticalStepPerTile: 1);

        private readonly MoveActionPolicy? _policyOverride;

        /// <summary>
        /// Construit une commande de déplacement pour un acteur donné.
        /// </summary>
        public MoveAction(EntityId actorId, GridCoord3 origin, GridCoord3 destination)
            : this(actorId, origin, destination, null)
        {
        }

        /// <summary>
        /// Construit une commande de déplacement avec politique data-driven explicite.
        /// </summary>
        public MoveAction(EntityId actorId, GridCoord3 origin, GridCoord3 destination, MoveActionPolicy? policyOverride)
        {
            ActorId = actorId;
            Origin = origin;
            Destination = destination;
            _policyOverride = policyOverride;
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
            var policy = _policyOverride ?? DefaultPolicy;

            var validationService = new MoveValidationService();
            var validation = validationService.Validate(state, ActorId, Origin, Destination, policy);
            if (!validation.Success)
            {
                return validation.Failure switch
                {
                    MoveValidationFailure.InvalidOrigin =>
                        new ActionResolution(false, ActionResolutionCode.Rejected, $"MoveActionRejected: invalid origin for {ActorId} ({Origin} -> {Destination})", ActionFailureReason.InvalidOrigin),
                    MoveValidationFailure.DestinationBlocked =>
                        new ActionResolution(false, ActionResolutionCode.Rejected, $"MoveActionRejected: destination blocked for {ActorId} ({Destination})", ActionFailureReason.DestinationBlocked),
                    MoveValidationFailure.DestinationOccupied =>
                        new ActionResolution(false, ActionResolutionCode.Rejected, $"MoveActionRejected: destination occupied for {ActorId} ({Destination})", ActionFailureReason.DestinationOccupied),
                    MoveValidationFailure.BlockedPath =>
                        new ActionResolution(false, ActionResolutionCode.Rejected, $"MoveActionRejected: blocked path for {ActorId} ({Origin} -> {Destination})", ActionFailureReason.BlockedPath),
                    MoveValidationFailure.VerticalStepTooHigh =>
                        new ActionResolution(false, ActionResolutionCode.Rejected, $"MoveActionRejected: vertical step too high for {ActorId} ({Origin} -> {Destination})", ActionFailureReason.VerticalStepTooHigh),
                    MoveValidationFailure.MovementBudgetExceeded =>
                        new ActionResolution(false, ActionResolutionCode.Rejected, $"MoveActionRejected: movement cost exceeded for {ActorId} ({validation.MovementCost}/{policy.MaxMovementCostPerAction})", ActionFailureReason.MovementBudgetExceeded),
                    MoveValidationFailure.NoMovement =>
                        new ActionResolution(false, ActionResolutionCode.Rejected, $"MoveActionRejected: origin and destination are identical for {ActorId} ({Origin})", ActionFailureReason.NoMovement),
                    _ =>
                        new ActionResolution(false, ActionResolutionCode.Rejected, $"MoveActionRejected: validation failed for {ActorId} ({Origin} -> {Destination})", ActionFailureReason.InvalidTargeting),
                };
            }

            var moved = state.TryMoveEntity(ActorId, new Position3(Origin.X, Origin.Y, Origin.Z), new Position3(Destination.X, Destination.Y, Destination.Z));
            if (!moved)
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"MoveActionRejected: state mutation failed for {ActorId} ({Origin} -> {Destination})", ActionFailureReason.StateMutationFailed);
            }

            return new ActionResolution(true, ActionResolutionCode.Succeeded, $"MoveActionResolved: {ActorId} {Origin} -> {Destination} [cost:{validation.MovementCost}]");
        }
    }
}
