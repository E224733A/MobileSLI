# Documentation fonctionnelle et technique — Application mobile MobileSLI

## 1. Objectif de l’application mobile

L’application mobile **MobileSLI** a pour objectif de remplacer progressivement la fiche papier utilisée par les livreurs pour les tournées quotidiennes.

Elle doit conserver la logique métier actuelle de l’entreprise, tout en apportant une saisie plus fiable, une meilleure traçabilité et une réduction des ressaisies manuelles.

L’application doit permettre au livreur de :

- se connecter au dépôt ;
- vérifier que l’API est accessible ;
- s’identifier avec son code livreur ;
- choisir la tournée du jour ;
- charger les points de livraison ;
- consulter les clients à livrer ;
- consulter les informations de point de livraison ;
- saisir les quantités livrées ;
- saisir les quantités récupérées ;
- valider les passages ;
- signaler un passage non fait ;
- signaler une anomalie ;
- ajouter un commentaire lorsque c’est nécessaire ;
- consulter une aide au déchargement des articles récupérés ;
- vérifier un récapitulatif de fin de tournée ;
- synchroniser les données vers l’API en fin de journée.

L’application mobile ne se connecte jamais directement à SQL Server.

Toutes les données passent par l’API ASP.NET Core. L’API sert d’intermédiaire entre l’application mobile et la base de données de l’entreprise.

```text
Application mobile Android
↓ HTTP/JSON
API ASP.NET Core
↓ SQL
SQL Server / vues ABSSolute
```

Ce choix permet de :

- protéger l’accès direct à SQL Server ;
- centraliser les règles métier côté API ;
- limiter les risques liés à une application mobile connectée directement à la base ;
- faire évoluer l’API indépendamment de l’application mobile ;
- préparer une architecture plus maintenable pour l’entreprise.

---

## 2. Fonctionnement général retenu

L’application est destinée aux téléphones professionnels des livreurs.

Elle doit fonctionner dans deux contextes :

- au dépôt, avec accès au réseau de l’entreprise ;
- pendant la tournée, sans dépendre d’une connexion réseau permanente.

Le fonctionnement retenu est donc un fonctionnement **hors connexion après chargement**.

L’application doit être capable de charger la tournée le matin, de fonctionner localement pendant la journée, puis de synchroniser les données le soir.

---

## 3. Cycle métier complet

### 3.1 Matin — chargement de la tournée

Le livreur est au dépôt et connecté au réseau de l’entreprise.

Il ouvre l’application mobile, puis :

1. arrive sur l’écran d’accueil ;
2. vérifie la connexion à l’API ;
3. saisit ou sélectionne son code livreur ;
4. consulte les tournées disponibles ;
5. choisit la tournée du jour ;
6. confirme le chargement ;
7. récupère les données depuis l’API ;
8. sauvegarde la tournée localement dans SQLite.

Route principale utilisée pour charger une tournée :

```http
GET /api/tournees/jour?dateTournee=YYYY-MM-DD&codeTournee=XXXX&codeLivreur=YY
```

Exemple :

```http
GET /api/tournees/jour?dateTournee=2026-04-27&codeTournee=1001&codeLivreur=3
```

Une fois les données enregistrées localement, le livreur peut quitter le dépôt et travailler sans connexion réseau.

### 3.2 Pendant la journée — saisie locale

Pendant la tournée, toutes les actions sont enregistrées localement sur le téléphone.

L’application ne doit pas dépendre d’une connexion permanente à l’API.

Les actions locales comprennent :

- consultation des points de livraison ;
- consultation des informations client ;
- consultation des instructions ;
- saisie des quantités livrées ;
- saisie des quantités récupérées ;
- choix du statut de passage ;
- ajout d’un commentaire en cas de passage non fait ;
- ajout d’un commentaire en cas d’anomalie ;
- validation du point de livraison ;
- génération automatique de l’heure de validation ;
- modification des saisies tant que la tournée n’est pas synchronisée.

L’application doit sauvegarder les modifications au fur et à mesure afin d’éviter toute perte de données.

### 3.3 Soir — synchronisation

En fin de journée, le livreur revient au dépôt et se reconnecte au réseau de l’entreprise.

Il consulte le récapitulatif de la tournée, vérifie les données, puis envoie les informations à l’API.

Route utilisée pour l’envoi final :

```http
POST /api/synchronisations
```

Avant l’envoi définitif, l’application doit afficher un avertissement clair :

```text
Après synchronisation réussie, la tournée ne sera plus modifiable sur le téléphone.
```

Si l’envoi réussit :

- la tournée est marquée comme synchronisée ;
- la tournée est verrouillée localement ;
- les données ne sont plus modifiables ;
- l’application ne propose plus de renvoyer la tournée.

Si l’envoi échoue :

- la tournée reste disponible localement ;
- aucune donnée ne doit être supprimée ;
- le livreur peut corriger ou réessayer plus tard ;
- l’erreur doit être affichée de manière compréhensible.

Cas particulier :

