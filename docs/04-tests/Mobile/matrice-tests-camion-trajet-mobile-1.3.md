# Matrice de tests mobile — Choix camion / trajet mobile 1.3

## Objectif

Valider le comportement attendu du flux camion / trajet côté application mobile **MobileSLI** en contrat strict :

```text
schemaVersion = "1.3" uniquement
```

Cette matrice reflète l'état du code inspecté après le tag :

```text
v1.3-mobile-https-final
```

## Périmètre

Cette matrice concerne uniquement le dépôt mobile **MobileSLI**.

Elle ne couvre pas :

- l'API serveur ASP.NET Core ;
- les routes serveur ;
- les DTO serveur ;
- les scripts SQL serveur ;
- la base SQL Server ;
- ServeWeb.

## Faits vérifiés par inspection du code

| Élément | Fichier inspecté | État |
|---|---|---|
| Version mobile officielle | `Configuration/AppConfig.cs` | `SchemaVersion = "1.3"` |
| Chargement camions | `Services/Api/CamionsApiService.cs` | `GET /api/camions/disponibles` |
| Version camion attendue | `Services/Api/CamionsApiService.cs` | `ExpectedSchemaVersion = "1.3"` |
| Refus version camion différente | `Services/Api/CamionsApiService.cs` | exception si version différente de `1.3` |
| Filtrage camions actifs | `Services/Api/CamionsApiService.cs` | filtre `EstActif`, `IdCamion`, `CodeCamion` |
| Choix camion UI/ViewModel | `ViewModels/ChoixCamionViewModel.cs` | présent |
| Kilométrage départ obligatoire | `ViewModels/ChoixCamionViewModel.cs` | présent |
| Persistance trajet départ | `Services/DatabaseService.cs` | `PersistTrajetDepartAsync` |
| Persistance trajet arrivée | `Services/DatabaseService.cs` | `PersistTrajetArriveeAsync` |
| Restauration trajet local | `Services/DatabaseService.cs` | `RestaurerTrajetDansAppStateAsync` |
| Validation trajet avant envoi | `Services/SynchronisationService.cs` | présente |
| Payload final avec trajet | `Models/SynchronisationTrajetRequest.cs` et `Services/SynchronisationService.cs` | présent |

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

| ID | Catégorie | Précondition | Action | Résultat attendu | État code inspecté | Test manuel |
|---|---|---|---|---|---|---|
| MOB-CAM-001 | Liste camions | API disponible | Charger `GET /api/camions/disponibles` | La réponse JSON est parseable côté mobile | Implémenté | À rejouer |
| MOB-CAM-002 | Liste camions | Réponse `schemaVersion: "1.3"` | Charger la liste camions | Le mobile accepte la réponse | Implémenté | À rejouer |
| MOB-CAM-003 | Liste camions | Réponse `schemaVersion: "1.2"` | Charger la liste camions | Le mobile refuse la réponse avec un message clair | Implémenté | À rejouer |
| MOB-CAM-004 | Liste camions | Réponse avec version différente de `1.3` | Charger la liste camions | Le mobile refuse la réponse avec un message clair | Implémenté | À rejouer |
| MOB-CAM-005 | Filtrage camions | Réponse avec `estActif=true` et `estActif=false` | Afficher la liste | Seuls les camions actifs exploitables sont affichés | Implémenté | À rejouer |
| MOB-CAM-006 | Liste vide | Réponse avec `camions: []` | Charger la page choix camion | La progression est bloquée avec un message clair | Implémenté | À rejouer |
| MOB-CAM-007 | Réseau | API indisponible ou coupure réseau | Charger la page choix camion | Pas de crash, message clair, possibilité de réessayer | Implémenté partiellement | À rejouer |
| MOB-CAM-008 | Choix camion | Aucun camion sélectionné | Continuer | La progression est bloquée | Implémenté | À rejouer |
| MOB-CAM-009 | Kilométrage départ | Camion sélectionné, champ départ vide | Continuer | La progression est bloquée | Implémenté | À rejouer |
| MOB-CAM-010 | Kilométrage départ | Camion sélectionné, départ négatif | Continuer | La valeur est refusée | Implémenté | À rejouer |
| MOB-CAM-011 | Kilométrage départ | Camion sélectionné, départ non numérique | Continuer | La valeur est refusée | Implémenté | À rejouer |
| MOB-CAM-012 | Kilométrage départ | Camion sélectionné, départ entier `>= 0` | Continuer | Le flux peut continuer | Implémenté | À rejouer |
| MOB-CAM-013 | Date départ | Camion sélectionné, départ valide | Valider le choix camion | `dateDepartMobile` est renseignée automatiquement | Implémenté | À rejouer |
| MOB-CAM-014 | Changement livreur | Un livreur A a choisi un camion | Changer de livreur | Le camion sélectionné et le trajet temporaire sont réinitialisés | À vérifier dans le flux réel | À rejouer |
| MOB-CAM-015 | Reprise flux | Camion validé | Revenir dans le flux courant | Le camion sélectionné reste disponible pendant le flux de tournée | Implémenté via `AppStateService` et persistance locale | À rejouer |
| MOB-CAM-016 | Confirmation | Camion validé | Afficher la page avant “Charger tournée” | Le camion sélectionné est affiché | Implémenté | À rejouer |
| MOB-CAM-017 | Confirmation | Kilométrage départ validé | Afficher la page avant “Charger tournée” | Le kilométrage départ est affiché | Implémenté | À rejouer |
| MOB-CAM-018 | Ergonomie | Téléphone à écran étroit | Afficher identification + confirmation | L'affichage reste lisible et non surchargé | Non vérifié par inspection seule | À rejouer sur téléphone |
| MOB-CAM-019 | Kilométrage arrivée | Champ arrivée vide | Envoyer la synchronisation finale | L'envoi est bloqué | Implémenté | À rejouer |
| MOB-CAM-020 | Kilométrage arrivée | Arrivée négative ou inférieure au départ | Envoyer la synchronisation finale | La valeur est refusée | Implémenté | À rejouer |
| MOB-CAM-021 | Payload final | Camion + départ + arrivée valides | Envoyer la synchronisation finale | `dateArriveeMobile` est générée automatiquement et le payload 1.3 contient `trajet` complet | Implémenté | À rejouer |

## Tests techniques connus

D'après les validations locales communiquées avant cette mise à jour documentaire :

| Test | État |
|---|---|
| `dotnet clean` | OK localement |
| `dotnet restore` | OK localement |
| `dotnet build -f net10.0-android -c Release` | OK localement avec avertissement XA0141 |
| `dotnet publish -f net10.0-android -c Release` | OK localement |
| APK Release installé | OK localement |
| `Verify-MobileSLI-AndroidHttps.ps1 -FinalHttpsOnly` | OK localement |
| `Verify-MobileSLI-AndroidHttps.ps1 -RunAdbChecks` | OK localement |

## Tests non exécutés dans cette correction documentaire

Cette correction documentaire a été faite par inspection GitHub.

```text
dotnet clean : non exécuté
dotnet restore : non exécuté
dotnet build -f net10.0-android -c Release : non exécuté
dotnet publish -f net10.0-android -c Release : non exécuté
installation APK : non exécuté
tests fonctionnels téléphone : non exécuté
```

## Risques restants

- Les tests fonctionnels camion / trajet doivent être rejoués sur téléphone réel après installation de l'APK Release.
- L'avertissement XA0141 Android 16 reste à traiter.
- L'écran d'accueil conserve des éléments de maintenance visibles : adresse API, test connexion, diagnostic et export SQLite.
