# Documentation fonctionnelle et technique — Application mobile MobileSLI

## 1. Objectif de l’application mobile

L’application mobile **MobileSLI** a pour objectif de remplacer progressivement la fiche papier utilisée par les livreurs pour les tournées quotidiennes.

Elle doit conserver la logique métier actuelle de l’entreprise, tout en apportant :

- une saisie plus fiable ;
- une meilleure traçabilité ;
- une réduction des ressaisies manuelles ;
- un fonctionnement utilisable hors connexion pendant la tournée ;
- une synchronisation contrôlée vers l’API en fin de journée.

L’application doit permettre au livreur de :

- vérifier que l’API est accessible au dépôt ;
- s’identifier avec son code livreur ;
- choisir la tournée du jour ;
- charger les points de livraison depuis l’API ;
- sauvegarder la tournée localement dans SQLite ;
- consulter les clients à livrer ;
- consulter les informations de point de livraison ;
- consulter les instructions permanentes ;
- consulter les commentaires exceptionnels transmis par l’administration ou l’expédition ;
- consulter les informations de retour et de déchargement ;
- saisir les quantités livrées ;
- saisir les quantités récupérées ;
- modifier les quantités préremplies par l’expédition lorsque la réalité terrain est différente ;
- valider les passages ;
- signaler un passage non fait ;
- signaler une anomalie ;
- ajouter un commentaire lorsque c’est obligatoire ;
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

Le cycle complet est le suivant :

```text
Matin au dépôt
↓
Chargement de la tournée depuis l’API
↓
Stockage local SQLite
↓
Utilisation hors connexion pendant la tournée
↓
Retour au dépôt
↓
Récapitulatif
↓
Synchronisation finale vers l’API
↓
Verrouillage local après succès
```

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

Route de liste des tournées disponibles :

```http
GET /api/tournees/disponibles?dateTournee=YYYY-MM-DD&codeLivreur=YY
```

Route principale utilisée pour charger une tournée complète :

```http
GET /api/tournees/jour?dateTournee=YYYY-MM-DD&codeTournee=XXXX&codeLivreur=YY
```

Exemple :

```http
GET /api/tournees/jour?dateTournee=2026-05-07&codeTournee=4006&codeLivreur=2
```

Une fois les données enregistrées localement, le livreur peut quitter le dépôt et travailler sans connexion réseau.

### 3.2 Pendant la journée — saisie locale

Pendant la tournée, toutes les actions sont enregistrées localement sur le téléphone.

L’application ne doit pas dépendre d’une connexion permanente à l’API.

Les actions locales comprennent :

- consultation des points de livraison ;
- consultation des informations client ;
- consultation des instructions permanentes ;
- consultation des commentaires exceptionnels ;
- consultation des informations de retour ;
- consultation de la zone de déchargement ;
- saisie des quantités livrées ;
- saisie des quantités récupérées ;
- modification des quantités livrées préremplies par l’expédition ;
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

| Élément | Valeur |
|---|---|
| Version minimale entreprise recommandée | Android 12 |
| API minimale recommandée | API level 31 |
| SDK Android recommandé pour compiler | API 35 ou API 36 selon l’environnement .NET |
| Téléphone de test actuel | Android récent / Android 16 |
| Spécificité Android 16 | Vérifier la compatibilité SQLite avec les pages mémoire 16 Ko |

La cible métier principale reste :

```text
Android 12 / API 31 minimum pour les téléphones professionnels
```

Le SDK de compilation peut être plus récent que la version minimale supportée.

Configuration actuelle possible :

```xml
<TargetFramework>net10.0-android</TargetFramework>
<SupportedOSPlatformVersion>24.0</SupportedOSPlatformVersion>
<AndroidTargetSdkVersion>36</AndroidTargetSdkVersion>
<AndroidCompileSdkVersion>36</AndroidCompileSdkVersion>
```

`SupportedOSPlatformVersion` indique la version minimale supportée côté application .NET. La cible métier peut rester Android 12 même si la valeur technique minimale est plus basse pour faciliter les tests.

---

## 5. Environnement de développement

### 5.1 Outils nécessaires

Outils recommandés :

- Visual Studio 2026 ou version compatible avec .NET MAUI ;
- SDK .NET 10 ;
- workload .NET MAUI ;
- workload Android ;
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

### 5.3 Chemins utilisés dans les commandes

Adapter les chemins si nécessaire.

Projet API :

```powershell
C:\Users\Logistique\Downloads\Stage\ProjetMobileTournee\backend\API-ASP.NET-Core
```

Projet mobile :

```powershell
C:\Users\Logistique\Downloads\Stage\ProjetMobileTournee\MobileSLI
```

ADB :

```powershell
C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe
```

---

## 6. Commandes complètes de test sur téléphone physique

Cette section décrit le protocole complet pour tester l’application mobile sur un téléphone Android physique avec `adb reverse`.

Dans ce mode :

- l’API tourne sur le PC ;
- le téléphone est branché en USB ;
- `adb reverse` redirige le port `5000` du téléphone vers le port `5000` du PC ;
- l’application mobile doit utiliser l’adresse API `http://127.0.0.1:5000`.

### 6.1 Ouvrir PowerShell

Ouvrir PowerShell en utilisateur normal.

Si une commande réseau ou firewall échoue plus tard, ouvrir PowerShell en administrateur uniquement pour cette commande.