Si l’API répond que la tournée a déjà été synchronisée, l’application doit bloquer le renvoi et afficher un message clair.

---

## 4. Configuration technique mobile

### 4.1 Plateforme cible

L’application est prévue pour Android.

| Élément | Choix retenu |
|---|---|
| Framework | .NET MAUI |
| Langage | C# |
| Interface | XAML |
| Architecture | MVVM |
| Stockage local | SQLite |
| Communication API | HTTP/JSON en développement, HTTPS recommandé en production |
| Plateforme principale | Android |
| Cible entreprise | Téléphones Android professionnels |
| Mode de fonctionnement | Hors connexion après chargement |
| Base de données mobile | SQLite locale |
| Accès SQL Server direct | Interdit |

### 4.2 Version Android

Pour le projet actuel, la cible métier reste :

| Élément | Valeur |
|---|---|
| Version minimale entreprise recommandée | Android 12 |
| API minimale recommandée | API level 31 |
| SDK Android recommandé pour compiler | API 35 ou API 36 selon environnement .NET |
| Téléphone de test actuel | Android récent / Android 16 |
| Spécificité Android 16 | Attention à la compatibilité des bibliothèques natives SQLite avec les pages mémoire 16 Ko |

Le téléphone personnel utilisé pour les tests peut être plus récent que les futurs téléphones professionnels.

Cela ne change pas la cible métier principale :

```text
Android 12 / API 31 minimum pour les téléphones professionnels
```

Le SDK de compilation peut être plus récent que la version minimale supportée.

Exemple :

```xml
<TargetFramework>net10.0-android</TargetFramework>
<SupportedOSPlatformVersion>24.0</SupportedOSPlatformVersion>
<AndroidTargetSdkVersion>36</AndroidTargetSdkVersion>
<AndroidCompileSdkVersion>36</AndroidCompileSdkVersion>
```

Remarque :

`SupportedOSPlatformVersion` indique la version minimale supportée côté application .NET. 
La cible métier peut rester Android 12 même si la valeur technique minimale est plus basse pour faciliter les tests.

---

## 5. Environnement de développement

### 5.1 Outils nécessaires

Outils recommandés :

- Visual Studio 2026 ou version compatible avec .NET MAUI ;
- SDK .NET 10 ;
- workloads Android et MAUI ;
- Android SDK Platform Tools ;
- JDK compatible ;
- téléphone Android avec débogage USB ;
- API ASP.NET Core lancée localement ou sur une VM.

### 5.2 Workload Visual Studio

Dans Visual Studio Installer, installer le workload :

```text
.NET Multi-platform App UI development
```

En français :

```text
Développement d’interface utilisateur d’application multiplateforme .NET
```

### 5.3 Vérifications utiles

Commandes utiles :

```powershell
dotnet --info
dotnet workload list
adb devices -l
adb reverse --list
```

Pour compiler :

```powershell
dotnet build -f net10.0-android
```

Pour lancer sur téléphone physique :

```powershell
dotnet run -f net10.0-android -c Debug -p:AdbTarget=-d
```

---

## 6. Tests sur téléphone physique

Pour tester l’application sur un vrai téléphone Android, la configuration attendue est :

- téléphone Android ;
- mode développeur activé ;
- débogage USB activé ;
- connexion USB fonctionnelle avec le PC ;
- téléphone reconnu par ADB ;
- API lancée sur le PC de développement ou sur une VM ;
- application configurée avec la bonne adresse API.

### 6.1 Vérifier que le téléphone est reconnu

Commande :

```powershell
adb devices -l
```

Résultat attendu :

```text
device
```

Exemple :

```text
RZGYB1XV04B device product:a56xnaeea model:SM_A566B device:a56x
```

Si le téléphone apparaît en `unauthorized`, il faut accepter la demande de débogage USB sur le téléphone.

---

## 7. Configuration réseau

L’application communique uniquement avec l’API ASP.NET Core.

Elle ne communique jamais directement avec SQL Server.

### 7.1 Problème rencontré en développement

Dans l’environnement de test actuel :

- le PC arrive à communiquer avec le téléphone ;
- le téléphone n’arrive pas à joindre directement le PC par son IP locale.

Constat :

```text
PC → téléphone : OK
téléphone → PC : KO
```

La solution temporaire retenue pour le développement est donc :

```powershell
adb reverse tcp:5000 tcp:5000
```

Puis dans le téléphone ou l’application :

```text
http://127.0.0.1:5000
```

Dans ce mode, `127.0.0.1` côté téléphone est redirigé vers le port 5000 du PC grâce à ADB.

### 7.2 Pourquoi ne pas utiliser l’IP du PC dans ce cas ?

L’IP du PC était par exemple :

```text
192.168.1.66
```

Le téléphone avait par exemple :

```text
192.168.1.26
```

Même si les deux adresses semblent appartenir au même réseau `192.168.1.0/24`, les tests ont montré que la communication directe ne fonctionne pas. Car le reseaux stralink sur le téléphone n'est pas le même reseau que la connexion filaire du PC. 

