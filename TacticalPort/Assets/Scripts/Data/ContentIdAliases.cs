#region _____________________________/ INFOS
//  AUTHOR : Nathan THEOPHILE (2026)
//  Engine : Unity
//  Note : Backward-compatible aliases for persisted content identifiers.
#endregion

using System;
using System.Collections.Generic;

namespace TacticalPort.Data
{
    public static class ContentIdAliases
    {
        private static readonly IReadOnlyDictionary<string, string> Aliases =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["forban"] = "freebooter",
                ["soif_de_richesse"] = "thirst_for_wealth",
                ["cuir_de_flibustier"] = "buccaneers_hide",
                ["harpon_abordage"] = "boarding_harpoon",
                ["sabre_crochet"] = "hooked_sabre",
                ["tir_pistolet"] = "pistol_shot",
                ["pluie_doublons"] = "doubloon_rain",
                ["brise_carene"] = "hull_breaker",
                ["coup_filet"] = "net_cast",
                ["grappin_malin"] = "clever_grapple",
                ["partage_tresor"] = "share_the_treasure",
                ["tir_sape"] = "sapping_shot",
                ["tir_rapine"] = "plunder_shot",
                ["harpon_abordage_variant"] = "boarding_harpoon_variant",
                ["sabre_crochet_variant"] = "hooked_sabre_variant",
                ["tir_pistolet_variant"] = "pistol_shot_variant",
                ["brise_carene_variant"] = "hull_breaker_variant",
                ["tir_rapine_variant"] = "plunder_shot_variant",
                ["forban_damage_progression"] = "freebooter_damage_progression",
                ["forban_resistance_progression"] = "freebooter_resistance_progression",
                ["coup_filet_state"] = "net_cast_state",
                ["pactole_damage"] = "jackpot_damage",
                ["pactole_resistance"] = "jackpot_resistance",
                ["partage_tresor_state"] = "share_the_treasure_state",
                ["sabre_crochet_state"] = "hooked_sabre_state",
                ["tir_pistolet_caster_state"] = "pistol_shot_caster_state",
                ["tir_pistolet_target_state"] = "pistol_shot_target_state",
                ["tir_sape_state"] = "sapping_shot_state",
                ["Carpenter"] = "carpenter",
                ["CarpenterPassivePermanentMaintenance"] = "carpenter_permanent_maintenance",
                ["CarpenterPassiveWorkshopNetwork"] = "carpenter_workshop_network",
                ["CarpenterDeckBallistaShot"] = "carpenter_deck_ballista_shot",
                ["CarpenterGeneralResistanceSupport"] = "carpenter_collective_plating",
                ["CarpenterHinderingShot"] = "carpenter_hindering_shot",
                ["CarpenterLongRangeShot"] = "carpenter_precision_shot",
                ["CarpenterPerpendicularEnergyBreak"] = "carpenter_energy_break",
                ["CarpenterPersistentMeleeStrike"] = "carpenter_persistent_strike",
                ["CarpenterRangedResistanceSiphon"] = "carpenter_armor_siphon",
                ["CarpenterSummonDeckBallista"] = "carpenter_summon_deck_ballista",
                ["CarpenterSummonHarpoonTurret"] = "carpenter_summon_harpoon_turret",
                ["CarpenterSummonMooringPulley"] = "carpenter_summon_mooring_pulley",
                ["CarpenterToggleMaintenance"] = "carpenter_alternating_maintenance",
                ["CarpenterGeneralResistanceSupportVariant"] = "carpenter_collective_plating_variant",
                ["CarpenterHinderingShotVariant"] = "carpenter_hindering_shot_variant",
                ["CarpenterLongRangeShotVariant"] = "carpenter_long_range_shot_variant",
                ["CarpenterPersistentMeleeStrikeVariant"] = "carpenter_persistent_strike_variant",
                ["CarpenterRangedResistanceSiphonVariant"] = "carpenter_armor_siphon_variant",
                ["CarpenterDeckBallista"] = "carpenter_deck_ballista",
                ["CarpenterHarpoonTurret"] = "carpenter_harpoon_turret",
                ["CarpenterMooringPulley"] = "carpenter_mooring_pulley",
                ["CarpenterBallistaAura"] = "carpenter_ballista_aura",
                ["CarpenterConstruction"] = "carpenter_construction",
                ["CarpenterEnergyMinus2"] = "carpenter_energy_minus_2",
                ["CarpenterGeneralResistance15"] = "carpenter_general_resistance_15",
                ["CarpenterHarpoonAura"] = "carpenter_harpoon_aura",
                ["CarpenterMobilityMinus1"] = "carpenter_mobility_minus_1",
                ["CarpenterMobilityMinus2"] = "carpenter_mobility_minus_2",
                ["CarpenterPersistentDamage"] = "carpenter_persistent_damage",
                ["CarpenterPulleyAura"] = "carpenter_pulley_aura",
                ["CarpenterRangedResistanceMinus5"] = "carpenter_ranged_resistance_minus_5",
                ["CarpenterRangedResistanceStolen5"] = "carpenter_ranged_resistance_stolen_5",
                ["CarpenterTurretActive"] = "carpenter_turret_active",
                ["CarpenterTurretInactive"] = "carpenter_turret_inactive",
                ["maree_solidaire"] = "solidarity_tide",
                ["egide_des_profondeurs"] = "deep_aegis",
                ["corail_nourricier"] = "nourishing_coral",
                ["deferlante_transversale"] = "transverse_surge",
                ["don_des_profondeurs"] = "gift_of_the_depths",
                ["chant_de_l_allant"] = "momentum_song",
                ["coquillage_siphonneur"] = "siphoning_shell",
                ["conque_des_horizons"] = "horizon_conch",
                ["maelstrom_abyssal"] = "abyssal_maelstrom",
                ["defaire_les_charmes"] = "unravel_charms",
                ["melodie_reparatrice"] = "mending_melody",
                ["erosion_saline"] = "salt_erosion"
            };

        public static string Normalize(string pId) =>
            !string.IsNullOrWhiteSpace(pId) && Aliases.TryGetValue(pId, out string lCanonicalId)
                ? lCanonicalId
                : pId;

        public static bool Matches(string pCanonicalId, string pCandidateId) =>
            string.Equals(Normalize(pCanonicalId), Normalize(pCandidateId), StringComparison.Ordinal);
    }
}