---

### 6.2 Vérifier .NET

```powershell
dotnet --info
```

```powershell
dotnet workload list
```

Vérifier que les workloads MAUI / Android sont présents.

---

### 6.3 Vérifier ADB

```powershell
cd "C:\Program Files (x86)\Android\android-sdk\platform-tools"
```

```powershell
.\adb.exe version
```

```powershell
.\adb.exe kill-server
```

```powershell
.\adb.exe start-server
```

```powershell
.\adb.exe devices -l
```

Résultat attendu :

```text
List of devices attached
RZGYB1XV04B device product:a56xnaeea model:SM_A566B device:a56x transport_id:...
```

Si le téléphone apparaît en `unauthorized` :

1. déverrouiller le téléphone ;
2. accepter la demande de débogage USB ;
3. relancer :

```powershell
.\adb.exe devices -l
```

Si aucun téléphone n’apparaît :

1. vérifier le câble USB ;
2. mettre le mode USB en transfert de fichiers ;
3. vérifier que le débogage USB est activé ;
4. relancer :

```powershell
.\adb.exe kill-server
.\adb.exe start-server
.\adb.exe devices -l
```

---

### 6.4 Lancer l’API avec la bonne adresse pour le test USB

Ouvrir un deuxième PowerShell.

Aller dans le dossier de l’API :

```powershell
cd "C:\Users\Logistique\Downloads\Stage\ProjetMobileTournee\backend\API-ASP.NET-Core"
```

Nettoyer et restaurer si nécessaire :

```powershell
dotnet restore
```

Lancer l’API sur `127.0.0.1:5000` :

```powershell
dotnet run --urls "http://127.0.0.1:5000"
```

Laisser cette fenêtre ouverte.

Adresse attendue côté PC :

```text
http://127.0.0.1:5000
```

Adresse attendue côté application mobile avec `adb reverse` :

```text
http://127.0.0.1:5000
```

Explication : grâce à `adb reverse`, le `127.0.0.1` du téléphone est redirigé vers le `127.0.0.1` du PC.

---

### 6.5 Vérifier que l’API répond depuis le PC

Ouvrir un troisième PowerShell.

Tester la route de santé :

```powershell
curl.exe "http://127.0.0.1:5000/api/health"
```

Tester la connexion ABSSolute si la route existe :

```powershell
curl.exe "http://127.0.0.1:5000/api/health/abssolute"
```

Tester la connexion base mobile si la route existe :

```powershell
curl.exe "http://127.0.0.1:5000/api/health/mobile"
```

Tester les livreurs :

```powershell
curl.exe "http://127.0.0.1:5000/api/livreurs"
```

Tester les tournées disponibles :

```powershell
curl.exe "http://127.0.0.1:5000/api/tournees/disponibles?dateTournee=2026-05-07&codeLivreur=2"
```

Tester le chargement d’une tournée complète :

```powershell
curl.exe "http://127.0.0.1:5000/api/tournees/jour?dateTournee=2026-05-07&codeTournee=4006&codeLivreur=2"
```

Si une route renvoie une erreur métier, vérifier que la date, le code tournée et le code livreur existent réellement dans les données de test.

---

### 6.6 Configurer `adb reverse`

Revenir dans le PowerShell où ADB est ouvert :

```powershell
cd "C:\Program Files (x86)\Android\android-sdk\platform-tools"
```

Supprimer les anciens reverse :

```powershell
.\adb.exe reverse --remove-all
```

Créer la redirection :

```powershell
.\adb.exe reverse tcp:5000 tcp:5000
```

Vérifier la redirection :

```powershell
.\adb.exe reverse --list
```

Résultat attendu :

```text
RZGYB1XV04B tcp:5000 tcp:5000
```

---

### 6.7 Vérifier l’accès API depuis le téléphone

Toujours depuis ADB :

```powershell
.\adb.exe shell am start -a android.intent.action.VIEW -d "http://127.0.0.1:5000/api/health"
```

Le navigateur du téléphone doit s’ouvrir sur la réponse de l’API.

Si le navigateur ne répond pas :

1. vérifier que l’API tourne toujours ;
2. vérifier `adb devices -l` ;
3. relancer :

```powershell
.\adb.exe reverse --remove-all
.\adb.exe reverse tcp:5000 tcp:5000
.\adb.exe reverse --list
```

Puis retester :

```powershell
.\adb.exe shell am start -a android.intent.action.VIEW -d "http://127.0.0.1:5000/api/health"
```

---

### 6.8 Vérifier l’adresse API dans l’application mobile

Dans le projet mobile, le fichier à vérifier est :

```text
Configuration/AppConfig.cs
```

Aller dans le dossier du projet mobile :

```powershell
cd "C:\Users\Logistique\Downloads\Stage\ProjetMobileTournee\MobileSLI"
```

Afficher la configuration :

```powershell
Get-Content ".\Configuration\AppConfig.cs"
```

La valeur attendue pour le test sur téléphone physique avec `adb reverse` est :

```csharp
public const string ApiBaseUrl = "http://127.0.0.1:5000";
```

La version JSON attendue est :

```csharp
public const string SchemaVersion = "1.2";
```

Si l’adresse n’est pas bonne, modifier `Configuration/AppConfig.cs`.

---

### 6.9 Compiler l’application mobile

Dans le dossier du projet mobile :

