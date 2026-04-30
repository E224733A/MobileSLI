using SQLite;

namespace TourneesMobile.Models;

[Table("tournees")]
public sealed class TourneeEntity
{
    [PrimaryKey]
    public string IdTourneeLocale { get; set; } = string.Empty;

    public string SchemaVersion { get; set; } = "1.0";
    public string IdSynchronisation { get; set; } = Guid.NewGuid().ToString();
    public string DateTournee { get; set; } = string.Empty;
    public int? JourTournee { get; set; }
    public string? JourLibelle { get; set; }
    public string CodeTournee { get; set; } = string.Empty;
    public string? LibelleTournee { get; set; }
    public string CodeLivreur { get; set; } = string.Empty;
    public string NomLivreur { get; set; } = string.Empty;
    public DateTime DateChargementMobile { get; set; } = DateTime.Now;
    public DateTime? DateEnvoiMobile { get; set; }
    public string StatutSynchronisation { get; set; } = StatutSynchronisation.NonEnvoyee;
    public string? CommentaireGlobal { get; set; }
    public bool EstVerrouillee { get; set; }
}

[Table("arrets")]
public sealed class ArretEntity
{
    [PrimaryKey]
    public string IdLigneSource { get; set; } = string.Empty;

    [Indexed]
    public string IdTourneeLocale { get; set; } = string.Empty;

    public int? OrdreArret { get; set; }
    public int? Horaire { get; set; }

    public string NumClient { get; set; } = string.Empty;
    public string NomClient { get; set; } = string.Empty;
    public string? NomAffiche { get; set; }

    public string? CodePDL { get; set; }
    public string? DescriptionPDL { get; set; }
    public string? AdresseLigne1 { get; set; }
    public string? AdresseLigne2 { get; set; }
    public string? AdresseLigne3 { get; set; }
    public string? Ville { get; set; }
    public string? CodePostal { get; set; }

    public string? SchemaLivraison { get; set; }
    public string? Instructions { get; set; }
    public string? CommentaireFiche { get; set; }
    public string? ZoneDechargement { get; set; }
    public string? Zone { get; set; }
    public string? Precision { get; set; }
    public string? Cle { get; set; }
    public bool EstFerme { get; set; }
    public string? DateFermeture { get; set; }
    public string? MotifFermeture { get; set; }
    public string? TypeLinge { get; set; }

    public int? JourTourneeRetour { get; set; }
    public string? JourRetourLibelle { get; set; }
    public string? CodeTourneeRetour { get; set; }
    public string? LibelleTourneeRetour { get; set; }

    public int NbExpes { get; set; }
    public int NbRolls { get; set; }
    public int NbVetements { get; set; }
    public int NbTapis { get; set; }
    public int NbSacs { get; set; }
    public int NbRecuperes { get; set; }

    public string? PrecisionLivreur { get; set; }
    public string StatutPassage { get; set; } = StatutPassage.AFaire;
    public string? CommentaireLivreur { get; set; }
    public DateTime? HeureValidation { get; set; }
    public bool EstValidee { get; set; }

    [Ignore]
    public string NomAfficheCourt => string.IsNullOrWhiteSpace(NomAffiche) ? NomClient : NomAffiche!;

    [Ignore]
    public string AdresseComplete => string.Join(" ", new[] { AdresseLigne1, AdresseLigne2, AdresseLigne3, CodePostal, Ville }
        .Where(v => !string.IsNullOrWhiteSpace(v)));

    [Ignore]
    public bool DemandeCommentaire => StatutPassage.DemandeCommentaire(StatutPassage);

    [Ignore]
    public bool AUneCle => !string.IsNullOrWhiteSpace(Cle);

    [Ignore]
    public bool AUnCommentaireFiche => !string.IsNullOrWhiteSpace(CommentaireFiche) || !string.IsNullOrWhiteSpace(Instructions);

    [Ignore]
    public bool AUnePrecision => !string.IsNullOrWhiteSpace(Precision);
}
