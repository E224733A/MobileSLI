using MobileSLI.Models;

namespace MobileSLI.Services;

/// <summary>
/// Service de données de démonstration utilisé uniquement comme secours de développement ou de test.
/// Les données sont codées en dur et ne doivent pas être considérées comme une source métier réelle.
/// En production, les livreurs, tournées, lignes et articles doivent venir de l'API MobileSLI.
/// </summary>
public sealed class DemoDataService
{
    /// <summary>
    /// Recherche un livreur de démonstration à partir du code saisi.
    /// Cette méthode ne vérifie que les codes présents dans ce fichier ; elle ne remplace pas l'identification API.
    /// </summary>
    /// <param name="codeLivreur">Code livreur saisi dans l'écran d'identification.</param>
    /// <returns>Un livreur de démonstration si le code est connu, sinon <c>null</c>.</returns>
    public LivreurDto? FindLivreurByCode(string codeLivreur)
    {
        // Normalisation minimale pour éviter qu'un espace saisi par erreur bloque la démonstration.
        var normalized = (codeLivreur ?? string.Empty).Trim();

        return normalized switch
        {
            "2" => new LivreurDto { CodeLivreur = "2", NomLivreur = "DAVID LEBAS" },
            "1" => new LivreurDto { CodeLivreur = "1", NomLivreur = "LIVREUR TEST" },
            _ => null
        };
    }

    /// <summary>
    /// Retourne une liste statique de tournées de démonstration.
    /// Les codes, libellés et nombres de points servent uniquement à simuler l'écran de choix de tournée.
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
    /// La structure produite imite le contrat de chargement API afin de tester les écrans sans serveur,
    /// mais les valeurs ne doivent pas servir à valider les règles de production.
    /// </summary>
    /// <param name="tournee">Résumé de tournée sélectionné dans la démonstration.</param>
    /// <param name="livreur">Livreur de démonstration associé à la tournée.</param>
    /// <returns>Une tournée complète prête à être utilisée par les écrans de saisie.</returns>
    public TourneeJourDto BuildTourneeJour(TourneeResumeDto tournee, LivreurDto livreur)
    {
        var date = DateTime.Today;

        return new TourneeJourDto
        {
            /*
             * Attention : cette version de schéma est historique dans le jeu de démonstration.
             * Ne pas s'appuyer sur cette valeur pour valider le contrat mobile/API final.
             */
            SchemaVersion = "1.1",
            DateTournee = date,
            DateModifiable = false,
            CodeTournee = tournee.CodeTournee,
            LibelleTournee = tournee.LibelleTournee,
            Livreur = livreur,

            /*
             * Articles proposés à la saisie dans le scénario de démonstration.
             * Le jeu de démonstration ne couvre pas forcément tous les articles autorisés par le contrat réel.
             */
            ArticlesSaisissables = new List<ArticleSaisissableDto>
            {
                new() { CodeArticle = "ROLLS", Libelle = "Rolls" },
                new() { CodeArticle = "TAPIS", Libelle = "Tapis" },
                new() { CodeArticle = "SACS", Libelle = "Sacs" }
            },

            /*
             * Lignes volontairement variées pour tester l'affichage :
             * - instructions présentes ou absentes ;
             * - zones de déchargement différentes ;
             * - commentaire fiche présent ou absent ;
             * - villes et codes postaux différents.
             */
            Lignes = new List<TourneeLigneDto>
            {
                new()
                {
                    // Identifiant stable de démonstration : date, tournée, client, PDL et ordre d'arrêt.
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
                    CommentaireFiche = null
                },
                new()
                {
                    // Ligne avec commentaire fiche pour vérifier l'affichage d'une consigne complémentaire.
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
                    CommentaireFiche = "Accès par la porte de service"
                },
                new()
                {
                    // Ligne de démonstration en zone retour pour tester les libellés spécifiques de déchargement.
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
                    CommentaireFiche = "Instruction arrière"
                },
                new()
                {
                    // Ligne sans instruction ni commentaire pour vérifier que l'écran reste lisible avec des champs optionnels vides.
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
                    Instructions = null,
                    CommentaireFiche = null
                }
            }
        };
    }
}