```powershell
cd "C:\Users\Logistique\Downloads\Stage\ProjetMobileTournee\MobileSLI"
```

Restaurer :

```powershell
dotnet restore
```

Compiler :

```powershell
dotnet build -f net10.0-android
```

Si la compilation échoue, corriger les erreurs avant d’essayer de lancer sur téléphone.

---

### 6.10 Nettoyer l’application installée sur le téléphone si nécessaire

Cette étape est utile après une modification SQLite importante.

Attention : cela supprime les données locales de l’application sur le téléphone.

```powershell
cd "C:\Program Files (x86)\Android\android-sdk\platform-tools"
```

```powershell
.\adb.exe uninstall fr.sli.mobiletournee
```

Si le résultat indique que le package n’existe pas, ce n’est pas bloquant.

---

### 6.11 Lancer l’application sur téléphone physique

Revenir dans le projet mobile :

```powershell
cd "C:\Users\Logistique\Downloads\Stage\ProjetMobileTournee\MobileSLI"
```

Vérifier que le téléphone est toujours reconnu :

```powershell
& "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" devices -l
```

Vérifier que le reverse est toujours actif :

```powershell
& "C:\Program Files (x86)\Android\android-sdk\platform-tools\adb.exe" reverse --list
```

Lancer l’application :

```powershell
dotnet run -f net10.0-android -c Debug -p:AdbTarget=-d
```

L’application doit s’installer et s’ouvrir sur le téléphone.

---

### 6.12 Lire les logs Android pendant le test

Ouvrir un PowerShell supplémentaire :

```powershell
cd "C:\Program Files (x86)\Android\android-sdk\platform-tools"
```

Nettoyer les anciens logs :

```powershell
.\adb.exe logcat -c
```

Lire les logs utiles :

```powershell
.\adb.exe logcat | Select-String -Pattern "MobileSLI|monodroid|DOTNET|SQLite|AndroidRuntime|System.err"
```

Pour arrêter les logs :

```text
Ctrl + C
```

---

### 6.13 Test fonctionnel complet sur le téléphone

Dans l’application mobile :

1. ouvrir l’écran d’accueil ;
2. vérifier que l’adresse API affichée est `http://127.0.0.1:5000` ;
3. appuyer sur le bouton de test de connexion ;
4. vérifier que l’application affiche un état positif ;
5. continuer vers l’identification ;
6. sélectionner ou saisir un code livreur réel, par exemple `2` ;
7. charger les tournées disponibles ;
8. choisir une tournée, par exemple `4006` si elle existe pour la date testée ;
9. confirmer le chargement ;
10. vérifier que les points de livraison s’affichent ;
11. ouvrir un point ;
12. vérifier les instructions et le commentaire exceptionnel s’ils existent ;
13. vérifier les quantités préremplies ;
14. modifier une quantité livrée ;
15. saisir une quantité récupérée ;
16. valider le passage ;
17. revenir à la liste ;
18. vérifier l’aide au déchargement ;
19. vérifier le récapitulatif ;
20. synchroniser ;
21. vérifier que la tournée est verrouillée après succès.

---

## 7. Configuration réseau

L’application communique uniquement avec l’API ASP.NET Core.

Elle ne communique jamais directement avec SQL Server.

### 7.1 Mode actuel recommandé en développement

Dans l’environnement de test actuel, le téléphone n’arrive pas toujours à joindre directement le PC par son IP locale.

La solution temporaire retenue pour le développement est donc :

```powershell
adb reverse tcp:5000 tcp:5000
```

Puis dans l’application :

```text
http://127.0.0.1:5000
```

Dans ce mode, `127.0.0.1` côté téléphone est redirigé vers le port `5000` du PC grâce à ADB.

### 7.2 Pourquoi ne pas utiliser l’IP du PC dans ce cas ?

L’IP du PC était par exemple :

```text
192.168.1.66
```

Le téléphone avait par exemple :

```text
192.168.1.26
```

Même si les deux adresses semblent appartenir au même réseau `192.168.1.0/24`, les tests ont montré que la communication directe ne fonctionne pas. Le réseau Starlink utilisé par le téléphone n’est pas forcément le même réseau réel que la connexion filaire du PC.

Informations observées :

| Équipement | Adresse IP | Passerelle | MAC passerelle observée |
|---|---|---|---|
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

| Contexte | Adresse à utiliser dans l’application | Commande API recommandée |
|---|---|---|
| Téléphone physique avec `adb reverse` | `http://127.0.0.1:5000` | `dotnet run --urls "http://127.0.0.1:5000"` |
| Téléphone physique sans `adb reverse` | `http://IP_DU_PC:5000` | `dotnet run --urls "http://0.0.0.0:5000"` |
| Émulateur Android | `http://10.0.2.2:5000` | `dotnet run --urls "http://127.0.0.1:5000"` |
| Installation durable en entreprise | `https://nom-dns-interne` ou IP fixe | IIS / HTTPS |
| Production recommandée | HTTPS avec certificat reconnu | IIS / HTTPS |

### 7.4 Mode téléphone physique sans `adb reverse`

Ce mode est utile uniquement si le téléphone et le PC sont vraiment sur le même réseau.

Lancer l’API sur toutes les interfaces réseau :

```powershell
cd "C:\Users\Logistique\Downloads\Stage\ProjetMobileTournee\backend\API-ASP.NET-Core"
```

