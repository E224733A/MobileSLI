# Matrice de tests mobile — Choix camion / trajet mobile 1.3

## Objectif

Valider le comportement attendu du futur flux camion / trajet côté application mobile **MobileSLI**.

Cette matrice prépare les lots de développement suivants sans modifier le code applicatif.

## Périmètre

Cette matrice concerne uniquement le dépôt mobile **MobileSLI**.

Elle ne modifie pas :

- l’API serveur ASP.NET Core ;
- les routes serveur ;
- les DTO serveur ;
- les scripts SQL serveur ;
- la base SQL Server ;
- le contrat serveur ;
- le code C# mobile ;
- le XAML mobile.

## Règle contractuelle stricte

```text
schemaVersion = "1.3" uniquement
schemaVersion = "1.2" refusé côté mobile
```

Le mobile ne doit plus accepter `schemaVersion: "1.2"` pour la liste des camions.

## Données de référence

### Réponse camion valide

```json
{
  "schemaVersion": "1.3",
  "camions": [
    {
      "idCamion": "DY-662-QN",
      "codeCamion": "DY-662-QN",
      "libelleCamion": "VL RENAULT",
      "immatriculation": "DY-662-QN",
      "estActif": true
    }
  ]
}
```

### Réponse camion invalide — ancienne version

```json
{
  "schemaVersion": "1.2",
  "camions": [
    {
      "idCamion": "DY-662-QN",
      "codeCamion": "DY-662-QN",
      "libelleCamion": "VL RENAULT",
      "immatriculation": "DY-662-QN",
      "estActif": true
    }
  ]
}
```

Résultat attendu côté mobile :

```text
Version du contrat camion incompatible. Version attendue : 1.3.
```

### Trajet cible dans le payload final

```json
{
  "schemaVersion": "1.3",
  "trajet": {
    "camion": {
      "idCamion": "DY-662-QN",
      "codeCamion": "DY-662-QN",
      "libelleCamion": "VL RENAULT",
      "immatriculation": "DY-662-QN"
    },
    "kilometrageDepart": 128100,
    "kilometrageArrivee": 128450,
    "dateDepartMobile": "2026-06-04T07:30:00+02:00",
    "dateArriveeMobile": "2026-06-04T15:45:00+02:00"
  }
}
```

## Matrice

