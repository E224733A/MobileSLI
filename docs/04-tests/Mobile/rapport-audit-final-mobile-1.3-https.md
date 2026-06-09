# Rapport d'audit final MobileSLI 1.3 HTTPS

Date : 2026-06-09

## Résumé

Le dépôt mobile a été inspecté après le tag `v1.3-mobile-https-final`.

## Fichiers inspectés

- `README.md`
- `Configuration/AppConfig.cs`
- `Services/SettingsService.cs`
- `Services/Api/ApiClient.cs`
- `Services/Api/CamionsApiService.cs`
- `Models/CamionDto.cs`
- `Models/SynchronisationTrajetRequest.cs`
- `Models/LocalEntities.cs`
- `Services/SynchronisationService.cs`
- `Services/DatabaseService.cs`
- `ViewModels/AccueilViewModel.cs`
- `ViewModels/ChoixCamionViewModel.cs`
- `ViewModels/ConfirmationTourneeViewModel.cs`
- `ViewModels/RecapitulatifTourneeViewModel.cs`
- `Pages/AccueilPage.xaml`
- `MauiProgram.cs`
- `AppShell.xaml.cs`
- `.gitignore`
- `Platforms/Android/AndroidManifest.xml`
- `Platforms/Android/Resources/xml/network_security_config.xml`
- `docs/04-tests/Mobile/matrice-tests-camion-trajet-mobile-1.3.md`

## Faits vérifiés

- L'URL active est `https://srvapi1.sli.local`.
- Le contrat mobile actif est `schemaVersion = "1.3"`.
- Le flux camion est présent côté mobile.
- Le trajet final contient camion, kilométrage départ, kilométrage arrivée, date départ mobile et date arrivée mobile.
- La configuration Android interdit le trafic clair pour le domaine final.
- La CA Android locale est ignorée par Git.

## Points à surveiller

- L'avertissement Android 16 `XA0141` reste présent selon la dernière sortie locale communiquée.
- L'écran d'accueil conserve des outils de maintenance visibles.
- Les tests fonctionnels téléphone doivent être rejoués après tout nouveau build.

## Tests non exécutés par cette correction documentaire

```text
dotnet clean : non exécuté
dotnet restore : non exécuté
dotnet build Release : non exécuté
dotnet publish Release : non exécuté
installation APK : non exécuté
tests téléphone : non exécuté
```

## Conclusion

La documentation principale et la matrice camion / trajet ont été mises à jour pour refléter l'état final mobile 1.3 HTTPS.
