// Utilité : ce script orchestre le pipeline d'action déterministe :
// exécution de commande, journalisation d'événement, progression du tick.
using PES.Core.Random;

namespace PES.Core.Simulation
{
    /// <summary>
    /// Service métier central chargé de résoudre les commandes d'action.
    /// Il impose un flux unique : résolution -> log événement -> incrément du tick.
    /// </summary>
    public sealed class ActionResolver
    {
        // Instance RNG injectée une fois, pour garantir des simulations reproductibles.
        private readonly IRngService _rngService;

        /// <summary>
        /// Construit un resolver lié à un service RNG donné.
        /// </summary>
        public ActionResolver(IRngService rngService)
        {
            _rngService = rngService;
        }

        /// <summary>
        /// Exécute une commande d'action et applique le post-traitement standard du pipeline.
        /// </summary>
        public ActionResolution Resolve(BattleState state, IActionCommand command)
        {
            // 1) L'action se valide et mutile l'état si nécessaire.
            var result = command.Resolve(state, _rngService);

            // 2) On persiste un événement structuré + lisible pour replay/debug.
            state.AddEvent(new CombatEventRecord(state.Tick, result.Code, result.Description));

            // 3) On avance la timeline déterministe.
            state.AdvanceTick();
            return result;
        }
    }

    /// <summary>
    /// Codes standards pour classifier les résultats d'action sans parser du texte.
    /// </summary>
    public enum ActionResolutionCode
    {
        Succeeded = 0,
        Rejected = 1,
        Missed = 2,
    }

    /// <summary>
    /// Raisons normalisées pour outillage/test/replay sans dépendre des messages texte.
    /// </summary>
    public enum ActionFailureReason
    {
        None = 0,
        InvalidOrigin = 1,
        DestinationBlocked = 2,
        DestinationOccupied = 3,
        BlockedPath = 4,
        VerticalStepTooHigh = 5,
        MovementBudgetExceeded = 6,
        StateMutationFailed = 7,
        MissingPositions = 8,
        TooClose = 9,
        OutOfRange = 10,
        LineOfSightBlocked = 11,
        MissingHitPoints = 12,
        DamageApplicationFailed = 13,
        HitRollMissed = 14,
        InvalidTargeting = 15,
        NoMovement = 16,
        TurnTimedOut = 17,
    }

    /// <summary>
    /// DTO standard décrivant le résultat d'une résolution de commande.
    /// </summary>
    public readonly struct ActionResolution
    {
        /// <summary>
        /// Construit un nouvel objet résultat d'action.
        /// </summary>
        public ActionResolution(bool success, ActionResolutionCode code, string description, ActionFailureReason failureReason = ActionFailureReason.None)
        {
            Success = success;
            Code = code;
            Description = description;
            FailureReason = failureReason;
        }

        /// <summary>
        /// True si l'action est acceptée/réussie par les règles métier.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Code structuré exploitable par les tests, UI et logs analytiques.
        /// </summary>
        public ActionResolutionCode Code { get; }

        /// <summary>
        /// Raison normalisée de l'échec (None si succès).
        /// </summary>
        public ActionFailureReason FailureReason { get; }

        /// <summary>
        /// Résumé textuel pour logs/debug et futur flux d'événements.
        /// </summary>
        public string Description { get; }
    }
}
