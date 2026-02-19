// Utilité : ce script contient l'état mémoire du combat utilisé par le pipeline de résolution.
// Il centralise les mutations déterministes (positions, ticks, journal d'événements).
using System.Collections.Generic;

namespace PES.Core.Simulation
{
    /// <summary>
    /// Conteneur d'état métier pour la simulation tactique.
    /// Cette classe n'utilise volontairement aucune API Unity afin de rester testable et portable.
    /// </summary>
    public sealed class BattleState
    {
        // Position actuelle de chaque entité connue.
        // Un dictionnaire offre un accès/mise à jour en O(1) dans la majorité des cas.
        private readonly Dictionary<EntityId, Position3> _entityPositions = new();

        // Journal ordonné (append-only) des événements textuels produits par les actions résolues.
        private readonly List<string> _eventLog = new();

        /// <summary>
        /// Vue en lecture seule du journal d'événements (inspection externe sans mutation directe).
        /// </summary>
        public IReadOnlyList<string> EventLog => _eventLog;

        /// <summary>
        /// Compteur monotone de simulation, incrémenté après chaque action résolue.
        /// </summary>
        public int Tick { get; private set; }

        /// <summary>
        /// Avance la simulation d'un tick.
        /// </summary>
        public void AdvanceTick()
        {
            Tick++;
        }

        /// <summary>
        /// Ajoute un événement textuel au journal métier.
        /// </summary>
        public void AddEvent(string evt)
        {
            _eventLog.Add(evt);
        }

        /// <summary>
        /// Définit ou remplace la position d'une entité.
        /// Utile pour l'initialisation et certaines mutations métier explicites.
        /// </summary>
        public void SetEntityPosition(EntityId entityId, Position3 position)
        {
            _entityPositions[entityId] = position;
        }

        /// <summary>
        /// Tente de lire la position d'une entité.
        /// Retourne false si l'entité n'a pas de position connue dans l'état courant.
        /// </summary>
        public bool TryGetEntityPosition(EntityId entityId, out Position3 position)
        {
            return _entityPositions.TryGetValue(entityId, out position);
        }

        /// <summary>
        /// Déplace une entité uniquement si sa position courante correspond à l'origine attendue.
        /// Cela protège contre des commandes obsolètes ou mal ordonnées.
        /// </summary>
        public bool TryMoveEntity(EntityId entityId, Position3 expectedOrigin, Position3 destination)
        {
            // Rejet si l'entité n'existe pas OU si l'origine de la commande ne correspond pas.
            if (!_entityPositions.TryGetValue(entityId, out var current) || !current.Equals(expectedOrigin))
            {
                return false;
            }

            // Application du déplacement une fois la précondition validée.
            _entityPositions[entityId] = destination;
            return true;
        }
    }

    /// <summary>
    /// Position 3D entière immuable utilisée pour le stockage dans BattleState.
    /// </summary>
    public readonly struct Position3
    {
        /// <summary>
        /// Construit une nouvelle position 3D.
        /// </summary>
        public Position3(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>Composante horizontale X.</summary>
        public int X { get; }

        /// <summary>Composante horizontale Y.</summary>
        public int Y { get; }

        /// <summary>Composante verticale Z.</summary>
        public int Z { get; }

        /// <summary>
        /// Format lisible de coordonnées pour logs et débogage.
        /// </summary>
        public override string ToString()
        {
            return $"({X},{Y},{Z})";
        }
    }
}
