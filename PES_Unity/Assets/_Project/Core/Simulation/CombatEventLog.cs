// Utilité : ce script définit un journal d'événements de combat structuré pour éviter
// de dépendre uniquement de chaînes de texte dans les flux de résolution.
using System.Collections.Generic;

namespace PES.Core.Simulation
{
    /// <summary>
    /// Journal append-only des événements de simulation produits par le pipeline d'action.
    /// </summary>
    public sealed class CombatEventLog
    {
        // Stockage ordonné des entrées d'événements métier.
        private readonly List<CombatEventRecord> _entries = new();

        /// <summary>
        /// Expose les entrées en lecture seule pour inspection/tests/replay.
        /// </summary>
        public IReadOnlyList<CombatEventRecord> Entries => _entries;

        /// <summary>
        /// Ajoute une entrée structurée au journal.
        /// </summary>
        public void Add(CombatEventRecord record)
        {
            _entries.Add(record);
        }
    }

    /// <summary>
    /// Enregistrement immutable d'un événement de pipeline d'action.
    /// </summary>
    public readonly struct CombatEventRecord
    {
        /// <summary>
        /// Construit un enregistrement d'événement complet.
        /// </summary>
        public CombatEventRecord(int tick, ActionResolutionCode code, string description)
        {
            Tick = tick;
            Code = code;
            Description = description;
        }

        /// <summary>Tick de simulation auquel l'événement a été écrit.</summary>
        public int Tick { get; }

        /// <summary>Code structuré du résultat d'action.</summary>
        public ActionResolutionCode Code { get; }

        /// <summary>Message lisible (debug/log humain).</summary>
        public string Description { get; }
    }
}