Informations observées :

| Équipement | Adresse IP | Passerelle | MAC passerelle observée |
|---|---:|---:|---|
| PC développement / API | 192.168.1.66/24 | 192.168.1.1 | 00-e0-4c-6e-d8-c1 |
| Téléphone Android | 192.168.1.26/24 | 192.168.1.1 | 74:24:9f:d3:fa:7b |

Le fait que la même passerelle `192.168.1.1` soit vue avec deux adresses MAC différentes peut indiquer :

- deux réseaux différents utilisant la même plage IP ;
- une isolation Wi-Fi ;
- un VLAN différent entre Wi-Fi et Ethernet ;
- un filtrage entre Wi-Fi et réseau câblé ;
- un proxy ARP ;
- une configuration réseau spécifique de l’entreprise.

Tests observés depuis le téléphone :

```text
ping 192.168.1.66 → Destination Host Unreachable
ip neigh show 192.168.1.66 → FAILED
```

Question à poser aux informaticiens :

```text
Pouvez-vous vérifier si le Wi-Fi du téléphone et le réseau Ethernet du PC sont bien sur le même LAN, ou s’il existe deux réseaux différents utilisant tous les deux 192.168.1.0/24, une isolation Wi-Fi, un VLAN, un proxy ARP ou un filtrage Wi-Fi vers Ethernet ?
```

### 7.3 Modes d’adresse API selon le contexte

| Contexte | Adresse à utiliser dans l’application |
|---|---|
| Téléphone physique avec adb reverse | `http://127.0.0.1:5000` |
| Téléphone physique sans adb reverse | `http://IP_DU_PC:5000` |
| Émulateur Android | `http://10.0.2.2:5000` |
| Installation durable en entreprise | `https://nom-dns-interne` ou IP fixe |
| Production recommandée | HTTPS avec certificat reconnu |

### 7.4 Configuration centralisée recommandée

Pour éviter de modifier l’URL API dans plusieurs fichiers, l’application doit avoir un fichier unique :

```text
Configuration/AppConfig.cs
```

Exemple pour le développement actuel :

```csharp
namespace MobileSLI.Configuration;

public static class AppConfig
{
    public const string ApiBaseUrl = "http://127.0.0.1:5000";
}
```

Exemple pour un accès réseau direct :

```csharp
public const string ApiBaseUrl = "http://192.168.1.66:5000";
```

Exemple pour l’émulateur :

```csharp
public const string ApiBaseUrl = "http://10.0.2.2:5000";
```

### 7.5 Production ou installation durable

Pour une installation durable dans l’entreprise, l’utilisation de HTTPS est préférable.

HTTPS permet :

- de chiffrer les échanges ;
- d’éviter les blocages Android liés au trafic HTTP clair ;
- de sécuriser les données échangées ;
- de préparer une exposition contrôlée de l’API si nécessaire ;
- de réduire les risques liés aux interceptions réseau.

L’adresse de l’API doit être stable, idéalement via :

- une IP fixe ;
- une réservation DHCP ;
- un nom DNS interne.

---

## 8. Données manipulées

### 8.1 Données préremplies depuis ABSSolute / SQL Server

Les données de départ sont connues avant la tournée.

Elles proviennent de SQL Server via l’API ASP.NET Core.

Le téléphone ne consulte jamais directement SQL Server.

Les données préremplies peuvent contenir :

- date de tournée ;
- code tournée ;
- libellé tournée ;
- code livreur ;
- nom livreur ;
- numéro client ;
- nom client ;
- code point de livraison ;
- description du point de livraison ;
- adresse ;
- ville ;
- code postal ;
- ordre d’arrêt ;
- jour de tournée ;
- tournée retour si disponible ;
- description du retour si disponible ;
- instructions ;
- commentaire de fiche ;
- zone de déchargement ;
- articles saisissables ;
- quantités ou informations prévues si disponibles.

Ces données servent à éviter que le livreur ressaisisse des informations déjà connues.

### 8.2 Données saisies par le livreur

Les données saisies dans l’application sont :

- code livreur sélectionné ;
- tournée choisie ;
- statut du passage ;
- quantité livrée par article ;
- quantité récupérée par article ;
- commentaire livreur ;
- anomalie éventuelle ;
- heure de validation ;
- commentaire global de fin de tournée si nécessaire.

Les quantités doivent toujours être des entiers positifs ou nuls.

Les quantités récupérées doivent être séparées des quantités livrées.

Elles ne doivent jamais être représentées par une valeur négative.

---

## 9. Écrans de l’application mobile

La maquette mobile prévoit les écrans suivants.

---

### 00 — Accueil

Objectif : vérifier que le téléphone peut communiquer avec l’API au dépôt.

L’écran affiche :

- le titre de l’application ;
- un bouton de test de connexion ;
- l’état de connexion ;
- un avertissement si la connexion est impossible ;
- un bouton pour continuer vers l’identification.

États possibles :

- connecté ;
- non connecté ;
- erreur API ;
- réseau indisponible.