```powershell
dotnet run --urls "http://0.0.0.0:5000"
```

Trouver l’IP du PC :

```powershell
ipconfig
```

Tester depuis le PC :

```powershell
curl.exe "http://127.0.0.1:5000/api/health"
```

```powershell
curl.exe "http://IP_DU_PC:5000/api/health"
```

Dans `Configuration/AppConfig.cs`, utiliser :

```csharp
public const string ApiBaseUrl = "http://IP_DU_PC:5000";
```

Si le téléphone ne peut pas accéder à l’IP du PC, revenir au mode `adb reverse`.

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
- statut de synchronisation initial ;
- code livreur ;
- nom livreur ;
- nombre de points envoyés ;
- liste des articles saisissables ;
- identifiant source de ligne ;
- ordre d’arrêt ;
- horaire ;
- client ;
- numéro client ;
- nom client ;
- nom affiché du client ;
- point de livraison ;
- code point de livraison ;
- description du point de livraison ;
- adresse ;
- ville ;
- code postal ;
- informations de tournée ;
- informations de retour ;
- jour de tournée retour ;
- code tournée retour ;
- libellé tournée retour ;
- instructions permanentes ;
- commentaire exceptionnel ;
- zone ;
- zone de déchargement ;
- zone de déchargement affichée ;
- clé ;
- information de fermeture ;
- motif de fermeture si disponible ;
- quantités préremplies par l’expédition si disponibles.

Ces données servent à éviter que le livreur ressaisisse des informations déjà connues.

### 8.2 Données saisies par le livreur

Les données saisies dans l’application sont :

- code livreur sélectionné ;
- tournée choisie ;
- statut du passage ;
- précision libre du livreur ;
- quantité livrée par article ;
- quantité récupérée par article ;
- commentaire livreur ;
- anomalie éventuelle ;
- heure de validation ;
- commentaire global de fin de tournée si nécessaire.

Les quantités doivent toujours être des entiers positifs ou nuls.

Les quantités récupérées doivent être séparées des quantités livrées.

Elles ne doivent jamais être représentées par une valeur négative.

### 8.3 Quantités préremplies par l’expédition

Les préremplissages concernent uniquement la colonne `Livré`.

La colonne `Récupéré` reste saisie par le livreur pendant la tournée.

La quantité prévue doit rester distincte de la quantité réelle saisie par le livreur.

Règle importante :

```text
quantiteLivreePrevue = null → l’expédition n’a rien renseigné
quantiteLivreePrevue = 0    → l’expédition a volontairement prévu zéro
quantiteLivreePrevue > 0    → l’expédition a prévu une quantité
```

Le livreur peut modifier `quantiteLivree`, car la réalité terrain peut être différente de la valeur prévue.

---

## 9. Routes API utilisées par le mobile

### 9.1 Vérification de l’API

```http
GET /api/health
```

Routes complémentaires utiles :

```http
GET /api/health/abssolute
GET /api/health/mobile
```

### 9.2 Liste des livreurs

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

### 9.3 Liste des tournées disponibles

```http
GET /api/tournees/disponibles?dateTournee=2026-05-07&codeLivreur=2
```

Réponse v1.2 recommandée :

```json
{
  "schemaVersion": "1.2",
  "dateTournee": "2026-05-07",
  "dateModifiable": false,
  "livreur": {
    "codeLivreur": "2",
    "nomLivreur": "DAVID LEBAS"
  },
  "tournees": [
    {
      "codeTournee": "4006",
      "libelleTournee": "TOURNEE EXEMPLE",
      "jourTournee": 4,
      "jourLibelle": "Jeudi",
      "nombrePoints": 18
    }
  ]
}
```

### 9.4 Chargement d’une tournée complète

```http
GET /api/tournees/jour?dateTournee=2026-05-07&codeTournee=4006&codeLivreur=2
```

### 9.5 Envoi final d’une tournée

```http
POST /api/synchronisations
```

---

## 10. Écrans de l’application mobile

### 00 — Accueil

Objectif : vérifier que le téléphone peut communiquer avec l’API au dépôt.

L’écran affiche :

- le titre de l’application ;
- l’adresse API utilisée ;
- un bouton de test de connexion ;
- l’état de connexion ;
- un avertissement si la connexion est impossible ;
- un bouton pour continuer.

Le test de connexion sert uniquement à informer le livreur.

Il ne doit pas bloquer l’accès si une tournée est déjà chargée localement.

Comportement attendu :

```text
Si API accessible :
    afficher un état positif
    permettre le chargement d’une tournée

Si API inaccessible mais tournée locale non synchronisée existante :
    permettre de continuer en mode hors connexion

Si API inaccessible et aucune tournée locale :
    afficher un message d’erreur clair
```

### 01 — Identification livreur

Objectif : identifier le livreur à partir de son code.

Route API associée :

```http
GET /api/livreurs
```

Règles :

- le code livreur est obligatoire ;
- le code doit être reconnu ;
- le nom du livreur doit être affiché après reconnaissance ;
- les données chargées ensuite seront associées à ce livreur ;
- le code livreur ne doit pas être imposé en dur ;
- l’application ne doit pas être limitée au livreur `2`.

### 02 — Choix de la tournée

Objectif : afficher les tournées disponibles pour le livreur et la date du jour.

