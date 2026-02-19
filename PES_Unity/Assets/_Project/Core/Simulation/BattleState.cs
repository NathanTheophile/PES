// Utilité : ce script contient l'état mémoire du combat utilisé par le pipeline de résolution.
// Il centralise les mutations déterministes (positions, points de vie, ticks, journal d'événements).
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

        // Points de vie courants par entité.
        private readonly Dictionary<EntityId, int> _entityHitPoints = new();

        // Cellules bloquées de la grille (murs, obstacles statiques) côté simulation.
        private readonly HashSet<Position3> _blockedPositions = new();

        // Coûts de terrain par cellule (1 par défaut si non configuré).
        private readonly Dictionary<Position3, int> _positionMovementCosts = new();

        // Journal ordonné (append-only) des événements textuels produits par les actions résolues.
        private readonly List<string> _eventLog = new();

        // Journal structuré pour le futur replay/réseau/UI sans parsing de texte.
        private readonly CombatEventLog _combatEventLog = new();

        /// <summary>
        /// Vue en lecture seule du journal d'événements textuels (inspection externe sans mutation directe).
        /// </summary>
        public IReadOnlyList<string> EventLog => _eventLog;

        /// <summary>
        /// Vue en lecture seule du journal structuré (tick + code + description).
        /// </summary>
        public IReadOnlyList<CombatEventRecord> StructuredEventLog => _combatEventLog.Entries;

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
        /// Ajoute un événement structuré au journal métier pour consommation machine.
        /// </summary>
        public void AddEvent(CombatEventRecord record)
        {
            _combatEventLog.Add(record);
            _eventLog.Add(record.Description);
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
        /// Définit ou remplace les points de vie d'une entité.
        /// </summary>
        public void SetEntityHitPoints(EntityId entityId, int hitPoints)
        {
            _entityHitPoints[entityId] = hitPoints;
        }

        /// <summary>
        /// Tente de lire les points de vie courants d'une entité.
        /// </summary>
        public bool TryGetEntityHitPoints(EntityId entityId, out int hitPoints)
        {
            return _entityHitPoints.TryGetValue(entityId, out hitPoints);
        }

        /// <summary>
        /// Applique des dégâts à une entité si elle existe dans la table HP.
        /// Les HP sont plafonnés à minimum 0.
        /// </summary>
        public bool TryApplyDamage(EntityId entityId, int damage)
        {
            if (!_entityHitPoints.TryGetValue(entityId, out var hp))
            {
                return false;
            }

            var safeDamage = damage < 0 ? 0 : damage;
            var next = hp - safeDamage;
            _entityHitPoints[entityId] = next < 0 ? 0 : next;
            return true;
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


        /// <summary>
        /// Marque (ou démarque) une cellule comme bloquée pour le pathfinding métier.
        /// </summary>
        public void SetBlockedPosition(Position3 position, bool blocked = true)
        {
            if (blocked)
            {
                _blockedPositions.Add(position);
                return;
            }

            _blockedPositions.Remove(position);
        }

        /// <summary>
        /// Retourne true si une cellule est bloquée par le terrain/obstacle.
        /// </summary>
        public bool IsPositionBlocked(Position3 position)
        {
            return _blockedPositions.Contains(position);
        }

        /// <summary>
        /// Retourne true si la cellule est occupée par une entité (optionnellement hors une entité ignorée).
        /// </summary>
        public bool IsPositionOccupied(Position3 position, EntityId? ignoredEntityId = null)
        {
            foreach (var pair in _entityPositions)
            {
                if (ignoredEntityId.HasValue && pair.Key.Equals(ignoredEntityId.Value))
                {
                    continue;
                }

                if (pair.Value.Equals(position))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Définit le coût de mouvement d'une cellule (minimum 1).
        /// </summary>
        public void SetMovementCost(Position3 position, int movementCost)
        {
            _positionMovementCosts[position] = movementCost < 1 ? 1 : movementCost;
        }

        /// <summary>
        /// Retourne le coût de mouvement d'une cellule (1 si non spécifié).
        /// </summary>
        public int GetMovementCost(Position3 position)
        {
            return _positionMovementCosts.TryGetValue(position, out var movementCost) ? movementCost : 1;
        }

        /// <summary>
        /// Retourne la collection des cellules explicitement bloquées par le terrain.
        /// </summary>
        public IEnumerable<Position3> GetBlockedPositions()
        {
            return _blockedPositions;
        }

        /// <summary>
        /// Retourne les couples (entité, position) courants de la simulation.
        /// </summary>
        public IEnumerable<KeyValuePair<EntityId, Position3>> GetEntityPositions()
        {
            return _entityPositions;
        }

        /// <summary>
        /// Crée un snapshot immuable de l'état courant pour sauvegarde/replay/debug.
        /// </summary>
        public BattleStateSnapshot CreateSnapshot()
        {
            var positions = new EntityPositionSnapshot[_entityPositions.Count];
            var index = 0;
            foreach (var pair in _entityPositions)
            {
                positions[index++] = new EntityPositionSnapshot(pair.Key, pair.Value);
            }

            var hitPoints = new EntityHitPointSnapshot[_entityHitPoints.Count];
            index = 0;
            foreach (var pair in _entityHitPoints)
            {
                hitPoints[index++] = new EntityHitPointSnapshot(pair.Key, pair.Value);
            }

            return new BattleStateSnapshot(Tick, positions, hitPoints);
        }

        /// <summary>
        /// Restaure complètement l'état depuis un snapshot (utile pour rollback/reprise/replay).
        /// </summary>
        public void ApplySnapshot(BattleStateSnapshot snapshot)
        {
            _entityPositions.Clear();
            foreach (var row in snapshot.EntityPositions)
            {
                _entityPositions[row.EntityId] = row.Position;
            }

            _entityHitPoints.Clear();
            foreach (var row in snapshot.EntityHitPoints)
            {
                _entityHitPoints[row.EntityId] = row.HitPoints;
            }

            Tick = snapshot.Tick;
        }
    }

    /// <summary>
    /// Snapshot immuable et sérialisable de l'état d'un combat à un tick donné.
    /// </summary>
    public readonly struct BattleStateSnapshot
    {
        /// <summary>
        /// Construit un snapshot complet de combat.
        /// </summary>
        public BattleStateSnapshot(int tick, EntityPositionSnapshot[] entityPositions, EntityHitPointSnapshot[] entityHitPoints)
        {
            Tick = tick;
            EntityPositions = entityPositions;
            EntityHitPoints = entityHitPoints;
        }

        /// <summary>Tick capturé.</summary>
        public int Tick { get; }

        /// <summary>Table immuable des positions au moment du snapshot.</summary>
        public IReadOnlyList<EntityPositionSnapshot> EntityPositions { get; }

        /// <summary>Table immuable des points de vie au moment du snapshot.</summary>
        public IReadOnlyList<EntityHitPointSnapshot> EntityHitPoints { get; }
    }

    /// <summary>
    /// Ligne de snapshot position (entité + coordonnée).
    /// </summary>
    public readonly struct EntityPositionSnapshot
    {
        /// <summary>
        /// Construit une ligne de position snapshot.
        /// </summary>
        public EntityPositionSnapshot(EntityId entityId, Position3 position)
        {
            EntityId = entityId;
            Position = position;
        }

        /// <summary>ID entité concernée.</summary>
        public EntityId EntityId { get; }

        /// <summary>Position capturée.</summary>
        public Position3 Position { get; }
    }

    /// <summary>
    /// Ligne de snapshot points de vie (entité + HP).
    /// </summary>
    public readonly struct EntityHitPointSnapshot
    {
        /// <summary>
        /// Construit une ligne de HP snapshot.
        /// </summary>
        public EntityHitPointSnapshot(EntityId entityId, int hitPoints)
        {
            EntityId = entityId;
            HitPoints = hitPoints;
        }

        /// <summary>ID entité concernée.</summary>
        public EntityId EntityId { get; }

        /// <summary>HP capturés.</summary>
        public int HitPoints { get; }
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
