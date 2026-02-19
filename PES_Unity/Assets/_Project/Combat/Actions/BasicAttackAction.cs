// Utilité : ce script fournit un premier stub de commande d'attaque de base, prêt pour du déterminisme.
// Il montre l'usage du service RNG centralisé.
using PES.Core.Random;
using PES.Core.Simulation;

namespace PES.Combat.Actions
{
    /// <summary>
    /// Commande représentant une attaque basique directe d'une entité vers une autre.
    /// </summary>
    public readonly struct BasicAttackAction : IActionCommand
    {
        /// <summary>
        /// Construit une commande d'attaque de base.
        /// </summary>
        public BasicAttackAction(EntityId attackerId, EntityId targetId)
        {
            AttackerId = attackerId;
            TargetId = targetId;
        }

        /// <summary>ID de l'entité attaquante.</summary>
        public EntityId AttackerId { get; }

        /// <summary>ID de l'entité cible.</summary>
        public EntityId TargetId { get; }

        /// <summary>
        /// Résout une règle temporaire toucher/rater en utilisant le RNG centralisé.
        /// </summary>
        public ActionResolution Resolve(BattleState state, IRngService rngService)
        {
            // Tirage dans [0,100) pour une probabilité de toucher bootstrap.
            var roll = rngService.NextInt(0, 100);

            // Règle temporaire : 75% de chance de toucher (>= 25).
            var hit = roll >= 25;
            var summary = hit ? "hit" : "miss";

            // Retourne un résultat détaillé pour logs/debug.
            return new ActionResolution(hit, $"BasicAttackAction: {AttackerId} -> {TargetId} [{summary}:{roll}]");
        }
    }
}