L’écran affiche :

- le livreur identifié ;
- la date du jour ;
- une liste de tournées disponibles ;
- le nombre de tournées trouvées ;
- un bouton pour continuer avec la tournée sélectionnée.

La date du jour est affichée mais non modifiable.

Une tournée doit être sélectionnée avant de continuer.

### 03 — Confirmation du choix de tournée

Objectif : confirmer la tournée avant son chargement local.

La confirmation appelle :

```http
GET /api/tournees/jour?dateTournee=YYYY-MM-DD&codeTournee=XXXX&codeLivreur=YY
```

Après réception, l’application sauvegarde la tournée dans SQLite.

### 04 — Liste des points de livraison

Objectif : suivre l’avancement de la tournée.

L’écran affiche :

- la liste des points de livraison ;
- le client ;
- le point de livraison ;
- l’adresse ou une information utile ;
- le statut du passage ;
- un badge indiquant l’état du point ;
- les instructions permanentes si elles existent ;
- le commentaire exceptionnel si celui-ci existe ;
- des filtres d’affichage.

Filtres prévus :

- tous ;
- à faire ;
- fait ;
- non fait ;
- anomalie ;
- fermé si l’information existe.

Les données affichées doivent provenir de SQLite après chargement de la tournée.

### 04 bis — Détail point / saisie

Objectif : saisir les informations du passage chez un client.

L’écran affiche :

- les informations principales du point ;
- l’ordre d’arrêt ;
- le client ;
- le point de livraison ;
- la zone ;
- les instructions permanentes ;
- le commentaire exceptionnel ;
- les quantités par article ;
- la valeur prévue par l’expédition si elle existe ;
- une colonne `Livré` ;
- une colonne `Récupéré` ;
- le statut du passage ;
- la précision livreur ;
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

### 05 — Déchargement du camion par client

Objectif : aider le livreur à décharger correctement ce qu’il a récupéré.

Cet écran ne remplace pas la synchronisation.

Il sert uniquement d’aide opérationnelle au dépôt.

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

- qu’il ne reste pas de point à faire dans l’envoi final ;
- que les anomalies ont un commentaire ;
- que les passages non faits ont un commentaire ;
- que les quantités sont valides ;
- que les heures de validation existent pour les points validés.

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

### 07 bis — Cas d’erreur

Objectif : gérer les erreurs de synchronisation de manière compréhensible.

Cas possibles :

- erreur réseau ;
- API indisponible ;
- erreur de validation ;
- tournée déjà synchronisée ;
- doublon d’identifiant de synchronisation ;
- double envoi métier de la même tournée ;
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

## 11. Architecture technique mobile

Structure recommandée :

```text
Pages/
ViewModels/
Models/
Services/
Resources/
Configuration/
```

### 11.1 Pages

Pages principales :

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

### 11.2 ViewModels

ViewModels principaux :

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

### 11.3 Models

Exemples de DTO reçus depuis l’API :

- `LivreurDto`
- `TourneesDisponiblesResponseDto`
- `TourneeResumeDto`
- `TourneeJourDto`
- `ChargementDto`
- `ArticleSaisissableDto`
- `TourneeLigneDto`
- `ClientDto`
- `PointLivraisonDto`
- `TourneeInfoDto`
- `RetourInfoDto`
- `InfosLivreurDto`
- `SaisieMobileDto`
- `QuantiteSaisieMobileDto`

Exemples de DTO envoyés à l’API :

- `SynchronisationTourneeRequest`
- `SynchronisationLivreurRequest`
- `SynchronisationMobileRequest`
- `SynchronisationLigneRequest`
- `SynchronisationClientRequest`
- `SynchronisationPointLivraisonRequest`
- `SynchronisationTourneeInfoRequest`
- `SynchronisationRetourInfoRequest`
- `SynchronisationInfosLivreurRequest`
- `SynchronisationSaisieRequest`
- `SynchronisationQuantiteRequest`

Entités SQLite :

- `LocalTournee`
- `LocalTourneeLigne`
- `LocalTourneeLigneQuantite`

La structure doit rester évolutive pour permettre l’ajout futur de nouveaux articles sans modifier toute l’application.

### 11.4 Services

Services principaux :

- `ApiClient`
- `HealthApiService`
- `LivreursApiService`
- `TourneesApiService`
- `SynchronisationsApiService`
- `DatabaseService`
- `SettingsService`
- `DemoDataService`
- `ConnectivityService`
- `SynchronisationService`
- `AppStateService`

---

## 12. États locaux d’une tournée

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

## 13. Format de chargement de la tournée

Route recommandée :

```http
GET /api/tournees/jour?dateTournee=2026-05-07&codeTournee=4006&codeLivreur=2
```

La date est calculée automatiquement par l’application à partir de la date du jour.

Elle est affichée dans l’interface mais non modifiable par le livreur.

### 13.1 Structure générale

```json
{
  "schemaVersion": "1.2",
  "dateTournee": "2026-05-07",
  "dateModifiable": false,
  "jourTournee": 4,
  "jourLibelle": "Jeudi",
  "codeTournee": "4006",
  "libelleTournee": "TOURNEE EXEMPLE",
  "statutSynchronisation": "NON_ENVOYEE",
  "livreur": {
    "codeLivreur": "2",
    "nomLivreur": "DAVID LEBAS"
  },
  "chargement": {
    "dateGenerationApi": "2026-05-07T07:30:00+02:00",
    "nombrePointsEnvoyes": 1
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
  "lignes": []
}
```

