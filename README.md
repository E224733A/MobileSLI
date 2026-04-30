# SLI Tournées Mobile

Application mobile **.NET MAUI Android** pour la dématérialisation d'une fiche de tournée livreur.

## Objectif

Cette version fournit un socle fonctionnel pour le dépôt `E224733A/MobileSLI` :

- chargement d'une tournée via `GET /api/tournees/jour` ;
- respect du contrat JSON choisi pour le chargement de tournée ;
- stockage local SQLite pour utilisation hors connexion pendant la tournée ;
- consultation des arrêts dans l'ordre réel `ordreArret` / `ARRET` ;
- saisie rapide des quantités avec boutons `+` / `-` ;
- validation d'un arrêt avec statut `FAIT`, `NON_FAIT` ou `ANOMALIE` ;
- commentaire obligatoire pour `NON_FAIT` et `ANOMALIE` ;
- génération du JSON compatible avec `POST /api/synchronisations` ;
- dates d'envoi au format local avec offset, par exemple `2026-04-28T16:45:00+02:00` ;
- verrouillage local après synchronisation réussie.

## Prérequis

- Visual Studio 2022 ou Visual Studio 2026 avec workload **.NET MAUI**
- .NET 8 SDK
- Android SDK Platform 31 pour Android 12
- Android SDK Platform 34 conseillé pour compiler confortablement
- Android Emulator ou téléphone Android physique

## Configuration API

Par défaut, l'application pointe vers :

```text
http://10.0.2.2:5000
```

Cette adresse fonctionne surtout depuis l'émulateur Android vers l'API lancée en local.  
Sur téléphone physique, remplacer par l'adresse IP de la VM ou du poste qui héberge l'API sur le Wi-Fi de l'entreprise, par exemple :

```text
http://192.168.1.50:5000
```

Dans l'application : écran **Tournée du jour** > champ **Adresse API**.

## Démarrage rapide

```bash
dotnet restore
dotnet build -f net8.0-android
```

Ou ouvrir `TourneesMobile.sln` dans Visual Studio puis lancer sur Android.

## Test sans API

L'écran de démarrage contient un bouton **Charger une tournée démo**.  
Il injecte une tournée locale cohérente avec le cahier des charges pour tester les écrans sans backend.

## Contrats JSON

Les contrats sont documentés dans :

```text
docs/CONTRATS_JSON.md
docs/sample-tournee-get.json
docs/post-sync-valide.json
Resources/Raw/sample-tournee-get.json
Resources/Raw/post-sync-valide.json
```

Les DTO importants sont :

```text
Models/ApiDtos.cs
Models/SynchronisationDtos.cs
```

La génération du JSON d'envoi est faite dans :

```text
Services/DatabaseService.cs
BuildSynchronisationRequestAsync()
```
