# LOT 0 — Tests et documentation mobile avant codage

## Objectif

Préparer l’ajout du choix camion et du trajet mobile avant de coder.

Ce lot sert uniquement à cadrer le comportement attendu côté mobile, la documentation et les scénarios de tests associés.

## Règle absolue

Ce lot concerne uniquement le dépôt mobile **MobileSLI**.

Aucune modification du serveur API ASP.NET Core n’est autorisée.

## Architecture cible

```text
Mobile .NET MAUI = MVVM + services
```

## Interdictions absolues

- ne pas modifier le dépôt API ASP.NET Core ;
- ne pas modifier de controller API serveur ;
- ne pas modifier de service métier serveur ;
- ne pas modifier de repository serveur ;
- ne pas modifier de script SQL serveur ;
- ne pas modifier de migration SQL serveur ;
- ne pas créer de route API serveur ;
- ne pas modifier les routes API serveur existantes ;
- ne pas modifier les DTO serveur ;
- ne pas modifier le contrat serveur ;
- ne pas modifier la base SQL serveur ;
- ne pas modifier de code C# mobile dans ce lot ;
- ne pas modifier de XAML dans ce lot ;
- ne pas modifier `AppConfig` dans ce lot ;
- ne pas modifier `DatabaseService` dans ce lot ;
- ne pas modifier `SynchronisationService` dans ce lot ;
- ne pas modifier les DTO mobiles dans ce lot.

## Clarification importante

Dans le dépôt mobile, le dossier `Services/Api` contient des clients HTTP mobiles.

Ces fichiers appellent des routes API déjà existantes.

```text
Services/Api mobile ≠ API serveur ASP.NET Core
DTO mobile ≠ DTO serveur
Payload construit par le mobile ≠ modification du serveur
SQLite mobile ≠ SQL Server central
```

## Règles d’architecture mobile à respecter

- `.xaml` = affichage uniquement.
- `.xaml.cs` = `InitializeComponent`, `BindingContext` et `OnAppearing` uniquement si nécessaire.
- `ViewModel` = état écran, commandes utilisateur et validations UI.
- `Services/Api` = clients HTTP mobiles uniquement.
- `DatabaseService` = SQLite mobile uniquement.
- `AppStateService` = état temporaire du flux courant.
- `SynchronisationService` = construction et envoi du payload mobile final.
- `DtoModels` = contrats JSON côté mobile.
- `Navigation` = `INavigationService` si ce service existe déjà.
- Aucun appel HTTP dans une page XAML ou dans un code-behind.
- Aucune logique métier dans le XAML.
- Aucun `Shell.Current.GoToAsync` direct dans un ViewModel si `INavigationService` existe.
- Ne pas mélanger choix camion, choix tournée et synchronisation finale dans un seul ViewModel.

## Contexte fonctionnel

Après l’identification du livreur, l’application mobile devra afficher une page de choix camion.

Cette page consommera une route API déjà existante :

```http
GET /api/camions/disponibles
```

Ce lot ne doit pas créer ni modifier cette route.

La route est supposée déjà disponible côté API serveur.

## Contrat camion consommé par le mobile

Le mobile doit accepter uniquement une réponse camion en `schemaVersion: "1.3"`.

Réponse attendue :

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

Règle stricte :

```text
schemaVersion = "1.3" obligatoire
schemaVersion = "1.2" interdit
```

Si l’API retourne `schemaVersion: "1.2"` ou une autre version, le mobile doit bloquer la progression avec un message clair.

Message recommandé :

```text
Version du contrat camion incompatible. Version attendue : 1.3.
```

Important :

- `schemaVersion: "1.3"` est obligatoire.
- `schemaVersion: "1.2"` n’est plus accepté.
- Le mobile ne doit plus gérer de compatibilité temporaire `1.2` sur la liste camions.
- Ce lot ne doit pas modifier le serveur API.
- Si le serveur retourne encore `1.2`, le test doit échouer côté mobile.
- Ce comportement permet de détecter immédiatement un décalage entre le mobile et l’API.

