using MobileSLI.Models;

namespace MobileSLI.Services;

/// <summary>
/// Service de données de démonstration utilisé pour simuler une tournée sans appeler l'API centrale.
///
/// Point sensible : ce service ne doit pas devenir une source métier réelle. Les livreurs, tournées,
/// clients, adresses, quantités et commentaires présents ici sont volontairement figés et servent
/// uniquement à tester l'interface mobile, la navigation et les écrans hors connexion API.
///
/// Règle de maintenance : toute donnée ajoutée ici doit rester clairement identifiable comme donnée
/// fictive. Ne pas utiliser ce fichier pour contourner un problème de chargement depuis l'API.
/// </summary>
public sealed class DemoDataService
{
    /// <summary>
    /// Recherche un livreur de démonstration à partir du code saisi.
    ///
    /// Règle métier simulée : le code livreur est comparé après suppression des espaces en début
    /// et fin de chaîne, comme dans un formulaire utilisateur classique. Aucun appel réseau ni SQL
    /// n'est effectué ici.
    ///
    /// Point sensible : la liste des codes est volontairement très courte. Ajouter un livreur ici
    /// n'ajoute pas un vrai livreur dans ABSSolute ni dans l'API centrale.
    /// </summary>
    /// <param name="codeLivreur">Code livreur saisi par l'utilisateur.</param>
    /// <returns>Le livreur de démonstration correspondant, ou <c>null</c> si le code est inconnu.</returns>
    public LivreurDto? FindLivreurByCode(string codeLivreur)
    {
        var normalized = (codeLivreur ?? string.Empty).Trim();

        return normalized switch
        {
            // Données fictives : ces codes permettent de tester le choix livreur sans dépendance API.
            "2" => new LivreurDto { CodeLivreur = "2", NomLivreur = "DAVID LEBAS" },
            "1" => new LivreurDto { CodeLivreur = "1", NomLivreur = "LIVREUR TEST" },
            _ => null
        };
    }

    /// <summary>
    /// Retourne les tournées disponibles en mode démonstration.
    ///
    /// Règle métier simulée : la liste est stable et ne dépend ni de la date du jour, ni du livreur,
    /// ni d'un état de verrouillage côté dépôt. En fonctionnement réel, ces informations doivent
    /// provenir de l'API et respecter la date métier autorisée.
    ///
    /// Point sensible : ne pas déduire les règles de production à partir de ces valeurs. Les codes,
    /// libellés et nombres de points servent uniquement à alimenter les écrans.
    /// </summary>
    public List<TourneeResumeDto> GetTourneesDisponibles()
    {
        return new List<TourneeResumeDto>
        {
            new() { CodeTournee = "2001", LibelleTournee = "MDR VENDEE", NombrePoints = 24 },
            new() { CodeTournee = "2011", LibelleTournee = "CLINIQUE LITTORAL", NombrePoints = 12 },
            new() { CodeTournee = "2022", LibelleTournee = "RESIDENCE BEL AIR", NombrePoints = 8 }
        };
    }