| ID | Lot cible | Catégorie | Précondition | Action | Résultat attendu | Statut LOT 0 |
|---|---:|---|---|---|---|---|
| MOB-CAM-001 | 1 | Liste camions | API disponible | Charger `GET /api/camions/disponibles` | La réponse JSON est parseable côté mobile | Documenté |
| MOB-CAM-002 | 1 | Liste camions | Réponse `schemaVersion: "1.3"` | Charger la liste camions | Le mobile accepte la réponse | Documenté |
| MOB-CAM-003 | 1 | Liste camions | Réponse `schemaVersion: "1.2"` | Charger la liste camions | Le mobile refuse la réponse avec un message clair | Documenté |
| MOB-CAM-004 | 1 | Liste camions | Réponse avec version différente de `1.3` | Charger la liste camions | Le mobile refuse la réponse avec un message clair | Documenté |
| MOB-CAM-005 | 2 | Filtrage camions | Réponse avec `estActif=true` et `estActif=false` | Afficher la liste | Seuls les camions actifs sont affichés | Documenté |
| MOB-CAM-006 | 2 | Liste vide | Réponse avec `camions: []` | Charger la page choix camion | La progression est bloquée avec un message clair | Documenté |
| MOB-CAM-007 | 2 | Réseau | API indisponible ou coupure réseau | Charger la page choix camion | Pas de crash, message clair, possibilité de réessayer | Documenté |
| MOB-CAM-008 | 2 | Choix camion | Aucun camion sélectionné | Continuer | La progression est bloquée | Documenté |
| MOB-CAM-009 | 3 | Kilométrage départ | Camion sélectionné, champ départ vide | Continuer | La progression est bloquée | Documenté |
| MOB-CAM-010 | 3 | Kilométrage départ | Camion sélectionné, départ négatif | Continuer | La valeur est refusée | Documenté |
| MOB-CAM-011 | 3 | Kilométrage départ | Camion sélectionné, départ non numérique | Continuer | La valeur est refusée | Documenté |
| MOB-CAM-012 | 3 | Kilométrage départ | Camion sélectionné, départ entier `>= 0` | Continuer | Le flux peut continuer | Documenté |
| MOB-CAM-013 | 3 | Date départ | Camion sélectionné, départ valide | Valider le choix camion | `dateDepartMobile` est renseignée automatiquement | Documenté |
| MOB-CAM-014 | 2 | État courant | Un livreur A a choisi un camion | Changer de livreur | Le camion sélectionné et le trajet temporaire sont réinitialisés | Documenté |
| MOB-CAM-015 | 2/7 | État courant | Camion validé | Revenir dans le flux courant | Le camion sélectionné reste disponible pendant le flux de tournée | Documenté |
| MOB-CAM-016 | 4 | Confirmation | Camion validé | Afficher la page avant “Charger tournée” | Le camion sélectionné est affiché | Documenté |
| MOB-CAM-017 | 4 | Confirmation | Kilométrage départ validé | Afficher la page avant “Charger tournée” | Le kilométrage départ est affiché | Documenté |
| MOB-CAM-018 | 4 | Ergonomie | Téléphone à écran étroit | Afficher identification + confirmation | L’affichage reste lisible et non surchargé | Documenté |
| MOB-CAM-019 | 5 | Kilométrage arrivée | Champ arrivée vide | Envoyer la synchronisation finale | L’envoi est bloqué | Documenté |
| MOB-CAM-020 | 5 | Kilométrage arrivée | Arrivée négative ou inférieure au départ | Envoyer la synchronisation finale | La valeur est refusée | Documenté |
| MOB-CAM-021 | 6 | Payload final | Camion + départ + arrivée valides | Envoyer la synchronisation finale | `dateArriveeMobile` est générée automatiquement et le payload 1.3 contient `trajet` complet | Documenté |

## Points à vérifier pendant les lots suivants

### LOT 1 — DTO camion + client HTTP mobile

- Les DTO mobiles acceptent uniquement `schemaVersion: "1.3"`.
- Le client HTTP mobile appelle uniquement `GET /api/camions/disponibles`.
- Aucun code serveur n’est modifié.

### LOT 2 — Page choix camion + ViewModel + navigation

- Une page dédiée est ajoutée après identification livreur.
- La navigation passe par `INavigationService` si présent.
- Aucun appel HTTP n’est placé dans le code-behind.

### LOT 3 — Validation kilométrage départ

- Validation dans le ViewModel.
- Entier obligatoire.
- Valeur `>= 0`.
- Date départ générée automatiquement.

### LOT 4 — Confirmation avant chargement tournée

- Camion affiché.
- Kilométrage départ affiché.
- Écran petit respecté.

### LOT 5 — Kilométrage arrivée avant synchronisation

- Arrivée obligatoire.
- Arrivée `>= 0`.
- Arrivée `>= départ`.
- Blocage de l’envoi en cas d’erreur.

### LOT 6 — Payload mobile final schemaVersion 1.3

- Payload final en `schemaVersion: "1.3"`.
- Section `trajet` complète.
- Pas de compatibilité 1.2.
- Modification à faire uniquement après validation API serveur 1.3.

### LOT 7 — Persistance robuste SQLite mobile

- À faire uniquement si la reprise application exige la conservation camion/trajet.
- Ne pas mélanger état temporaire et historique métier.

### LOT 8 — Audit final avant tag

- Build Release.
- Tests manuels téléphone.
- Vérification qu’aucun fichier serveur n’a été modifié.
- Vérification que le contrat 1.3 est strict.

## Statut des tests

tests non exécutés
