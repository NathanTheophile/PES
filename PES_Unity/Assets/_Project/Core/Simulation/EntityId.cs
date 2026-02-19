// Utilité : ce script définit un identifiant métier léger pour référencer les entités
// (unités, acteurs, cibles) de manière déterministe et sérialisable.
namespace PES.Core.Simulation
{
    /// <summary>
    /// Objet valeur immuable représentant un identifiant stable d'entité dans la simulation de combat.
    /// Conserver un ID en entier facilite sauvegarde/chargement, logs d'événements et réplication réseau.
    /// </summary>
    public readonly struct EntityId
    {
        /// <summary>
        /// Construit un identifiant d'entité à partir d'une valeur entière brute.
        /// </summary>
        public EntityId(int value)
        {
            // La valeur est affectée une seule fois à la construction car la struct est immuable.
            Value = value;
        }

        /// <summary>
        /// Valeur numérique brute de l'identifiant.
        /// </summary>
        public int Value { get; }

        /// <summary>
        /// Format lisible utile pour le débogage et les logs.
        /// </summary>
        public override string ToString()
        {
            return $"Entity({Value})";
        }
    }
}
