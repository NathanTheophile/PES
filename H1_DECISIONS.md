# Sprint H.1 — Décisions de dé-risquage MMO (court terme)

## Décision 1 — Contrat snapshot versionné

- Le snapshot de combat embarque désormais un champ de contrat: `BattleStateSnapshot.ContractVersion`.
- Version courante: `BattleStateSnapshot.CurrentContractVersion = 1`.
- Règle de robustesse: toute version invalide (<= 0) retombe sur la version courante.

Objectif: préparer la compatibilité inter-build des replays/saves de combat.

## Décision 2 — Hors périmètre Sprint H (report explicite)

Les sujets suivants sont **reportés** après le milestone jouable:

- transport réseau temps réel,
- serveur autoritaire / infra dédiée,
- comptes, persistance live, social/guilde/chat,
- live-ops et outils de production MMO.

Raison: priorité au niveau jouable UX/gameplay déterministe, sans dispersion d’effort.

## Critère d'entrée post-milestone jouable

Le chantier réseau/MMO ne démarre qu'après:
1. validation GO de la démo jouable,
2. replay/snapshot déterministes validés,
3. contrat d'action/payload/snapshot stabilisé.