### 13.2 Structure d’une ligne

```json
{
  "idLigneSource": "2026-05-07|4006|4|1058|1|1",
  "ordreArret": 1,
  "horaire": "1",
  "client": {
    "numClient": "1058",
    "nomClient": "EHPAD L EQUAIZIERE",
    "nomAffiche": "EHPAD EQUAIZIERE GARNACHE"
  },
  "pointLivraison": {
    "codePDL": "1",
    "descriptionPDL": "EHPAD EQUAIZIERE GARNACHE",
    "adresseLigne1": "7 RUE JAN ET JOEL MARTEL",
    "adresseLigne2": null,
    "adresseLigne3": "-",
    "ville": "LA GARNACHE",
    "codePostal": "85710"
  },
  "tournee": {
    "codeTournee": "4006",
    "libelleTournee": "TOURNEE EXEMPLE",
    "jourTournee": 4,
    "jourLibelle": "Jeudi",
    "schemaLivraison": "1W1"
  },
  "retour": {
    "jourTourneeRetour": 4,
    "jourRetourLibelle": "Jeudi",
    "codeTourneeRetour": "4006",
    "libelleTourneeRetour": "TOURNEE EXEMPLE"
  },
  "infosLivreur": {
    "instructions": null,
    "commentaireExceptionnel": "Prévoir passage avant 10h ce jour-là.",
    "zoneDechargement": "EHPAD",
    "zoneDechargementAffichee": "EHPAD",
    "zone": "EHPAD",
    "precision": null,
    "cle": null,
    "estFerme": false,
    "dateFermeture": null,
    "motifFermeture": null
  },
  "saisie": {
    "precisionLivreur": null,
    "statutPassage": "A_FAIRE",
    "commentaireLivreur": null,
    "heureValidation": null,
    "estValidee": false,
    "quantites": [
      {
        "codeArticle": "ROLLS",
        "libelle": "Rolls",
        "quantiteLivreePrevue": 2,
        "quantiteLivree": 2,
        "quantiteRecuperee": 0
      },
      {
        "codeArticle": "TAPIS",
        "libelle": "Tapis",
        "quantiteLivreePrevue": null,
        "quantiteLivree": 0,
        "quantiteRecuperee": 0
      },
      {
        "codeArticle": "SACS",
        "libelle": "Sacs",
        "quantiteLivreePrevue": 0,
        "quantiteLivree": 0,
        "quantiteRecuperee": 0
      }
    ]
  }
}
```

---

## 14. Format de synchronisation de fin de journée

Route recommandée :

```http
POST /api/synchronisations
```

### 14.1 Exemple de JSON envoyé

```json
{
  "schemaVersion": "1.2",
  "idSynchronisation": "4e17a871-5fc5-4f49-8d01-4d791a6d9941",
  "dateTournee": "2026-05-07",
  "codeTournee": "4006",
  "libelleTournee": "TOURNEE EXEMPLE",
  "statutSynchronisation": "ENVOYEE",
  "livreur": {
    "codeLivreur": "2",
    "nomLivreur": "DAVID LEBAS"
  },
  "mobile": {
    "nomAppareil": "Samsung A15",
    "versionApplication": "1.0.0",
    "dateChargementMobile": "2026-05-07T07:30:00+02:00",
    "dateEnvoiMobile": "2026-05-07T16:45:00+02:00"
  },
  "commentaireGlobal": null,
  "lignes": [
    {
      "idLigneSource": "2026-05-07|4006|4|1058|1|1",
      "ordreArret": 1,
      "horaire": "1",
      "client": {
        "numClient": "1058",
        "nomClient": "EHPAD L EQUAIZIERE",
        "nomAffiche": "EHPAD EQUAIZIERE GARNACHE"
      },
      "pointLivraison": {
        "codePDL": "1",
        "descriptionPDL": "EHPAD EQUAIZIERE GARNACHE",
        "adresseLigne1": "7 RUE JAN ET JOEL MARTEL",
        "adresseLigne2": null,
        "adresseLigne3": "-",
        "ville": "LA GARNACHE",
        "codePostal": "85710"
      },
      "tournee": {
        "codeTournee": "4006",
        "libelleTournee": "TOURNEE EXEMPLE",
        "jourTournee": 4,
        "jourLibelle": "Jeudi",
        "schemaLivraison": "1W1"
      },
      "retour": {
        "jourTourneeRetour": 4,
        "jourRetourLibelle": "Jeudi",
        "codeTourneeRetour": "4006",
        "libelleTourneeRetour": "TOURNEE EXEMPLE"
      },
      "infosLivreur": {
        "instructions": null,
        "commentaireExceptionnel": "Prévoir passage avant 10h ce jour-là.",
        "zoneDechargement": "EHPAD",
        "zoneDechargementAffichee": "EHPAD",
        "zone": "EHPAD",
        "precision": null,
        "cle": null,
        "estFerme": false,
        "dateFermeture": null,
        "motifFermeture": null
      },
      "saisie": {
        "precisionLivreur": "2 rolls repris au local arrière",
        "statutPassage": "FAIT",
        "commentaireLivreur": null,
        "heureValidation": "2026-05-07T09:12:00+02:00",
        "estValidee": true,
        "quantites": [
          {
            "codeArticle": "ROLLS",
            "libelle": "Rolls",
            "quantiteLivreePrevue": 2,
            "quantiteLivree": 3,
            "quantiteRecuperee": 2
          },
          {
            "codeArticle": "TAPIS",
            "libelle": "Tapis",
            "quantiteLivreePrevue": null,
            "quantiteLivree": 1,
            "quantiteRecuperee": 0
          },
          {
            "codeArticle": "SACS",
            "libelle": "Sacs",
            "quantiteLivreePrevue": 0,
            "quantiteLivree": 0,
            "quantiteRecuperee": 0
          }
        ]
      }
    }
  ]
}
```

