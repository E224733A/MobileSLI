# Documentation fonctionnelle et technique — Application mobile MobileSLI

## Objectif de l’application mobile

L’application mobile **MobileSLI** a pour objectif de remplacer progressivement la fiche papier utilisée par les livreurs pour les tournées quotidiennes.

## Documentation choix camion / trajet mobile 1.3

Le flux camion / trajet mobile 1.3 est documenté dans les fichiers suivants :

| Fichier | Rôle |
|---|---|
| `docs/00-prompts/lot-0-tests-documentation-mobile-1.3.md` | Prompt corrigé du LOT 0, strictement limité au dépôt mobile |
| `docs/04-tests/Mobile/matrice-tests-camion-trajet-mobile-1.3.md` | Matrice de tests mobile pour les scénarios `MOB-CAM-001` à `MOB-CAM-021` |
| `docs/04-tests/Mobile/rapport-lot-0-camion-trajet-1.3.md` | Rapport d’inspection et de correction du LOT 0 |

Décision contractuelle mobile :

```text
schemaVersion = "1.3" uniquement
schemaVersion = "1.2" refusé côté mobile
```

Ce cadrage concerne uniquement le dépôt mobile **MobileSLI**.

Aucune modification serveur API ASP.NET Core, SQL Server, route serveur ou DTO serveur n’est incluse dans ce lot.

## Configuration réseau

L’application communique uniquement avec l’API ASP.NET Core.

Elle ne communique jamais directement avec SQL Server.

### Mode actuel recommandé en développement

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

### Pourquoi ne pas utiliser l’IP du PC dans ce cas ?

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

### Modes d’adresse API selon le contexte

| Contexte | Adresse à utiliser dans l’application | Commande API recommandée |
|---|---|---|
| Téléphone physique avec `adb reverse` | `http://127.0.0.1:5000` | `dotnet run --urls "http://127.0.0.1:5000"` |
| Téléphone physique sans `adb reverse` | `http://IP_DU_PC:5000` | `dotnet run --urls "http://0.0.0.0:5000"` |
| Émulateur Android | `http://10.0.2.2:5000` | `dotnet run --urls "http://127.0.0.1:5000"` |
| Installation durable en entreprise | `https://nom-dns-interne` ou IP fixe | IIS / HTTPS |
| Production recommandée | HTTPS avec certificat reconnu | IIS / HTTPS |

### Mode téléphone physique sans `adb reverse`

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

### Compiler et lancer l’application mobile

```powershell
cd "C:\Users\Logistique\Downloads\Stage\ProjetMobileTournee\mobile\MobileSLI"

Select-String -Path ".\Configuration\AppConfig.cs" -Pattern "ApiBaseUrl"

dotnet restore
dotnet build -f net10.0-android
dotnet publish -f net10.0-android -c Debug -p:AndroidPackageFormat=apk

$apk = Get-ChildItem ".\bin\Debug\net10.0-android\publish" -Filter "*.apk" -Recurse |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1 -ExpandProperty FullName

$apk
```

### Nettoyer l’installation mobile si la base SQLite locale pose problème

```powershell
cd "C:\Program Files (x86)\Android\android-sdk\platform-tools"
.\adb.exe uninstall fr.sli.mobiletournee
```

### Installer : 

```powershell
cd "C:\Program Files (x86)\Android\android-sdk\platform-tools"
.\adb.exe install -r $apk
```


