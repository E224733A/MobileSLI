# HTTPS Android final — MobileSLI

## Objectif

Cette documentation décrit l'état final validé de l'application mobile MobileSLI pour l'accès HTTPS à l'API interne.

URL API officielle :

```text
https://srvapi1.sli.local
```

Le certificat serveur doit correspondre au nom DNS `srvapi1.sli.local`.
Ne pas utiliser l'adresse IP pour le HTTPS final.

## État validé

Tag de référence :

```text
v1.3-mobile-https-final
```

Commit de référence :

```text
32174a91208d55c299d9cee8555da8599f8639bb
```

Validation locale communiquée :

```text
Build Release : OK
Publish Release : OK
APK Release installé : OK
Verify-MobileSLI-AndroidHttps.ps1 -FinalHttpsOnly : OK
Verify-MobileSLI-AndroidHttps.ps1 -RunAdbChecks : OK
```

Avertissement restant :

```text
XA0141 Android 16 / SQLitePCLRaw.lib.e_sqlite3.android 2.1.2
```

## Fichiers concernés

```text
Configuration/AppConfig.cs
Services/SettingsService.cs
Services/Api/ApiClient.cs
Platforms/Android/AndroidManifest.xml
Platforms/Android/Resources/xml/network_security_config.xml
Platforms/Android/Resources/raw/mobilesli_root_ca.crt
.gitignore
scripts/security/copy-android-ca-local.ps1
```

## Configuration active

Dans `Configuration/AppConfig.cs`, l'URL active doit rester :

```csharp
public const string ApiBaseUrl = "https://srvapi1.sli.local";
```

La version de contrat mobile doit rester :

```csharp
public const string SchemaVersion = "1.3";
```

Les anciennes URL HTTP peuvent être conservées uniquement comme commentaires de développement ou comme historique contrôlé. Elles ne doivent pas redevenir la valeur active de `ApiBaseUrl`.

## Configuration Android finale

Dans `Platforms/Android/AndroidManifest.xml`, l'application doit rester configurée ainsi :

```xml
android:usesCleartextTraffic="false"
android:networkSecurityConfig="@xml/network_security_config"
```

Dans `Platforms/Android/Resources/xml/network_security_config.xml`, le domaine doit rester configuré sans trafic clair :

```xml
<domain-config cleartextTrafficPermitted="False">
    <domain includeSubdomains="false">srvapi1.sli.local</domain>
```

La configuration doit référencer les certificats système et la CA Android intégrée localement :

```xml
<certificates src="system" />
<certificates src="@raw/mobilesli_root_ca" />
```

## CA Android locale

Le fichier attendu localement est :

```text
Platforms/Android/Resources/raw/mobilesli_root_ca.crt
```

Ce fichier est nécessaire pour compiler et tester l'application, mais il ne doit pas être versionné dans Git.

Vérifier qu'il est bien ignoré :

```powershell
cd "C:\Users\Logistique\Downloads\Stage\ProjetMobileTournee\mobile\MobileSLI"
git check-ignore -v -- "Platforms/Android/Resources/raw/mobilesli_root_ca.crt"
```

Vérifier aussi l'état Git :

```powershell
git status --short --ignored
```

Le certificat doit apparaître en ignoré, pas en fichier à commiter.

## Contrôle HTTPS final

Depuis le dossier maintenance mobile :

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

Le contrôle final doit confirmer :

```text
CA Android présente
AppConfig.cs contient l'URL HTTPS cible active
network_security_config.xml ne permet pas le trafic clair pour le domaine configuré
AndroidManifest.xml ne permet pas le trafic clair globalement
aucun fichier sensible interdit détecté
aucun contournement TLS connu détecté
aucune référence API active interdite détectée
```

## Build Release

Depuis le dépôt mobile :

```powershell
cd "C:\Users\Logistique\Downloads\Stage\ProjetMobileTournee\mobile\MobileSLI"

dotnet clean
dotnet restore
dotnet build -f net10.0-android -c Release
dotnet publish -f net10.0-android -c Release
```

Trouver l'APK Release :

```powershell
Get-ChildItem ".\bin\Release\net10.0-android" -Recurse -Filter "*.apk" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 5 FullName, LastWriteTime
```

Installer l'APK :

```powershell
$apk = Get-ChildItem ".\bin\Release\net10.0-android" -Recurse -Filter "*.apk" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1 -ExpandProperty FullName

cd "C:\Program Files (x86)\Android\android-sdk\platform-tools"
.\adb.exe devices -l
.\adb.exe install -r $apk
```

## Tests fonctionnels à rejouer

Après installation de l'APK, tester dans l'application :

```text
ouverture de l'application
connexion API
chargement tournée
choix camion
choix tournée
kilométrage départ
liste des points de livraison
détail point de livraison
lien adresse livraison
client fermé
ROLLS_VIDES
kilométrage arrivée
synchronisation finale
comportement Wi-Fi coupé
comportement API indisponible
```

## Règles à conserver

Ne pas réactiver le mode HTTP transition.

Ne pas remplacer `srvapi1.sli.local` par une adresse IP.

Ne pas ajouter de contournement TLS.

Ne pas versionner le certificat Android local.

Ne pas utiliser `git add .` pour les opérations de maintenance sensible.

## Statut documentaire

Ce fichier remplace l'ancienne documentation de transition HTTPS Android.

Le fichier `docs/05-securite/https-mobile-android-final.md` reste une synthèse courte de l'état final validé.
