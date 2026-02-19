// Utilité : ce script implémente une première version d'attaque de base avec validations
// de portée, ligne de vue simplifiée, bonus de hauteur et dégâts déterministes via RNG.
using System;
using PES.Core.Random;
using PES.Core.Simulation;

namespace PES.Combat.Actions
{
    /// <summary>
    /// Commande représentant une attaque basique directe d'une entité vers une autre.
    /// Cette version reste volontairement simple mais pose les bases du calcul tactique 3D.
    /// </summary>
    public readonly struct BasicAttackAction : IActionCommand
    {
        // Portée maximale en Manhattan XY pour l'attaque de base.
        private const int MaxRange = 2;

        // Seuil de delta vertical au-delà duquel la ligne de vue est considérée bloquée (stub).
        private const int MaxLineOfSightDelta = 2;

        // Dégâts de base avant bonus de hauteur.
        private const int BaseDamage = 12;

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
        /// Résout l'attaque avec une validation de portée/LOS/hauteur puis applique des dégâts.
        /// </summary>
        public ActionResolution Resolve(BattleState state, IRngService rngService)
        {
            // On doit connaître les positions des deux entités pour valider portée et hauteur.
            if (!state.TryGetEntityPosition(AttackerId, out var attackerPosition) ||
                !state.TryGetEntityPosition(TargetId, out var targetPosition))
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"BasicAttackRejected: missing positions ({AttackerId} -> {TargetId})");
            }

            // Vérification de portée en distance Manhattan sur le plan XY.
            var horizontalDistance = Math.Abs(targetPosition.X - attackerPosition.X) + Math.Abs(targetPosition.Y - attackerPosition.Y);
            if (horizontalDistance > MaxRange)
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"BasicAttackRejected: out of range ({AttackerId} -> {TargetId}, range:{horizontalDistance})");
            }

            // Vérification de ligne de vue simplifiée via un seuil de différence verticale.
            var verticalDelta = Math.Abs(targetPosition.Z - attackerPosition.Z);
            if (verticalDelta > MaxLineOfSightDelta)
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"BasicAttackRejected: line of sight blocked ({AttackerId} -> {TargetId}, z:{verticalDelta})");
            }

            // Vérifie que la cible possède des points de vie (sinon combat state incomplet).
            if (!state.TryGetEntityHitPoints(TargetId, out _))
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"BasicAttackRejected: missing hit points for {TargetId}");
            }

            // Tirage déterministe de précision : hit sur 80% des cas.
            var roll = rngService.NextInt(0, 100);
            var hit = roll < 80;
            if (!hit)
            {
                return new ActionResolution(false, ActionResolutionCode.Missed, $"BasicAttackMissed: {AttackerId} -> {TargetId} [roll:{roll}]");
            }

            // Bonus de hauteur : l'attaquant gagne +2 dégâts s'il est plus haut que la cible.
            var heightBonus = attackerPosition.Z > targetPosition.Z ? 2 : 0;

            // Légère variance déterministe des dégâts pour le ressenti de combat.
            var variance = rngService.NextInt(0, 4);
            var finalDamage = BaseDamage + heightBonus + variance;

            // Application des dégâts au state.
            var damageApplied = state.TryApplyDamage(TargetId, finalDamage);
            if (!damageApplied)
            {
                return new ActionResolution(false, ActionResolutionCode.Rejected, $"BasicAttackRejected: failed to apply damage to {TargetId}");
            }

            return new ActionResolution(
                true,
                ActionResolutionCode.Succeeded,
                $"BasicAttackResolved: {AttackerId} -> {TargetId} [roll:{roll}, dmg:{finalDamage}, hBonus:{heightBonus}]");
        }
    }
}
