# Rapport LOT 0 — Choix camion / trajet mobile 1.3

Date : 2026-06-04

## Résumé

Le LOT 0 a été préparé pour cadrer la future intégration mobile du choix camion et du trajet en `schemaVersion: "1.3"` strict.

La compatibilité `schemaVersion: "1.2"` a été supprimée du cadrage mobile.

Règle retenue :

```text
schemaVersion = "1.3" uniquement
schemaVersion = "1.2" refusé côté mobile
```

## Faits vérifiés

- Le dépôt ciblé est le dépôt mobile `MobileSLI`.
- Le fichier `README.md` existe et contient déjà une documentation fonctionnelle et technique mobile.
- Le projet est un projet .NET MAUI Android.
- Le dépôt contient des fichiers mobiles correspondant à l’architecture visée :
  - `Services/Api/`
  - `Services/AppStateService.cs`
  - `Services/DatabaseService.cs`
  - `Services/SynchronisationService.cs`
  - `Services/Navigation/`
  - `ViewModels/`
  - `Pages/`

## Fichiers inspectés

- `README.md`
- `MobileSLI.csproj`
- Index GitHub du dépôt pour repérer les fichiers d’architecture mobile existants
- Recherche du fichier attendu `docs/04-tests/Mobile/matrice-tests-mobile.md`
- Recherche du fichier attendu `docs/04-tests/Mobile/scripts/run-mobile-tests.ps1`

## Fichiers attendus mais absents

Les fichiers suivants n’ont pas été trouvés dans le dépôt au moment de l’inspection :

- `docs/04-tests/Mobile/matrice-tests-mobile.md`
- `docs/04-tests/Mobile/scripts/run-mobile-tests.ps1`

Conséquence :

- aucune modification de script de test n’a été faite ;
- une matrice documentaire dédiée est fournie dans le ZIP ;
- aucun fichier C#, XAML, SQL ou API serveur n’a été modifié.

## Fichiers modifiés ou ajoutés dans la correction ZIP

- `README.md`
- `docs/00-prompts/lot-0-tests-documentation-mobile-1.3.md`
- `docs/04-tests/Mobile/matrice-tests-camion-trajet-mobile-1.3.md`
- `docs/04-tests/Mobile/rapport-lot-0-camion-trajet-1.3.md`

## Scénarios ajoutés

- `MOB-CAM-001` — GET camions parseable
- `MOB-CAM-002` — `schemaVersion: "1.3"` accepté
- `MOB-CAM-003` — `schemaVersion: "1.2"` refusé
- `MOB-CAM-004` — autre version refusée
- `MOB-CAM-005` — seuls les camions actifs sont affichés
- `MOB-CAM-006` — liste vide bloquante
- `MOB-CAM-007` — coupure réseau sans crash
- `MOB-CAM-008` — camion obligatoire
- `MOB-CAM-009` — kilométrage départ obligatoire
- `MOB-CAM-010` — kilométrage départ négatif refusé
- `MOB-CAM-011` — kilométrage départ non numérique refusé
- `MOB-CAM-012` — kilométrage départ valide accepté
- `MOB-CAM-013` — `dateDepartMobile` automatique
- `MOB-CAM-014` — changement livreur réinitialise le trajet temporaire
- `MOB-CAM-015` — camion conservé pendant le flux courant
- `MOB-CAM-016` — camion affiché avant “Charger tournée”
- `MOB-CAM-017` — kilométrage départ affiché avant “Charger tournée”
- `MOB-CAM-018` — écran étroit lisible
- `MOB-CAM-019` — kilométrage arrivée obligatoire
- `MOB-CAM-020` — kilométrage arrivée négatif ou inférieur au départ refusé
- `MOB-CAM-021` — payload final 1.3 avec `trajet` complet

## Non modifié

- Aucun fichier C#.
- Aucun fichier XAML.
- Aucun DTO mobile.
- Aucun client HTTP mobile.
- Aucun `AppConfig`.
- Aucun `DatabaseService`.
- Aucun `SynchronisationService`.
- Aucun fichier API serveur.
- Aucun controller serveur.
- Aucun service serveur.
- Aucun repository serveur.
- Aucun script SQL serveur.
- Aucune migration SQL serveur.
- Aucune route serveur.
- Aucun contrat serveur.
- Aucune base SQL Server.

## Tests

tests non exécutés

## Risques identifiés

- Si l’API serveur retourne encore `schemaVersion: "1.2"` pour `GET /api/camions/disponibles`, le futur mobile strict 1.3 devra refuser la réponse.
- Le LOT 0 ne valide pas techniquement l’API serveur.
- Le LOT 0 ne valide pas techniquement l’APK.
- Le LOT 0 ne modifie pas la synchronisation mobile.
- Le LOT 0 prépare seulement la documentation et la matrice de tests.

## Actions utiles suivantes

1. Appliquer les fichiers du ZIP dans le dépôt mobile.
2. Vérifier avec `git diff` qu’aucun fichier C#, XAML, SQL ou API serveur n’est modifié.
3. Commiter ce LOT 0 seul.
4. Passer ensuite au LOT 1 : DTO camion + client HTTP mobile `CamionsApiService`.
