# Préparation HTTPS Android — MobileSLI

## Objectif

Préparer l'application mobile MobileSLI pour appeler l'API en HTTPS sur le réseau interne :

```text
https://srvapi1.sli.local
```

Le certificat serveur doit correspondre au nom DNS `srvapi1.sli.local`. Ne pas tester le HTTPS final avec `https://192.168.1.233`, car le certificat ne sera pas prévu pour cette adresse IP.

## Fichiers modifiés ou créés

```text
Configuration/AppConfig.cs
Services/SettingsService.cs
Platforms/Android/AndroidManifest.xml
Platforms/Android/Resources/xml/network_security_config.xml
.gitignore
scripts/security/copy-android-ca-local.ps1
docs/05-securite/https-mobile-android.md
```

## Ce qui n'est volontairement pas fourni

Le zip ne contient aucun certificat et aucune clé.

Ne jamais ajouter au dépôt :

```text
mobilesli-root-ca.key
srvapi1.sli.local.key
srvapi1.sli.local.pfx
*.key
*.pfx
*.p12
*.jks
*.keystore
*.pem
```

Le fichier `mobilesli_root_ca.crt` est le certificat public de la CA. Il ne contient normalement pas la clé privée, mais il révèle tout de même une information interne. Pour ce projet, il est donc traité comme un fichier local non versionné.

## Application du patch

Depuis ta machine, extraire le zip à la racine du dépôt mobile :

```powershell
cd "C:\Users\Logistique\Downloads\Stage\ProjetMobileTournee\mobile\MobileSLI"
```

Puis copier les fichiers du zip en écrasant les fichiers existants.

Vérifier ensuite :

```powershell
git status --short
```

Résultat attendu avant ajout du certificat local :

```text
 M .gitignore
 M Configuration/AppConfig.cs
 M Platforms/Android/AndroidManifest.xml
 M Services/SettingsService.cs
?? Platforms/Android/Resources/xml/network_security_config.xml
?? docs/05-securite/https-mobile-android.md
?? scripts/security/copy-android-ca-local.ps1
```

## Ajout local de la CA Android avant build

Le fichier attendu localement est :

```text
Platforms/Android/Resources/raw/mobilesli_root_ca.crt
```

Source locale prévue :

```text
C:\Users\Logistique\Downloads\Stage\ProjetMobileTournee\_certificats-locaux-ne-pas-commit\android\mobilesli_root_ca.crt
```

Commande directe :

```powershell
cd "C:\Users\Logistique\Downloads\Stage\ProjetMobileTournee\mobile\MobileSLI"

New-Item -ItemType Directory -Path ".\Platforms\Android\Resources\raw" -Force

Copy-Item `
  -Path "C:\Users\Logistique\Downloads\Stage\ProjetMobileTournee\_certificats-locaux-ne-pas-commit\android\mobilesli_root_ca.crt" `
  -Destination ".\Platforms\Android\Resources\raw\mobilesli_root_ca.crt" `
  -Force
```

Commande avec script fourni :

```powershell
cd "C:\Users\Logistique\Downloads\Stage\ProjetMobileTournee\mobile\MobileSLI"
Set-ExecutionPolicy -Scope Process Bypass -Force
.\scripts\security\copy-android-ca-local.ps1
```

## Vérifier que le certificat ne sera pas push

Après copie du certificat :

```powershell
git check-ignore -v -- "Platforms/Android/Resources/raw/mobilesli_root_ca.crt"
```

Résultat attendu : une ligne qui indique que `.gitignore` ignore bien le fichier.

Si la commande ne retourne rien, ne pas faire de commit. Corriger `.gitignore` avant de continuer.

Vérifier aussi l'état Git :

```powershell
git status --short --ignored
```

Le certificat doit apparaître en ignoré, pas en fichier à commiter.

## Ajout Git recommandé

Ne pas utiliser `git add .` dans cette étape.

Utiliser explicitement :

```powershell
git add .gitignore

git add Configuration/AppConfig.cs

git add Services/SettingsService.cs

git add Platforms/Android/AndroidManifest.xml

git add Platforms/Android/Resources/xml/network_security_config.xml

git add docs/05-securite/https-mobile-android.md

git add scripts/security/copy-android-ca-local.ps1
```

Puis contrôler ce qui est préparé :

```powershell
git diff --cached --name-only
```

La liste ne doit pas contenir :

```text
Platforms/Android/Resources/raw/mobilesli_root_ca.crt
*.key
*.pfx
*.p12
*.jks
*.keystore
*.pem
```

## Build local

Le build Android échouera si `Platforms/Android/Resources/raw/mobilesli_root_ca.crt` n'existe pas localement, car `network_security_config.xml` référence `@raw/mobilesli_root_ca`.

Commandes :

```powershell
cd "C:\Users\Logistique\Downloads\Stage\ProjetMobileTournee\mobile\MobileSLI"

dotnet clean

dotnet restore

dotnet build -f net10.0-android -c Release

dotnet publish -f net10.0-android -c Release
```

## Installation téléphone

Trouver l'APK :

```powershell
Get-ChildItem ".\bin\Release\net10.0-android" -Recurse -Filter "*.apk" |
  Sort-Object LastWriteTime -Descending |
  Select-Object -First 5 FullName, LastWriteTime
```

Installer :

```powershell
cd "C:\Program Files (x86)\Android\android-sdk\platform-tools"

.\adb.exe devices -l
.\adb.exe uninstall fr.sli.mobiletournee
.\adb.exe install -r "CHEMIN_COMPLET_DE_L_APK.apk"
```

Si tu ne désinstalles pas l'application, la correction `SettingsService` migre automatiquement l'ancienne URL enregistrée `http://srvapi1.sli.local:5000` vers `https://srvapi1.sli.local`.

## Test fonctionnel

Sur le téléphone :

```text
URL API = https://srvapi1.sli.local
```

Tester la connexion depuis l'écran d'accueil.

Logs utiles si échec :

```powershell
cd "C:\Program Files (x86)\Android\android-sdk\platform-tools"

.\adb.exe logcat -c
.\adb.exe logcat | findstr /i "SSLHandshake CertPathValidator Trust anchor MobileSLI NetworkSecurity"
```

Erreur typique si la CA n'est pas chargée :

```text
Trust anchor for certification path not found
```

## Phase de transition

La configuration fournie garde temporairement :

```text
android:usesCleartextTraffic="true"
cleartextTrafficPermitted="true"
```

Raison : conserver un secours HTTP explicite pendant les tests terrain, par exemple :

```text
http://srvapi1.sli.local:5000
```

Quand les tests HTTPS Android sont validés sur téléphone réel, durcir ensuite :

```xml
<domain-config cleartextTrafficPermitted="false">
```

Et dans `AndroidManifest.xml` :

```xml
android:usesCleartextTraffic="false"
```

Ne pas faire ce durcissement avant validation complète sur téléphone réel.
