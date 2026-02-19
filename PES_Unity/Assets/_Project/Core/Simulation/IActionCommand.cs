// Utilité : ce script définit le contrat que chaque action de gameplay doit implémenter
// pour être exécutée par le pipeline central de résolution.
using PES.Core.Random;

namespace PES.Core.Simulation
{
    /// <summary>
    /// Abstraction de commande métier pour la simulation orientée actions.
    /// Chaque implémentation encapsule validation et mutation d'état.
    /// </summary>
    public interface IActionCommand
    {
        /// <summary>
        /// Résout l'action sur l'état de combat fourni en utilisant le service RNG centralisé.
        /// </summary>
        ActionResolution Resolve(BattleState state, IRngService rngService);
    }
}