Cette structure conserve :

- la quantité livrée prévue par l’expédition ;
- la quantité livrée réelle saisie ou confirmée par le livreur ;
- la quantité récupérée réelle saisie par le livreur.

Elle reste évolutive : si l’entreprise ajoute plus tard des vêtements, draps, serviettes ou d’autres articles, l’API pourra ajouter un nouvel objet dans `quantites[]` sans modifier toute l’application.

---

## 15. Réponses attendues de l’API

### 15.1 Succès

```json
{
  "code": "SUCCESS",
  "message": "Synchronisation enregistrée avec succès.",
  "idSynchronisation": "4e17a871-5fc5-4f49-8d01-4d791a6d9941",
  "dateReceptionApi": "2026-05-07T17:45:12+02:00",
  "dateTournee": "2026-05-07",
  "codeTournee": "4006",
  "codeLivreur": "2",
  "nombreLignesRecues": 1,
  "nombreQuantitesRecues": 3
}
```

### 15.2 Erreur de validation

```json
{
  "code": "VALIDATION_ERROR",
  "message": "La synchronisation contient des données invalides.",
  "erreurs": [
    {
      "champ": "lignes[0].saisie.commentaireLivreur",
      "message": "Le commentaire livreur est obligatoire pour le statut ANOMALIE ou NON_FAIT."
    }
  ]
}
```

### 15.3 Double envoi

```json
{
  "code": "TOURNEE_ALREADY_SENT",
  "message": "Cette tournée a déjà été envoyée pour ce livreur et cette date.",
  "dateTournee": "2026-05-07",
  "codeTournee": "4006",
  "codeLivreur": "2"
}
```

### 15.4 Doublon technique

```json
{
  "code": "SYNCHRONISATION_ALREADY_EXISTS",
  "message": "Cette synchronisation a déjà été reçue."
}
```

### 15.5 Erreur serveur

```json
{
  "code": "SERVER_ERROR",
  "message": "Une erreur technique est survenue pendant le traitement de la synchronisation."
}
```

---

## 16. Règles métier intégrées côté mobile

### 16.1 Identification

- le livreur doit obligatoirement saisir ou sélectionner son code ;
- le code livreur doit être reconnu avant le chargement d’une tournée ;
- le nom du livreur est affiché après identification ;
- les données sont associées au livreur identifié.

### 16.2 Tournée

- la date du jour est affichée mais non modifiable ;
- le choix d’une tournée est obligatoire ;
- les données sont associées à la tournée choisie ;
- une tournée chargée est sauvegardée localement ;
- une tournée locale non synchronisée doit être reprise automatiquement au redémarrage ;
- une tournée synchronisée est verrouillée ;
- une tournée déjà envoyée ne doit pas être renvoyée.

### 16.3 Quantités

- les quantités doivent être des entiers ;
- les quantités doivent être positives ou nulles ;
- les quantités négatives sont interdites ;
- les quantités livrées et récupérées doivent être séparées ;
- les récupérations ne doivent jamais être saisies sous forme négative ;
- les champs de quantité doivent utiliser un clavier numérique ;
- les boutons `+` et `-` ne sont pas retenus pour la première version.

### 16.4 Statuts

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

### 16.5 Synchronisation

- la synchronisation se fait uniquement depuis le récapitulatif ;
- l’application doit afficher un avertissement avant l’envoi ;
- après succès, la tournée est verrouillée ;
- en cas d’échec réseau, les données restent locales ;
- en cas de doublon détecté par l’API, le renvoi est bloqué ;
- la protection finale contre les doubles envois est gérée côté API ;
- les corrections après synchronisation sont réservées à l’administration ou à un responsable habilité ;
- les corrections après synchronisation doivent être tracées.

### 16.6 Validation avant envoi

Avant d’appeler `POST /api/synchronisations`, l’application doit vérifier :

- `schemaVersion = "1.2"` ;
- `idSynchronisation` renseigné ;
- `dateTournee` renseignée ;
- `codeTournee` renseigné ;
- `livreur.codeLivreur` renseigné ;
- objet `mobile` renseigné ;
- `lignes[]` non vide ;
- `idLigneSource` renseigné pour chaque ligne ;
- `idLigneSource` unique dans la requête ;
- objet `saisie` renseigné pour chaque ligne ;
- `saisie.quantites[]` non vide pour chaque ligne ;
- `codeArticle` unique dans une même ligne ;
- quantités positives ou nulles ;
- `A_FAIRE` absent de l’envoi final ;
- `NON_FAIT` avec commentaire obligatoire ;
- `ANOMALIE` avec commentaire obligatoire ;
- `estValidee = true` pour chaque ligne envoyée ;
- `heureValidation` renseignée pour chaque ligne validée.