## Décisions fonctionnelles à documenter

1. Le choix camion est obligatoire.
2. Le kilométrage départ est obligatoire.
3. Le kilométrage départ doit être un entier supérieur ou égal à `0`.
4. `dateDepartMobile` est renseignée automatiquement au moment de la validation du camion.
5. Le camion sélectionné et le kilométrage départ doivent être conservés pendant le flux de tournée.
6. Le camion sélectionné et le kilométrage départ doivent être affichés sur la page de confirmation où se trouve le bouton “Charger tournée”.
7. L’écran livreur reste petit : ne pas surcharger l’écran d’identification.
8. Le kilométrage arrivée est obligatoire avant l’envoi final.
9. Le kilométrage arrivée doit être un entier supérieur ou égal à `0`.
10. Le kilométrage arrivée doit être supérieur ou égal au kilométrage départ.
11. `dateArriveeMobile` est renseignée automatiquement au moment de l’envoi final.
12. Changer de livreur doit réinitialiser le camion sélectionné et les données trajet temporaires.
13. Le mobile doit accepter uniquement `schemaVersion: "1.3"` pour la liste camions.
14. Le mobile doit refuser `schemaVersion: "1.2"` ou toute autre version.
15. Le payload mobile final devra être en `schemaVersion: "1.3"` avec une section `trajet` obligatoire.
16. Le payload mobile final ne doit être ajouté au code que lorsque l’API serveur 1.3 est validée par les tests.
17. La persistance SQLite mobile ne doit être ajoutée que si la reprise d’application exige de conserver le camion et le trajet.

## Section trajet cible du payload mobile final

À documenter uniquement dans ce lot.

Ne pas modifier le code de synchronisation dans ce lot.

Structure cible future :

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

Règles associées :

- `trajet` obligatoire dans le payload final 1.3 ;
- `trajet.camion` obligatoire ;
- `trajet.camion.idCamion` obligatoire ;
- `trajet.camion.codeCamion` obligatoire ;
- `trajet.camion.libelleCamion` obligatoire ;
- `trajet.camion.immatriculation` obligatoire si fourni par la liste camions ;
- `kilometrageDepart` obligatoire ;
- `kilometrageArrivee` obligatoire avant synchronisation finale ;
- `kilometrageArrivee >= kilometrageDepart` ;
- `dateDepartMobile` générée automatiquement côté mobile ;
- `dateArriveeMobile` générée automatiquement côté mobile ;
- aucun champ trajet ne doit être saisi manuellement hors camion et kilométrages.

## Périmètre autorisé

Tu peux modifier uniquement :

- la documentation mobile ;
- la matrice de tests mobile ;
- les fichiers de scénarios de tests mobile si le dépôt en contient déjà ;
- les scripts de tests mobile si le dépôt en contient déjà.

## Périmètre interdit

Tu ne dois modifier aucun fichier de code applicatif :

- aucun fichier C# ;
- aucun fichier XAML ;
- aucun DTO ;
- aucun ViewModel ;
- aucun service applicatif ;
- aucun client HTTP mobile ;
- aucun `AppConfig` ;
- aucun `DatabaseService` ;
- aucun `SynchronisationService` ;
- aucun fichier API serveur ;
- aucun fichier SQL serveur.

## Travail demandé

1. Inspecter les fichiers de documentation et de tests existants du dépôt mobile.
2. Identifier les fichiers réellement présents.
3. Ajouter la documentation du flux camion/trajet mobile 1.3.
4. Ajouter ou compléter la matrice de tests mobile avec les scénarios `MOB-CAM-001` à `MOB-CAM-021`.
5. Ne pas inventer de fichier si une structure existante suffit.
6. Si un fichier attendu n’existe pas, le signaler clairement.
7. Ne modifier aucun code applicatif.
8. Ne modifier aucun contrat serveur.
9. Ne modifier aucune synchronisation dans ce lot.
10. Ne conserver aucune compatibilité mobile avec `schemaVersion: "1.2"` pour la liste camions.

