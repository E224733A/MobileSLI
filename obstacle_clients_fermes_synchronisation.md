# Obstacle rencontré — clients fermés bloquant la synchronisation mobile

## Contexte

Pendant les tests fonctionnels du mobile, une tournée contenait un client fermé.
Dans l'interface, ce client n'apparaissait pas dans le filtre **À faire**, car les clients fermés sont volontairement exclus de cette vue pour ne pas demander au livreur de les traiter manuellement.

Lors de l'envoi final, la synchronisation était pourtant refusée avec le message :

```text
Le point 679 - RESTAURANT LE BOUT DU MONDE est encore à faire.
```

## Cause identifiée

Le client fermé était masqué du filtre **À faire**, mais il restait stocké en SQLite avec l'état technique suivant :

```text
EstFerme = true
StatutPassage = A_FAIRE
EstValidee = false
HeureValidation = null
CommentaireLivreur = null
```

La synchronisation mobile refuse volontairement toute ligne encore en `A_FAIRE`, car le JSON final ne doit contenir que des lignes traitées.
Cette règle est correcte pour les clients normaux, mais elle ne couvrait pas correctement le cas métier des clients fermés.

## Spécification métier corrigée

Un client fermé ne doit pas être traité manuellement par le livreur.
Il doit être transformé automatiquement en ligne traitée avant l'envoi final.

Règle retenue :

```text
Si EstFerme = true :
- StatutPassage = NON_FAIT
- EstValidee = true
- HeureValidation = date/heure automatique
- CommentaireLivreur = "Client fermé"
- QuantiteLivree = 0
- QuantiteRecuperee = 0
```

Le client fermé reste présent dans la tournée et dans le JSON, mais il ne bloque plus la synchronisation.

## Solution apportée

### 1. Normalisation centralisée dans DatabaseService

Ajout d'une méthode :

```csharp
NormalizeClosedLinesAsync(int tourneeId)
```

Cette méthode corrige toutes les lignes fermées d'une tournée locale non verrouillée.
Elle sert aussi à réparer les tournées déjà présentes en SQLite avant la correction.

### 2. Correction lors du chargement initial

Lorsqu'une tournée est chargée depuis l'API, une ligne dont `EstFerme = true` est maintenant directement créée comme :

```text
NON_FAIT + commentaire "Client fermé" + validée
```

Les quantités du client fermé sont forcées à zéro.

### 3. Sécurités ajoutées avant les écrans importants

La normalisation est appelée :

```text
- après le chargement local d'une tournée ;
- à l'ouverture de la liste des points ;
- à l'ouverture du récapitulatif ;
- juste avant la synchronisation finale ;
- lors de la construction du JSON de synchronisation.
```

Cela évite qu'une ancienne base locale ou un écran non rechargé puisse encore envoyer un client fermé en `A_FAIRE`.

### 4. Amélioration de l'expérience utilisateur

La page d'erreur distingue mieux les erreurs de validation locale.
Une tournée incomplète n'est plus présentée comme un problème Wi-Fi ou un doublon.

Une erreur de type `VALIDATION_ERROR` affiche maintenant une action cohérente : retourner au récapitulatif ou à la liste des clients pour corriger le point indiqué.

### 5. Ne pas modifier l'API

L'API ne doit pas être assouplie pour accepter `A_FAIRE`.
Elle reste le garde-fou final :

```text
- A_FAIRE interdit dans la synchronisation finale ;
- NON_FAIT autorisé avec commentaire ;
- ANOMALIE autorisé avec commentaire ;
- heure de validation obligatoire ;
- estValidee obligatoire ;
- quantités non négatives.
```

La correction est donc faite côté mobile, là où le cas métier doit être préparé correctement avant l'envoi.

## Fichiers corrigés

```text
Services/DatabaseService.cs
Services/SynchronisationService.cs
ViewModels/ListePointsLivraisonViewModel.cs
ViewModels/RecapitulatifTourneeViewModel.cs
ViewModels/SyncErrorViewModel.cs
Pages/SyncErrorPage.xaml
```

## Tests à réaliser après correction

### Test 1 — Client fermé après chargement

1. Charger une tournée contenant un client fermé.
2. Exporter la base SQLite.
3. Vérifier la ligne fermée.

Requête de contrôle :

```sql
SELECT
    NumClient,
    NomClient,
    EstFerme,
    StatutPassage,
    EstValidee,
    HeureValidation,
    CommentaireLivreur
FROM LocalTourneeLigne
WHERE EstFerme = 1;
```

Attendu :

```text
EstFerme = 1
StatutPassage = NON_FAIT
EstValidee = 1
HeureValidation non null
CommentaireLivreur = Client fermé
```

### Test 2 — Quantités client fermé

```sql
SELECT
    q.CodeArticle,
    q.QuantiteLivree,
    q.QuantiteRecuperee
FROM LocalTourneeLigneQuantite q
JOIN LocalTourneeLigne l ON l.Id = q.LigneId
WHERE l.EstFerme = 1;
```

Attendu :

```text
QuantiteLivree = 0
QuantiteRecuperee = 0
```

### Test 3 — Récapitulatif

1. Ouvrir le récapitulatif.
2. Vérifier que le client fermé est compté comme traité.
3. Vérifier que le total validé correspond au nombre total de clients si tous les autres points sont traités.

### Test 4 — Synchronisation

1. Traiter tous les clients non fermés.
2. Envoyer la tournée.

Attendu :

```text
Synchronisation réussie.
Le client fermé est envoyé en NON_FAIT avec commentaire "Client fermé".
```

### Test 5 — Client normal encore à faire

1. Laisser un client non fermé en `A_FAIRE`.
2. Tenter l'envoi.

Attendu :

```text
Envoi bloqué.
Message indiquant le point encore à faire.
Le blocage reste volontaire pour les clients non fermés.
```
