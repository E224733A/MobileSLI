using SQLite;

namespace MobileSLI.Models;

public sealed class LocalTournee
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public DateTime DateTournee { get; set; }

    [Indexed]
    public string CodeTournee { get; set; } = string.Empty;

    public string LibelleTournee { get; set; } = string.Empty;
    public string CodeLivreur { get; set; } = string.Empty;
    public string NomLivreur { get; set; } = string.Empty;
    public string StatutLocal { get; set; } = TourneeLocalStatus.Chargee;
    public DateTime DateChargement { get; set; }
    public DateTime? DateEnvoi { get; set; }
    public string IdSynchronisation { get; set; } = Guid.NewGuid().ToString();
    public bool EstVerrouillee { get; set; }
    public string? CommentaireGlobal { get; set; }
}

public sealed class LocalTourneeLigne
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int TourneeId { get; set; }

    [Indexed]
    public string IdLigneSource { get; set; } = string.Empty;

    public int OrdreArret { get; set; }
    public string NumClient { get; set; } = string.Empty;
    public string NomClient { get; set; } = string.Empty;
    public string? CodePDL { get; set; }
    public string? DescriptionPDL { get; set; }
    public string? AdresseLigne1 { get; set; }
    public string? Ville { get; set; }
    public string? CodePostal { get; set; }
    public string? Zone { get; set; }
    public string? ZoneDechargement { get; set; }
    public int? JourTourneeRetour { get; set; }

    public bool EstFerme { get; set; }
    public DateTime? DateFermeture { get; set; }
    public string? MotifFermeture { get; set; }

    public string? Instructions { get; set; }
    public string? CommentaireFiche { get; set; }
    public string StatutPassage { get; set; } = StatutPassageConstants.AFaire;
    public bool EstValidee { get; set; }
    public DateTime? HeureValidation { get; set; }
    public string? CommentaireLivreur { get; set; }

    [Ignore]
    public string? ZoneDechargementAffichee
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ZoneDechargement))
            {
                return JourTourneeRetour?.ToString();
            }

            var zone = ZoneDechargement.Trim();

            if (zone.StartsWith("+", StringComparison.Ordinal))
            {
                return JourTourneeRetour.HasValue
                    ? $"{JourTourneeRetour.Value} {zone}"
                    : zone;
            }

            return zone;
        }
    }

    [Ignore]
    public bool IsFermetureVisible => EstFerme;

    [Ignore]
    public string FermetureText
    {
        get
        {
            if (!EstFerme)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(MotifFermeture))
            {
                return $"Client fermé : {MotifFermeture}";
            }

            if (DateFermeture.HasValue)
            {
                return $"Client fermé le {DateFermeture.Value:dd/MM/yyyy}";
            }

            return "Client fermé.";
        }
    }
}

public sealed class LocalTourneeLigneQuantite
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int LigneId { get; set; }

    public string CodeArticle { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;
    public int QuantiteLivree { get; set; }
    public int QuantiteRecuperee { get; set; }
}