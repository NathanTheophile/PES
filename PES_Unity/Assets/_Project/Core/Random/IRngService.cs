// Utilité : ce script définit l'abstraction RNG centralisée pour un gameplay déterministe.
namespace PES.Core.Random
{
    /// <summary>
    /// Contrat RNG utilisé par les règles métier pour rendre l'aléatoire explicite et injectable.
    /// </summary>
    public interface IRngService
    {
        /// <summary>
        /// Retourne un entier dans l'intervalle [minInclusive, maxExclusive).
        /// </summary>
        int NextInt(int minInclusive, int maxExclusive);
    }
}
