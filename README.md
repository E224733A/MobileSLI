# Documentation fonctionnelle et technique — Application mobile MobileSLI

## Objectif de l'application mobile

L'application mobile **MobileSLI** remplace progressivement la fiche papier utilisée par les livreurs pendant les tournées quotidiennes.

Elle permet de :

- identifier le livreur ;
- choisir le camion utilisé ;
- saisir le kilométrage départ ;
- charger la tournée du jour depuis l'API ;
- travailler localement sur le téléphone pendant la tournée ;
- consulter le détail des points de livraison ;
- ouvrir le lien d'adresse de livraison lorsqu'il est fourni ;
- gérer les clients fermés ;
- saisir les quantités livrées et récupérées, dont `ROLLS_VIDES` ;
- saisir le kilométrage arrivée ;
- synchroniser la tournée finale vers l'API en contrat mobile `schemaVersion = "1.3"`.

L'application ne communique jamais directement avec SQL Server. Elle communique uniquement avec l'API ASP.NET Core.

## État final validé

Date de validation technique : 09/06/2026.

Tag Git :

```text
v1.3-mobile-https-final
```

Commit associé au tag :

```text
32174a91208d55c299d9cee8555da8599f8639bb
```

État validé d'après les contrôles locaux réalisés avant tag :

| Élément | État |
|---|---|
| API active | `https://srvapi1.sli.local` |
| ApplicationId Android | `fr.sli.mobiletournee` |
| Framework | `.NET MAUI` / `net10.0-android` |
| Android target/compile SDK | `36` |
| Contrat mobile | `schemaVersion = "1.3"` uniquement |
| Build Release | validé localement |
| Publish Release | validé localement |
| APK Release | généré et installé localement |
| HTTPS final strict Android | validé localement |
| ADB DNS/ping téléphone | validé localement |
| Bypass TLS connu | aucun détecté par le script de vérification |
| Fichier sensible dans le projet mobile | aucun détecté par le script de vérification |

Avertissement restant connu :

```text
XA0141 Android 16 / SQLitePCLRaw.lib.e_sqlite3.android 2.1.2
```

Cet avertissement n'a pas bloqué le build ni l'installation locale de l'APK, mais il reste à traiter pour une compatibilité Android 16 totalement propre.

## Configuration HTTPS finale

URL API active dans le code :

```text
https://srvapi1.sli.local
```

Fichier concerné :

```text
Configuration/AppConfig.cs
```

Valeur attendue :

```csharp
public const string ApiBaseUrl = "https://srvapi1.sli.local";
```

Le certificat serveur doit correspondre au nom DNS :

```text
srvapi1.sli.local
```

Ne pas utiliser d'adresse IP pour le HTTPS final, notamment :

```text
https://192.168.1.233
```

## Certificat Android local

La CA publique Android est attendue localement dans le projet mobile avant build :

```text
Platforms/Android/Resources/raw/mobilesli_root_ca.crt
```

Source locale prévue hors dépôt :

```text
C:\Users\Logistique\Downloads\Stage\ProjetMobileTournee\_certificats-locaux-ne-pas-commit\android\mobilesli_root_ca.crt
```

CA validée :

```text
Subject    : CN=MobileSLI Root CA, OU=MobileSLI, O=SLI, L=Nantes, S=France, C=FR
Thumbprint : EEB5ADC4A3B04330FCD1462B652819646EE6B22F
NotBefore  : 06/05/2026 14:58:37
NotAfter   : 31/05/2046 14:58:37
```

Le certificat local est ignoré par Git :

```powershell
git check-ignore -v -- "Platforms/Android/Resources/raw/mobilesli_root_ca.crt"
```

Résultat attendu : une ligne issue de `.gitignore` indiquant que le fichier est ignoré.

Ne jamais versionner :

```text
mobilesli_root_ca.crt
mobilesli-root-ca.key
srvapi1.sli.local.key
srvapi1.sli.local.pfx
*.key
*.pfx
*.p12
*.jks
*.keystore
*.pem
*.cer
*.csr
```

Ne pas utiliser :

```powershell
git add .
```

Préférer des ajouts ciblés avec `git add <chemin-du-fichier>`.

## Configuration Android réseau

Fichiers HTTPS Android :

```text
Platforms/Android/AndroidManifest.xml
Platforms/Android/Resources/xml/network_security_config.xml
```

État final attendu :

```xml
android:usesCleartextTraffic="false"
```

```xml
<domain-config cleartextTrafficPermitted="False">
    <domain includeSubdomains="false">srvapi1.sli.local</domain>
    <trust-anchors>
        <certificates src="system" />
        <certificates src="@raw/mobilesli_root_ca" />
    </trust-anchors>
</domain-config>
```

Le mode HTTP de transition n'est plus l'état final attendu.

Les anciennes URL HTTP peuvent encore apparaître dans des commentaires ou exemples de développement, mais ne doivent pas être utilisées comme valeur active de production :

```text
http://127.0.0.1:5000
http://10.0.2.2:5000
http://srvapi1.sli.local:5000
http://IP_DU_PC:5000
```

## Flux fonctionnel mobile 1.3

Flux principal :

```text
Accueil
→ Identification livreur
→ Choix camion
→ Choix tournée
→ Confirmation tournée
→ Liste des points de livraison
→ Détail point de livraison
→ Récapitulatif
→ Synchronisation finale
```

Le choix camion est obligatoire avant le chargement de la tournée.

Le kilométrage départ est obligatoire au moment du choix camion.

