# Contrats JSON de l'application mobile

L'application mobile doit être alignée sur les deux contrats JSON retenus pour le projet :

1. chargement de la tournée du jour ;
2. synchronisation de fin de tournée.

## 1. Chargement de tournée

Route utilisée :

```text
GET /api/tournees/jour?dateTournee=2026-04-28&codeTournee=2001&codeLivreur=2
```

Le mobile attend une réponse structurée avec :

```text
schemaVersion
dateTournee
jourTournee
jourLibelle
codeTournee
libelleTournee
statutSynchronisation
livreur
chargement
lignes
```

Chaque ligne doit contenir :

```text
idLigneSource
ordreArret
horaire
client
pointLivraison
tournee
retour
infosLivreur
saisie
```

Le champ `idLigneSource` est utilisé comme clé locale SQLite pour identifier une ligne même hors connexion.

Exemple recommandé :

```text
2026-04-28|2001|2|1058|1|1
```

Signification :

```text
Date tournée : 2026-04-28
Tournée      : 2001
Jour         : 2
Client       : 1058
PDL          : 1
Arrêt        : 1
```

Le fichier exemple est disponible ici :

```text
docs/sample-tournee-get.json
```

## 2. Envoi de fin de tournée

Route utilisée :

```text
POST /api/synchronisations
```

Le mobile génère un JSON avec :

```text
schemaVersion
idSynchronisation
dateTournee
codeTournee
libelleTournee
livreur
mobile
commentaireGlobal
lignes
```

Chaque ligne envoyée contient volontairement uniquement les données nécessaires à l'API :

```text
idLigneSource
ordreArret
client
pointLivraison
saisie
```

La partie `saisie` contient :

```text
nbExpes
nbRolls
nbVetements
nbTapis
nbSacs
nbRecuperes
precisionLivreur
statutPassage
commentaireLivreur
heureValidation
estValidee
```

Les dates générées par le mobile utilisent l'heure locale avec offset :

```text
2026-04-28T16:45:00+02:00
```

Cela évite les ambiguïtés entre l'heure du téléphone, l'heure SQL Server et l'heure d'exploitation côté entreprise.

Le fichier exemple est disponible ici :

```text
docs/post-sync-valide.json
```

## Points importants de conformité

- Le mobile lit les champs `cle`, `estFerme`, `dateFermeture` et `motifFermeture` dans `infosLivreur`.
- Le mobile lit `schemaLivraison` dans `tournee`.
- Le mobile conserve `idLigneSource` comme clé locale.
- Le POST n'envoie pas les champs d'adresse complets dans `pointLivraison`, seulement `codePDL` et `descriptionPDL`, conformément au contrat d'envoi.
- Le POST garde les champs null utiles, comme `commentaireGlobal` ou `commentaireLivreur`.
- Les quantités négatives sont interdites avant l'envoi.
- `NON_FAIT` et `ANOMALIE` nécessitent un commentaire.