Le test de connexion sert uniquement à informer le livreur.

Il ne doit pas forcément bloquer l’accès si une tournée est déjà chargée localement.

Comportement attendu :

```text
Si API accessible :
    afficher un état positif
    permettre le chargement d’une tournée

Si API inaccessible mais tournée locale existante :
    permettre de continuer en mode hors connexion

Si API inaccessible et aucune tournée locale :
    afficher un message d’erreur clair
```

---

### 01 — Identification livreur

Objectif : identifier le livreur à partir de son code.

L’écran affiche :

- un champ de saisie du code livreur ;
- éventuellement une liste des livreurs ;
- un bouton de validation ;
- le nom du livreur si le code est reconnu ;
- un message d’erreur si le code est absent ou inconnu.

Route API associée :

```http
GET /api/livreurs
```

Réponse attendue :

```json
[
  {
    "codeLivreur": "2",
    "nomLivreur": "DAVID LEBAS"
  },
  {
    "codeLivreur": "3",
    "nomLivreur": "DAVID VARIN"
  }
]
```

Règles :

- le code livreur est obligatoire ;
- le code doit être reconnu ;
- le nom du livreur doit être affiché après reconnaissance ;
- les données chargées ensuite seront associées à ce livreur ;
- le code livreur ne doit pas être imposé en dur ;
- l’application ne doit pas être limitée au livreur `2`.

---

### 02 — Choix de la tournée

Objectif : afficher les tournées disponibles pour le livreur et la date du jour.

L’écran affiche :

- le livreur identifié ;
- la date du jour ;
- une barre de recherche ou de filtre ;
- la liste des tournées disponibles ;
- le nombre de tournées trouvées ;
- un bouton pour continuer avec la tournée sélectionnée.

La date du jour est affichée mais non modifiable.

Une tournée doit être sélectionnée avant de continuer.

Point important :

La route confirmée actuellement pour l’API charge une tournée complète à partir de `dateTournee`, `codeTournee` et `codeLivreur`.

Exemple :

```http
GET /api/tournees/jour?dateTournee=2026-04-27&codeTournee=1001&codeLivreur=3
```

Pour afficher une vraie liste de tournées dans cet écran, deux possibilités existent :

#### — Ajouter une route API dédiée

Exemple cible :

```http
GET /api/tournees/disponibles?dateTournee=2026-05-04&codeLivreur=2
```

Réponse attendue :

```json
[
  {
    "codeTournee": "1001",
    "libelleTournee": "CHATAIGNERAIE LES HERBIERS",
    "jourTournee": 1,
    "jourLibelle": "Lundi",
    "nombrePoints": 7
  }
]
```

### 03 — Confirmation du choix de tournée

Objectif : confirmer la tournée avant son chargement local.

L’écran affiche :

- le livreur ;
- la date ;
- le code tournée ;
- le libellé tournée ;
- le nombre de points de livraison prévus ;
- un avertissement indiquant qu’une fois chargée, la tournée est enregistrée sur le téléphone ;
- une information indiquant que le livreur pourra travailler sans connexion.

Actions possibles :

- charger la tournée ;
- revenir au choix de tournée.

La confirmation doit appeler l’API uniquement si les informations nécessaires sont connues :

```text
dateTournee
codeTournee
codeLivreur
```
Exemple :

```http
GET /api/tournees/jour?dateTournee=2026-04-27&codeTournee=1001&codeLivreur=3
```

---

### 04 — Liste des points de livraison

Objectif : suivre l’avancement de la tournée.

L’écran affiche :

- la liste des points de livraison ;
- le client ;
- le point de livraison ;
- l’adresse ou une information utile ;
- le statut du passage ;
- un badge indiquant l’état du point ;
- des filtres d’affichage.

Filtres prévus :

- tous ;
- à faire ;
- fait ;
- non fait ;
- anomalie ;
- fermé si l’information existe dans les données.

Chaque point de livraison doit être ouvrable pour accéder à la saisie détaillée.

Les données affichées doivent provenir de SQLite après chargement de la tournée.

---

### 04 bis — Détail point / saisie

Objectif : saisir les informations du passage chez un client.

L’écran affiche :

- les informations principales du point ;
- l’ordre d’arrêt ;
- le client ;
- le point de livraison ;
- la zone ;
- les instructions ;
- le commentaire de fiche ;
- les quantités par article ;
- une colonne `Livré` ;
- une colonne `Récupéré` ;
- le statut du passage ;
- le commentaire livreur ;
- le bouton de validation.

Statuts possibles :

```text
A_FAIRE
FAIT
NON_FAIT
ANOMALIE
```

Règles :

- `A_FAIRE` correspond à un point non encore validé ;
- `FAIT` peut être validé sans commentaire ;
- `NON_FAIT` nécessite un commentaire ;
- `ANOMALIE` nécessite un commentaire ;
- l’heure de validation est générée automatiquement ;
- les quantités doivent être positives ou nulles ;
- les champs de quantité doivent utiliser un clavier numérique ;
- les boutons `+` et `-` ne sont pas retenus pour la première version.

