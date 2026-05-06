using MobileSLI.Models;

namespace MobileSLI.Services;

public sealed class DemoDataService
{
    public LivreurDto? FindLivreurByCode(string codeLivreur)
    {
        var normalized = (codeLivreur ?? string.Empty).Trim();

        return normalized switch
        {
            "2" => new LivreurDto { CodeLivreur = "2", NomLivreur = "DAVID LEBAS" },
            "1" => new LivreurDto { CodeLivreur = "1", NomLivreur = "LIVREUR TEST" },
            _ => null
        };
    }

    public List<TourneeResumeDto> GetTourneesDisponibles()
    {
        return new List<TourneeResumeDto>
        {
            new() { CodeTournee = "2001", LibelleTournee = "MDR VENDEE", NombrePoints = 24 },
            new() { CodeTournee = "2011", LibelleTournee = "CLINIQUE LITTORAL", NombrePoints = 12 },
            new() { CodeTournee = "2022", LibelleTournee = "RESIDENCE BEL AIR", NombrePoints = 8 }
        };
    }

    public TourneeJourDto BuildTourneeJour(TourneeResumeDto tournee, LivreurDto livreur)
    {
        var date = DateTime.Today;

        return new TourneeJourDto
        {
            SchemaVersion = "1.1",
            DateTournee = date,
            DateModifiable = false,
            CodeTournee = tournee.CodeTournee,
            LibelleTournee = tournee.LibelleTournee,
            Livreur = livreur,
            ArticlesSaisissables = new List<ArticleSaisissableDto>
            {
                new() { CodeArticle = "ROLLS", Libelle = "Rolls" },
                new() { CodeArticle = "TAPIS", Libelle = "Tapis" },
                new() { CodeArticle = "SACS", Libelle = "Sacs" }
            },
            Lignes = new List<TourneeLigneDto>
            {
                new()
                {
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