    /// <summary>
    /// Construit une tournée complète de démonstration à partir d'un résumé de tournée et d'un livreur.
    ///
    /// Règle métier simulée : la date utilisée est la date locale du téléphone au moment de l'appel.
    /// En production, la date de tournée autorisée est pilotée par l'API centrale et ne doit pas être
    /// remplacée par cette logique de démonstration.
    ///
    /// Point sensible : les identifiants <c>IdLigneSource</c> sont construits pour ressembler à des
    /// identifiants stables de lignes réelles. Ils doivent rester uniques dans la tournée, car la
    /// synchronisation s'appuie ensuite sur cet identifiant pour éviter les doublons.
    /// </summary>
    /// <param name="tournee">Résumé de la tournée de démonstration sélectionnée.</param>
    /// <param name="livreur">Livreur de démonstration affecté à la tournée.</param>
    /// <returns>Une tournée de démonstration complète, prête à être affichée dans l'application.</returns>
    public TourneeJourDto BuildTourneeJour(TourneeResumeDto tournee, LivreurDto livreur)
    {
        var date = DateTime.Today;

        return new TourneeJourDto
        {
            // Contrat de démonstration historique : ne pas utiliser cette version comme référence
            // pour décider de la version JSON acceptée par l'API en production.
            SchemaVersion = "1.1",
            DateTournee = date,
            // En démonstration, la date n'est pas modifiable afin de garder le même comportement
            // visuel que le flux réel piloté par la date métier API.
            DateModifiable = false,
            CodeTournee = tournee.CodeTournee,
            LibelleTournee = tournee.LibelleTournee,
            Livreur = livreur,
            ArticlesSaisissables = new List<ArticleSaisissableDto>
            {
                // Articles fictifs utilisés pour tester les champs de saisie des quantités.
                // La liste réelle des articles saisissables doit venir du contrat API.
                new() { CodeArticle = "ROLLS", Libelle = "Rolls" },
                new() { CodeArticle = "TAPIS", Libelle = "Tapis" },
                new() { CodeArticle = "SACS", Libelle = "Sacs" }
            },
            Lignes = new List<TourneeLigneDto>
            {
                new()
                {
                    // Identifiant stable fictif : format volontairement proche d'une ligne réelle
                    // pour tester les validations locales et la construction du JSON de synchronisation.
                    IdLigneSource = $"{date:yyyy-MM-dd}|{tournee.CodeTournee}|1|1058|PDL01|1",
                    OrdreArret = 1,
                    NumClient = "1058",
                    NomClient = "HOTEL EXEMPLE",
                    CodePDL = "PDL01",
                    DescriptionPDL = "Entrée principale",
                    AdresseLigne1 = "7 rue Jean et Marie La Gamarche",
                    Ville = "Nantes",
                    CodePostal = "44000",
                    Zone = "Centre",
                    ZoneDechargement = "Zone 1",
                    Instructions = "Livraison par l'arrière",
                    // Aucun commentaire fiche : permet de tester l'affichage sans information exceptionnelle.
                    CommentaireFiche = null
                },
                new()
                {
                    IdLigneSource = $"{date:yyyy-MM-dd}|{tournee.CodeTournee}|1|2044|PDL02|2",
                    OrdreArret = 2,
                    NumClient = "2044",
                    NomClient = "MAISON OCEANE",
                    CodePDL = "PDL02",
                    DescriptionPDL = "Lingerie",
                    AdresseLigne1 = "4 rue de la Gare",
                    Ville = "Machecoul",
                    CodePostal = "44270",
                    Zone = "Sud",
                    ZoneDechargement = "Zone 3",
                    Instructions = "Commentaire/instruction disponible",
                    // Commentaire fiche présent : permet de vérifier que les consignes client remontent
                    // correctement dans le détail de ligne et le récapitulatif.
                    CommentaireFiche = "Accès par la porte de service"
                },
                new()
                {
                    IdLigneSource = $"{date:yyyy-MM-dd}|{tournee.CodeTournee}|1|3071|PDL03|3",
                    OrdreArret = 3,
                    NumClient = "3071",
                    NomClient = "CLINIQUE LITTORAL",
                    CodePDL = "PDL03",
                    DescriptionPDL = "Quartier visiteurs",
                    AdresseLigne1 = "12 avenue des Pins",
                    Ville = "Challans",
                    CodePostal = "85300",
                    Zone = "Retour",
                    ZoneDechargement = "Zone RST",
                    Instructions = "Ne pas bloquer l'entrée principale",
                    // Deuxième cas avec commentaire fiche : utile pour tester plusieurs lignes annotées.
                    CommentaireFiche = "Instruction arrière"
                },
                new()
                {
                    IdLigneSource = $"{date:yyyy-MM-dd}|{tournee.CodeTournee}|1|4182|PDL04|4",
                    OrdreArret = 4,
                    NumClient = "4182",
                    NomClient = "RESIDENCE BEL AIR",
                    CodePDL = "PDL04",
                    DescriptionPDL = "Lingerie",
                    AdresseLigne1 = "12 impasse des Lilas",
                    Ville = "Aizenay",
                    CodePostal = "85190",
                    Zone = "VTS",
                    ZoneDechargement = "Zone VTS",
                    // Cas volontairement sans instruction ni commentaire : permet de vérifier que
                    // les écrans restent lisibles quand les champs optionnels sont absents.
                    Instructions = null,
                    CommentaireFiche = null
                }
            }
        };
    }
}