---

### 05 — Déchargement du camion par client

Objectif : aider le livreur à décharger correctement ce qu’il a récupéré.

Cet écran ne remplace pas la synchronisation.

Il sert uniquement d’aide opérationnelle au dépôt.

L’écran affiche les articles récupérés, regroupés par client.

Tri retenu :

```text
tri par client
```

Chaque carte client affiche :

- numéro client ;
- nom client ;
- point de livraison ;
- articles récupérés ;
- quantités récupérées ;
- zone de déchargement si disponible.

Exemples d’informations affichées :

- rolls récupérés ;
- tapis récupérés ;
- sacs récupérés ;
- vêtements récupérés si ajout futur ;
- zone ou emplacement de déchargement.

L’écran doit permettre au livreur de vérifier rapidement ce qu’il doit sortir du camion.

---

### 06 — Récapitulatif avant envoi

Objectif : contrôler les données avant synchronisation.

L’écran affiche :

- le code tournée ;
- le libellé tournée ;
- la date ;
- le livreur ;
- le nombre total de clients ;
- le nombre de points validés ;
- le nombre de points non faits ;
- le nombre d’anomalies ;
- les totaux par article ;
- les quantités livrées ;
- les quantités récupérées ;
- un avertissement avant envoi.

Avant l’envoi, l’application doit vérifier :

- qu’il reste ou non des points à faire ;
- que les anomalies ont un commentaire ;
- que les passages non faits ont un commentaire ;
- que les quantités sont valides ;
- que les heures de validation existent pour les points validés.

L’application peut autoriser l’envoi même s’il reste des points non faits, mais uniquement si les règles de commentaire sont respectées.

---

### 07 — Résultat synchronisation

Objectif : informer clairement le livreur du résultat de l’envoi.

Cas de succès :

- message indiquant que la tournée a été envoyée avec succès ;
- date et heure de l’envoi ;
- nombre de lignes envoyées ;
- bouton retour accueil.

Après succès :

- la tournée est verrouillée localement ;
- les données ne sont plus modifiables ;
- un renvoi ne doit pas être proposé.

---

### 07 bis — Cas d’erreur

Objectif : gérer les erreurs de synchronisation de manière compréhensible.

Cas possibles :

- erreur réseau ;
- API indisponible ;
- erreur de validation ;
- tournée déjà synchronisée ;
- doublon d’identifiant de synchronisation ;
- erreur serveur.

En cas d’erreur réseau ou API indisponible :

- la tournée reste stockée localement ;
- le livreur peut réessayer plus tard ;
- aucune donnée ne doit être supprimée.

En cas de tournée déjà synchronisée :

- l’application affiche que la tournée a déjà été envoyée ;
- le livreur ne doit pas renvoyer les données ;
- une action recommandée doit indiquer de contacter le responsable logistique ou informatique si une correction est nécessaire.

---

## 10. Architecture technique mobile

L’application mobile doit être organisée avec une architecture simple, lisible et maintenable.

Structure recommandée :

```text
Pages/
ViewModels/
Models/
Services/
Repositories/
Resources/
Configuration/
```

### 10.1 Pages

Les pages correspondent aux écrans XAML affichés à l’utilisateur.

Pages prévues :

- `AccueilPage`
- `IdentificationLivreurPage`
- `ChoixTourneePage`
- `ConfirmationTourneePage`
- `ListePointsLivraisonPage`
- `DetailPointLivraisonPage`
- `DechargementPage`
- `RecapitulatifTourneePage`
- `SyncResultPage`
- `SyncErrorPage`

### 10.2 ViewModels

Les ViewModels contiennent la logique d’affichage et les actions utilisateur.

ViewModels prévus :

- `AccueilViewModel`
- `IdentificationLivreurViewModel`
- `ChoixTourneeViewModel`
- `ConfirmationTourneeViewModel`
- `ListePointsLivraisonViewModel`
- `DetailPointLivraisonViewModel`
- `DechargementViewModel`
- `RecapitulatifTourneeViewModel`
- `SyncResultViewModel`
- `SyncErrorViewModel`

Ils doivent gérer :

- le test de connexion ;
- la validation du code livreur ;
- l’affichage de la date du jour ;
- le choix de la tournée ;
- le chargement de la tournée ;
- la sauvegarde locale ;
- la saisie des quantités ;
- la validation des statuts ;
- le commentaire obligatoire ;
- l’heure de validation automatique ;
- le calcul du récapitulatif ;
- le verrouillage après synchronisation.

### 10.3 Models

Les models représentent les données manipulées par l’application.

Exemples :

- `LivreurDto`
- `TourneeJourDto`
- `TourneeResumeDto`
- `TourneeLigneDto`
- `ArticleSaisissableDto`
- `SynchronisationTourneeRequest`
- `SynchronisationLigneRequest`
- `QuantiteArticleRequest`
- `MobileInfoDto`
- `LocalTournee`
- `LocalTourneeLigne`
- `LocalTourneeLigneQuantite`

