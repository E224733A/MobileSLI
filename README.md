# MobileSLI

Application mobile .NET MAUI Android pour la dématérialisation des fiches de tournée.

## Objectif

Cette application reprend le fonctionnement prévu dans le cahier des charges :

1. test de connexion API ;
2. identification du livreur ;
3. choix de la tournée ;
4. chargement de la tournée depuis l'API ;
5. sauvegarde locale SQLite ;
6. consultation et saisie hors connexion ;
7. aide au déchargement par client ;
8. récapitulatif ;
9. synchronisation vers l'API ;
10. verrouillage local après succès.

## Configuration Android

Le projet cible Android avec .NET MAUI.

- Version minimale configurée pour les tests physiques : Android 11 / API 30.
- Cible recommandée pour les téléphones livreurs : Android 12 / API 31 minimum.
- SDK de compilation conseillé : Android API 34.

Le fichier `MobileSLI.csproj` contient :

```xml
<SupportedOSPlatformVersion Condition="$([MSBuild]::GetTargetPlatformIdentifier('$(TargetFramework)')) == 'android'">30.0</SupportedOSPlatformVersion>
<AndroidTargetSdkVersion>34</AndroidTargetSdkVersion>
<AndroidCompileSdkVersion>34</AndroidCompileSdkVersion>
```

## URL de l'API

L'adresse API par défaut est définie dans `Services/SettingsService.cs` :

```csharp
http://192.168.1.50:5000
```

Pour un téléphone physique, remplacer cette adresse par l'adresse IP du PC ou de la VM qui héberge l'API.

Exemple :

```text
http://192.168.1.50:5000
```

Pour un émulateur Android :

```text
http://10.0.2.2:5000
```

Le manifeste Android autorise le trafic HTTP clair en développement avec `android:usesCleartextTraffic="true"`.

## Routes API utilisées

```http
GET /api/health
GET /api/tournees/jour?dateTournee=YYYY-MM-DD&codeTournee=2001&codeLivreur=2
POST /api/synchronisations
```

## Mode démonstration

La liste des tournées disponibles est alimentée par `DemoDataService` pour la première version, car le cahier des charges ne définit pas encore une route de listing des tournées.

Le chargement réel est tenté via l'API. Si l'API ne répond pas, l'application utilise une tournée de démonstration pour permettre de tester l'interface, la navigation et SQLite.

La synchronisation de fin de tournée, elle, tente réellement d'envoyer le JSON à l'API. En cas d'échec réseau, l'application affiche l'écran d'erreur et conserve les données localement.

## Tester sur téléphone physique

1. Brancher le téléphone Android en USB.
2. Activer les options développeur.
3. Activer le débogage USB.
4. Connecter le téléphone au même Wi-Fi que le PC ou la VM de l'API.
5. Vérifier que l'API écoute sur une adresse réseau accessible, pas uniquement sur `localhost`.
6. Autoriser le port API dans le pare-feu Windows.
7. Modifier l'URL dans `Services/SettingsService.cs` si nécessaire.
8. Ouvrir `MobileSLI.sln` dans Visual Studio.
9. Sélectionner le téléphone dans la liste des appareils Android.
10. Lancer en Debug.

## Tester l'API depuis le téléphone

Depuis le navigateur du téléphone, tester :

```text
http://ADRESSE_IP_DU_PC:5000/api/health
```

Si le téléphone ne peut pas ouvrir cette adresse, l'application ne pourra pas non plus joindre l'API.

## Workflow de test rapide dans l'application

1. Écran Accueil : appuyer sur `Tester la connexion`.
2. Continuer vers l'identification.
3. Saisir le code livreur `2`.
4. Valider le code.
5. Sélectionner `2001 - MDR VENDEE`.
6. Charger la tournée.
7. Ouvrir un point de livraison.
8. Saisir les quantités livrées/récupérées.
9. Choisir le statut `FAIT`.
10. Valider le passage.
11. Retourner à la liste.
12. Consulter le déchargement.
13. Aller au récapitulatif.
14. Envoyer la tournée.

## Règles métier implémentées

- Code livreur obligatoire.
- Tournée obligatoire.
- Saisie locale SQLite.
- Quantités livrées et récupérées séparées.
- Quantités négatives interdites.
- Commentaire obligatoire pour `NON_FAIT` et `ANOMALIE`.
- Heure de validation générée automatiquement.
- Statut `A_FAIRE` interdit dans le JSON final.
- Verrouillage local après synchronisation réussie.
- Gestion spécifique d'une tournée déjà synchronisée.