Le kilométrage arrivée est obligatoire avant l'envoi de la tournée.

Le payload final envoyé à l'API contient une section `trajet` avec :

```text
camion
kilometrageDepart
kilometrageArrivee
dateDepartMobile
dateArriveeMobile
```

## Contrat mobile 1.3

Décision contractuelle mobile :

```text
schemaVersion = "1.3" uniquement
schemaVersion = "1.2" refusé côté mobile
```

La liste des camions est chargée depuis :

```text
GET /api/camions/disponibles
```

La réponse camion doit être en `schemaVersion = "1.3"`.

Le mobile filtre les camions exploitables :

- camion non nul ;
- `estActif = true` ;
- `idCamion` renseigné ;
- `codeCamion` renseigné.

## Persistance locale SQLite

La base locale mobile est :

```text
mobile_sli.db3
```

Elle permet de conserver la tournée sur le téléphone pendant le travail terrain.

Champs trajet persistés sur `LocalTournee` :

```text
IdCamion
CodeCamion
LibelleCamion
Immatriculation
KilometrageDepart
KilometrageArrivee
DateDepartMobile
DateArriveeMobile
```

Purge locale actuellement documentée par le code :

| Type de tournée locale | Rétention |
|---|---:|
| Tournées synchronisées | 7 jours |
| Tournées abandonnées localement | 30 jours |

Les tournées expirées sont verrouillées localement et ne doivent pas être reprises comme une tournée du jour.

## Éléments de maintenance encore visibles dans l'application

Le code actuel conserve des éléments utiles à la maintenance terrain :

- champ d'adresse API sur l'écran d'accueil ;
- bouton d'enregistrement de l'adresse API ;
- bouton de test de connexion ;
- bloc diagnostic ;
- export de la base SQLite locale.

Ces éléments sont présents dans la version taguée. Pour une version livreur plus stricte, prévoir un lot séparé pour cacher ou protéger ces fonctions de maintenance sans modifier le contrat API.

## Commandes de build production

Depuis le dépôt mobile :

```powershell
cd "C:\Users\Logistique\Downloads\Stage\ProjetMobileTournee\mobile\MobileSLI"

# Vérifier l'URL active
Select-String -Path ".\Configuration\AppConfig.cs" -Pattern "ApiBaseUrl"

# Build production
dotnet clean
dotnet restore
dotnet build -f net10.0-android -c Release
dotnet publish -f net10.0-android -c Release
```

Trouver l'APK Release :

```powershell
$apk = Get-ChildItem ".\bin\Release\net10.0-android" -Recurse -Filter "*.apk" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1 -ExpandProperty FullName

$apk
```

Installer sur téléphone :

```powershell
cd "C:\Program Files (x86)\Android\android-sdk\platform-tools"
.\adb.exe devices -l
.\adb.exe install -r $apk
```

## Vérifications HTTPS finales

Depuis le dossier de maintenance mobile :

```powershell
cd "C:\Users\Logistique\Downloads\Stage\ProjetMobileTournee\maintenance\Mobile"
Set-ExecutionPolicy -Scope Process Bypass -Force

.\Verify-MobileSLI-AndroidHttps.ps1 -FinalHttpsOnly
.\Verify-MobileSLI-AndroidHttps.ps1 -RunAdbChecks
```

Résultat attendu :

```text
[OK] Verification HTTPS Android terminee.
```

Les vérifications ADB doivent confirmer :

- téléphone détecté ;
- Private DNS Android non bloquant ou désactivé ;
- résolution de `srvapi1.sli.local` vers `192.168.1.233` ;
- ping vers `srvapi1.sli.local` sans perte.

## Tests fonctionnels à rejouer après installation APK

À tester manuellement sur téléphone réel :

1. ouverture de l'application ;
2. test connexion dépôt ;
3. identification livreur ;
4. chargement de la liste des camions ;
5. choix camion ;
6. saisie kilométrage départ ;
7. choix tournée ;
8. chargement tournée ;
9. détail point de livraison ;
10. lien adresse livraison ;
11. client fermé ;
12. saisie `ROLLS_VIDES` ;
13. récapitulatif ;
14. saisie kilométrage arrivée ;
15. synchronisation finale ;
16. comportement Wi-Fi coupé ;
17. comportement API indisponible.

Les tests ci-dessus doivent être notés dans le dossier de recette si la version est destinée à être remise aux livreurs.

## Documentation complémentaire

| Fichier | Rôle |
|---|---|
| `docs/05-securite/https-mobile-android.md` | Procédure HTTPS Android finale |
| `docs/04-tests/Mobile/matrice-tests-camion-trajet-mobile-1.3.md` | Matrice de tests camion / trajet mobile 1.3 |
| `docs/04-tests/Mobile/rapport-audit-final-mobile-1.3-https.md` | Rapport d'audit documentaire final mobile 1.3 HTTPS |
| `docs/04-tests/Mobile/rapport-lot-0-camion-trajet-1.3.md` | Rapport historique du cadrage LOT 0 |
| `docs/00-prompts/lot-0-tests-documentation-mobile-1.3.md` | Prompt historique du LOT 0 |

## Commandes Git utiles avant commit

```powershell
git status --short --ignored
git check-ignore -v -- "Platforms/Android/Resources/raw/mobilesli_root_ca.crt"
git diff --name-only
```

Avant de pousser une nouvelle correction documentaire, vérifier qu'aucun fichier ignoré ou sensible n'est ajouté au commit.