Il faut distinguer :

- les DTO reçus depuis l’API ;
- les DTO envoyés à l’API ;
- les entités locales SQLite ;
- les objets utilisés uniquement pour l’affichage.

La structure doit rester évolutive pour permettre l’ajout futur de nouveaux articles sans modifier toute l’application.

### 10.4 Services

Services principaux :

- `ApiService`
- `DatabaseService`
- `SettingsService`
- `DemoDataService`
- `ConnectivityService`
- `SynchronisationService`
- `AppStateService`

#### ApiService

Le service `ApiService` gère la communication avec l’API ASP.NET Core.

Il appelle notamment :

```http
GET /api/health
GET /api/livreurs
GET /api/tournees/jour
POST /api/synchronisations
```

Il doit gérer :

- l’adresse de base de l’API ;
- le chargement des livreurs ;
- le chargement d’une tournée ;
- l’envoi d’une synchronisation ;
- les erreurs réseau ;
- les erreurs retournées par l’API ;
- la sérialisation JSON ;
- la désérialisation JSON ;
- les messages compréhensibles pour le livreur.

#### DatabaseService

Le service `DatabaseService` gère le stockage local SQLite.

Il permet de :

- mémoriser le livreur identifié ;
- sauvegarder une tournée chargée ;
- relire une tournée sans connexion ;
- enregistrer les saisies client par client ;
- enregistrer les quantités par article ;
- enregistrer les statuts de passage ;
- enregistrer les commentaires ;
- enregistrer l’heure de validation ;
- générer le JSON de synchronisation ;
- verrouiller la tournée après envoi réussi.

#### SettingsService

Le service `SettingsService` garde les paramètres locaux :

- adresse de l’API ;
- nom de l’appareil ;
- version de l’application ;
- dernier code livreur utilisé si l’entreprise l’autorise ;
- paramètres nécessaires aux tests.

L’adresse de l’API doit pouvoir être modifiée facilement entre :

- émulateur ;
- téléphone physique avec `adb reverse`;
- téléphone physique en accès réseau direct ;
- VM ;
- environnement final de l’entreprise.

#### DemoDataService

Le service `DemoDataService` permet de charger une tournée de démonstration.

Il sert uniquement au développement pour tester l’interface et la navigation sans dépendre immédiatement de l’API.

Il ne doit pas remplacer les tests réels avec l’API.

En production, le mode démonstration doit être désactivé ou inaccessible pour les livreurs.

#### AppStateService

Le service `AppStateService` garde l’état courant de navigation et de sélection.

Exemples :

- livreur sélectionné ;
- tournée sélectionnée ;
- identifiant de tournée locale ;
- ligne sélectionnée ;
- dernier résultat de synchronisation.

Il ne doit pas remplacer SQLite.

Son rôle est de conserver un état temporaire en mémoire pendant l’utilisation de l’application.

---

## 11. États locaux d’une tournée

L’application doit gérer clairement l’état local de la tournée.

États proposés :

```text
NON_CHARGEE
CHARGEE
EN_COURS
PRETE_A_SYNCHRONISER
SYNCHRONISEE
ERREUR_SYNCHRONISATION
DEJA_SYNCHRONISEE
```

| État | Signification |
|---|---|
| `NON_CHARGEE` | Aucune tournée n’est disponible localement |
| `CHARGEE` | La tournée a été récupérée depuis l’API et sauvegardée |
| `EN_COURS` | Le livreur a commencé à saisir des données |
| `PRETE_A_SYNCHRONISER` | La tournée peut être envoyée à l’API |
| `SYNCHRONISEE` | La tournée a été envoyée avec succès |
| `ERREUR_SYNCHRONISATION` | L’envoi a échoué, une nouvelle tentative peut être nécessaire |
| `DEJA_SYNCHRONISEE` | L’API indique que la tournée a déjà été envoyée |

Après l’état `SYNCHRONISEE`, la tournée est verrouillée localement.

---

## 12. Format de chargement de la tournée

Route recommandée :

```http
GET /api/tournees/jour?dateTournee=2026-05-04&codeTournee=2001&codeLivreur=2
```

La date est calculée automatiquement par l’application à partir de la date du jour.

Elle est affichée dans l’interface mais non modifiable par le livreur.

### Exemple de réponse

