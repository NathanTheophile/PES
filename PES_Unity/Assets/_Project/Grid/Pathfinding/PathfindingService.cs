// Utilité : ce script fournit l'API de pathfinding placeholder pour les futures règles de navigation.
using System.Collections.Generic;
using PES.Grid.Grid3D;

namespace PES.Grid.Pathfinding
{
    /// <summary>
    /// Stub temporaire de pathfinding.
    /// Il sera remplacé par une recherche tenant compte des obstacles et de la hauteur.
    /// </summary>
    public sealed class PathfindingService
    {
        /// <summary>
        /// Calcule un chemin entre deux coordonnées.
        /// Comportement bootstrap actuel : retourne un chemin minimal à 2 nœuds [from, to].
        /// </summary>
        public IReadOnlyList<GridCoord3> ComputePath(GridCoord3 from, GridCoord3 to)
        {
            // Intentionnellement simple pour garantir une base compilable au bootstrap.
            return new[] { from, to };
        }
    }
}
