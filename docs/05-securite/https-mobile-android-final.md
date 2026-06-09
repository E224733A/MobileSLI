# Synthèse HTTPS mobile finale

État de référence : `v1.3-mobile-https-final`.

URL API active : `https://srvapi1.sli.local`.

Le mode HTTP de transition n'est plus l'état final attendu.

Contrôles locaux communiqués avant cette mise à jour :

- build Release OK ;
- publish Release OK ;
- APK Release installé ;
- vérification HTTPS finale OK ;
- vérification ADB DNS/ping OK.

Tests non exécutés par cette correction documentaire :

```text
dotnet clean : non exécuté
dotnet restore : non exécuté
dotnet build Release : non exécuté
dotnet publish Release : non exécuté
installation APK : non exécuté
tests téléphone : non exécuté
```
