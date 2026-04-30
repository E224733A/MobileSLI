# Commandes Git pour envoyer dans `E224733A/MobileSLI`

Depuis le dossier qui contient ce projet :

```powershell
git clone https://github.com/E224733A/MobileSLI.git
cd MobileSLI

# Copier les fichiers du zip dans ce dossier, puis :
git add .
git commit -m "Initialise application mobile MAUI tournee livreur"
git push origin main
```

Si le dépôt contient déjà du code au moment de l'intégration, créer une branche :

```powershell
git checkout -b feature/app-mobile-tournees
git add .
git commit -m "Ajoute application mobile MAUI tournee livreur"
git push origin feature/app-mobile-tournees
```