```json
{
  "schemaVersion": "1.1",
  "dateTournee": "2026-05-04",
  "dateModifiable": false,
  "codeTournee": "2001",
  "libelleTournee": "MDR VENDEE",
  "livreur": {
    "codeLivreur": "2",
    "nomLivreur": "DAVID LEBAS"
  },
  "articlesSaisissables": [
    {
      "codeArticle": "ROLLS",
      "libelle": "Rolls"
    },
    {
      "codeArticle": "TAPIS",
      "libelle": "Tapis"
    },
    {
      "codeArticle": "SACS",
      "libelle": "Sacs"
    }
  ],
  "lignes": [
    {
      "idLigneSource": "2026-05-04|2001|1|1058|PDL01|1",
      "ordreArret": 1,
      "numClient": "1058",
      "nomClient": "HOTEL EXEMPLE",
      "codePDL": "PDL01",
      "descriptionPDL": "Entrée principale",
      "zoneDechargement": "Zone 1",
      "instructions": "Livraison par l'arrière",
      "commentaireFiche": null,
      "saisie": {
        "statutPassage": "A_FAIRE",
        "estValidee": false,
        "heureValidation": null,
        "commentaireLivreur": null,
        "quantites": [
          {
            "codeArticle": "ROLLS",
            "libelleArticle": "Rolls",
            "quantiteLivree": 0,
            "quantiteRecuperee": 0
          }
        ]
      }
    }
  ]
}
```

---

## 13. Format de synchronisation de fin de journée

Route recommandée :

```http
POST /api/synchronisations
```

### Exemple de JSON envoyé

```json
{
  "schemaVersion": "1.1",
  "idSynchronisation": "7d3b3d5a-8dc4-4b2c-9f20-6b2170f1b321",
  "dateTournee": "2026-05-04",
  "codeTournee": "2001",
  "libelleTournee": "MDR VENDEE",
  "livreur": {
    "codeLivreur": "2",
    "nomLivreur": "DAVID LEBAS"
  },
  "mobile": {
    "nomAppareil": "Samsung A12",
    "versionApplication": "1.0.0",
    "dateChargement": "2026-05-04T07:15:00",
    "dateEnvoi": "2026-05-04T17:45:00"
  },
  "commentaireGlobal": null,
  "lignes": [
    {
      "idLigneSource": "2026-05-04|2001|1|1058|PDL01|1",
      "ordreArret": 1,
      "numClient": "1058",
      "nomClient": "HOTEL EXEMPLE",
      "codePDL": "PDL01",
      "descriptionPDL": "Entrée principale",
      "statutPassage": "FAIT",
      "estValidee": true,
      "heureValidation": "2026-05-04T15:42:00",
      "commentaireLivreur": null,
      "quantites": [
        {
          "codeArticle": "ROLLS",
          "libelleArticle": "Rolls",
          "quantiteLivree": 2,
          "quantiteRecuperee": 1
        },
        {
          "codeArticle": "TAPIS",
          "libelleArticle": "Tapis",
          "quantiteLivree": 4,
          "quantiteRecuperee": 3
        },
        {
          "codeArticle": "SACS",
          "libelleArticle": "Sacs",
          "quantiteLivree": 1,
          "quantiteRecuperee": 0
        }
      ]
    }
  ]
}
```

Cette structure est volontairement évolutive.

Si l’entreprise ajoute plus tard d’autres articles, par exemple :

- vêtements ;
- draps ;
- serviettes ;
- couvertures ;
- autres produits ;

l’API pourra ajouter un nouvel objet dans le tableau `quantites` sans modifier entièrement le contrat JSON.

---

## 14. Règles métier intégrées côté mobile

### 14.1 Identification

Règles :

- le livreur doit obligatoirement saisir ou sélectionner son code ;
- le code livreur doit être reconnu avant le chargement d’une tournée ;
- le nom du livreur est affiché après identification ;
- les données sont associées au livreur identifié.

### 14.2 Tournée

Règles :

- la date du jour est affichée mais non modifiable ;
- le choix d’une tournée est obligatoire ;
- les données sont associées à la tournée choisie ;
- une tournée chargée est sauvegardée localement ;
- une tournée synchronisée est verrouillée ;
- une tournée déjà envoyée ne doit pas être renvoyée.

### 14.3 Quantités

Règles :

- les quantités doivent être des entiers ;
- les quantités doivent être positives ou nulles ;
- les quantités négatives sont interdites ;
- les quantités livrées et récupérées doivent être séparées ;
- les récupérations ne doivent jamais être saisies sous forme négative ;
- les champs de quantité doivent utiliser un clavier numérique ;
- les boutons `+` et `-` ne sont pas retenus pour la première version.

### 14.4 Statuts

Statuts autorisés :

```text
A_FAIRE
FAIT
NON_FAIT
ANOMALIE
```

Règles :

- `A_FAIRE` est autorisé localement tant que le point n’est pas validé ;
- `A_FAIRE` ne doit pas être envoyé dans la synchronisation finale ;
- `FAIT` peut être validé sans commentaire ;
- `NON_FAIT` nécessite un commentaire ;
- `ANOMALIE` nécessite un commentaire ;
- un passage validé doit avoir une heure de validation ;
- l’heure de validation est générée automatiquement.

### 14.5 Synchronisation

Règles :

- la synchronisation se fait uniquement depuis le récapitulatif ;
- l’application doit afficher un avertissement avant l’envoi ;
- après succès, la tournée est verrouillée ;
- en cas d’échec réseau, les données restent locales ;
- en cas de doublon détecté par l’API, le renvoi est bloqué ;
- la protection finale contre les doubles envois est gérée côté API ;
- les corrections après synchronisation sont réservées à l’administration ou à un responsable habilité ;
- les corrections après synchronisation doivent être tracées.

