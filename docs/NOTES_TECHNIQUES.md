# Notes techniques — SLI Tournées Mobile

## Architecture mobile

L'application suit une structure MVVM :

- `Pages/` : écrans XAML MAUI ;
- `ViewModels/` : logique d'affichage et commandes ;
- `Models/` : DTO API et entités SQLite ;
- `Services/ApiService.cs` : appels HTTP vers l'API ASP.NET Core ;
- `Services/DatabaseService.cs` : stockage local SQLite et génération du JSON de synchronisation ;
- `Services/DemoDataService.cs` : données de test sans backend.

## Parcours métier implémenté

1. Le livreur saisit son code, son nom, le code tournée et la date.
2. L'application charge la tournée depuis `GET /api/tournees/jour`.
3. La tournée est copiée dans SQLite.
4. Le livreur travaille hors connexion dans la liste des arrêts.
5. Chaque arrêt peut être renseigné puis validé.
6. À la fin, l'application vérifie que tous les arrêts sont validés.
7. Elle génère le JSON compatible avec `POST /api/synchronisations`.
8. Après succès, la tournée est verrouillée localement.

## Règles métier côté mobile

- Quantités négatives interdites.
- Les retours sont séparés dans `nbRecuperes`.
- `NON_FAIT` et `ANOMALIE` imposent un commentaire.
- Une validation enregistre automatiquement `heureValidation`.
- Après synchronisation réussie, la tournée est verrouillée.
- L'API reste responsable de la protection anti-doublon définitive.

## Routes attendues

### GET tournée du jour

```http
GET /api/tournees/jour?dateTournee=2026-04-28&codeTournee=2001&codeLivreur=2
```

Format attendu : voir `docs/sample-tournee-get.json`.

### POST synchronisation

```http
POST /api/synchronisations
Content-Type: application/json
```

Format attendu : voir `docs/post-sync-valide.json`.

## Points à adapter après intégration avec l'API réelle

- Vérifier exactement le format de réponse de `SynchronisationResponse`.
- Ajuster l'adresse API dans l'écran d'accueil.
- Remplacer les valeurs de démonstration par la liste réelle des livreurs si une route `GET /api/livreurs` est ajoutée.
- Passer en HTTPS en environnement final.
