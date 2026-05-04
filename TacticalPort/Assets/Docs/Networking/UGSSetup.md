# UGS Setup - V1 Smoke Test

## Objectif

Ce document fige le setup Unity Game Services V1 pour valider le flow minimal suivant :

- Unity Authentication en connexion anonyme.
- Unity Matchmaker avec la queue `quickmatch1v1unranked`.
- Pool Matchmaker en `Client Hosting`.
- Aucun EdgeGap, aucun Cloud Code et aucun secret pour cette passe.

L'objectif immediat est de rendre la queue `quickmatch1v1unranked` valide et de pouvoir lancer ensuite un test code minimal : sign-in anonyme, creation de ticket, polling du ticket, lecture d'une assignation client-hosted/session/match id.

## Etat Local Unity

Projet Unity Cloud lie dans les Project Settings :

- Project Name : `TacticalVersus`
- Unity Organization : `unity_DACC1972F84B2C9E967B`
- Unity Project ID : `4a6394f1-74d7-47cb-a918-12ad859c2d71`
- App cible enfants / COPPA : `No`

Packages observes dans `Packages/manifest.json` :

- `com.unity.services.multiplayer` : `2.2.1`
- `com.unity.services.cloudcode` : `2.10.2`
- `com.unity.services.deployment` : `1.7.2`
- Les dependances Authentication/Lobby/Matchmaker sont apportees par les packages Unity Services installes.

Les packages EdgeGap et PurrNet peuvent rester presents dans le projet, mais ne sont pas utilises pour ce smoke test.

## Etat Dashboard Observe

Projet Unity Cloud :

- Projet : `TacticalVersus`
- Environnement actif : `production`
- Environment ID : `42335bdb-92e9-409c-9dbd-db5cbe555b9b`
- Environnement cree le : `22 avr. 2026`

Services :

- Matchmaker : actif.
- Lobby : actif.
- Authentication : actif.
- Cloud Code : package installe et service visible, mais volontairement non utilise pour cette passe.
- Secrets : aucun secret de projet, et c'est normal pour le smoke test.

Authentication :

- Providers visibles : `Unity Player Accounts`, `Username & Password`, et autres providers optionnels.
- Pour la V1 smoke test, aucun provider supplementaire n'est necessaire : le code utilisera l'anonymous sign-in.
- L'avertissement sur `Unity Player Accounts` n'est pas bloquant pour l'authentification anonyme.

Lobby production :

- Minimum player slots : `1`
- Maximum player slots : `150`
- Active Lifespan : `30s`
- Disconnect Removal Time : `2m`
- Disconnect Host Migration Time : `10s`

Cette configuration est acceptable pour le smoke test. Le lobby prive sera branche apres validation du ticket Matchmaker minimal.

Matchmaker production :

- Matchmaker : `on`
- Queue : `quickmatch1v1unranked`
- Queue marquee `Default`
- Maximum players on a ticket : `2`
- Pools : `0`
- Statut actuel observe : `Invalid`
- Raison : `Requires a valid pool before Matchmaking can begin`
- Date creation queue observee : `May 4, 2026 at 1:30 AM`

## Fix Dashboard A Faire

Chemin dashboard :

`Matchmaker > Files d'attente > quickmatch1v1unranked > Pools > Create pool`

Pool details :

- `Pool name` : `defaultClientHosting`
- `Queue` : `quickmatch1v1unranked`
- `Pool type` : `Default pool`
- `Timeout (seconds)` : `60`

Hosting settings :

- Choisir `Client Hosting`.
- Ne pas choisir `Cloud Code` maintenant.
- Ne pas choisir `Multiplay Hosting (Deprecated)`.
- Si Unity demande une region QoS par defaut, choisir la region Europe la plus proche.

Rules :

- Passer en mode JSON si l'interface le propose.
- Utiliser une configuration 1v1 sans filtre, sans MMR, sans backfill.

```json
{
  "Name": "QuickMatch1v1Unranked",
  "BackfillEnabled": false,
  "MatchDefinition": {
    "Teams": [
      {
        "Name": "Teams",
        "TeamCount": {
          "Min": 2,
          "Max": 2
        },
        "PlayerCount": {
          "Min": 1,
          "Max": 1
        }
      }
    ],
    "MatchRules": []
  }
}
```