---

## 17. Règles d’ergonomie

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
- aucune manipulation complexe pendant la tournée ;
- blocage du bouton retour Android sur les pages critiques ;
- pas de bouton libre de réinitialisation de la tournée en production.

Les écrans doivent être utilisables :

- rapidement ;
- avec une seule main si possible ;
- dans un environnement de travail réel ;
- avec une luminosité variable ;
- par des utilisateurs qui ne sont pas informaticiens.

---

## 18. Points importants à respecter pour la première version

Priorités :

- identification livreur ;
- choix de la tournée ;
- chargement depuis l’API ;
- sauvegarde SQLite ;
- reprise d’une tournée locale non synchronisée ;
- consultation hors connexion ;
- affichage des instructions et commentaires exceptionnels ;
- affichage des quantités préremplies ;
- saisie livré / récupéré ;
- validation des statuts ;
- commentaire obligatoire si nécessaire ;
- récapitulatif ;
- synchronisation API ;
- verrouillage après succès ;
- gestion claire des erreurs ;
- blocage du bouton retour Android.

Fonctionnalités non prioritaires :

- statistiques avancées ;
- recherche complexe ;
- administration mobile ;
- modification d’une tournée après synchronisation ;
- gestion multi-profils complexe ;
- optimisation automatique de l’ordre de tournée.

Le plus important est d’obtenir une application fiable, simple et cohérente avec le fonctionnement réel des fiches de tournée.

---

## 19. Mode démonstration

Le mode démonstration sert uniquement au développement.

Il peut aider à valider :

- la navigation ;
- les écrans ;
- les boutons ;
- les transitions ;
- la saisie locale simulée ;
- la structure générale de l’application.

Il ne doit pas remplacer les tests réels avec l’API.

À terme :

```text
DemoDataService = uniquement pour développement
ApiClient + DatabaseService = fonctionnement réel
```

En production, le mode démonstration doit être désactivé ou inaccessible pour les livreurs.

---

## 20. Passage progressif du mode démo aux données réelles

Ordre recommandé :

### Étape 1 — Revenir à une version stable

Objectif :

- l’application compile ;
- l’application se lance ;
- la navigation fonctionne.

### Étape 2 — Centraliser la configuration API

Fichier :

```text
Configuration/AppConfig.cs
```

Objectif :

- ne changer l’adresse API qu’à un seul endroit ;
- garder `schemaVersion = "1.2"` à un seul endroit.

### Étape 3 — Stabiliser les services API

Objectif :

- tester `/api/health` ;
- charger `/api/livreurs` ;
- charger `/api/tournees/disponibles` ;
- charger `/api/tournees/jour` ;
- ne pas mélanger DTO API et données de démonstration.

### Étape 4 — Charger les vrais livreurs

Objectif :

- remplacer le livreur codé en dur ;
- afficher tous les livreurs réels ;
- stocker le livreur sélectionné.

### Étape 5 — Charger une vraie tournée

Objectif :

- sélectionner un code tournée ;
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

- générer le JSON final v1.2 ;
- envoyer à l’API ;
- gérer les réponses ;
- verrouiller la tournée après succès.

---

## 21. Commandes rapides à retenir

### Lancer l’API pour test téléphone avec ADB reverse

```powershell
cd "C:\Users\Logistique\Downloads\Stage\ProjetMobileTournee\backend\API-ASP.NET-Core"
dotnet run --urls "http://127.0.0.1:5000"
```

### Vérifier l’API depuis le PC

```powershell
curl.exe "http://127.0.0.1:5000/api/health"
curl.exe "http://127.0.0.1:5000/api/livreurs"
curl.exe "http://127.0.0.1:5000/api/tournees/disponibles?dateTournee=2026-05-07&codeLivreur=2"
curl.exe "http://127.0.0.1:5000/api/tournees/jour?dateTournee=2026-05-07&codeTournee=4006&codeLivreur=2"
```

### Vérifier le téléphone et activer ADB reverse

```powershell
cd "C:\Program Files (x86)\Android\android-sdk\platform-tools"
.\adb.exe kill-server
.\adb.exe start-server
.\adb.exe devices -l
.\adb.exe reverse --remove-all
.\adb.exe reverse tcp:5000 tcp:5000
.\adb.exe reverse --list
```

### Tester l’API depuis le téléphone

```powershell
.\adb.exe shell am start -a android.intent.action.VIEW -d "http://127.0.0.1:5000/api/health"
```

### Compiler et lancer l’application mobile

```powershell
cd "C:\Users\Logistique\Downloads\Stage\ProjetMobileTournee\MobileSLI"
dotnet restore
dotnet build -f net10.0-android
dotnet run -f net10.0-android -c Debug -p:AdbTarget=-d
```

### Nettoyer l’installation mobile si la base SQLite locale pose problème

```powershell
cd "C:\Program Files (x86)\Android\android-sdk\platform-tools"
.\adb.exe uninstall fr.sli.mobiletournee
```

---

## 22. Conclusion

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
↓
Verrouillage
```

Le mode démonstration est utile pour démarrer, mais il doit être progressivement remplacé par :

```text
API réelle + stockage local SQLite + règles métier contrôlées
```

Le développement doit continuer étape par étape afin d’éviter de casser une application qui compile et se lance déjà.
