namespace MobileSLI.Models;

public sealed class LivreurDto
{
    public string CodeLivreur { get; set; } = string.Empty;
    public string NomLivreur { get; set; } = string.Empty;
}

public sealed class TourneeResumeDto
{
    public string CodeTournee { get; set; } = string.Empty;
    public string LibelleTournee { get; set; } = string.Empty;
    public int NombrePoints { get; set; }
}

public sealed class TourneeJourDto
{
    public string SchemaVersion { get; set; } = "1.1";
    public DateTime DateTournee { get; set; }
    public bool DateModifiable { get; set; }
    public string CodeTournee { get; set; } = string.Empty;
    public string LibelleTournee { get; set; } = string.Empty;
    public LivreurDto Livreur { get; set; } = new();
    public List<ArticleSaisissableDto> ArticlesSaisissables { get; set; } = new();
    public List<TourneeLigneDto> Lignes { get; set; } = new();
}

public sealed class ArticleSaisissableDto
{
    public string CodeArticle { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;
}

public sealed class TourneeLigneDto
{
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
    public string? Instructions { get; set; }
    public string? CommentaireFiche { get; set; }
}

public sealed class SynchronisationTourneeRequest
{
    public string SchemaVersion { get; set; } = "1.1";
    public string IdSynchronisation { get; set; } = Guid.NewGuid().ToString();
    public DateTime DateTournee { get; set; }
    public string CodeTournee { get; set; } = string.Empty;
    public string LibelleTournee { get; set; } = string.Empty;
    public LivreurDto Livreur { get; set; } = new();
    public MobileInfoDto Mobile { get; set; } = new();
    public string? CommentaireGlobal { get; set; }
    public List<SynchronisationLigneRequest> Lignes { get; set; } = new();
}

public sealed class MobileInfoDto
{
    public string NomAppareil { get; set; } = string.Empty;
    public string VersionApplication { get; set; } = string.Empty;
    public DateTime DateChargement { get; set; }
    public DateTime DateEnvoi { get; set; }
}

public sealed class SynchronisationLigneRequest
{
    public string IdLigneSource { get; set; } = string.Empty;
    public int OrdreArret { get; set; }
    public string NumClient { get; set; } = string.Empty;
    public string NomClient { get; set; } = string.Empty;
    public string? CodePDL { get; set; }
    public string? DescriptionPDL { get; set; }
    public string StatutPassage { get; set; } = StatutPassageConstants.AFaire;
    public bool EstValidee { get; set; }
    public DateTime? HeureValidation { get; set; }
    public string? CommentaireLivreur { get; set; }
    public List<QuantiteArticleRequest> Quantites { get; set; } = new();
}

public sealed class QuantiteArticleRequest
{
    public string CodeArticle { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;
    public int QuantiteLivree { get; set; }
    public int QuantiteRecuperee { get; set; }
}

public sealed class ApiErrorResponse
{
    public string? Error { get; set; }
    public string? Code { get; set; }
    public string? Message { get; set; }
}