Si le dashboard Unity modifie legerement le schema ou les noms de champs, garder la semantique exacte :

- 2 equipes.
- 1 joueur par equipe.
- Pas de regle de skill/MMR.
- Pas de filtre.
- Backfill desactive.

Resultat attendu apres creation :

- Queue `quickmatch1v1unranked` en statut `Valid`.
- `Pools = 1`.
- Pool visible : `defaultClientHosting`.
- Aucun ticket unmatched inattendu avant les tests.

## Client Hosting vs EdgeGap

`Client Hosting` est uniquement un smoke test UGS. Il sert a verifier que Authentication + Matchmaker + flow session/ticket fonctionnent sans serveur dedie.

Ce smoke test ne valide pas encore :

- Allocation serveur dedie EdgeGap.
- Secrets EdgeGap.
- Cloud Code.
- Connexion PurrNet a un endpoint serveur dedie.
- Autorite serveur complete.

La phase serveur dedie viendra ensuite :

- Creer un secret EdgeGap cote UGS, jamais cote client.
- Creer un module Cloud Code avec fonctions `allocate` et `poll`.
- Creer ou convertir un pool Matchmaker en hosting via Cloud Code.
- Laisser Cloud Code appeler EdgeGap.
- Retourner endpoint + manifest de match aux clients.
- Connecter PurrNet au serveur dedie.

## Checklist Avant Code

Avant d'ajouter ou modifier du code UGS reel :

- `Packages/manifest.json` contient toujours `com.unity.services.multiplayer`.
- Authentication est active sur le dashboard.
- Lobby est actif sur le dashboard.
- Matchmaker est actif sur le dashboard.
- Environnement selectionne : `production`.
- Queue `quickmatch1v1unranked` existe.
- Queue `quickmatch1v1unranked` est `Valid`.
- Queue `quickmatch1v1unranked` indique `Pools = 1`.
- Pool `defaultClientHosting` existe.
- Pool `defaultClientHosting` utilise `Client Hosting` / `Peer-to-peer hosting`.
- Pool `defaultClientHosting` utilise un timeout de `60` secondes.
- Les rules correspondent au 1v1 sans filtre.
- Aucun secret EdgeGap n'est stocke dans le client.

## Suite Technique Autorisee

Uniquement apres validation dashboard :

1. Initialiser Unity Services.
2. Faire un anonymous sign-in.
3. Creer un ticket Matchmaker sur `quickmatch1v1unranked`.
4. Poller le ticket.
5. Logger l'assignation retournee par UGS.
6. Ne brancher le lobby/session flow qu'apres ce test minimal.

Pour cette etape, ne pas ajouter d'adapters UGS complets, ne pas brancher EdgeGap, ne pas ajouter de Cloud Code.

Composant de test actuel :

- Script : `UgsMatchmakerSmokeTest`.
- Queue par defaut : `quickmatch1v1unranked`.
- Option `_ClearSessionBeforeSignIn` : force Unity Authentication a oublier l'identite anonyme locale avant le sign-in.
- Polling par defaut : toutes les `5` secondes pour rester confortablement sous les rate limits UGS.
- Timeout local par defaut : `90` secondes, pour laisser le pool dashboard expirer a `60` secondes et recuperer le statut final.
- Usage : ajouter le composant sur un GameObject de test, puis lancer `Run UGS Matchmaker Smoke Test` depuis le menu contextuel du composant ou activer `_RunOnStart`.

## Sources

- [Unity Matchmaker Get Started](https://docs.unity.com/en-us/matchmaker/get-started)
- [Unity Matchmaker Queues and Pools](https://docs.unity.com/en-us/matchmaker/advanced-topics-queues-pools)
- [Unity Matchmaker Rules Sample 1v1](https://docs.unity.com/en-us/matchmaker/rules-sample)
- [Unity Matchmaker Hosting Providers via Cloud Code](https://docs.unity.com/en-us/matchmaker/multiplay-hosting-migration)