## Scénarios de tests à ajouter

| ID | Catégorie | Scénario | Résultat attendu |
|---|---|---|---|
| MOB-CAM-001 | Liste camions | `GET /api/camions/disponibles` retourne une réponse JSON parseable | La réponse est lue sans erreur côté mobile |
| MOB-CAM-002 | Liste camions | La liste camions retourne `schemaVersion: "1.3"` | Le mobile accepte la réponse |
| MOB-CAM-003 | Liste camions | La liste camions retourne `schemaVersion: "1.2"` | Le mobile refuse la réponse avec un message clair |
| MOB-CAM-004 | Liste camions | La liste camions retourne une version différente de `1.3` | Le mobile refuse la réponse avec un message clair |
| MOB-CAM-005 | Liste camions | La réponse contient des camions actifs et inactifs | Seuls les camions `estActif=true` sont affichés |
| MOB-CAM-006 | Liste camions | La liste camions est vide | La progression est bloquée avec un message clair |
| MOB-CAM-007 | Réseau | Coupure réseau pendant le chargement camion | Pas de crash, message clair, possibilité de réessayer |
| MOB-CAM-008 | Choix camion | Aucun camion sélectionné | La progression est bloquée |
| MOB-CAM-009 | Kilométrage départ | Kilométrage départ vide | La progression est bloquée |
| MOB-CAM-010 | Kilométrage départ | Kilométrage départ négatif | La valeur est refusée |
| MOB-CAM-011 | Kilométrage départ | Kilométrage départ non numérique | La valeur est refusée |
| MOB-CAM-012 | Kilométrage départ | Kilométrage départ valide | Le flux peut continuer |
| MOB-CAM-013 | Départ mobile | Validation du camion et du kilométrage départ | `dateDepartMobile` est renseignée automatiquement |
| MOB-CAM-014 | État courant | Changement de livreur | Le camion sélectionné et le trajet temporaire sont réinitialisés |
| MOB-CAM-015 | État courant | Reprise du flux courant | Le camion sélectionné reste disponible pendant le flux de tournée |
| MOB-CAM-016 | Confirmation | Page de confirmation avant “Charger tournée” | Le camion sélectionné est affiché |
| MOB-CAM-017 | Confirmation | Page de confirmation avant “Charger tournée” | Le kilométrage départ est affiché |
| MOB-CAM-018 | Ergonomie | Écran étroit livreur | L’affichage reste lisible et non surchargé |
| MOB-CAM-019 | Kilométrage arrivée | Kilométrage arrivée vide avant envoi final | L’envoi est bloqué |
| MOB-CAM-020 | Kilométrage arrivée | Kilométrage arrivée négatif ou inférieur au départ | La valeur est refusée |
| MOB-CAM-021 | Payload final | Envoi final avec trajet valide | `dateArriveeMobile` est renseignée automatiquement et le payload final 1.3 contient une section `trajet` complète |

## Contraintes qualité

- Inspecter les fichiers existants avant modification.
- Ne pas inventer de fichier si un fichier existant peut être complété proprement.
- Ne pas modifier le code.
- Ne pas modifier le contrat API serveur.
- Ne pas modifier la synchronisation.
- Ne pas modifier les routes.
- Ne pas modifier SQL Server.
- Ne pas modifier SQLite mobile dans ce lot.
- Si un fichier attendu n’existe pas, le signaler.
- À la fin, lister précisément les fichiers modifiés.
- Si aucun test n’est exécuté, écrire : `tests non exécutés`.

## Résultat attendu en fin de lot

À la fin de ce lot, fournir :

1. la liste des fichiers inspectés ;
2. la liste des fichiers modifiés ;
3. la liste des scénarios ajoutés ;
4. les fichiers attendus mais absents, s’il y en a ;
5. les tests exécutés ou la mention exacte : `tests non exécutés` ;
6. la confirmation qu’aucun fichier C#, XAML, SQL ou API serveur n’a été modifié.