---

## 15. Règles d’ergonomie issues de la maquette

L’interface doit rester simple, lisible et adaptée à une utilisation rapide par des livreurs.

Principes retenus :

- peu d’informations par écran ;
- boutons principaux visibles en bas d’écran ;
- textes courts ;
- badges de statut ;
- couleurs utilisées pour différencier succès, avertissement et erreur ;
- filtres simples sur la liste des points ;
- récapitulatif clair avant envoi ;
- messages d’erreur compréhensibles sans vocabulaire technique ;
- aucune manipulation complexe pendant la tournée.

Les écrans doivent être utilisables :

- rapidement ;
- avec une seule main si possible ;
- dans un environnement de travail réel ;
- avec une luminosité variable ;
- par des utilisateurs qui ne sont pas informaticiens.

---

## 16. Points importants à respecter pour la première version

Pour la première version, il faut prioriser :

- identification livreur ;
- choix de la tournée ;
- chargement depuis l’API ;
- sauvegarde SQLite ;
- consultation hors connexion ;
- saisie livré / récupéré ;
- validation des statuts ;
- commentaire obligatoire si nécessaire ;
- récapitulatif ;
- synchronisation API ;
- verrouillage après succès ;
- gestion claire des erreurs.

Les fonctionnalités secondaires ne sont pas prioritaires pour la première version.

Exemples de fonctionnalités non prioritaires :

- statistiques avancées ;
- recherche complexe ;
- administration mobile ;
- modification d’une tournée après synchronisation ;
- gestion multi-profils complexe ;
- optimisation automatique de l’ordre de tournée.

Le plus important est d’obtenir une application fiable, simple et cohérente avec le fonctionnement réel des fiches de tournée.

---

## 17. Fonctionnement actuel en mode démonstration

La version de démonstration sert à valider :

- la navigation ;
- les écrans ;
- les boutons ;
- les transitions ;
- la saisie locale simulée ;
- la structure générale de l’application.

Elle peut contenir :

- un livreur codé en dur ;
- une tournée fictive ;
- des clients fictifs ;
- des quantités fictives ;
- une synchronisation simulée.

Ce mode est utile au début du développement, mais il ne doit pas être confondu avec le fonctionnement final.

À terme :

```text
DemoDataService = uniquement pour développement
ApiService + DatabaseService = fonctionnement réel
```

En production, le mode démonstration doit être désactivé.

---

## 18. Passage progressif du mode démo aux données réelles

Le passage aux données réelles doit être fait progressivement.

Ordre recommandé :

### Étape 1 — Revenir à une version stable

Objectif :

- l’application compile ;
- l’application se lance ;
- la navigation fonctionne ;
- les écrans de démo ne plantent pas.

### Étape 2 — Centraliser la configuration API

Créer ou vérifier :

```text
Configuration/AppConfig.cs
```

Objectif :

- ne changer l’adresse API qu’à un seul endroit.

### Étape 3 — Stabiliser `ApiService`

Objectif :

- tester `/api/health` ;
- charger `/api/livreurs` ;
- charger `/api/tournees/jour` ;
- ne pas mélanger DTO API et données de démonstration.

### Étape 4 — Charger les vrais livreurs

Objectif :

- remplacer le livreur codé en dur ;
- afficher tous les livreurs réels ;
- stocker le livreur sélectionné.

### Étape 5 — Charger une vraie tournée

Objectif :

- sélectionner ou saisir un code tournée ;
- appeler `/api/tournees/jour` ;
- afficher les vraies informations de tournée.

### Étape 6 — Sauvegarder en SQLite

Objectif :

- enregistrer la tournée localement ;
- relire les points de livraison depuis SQLite ;
- permettre le travail hors connexion.

### Étape 7 — Corriger la saisie locale

Objectif :

- enregistrer les quantités ;
- enregistrer les statuts ;
- enregistrer les commentaires ;
- générer les heures de validation.

### Étape 8 — Corriger le récapitulatif

Objectif :

- calculer les totaux réels ;
- afficher les anomalies ;
- vérifier les règles métier avant envoi.

### Étape 9 — Corriger la synchronisation

Objectif :

- générer le JSON final ;
- envoyer à l’API ;
- gérer les réponses ;
- verrouiller la tournée après succès.

---

## 19. Conclusion

L’application mobile doit rester simple pour le livreur, mais robuste techniquement.

La priorité est de fiabiliser le flux principal :

```text
Identification
↓
Choix de tournée
↓
Chargement API
↓
Stockage SQLite
↓
Saisie hors connexion
↓
Récapitulatif
↓
Synchronisation
```

Le mode démonstration est utile pour démarrer, mais il doit être progressivement remplacé par :

```text
API réelle + stockage local SQLite + règles métier contrôlées
```

Le développement doit continuer étape par étape afin d’éviter de casser une application qui compile et se lance déjà.
