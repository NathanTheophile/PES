# FEATURE_TEMPLATE_GAMEPLAY

Template standard pour implémenter **toute nouvelle mécanique gameplay** (ex: skill, état, action contextuelle) avec une structure fixe et rejouable.

## Obligations (à valider avant merge)

- [ ] **REPLAY_CHECKLIST.md** appliqué (déterminisme, snapshots, replay cohérent).
- [ ] **PARITY_MATRIX.md** mis à jour (ou explicitement justifié si non applicable).

> Références obligatoires : [REPLAY_CHECKLIST.md](./REPLAY_CHECKLIST.md) et [PARITY_MATRIX.md](./PARITY_MATRIX.md).

---

## Squelette de développement (structure fixe)

## 1) Contrat de données (policy / command input)

**Objectif** : isoler les paramètres de gameplay dans une policy immutable + commande explicite.

### À créer

- `XxxActionPolicy` (readonly struct) avec valeurs par défaut, bornes, `IsValid`.
- `XxxAction` (readonly struct) implémentant `IActionCommand` avec:
  - `ActorId` (ou équivalent), `TargetId`/payload
  - option `policyOverride` pour tests

### Règles

- Aucun accès RNG ici hors pipeline standard.
- Les valeurs invalides doivent être détectables via `policy.IsValid`.
- Le contrat doit être minimal et sérialisable (replay/record).

### Mini-template

```csharp
public readonly struct XxxActionPolicy
{
    public bool IsValid => ...;
}

public readonly struct XxxAction : IActionCommand
{
    public ActionResolution Resolve(BattleState state, IRngService rngService)
    {
        var policy = _policyOverride ?? DefaultPolicy;
        if (!policy.IsValid)
        {
            return new ActionResolution(false, ActionResolutionCode.Rejected, "XxxRejected: invalid policy", ActionFailureReason.InvalidPolicy);
        }

        // suite: validation métier -> ciblage -> résolution -> mutation
    }
}
```

---

## 2) Validation métier (rejets normalisés)

**Objectif** : retourner des rejets déterministes, explicites et homogènes.

### Ordre recommandé

1. Acteur valide (existe, HP > 0)
2. Cible valide (existe, HP > 0, non self-target si interdit)
3. Préconditions de mécanique (ressource, cooldown, statut bloquant)
4. Préconditions spatiales (positions, range, LOS)

### Convention de rejet

- `ActionResolutionCode.Rejected`
- `ActionFailureReason` précis (pas de “Unknown” si un code existe)
- Message préfixé stable (`XxxRejected: ...`)
- `ActionResultPayload` si données utiles au debug/UI (ex: coût requis, ressource dispo)

---

## 3) Résolution (mutation de `BattleState`)

**Objectif** : séparer la logique probabiliste de la mutation d’état.

### Pattern recommandé

- Service dédié : `XxxResolutionService`
  - entrée: `IRngService` + valeurs de base
  - sortie: `XxxResolutionResult` (hit, roll, valeur finale...)
- Dans l’action:
  - appeler le service de résolution
  - appliquer les mutations atomiques sur `BattleState`
  - échouer explicitement si mutation impossible (`DamageApplicationFailed`, etc.)

### Règles

- RNG uniquement via `IRngService`.
- Clamp/bornes dans le service de résolution.
- Aucun effet secondaire caché hors `BattleState`.

---

## 4) Event log structuré

**Objectif** : rendre chaque action auditable et exploitable replay/diagnostic.

### Attendus

- `ActionResolution.Message` stable et verbeux (`XxxResolved`, `XxxMissed`, `XxxRejected`)
- `ActionResolution.Payload` rempli sur succès et échecs métier clés
- Données minimales recommandées:
  - id mécanique (`skillId`/`statusId`)
  - valeur d’effet (damage/heal/stacks)
  - contexte utile (roll, hitChance, ressource restante)

### Convention

- Types payload: `XxxResolved`, `XxxMissed`, `XxxResourceInsufficient`...
- Éviter les payloads ambigus sans clé métier.

---

## 5) Tests déterministes minimaux

**Objectif** : couvrir le flux nominal + rejets critiques + replay.

### Pack minimal requis

1. **Succès nominal** (payload structuré + mutation attendue)
2. **Rejet métier principal** (ex: OutOfRange / ResourceInsufficient)
3. **Cas spatial** (LOS ou min/max range)
4. **Replay déterministe** (même seed => même snapshot final)

### Règles de tests

- Seed fixe (`SeededRngService`).
- Assertions sur:
  - `ActionResolutionCode`
  - `ActionFailureReason`
  - payload (`Kind`, valeurs)
  - mutations `BattleState`
  - cohérence snapshot final en replay

---

## Exemple concret : “Skill simple mono-cible”

Exemple aligné sur l’existant:

- Action: `Combat/Actions/CastSkillAction.cs`
- Ciblage: `Combat/Targeting/SkillTargetingService.cs`
- Résolution: `Combat/Resolution/SkillResolutionService.cs`
- Tests: `Tests/EditMode/CastSkillActionTests.cs`

### Mapping du template vers l’exemple

1. **Contrat**
   - `SkillActionPolicy` porte range, hit chance, dégâts, coût, cooldown.
   - `CastSkillAction` encapsule `CasterId`, `TargetId`, `policyOverride`.

2. **Validation métier**
   - Rejets normalisés pour: caster/target KO, cooldown, ressource insuffisante.
   - Ciblage délégué à `SkillTargetingService` avec `SkillTargetingFailure` -> `ActionFailureReason`.

3. **Résolution + mutation**
   - `SkillResolutionService.Resolve(...)` produit `roll`, `hit`, `finalDamage`.
   - Sur hit: `TryApplyDamage`, `TryConsumeEntitySkillResource`, `SetSkillCooldown`.

4. **Event log structuré**
   - Messages: `CastSkillResolved`, `CastSkillMissed`, `CastSkillRejected`.
   - Payloads typés: `SkillResolved`, `SkillMissed`, `SkillResourceInsufficient`, `SkillCooldown`.

5. **Tests déterministes minimaux**
   - Succès avec payload + dégâts.
   - Rejets: out of range, LOS bloquée, ressource insuffisante, cooldown.
   - Cas portée via élévation.
   - Replay seedé reproduisant le snapshot final.

---

## Checklist PR “nouvelle mécanique”

- [ ] Contrat `Policy + Action` ajouté, immutable et validé.
- [ ] Rejets normalisés (`Rejected + FailureReason`) implémentés.
- [ ] Résolution isolée dans un service dédié (RNG centralisé).
- [ ] Mutations `BattleState` explicites et vérifiées.
- [ ] Event log structuré (message + payload) complet.
- [ ] Tests déterministes minimaux présents.
- [ ] Références `REPLAY_CHECKLIST.md` et `PARITY_MATRIX.md` traitées.
