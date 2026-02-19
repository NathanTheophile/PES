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

            // 2) On persiste un événement lisible pour replay/debug.
            state.AddEvent(result.Description);

            // 3) On avance la timeline déterministe.
            state.AdvanceTick();
            return result;
        }
    }

    /// <summary>
    /// DTO standard décrivant le résultat d'une résolution de commande.
    /// </summary>
    public readonly struct ActionResolution
    {
        /// <summary>
        /// Construit un nouvel objet résultat d'action.
        /// </summary>
        public ActionResolution(bool success, string description)
        {
            Success = success;
            Description = description;
        }

        /// <summary>
        /// True si l'action est acceptée/réussie par les règles métier.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Résumé textuel pour logs/debug et futur flux d'événements.
        /// </summary>
        public string Description { get; }
    }
}
