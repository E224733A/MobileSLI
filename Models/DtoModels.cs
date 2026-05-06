namespace MobileSLI.Models;

public sealed class LivreurDto
{
    public string CodeLivreur { get; set; } = string.Empty;

    public string NomLivreur { get; set; } = string.Empty;

    public string NomAffiche =>
        string.IsNullOrWhiteSpace(NomLivreur)
            ? CodeLivreur
            : $"{CodeLivreur} - {NomLivreur}";
}

public sealed class TourneeResumeDto
{
    public DateTime DateTournee { get; set; } = DateTime.Today;

    public string CodeTournee { get; set; } = string.Empty;

    public string LibelleTournee { get; set; } = string.Empty;

    public int? JourTournee { get; set; }

    public string? JourLibelle { get; set; }

    public int NombrePoints { get; set; }

    public string NomAffiche =>
        string.IsNullOrWhiteSpace(LibelleTournee)
            ? CodeTournee
            : $"{CodeTournee} — {LibelleTournee}";
}

public sealed class TourneeJourDto
{
    public string SchemaVersion { get; set; } = "1.1";

    public DateTime DateTournee { get; set; }

    public bool DateModifiable { get; set; }

    public int? JourTournee { get; set; }

    public string? JourLibelle { get; set; }

    public string CodeTournee { get; set; } = string.Empty;

    public string LibelleTournee { get; set; } = string.Empty;

    public string? StatutSynchronisation { get; set; }

    public LivreurDto Livreur { get; set; } = new();

    public ChargementDto Chargement { get; set; } = new();

    public List<ArticleSaisissableDto> ArticlesSaisissables { get; set; } = new();

    public List<TourneeLigneDto> Lignes { get; set; } = new();
}

public sealed class ChargementDto
{
    public DateTime? DateGenerationApi { get; set; }

    public int NombrePointsEnvoyes { get; set; }
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

    public int? Horaire { get; set; }

    public ClientDto Client { get; set; } = new();

    public PointLivraisonDto PointLivraison { get; set; } = new();

    public TourneeInfoDto Tournee { get; set; } = new();

    public RetourInfoDto Retour { get; set; } = new();

    public InfosLivreurDto InfosLivreur { get; set; } = new();

    public SaisieMobileDto Saisie { get; set; } = new();

    public string NumClient
    {
        get => Client.NumClient;
        set => Client.NumClient = value;
    }

    public string NomClient
    {
        get => Client.NomClient;
        set => Client.NomClient = value;
    }

    public string? CodePDL
    {
        get => PointLivraison.CodePDL;
        set => PointLivraison.CodePDL = value ?? string.Empty;
    }

    public string? DescriptionPDL
    {
        get => PointLivraison.DescriptionPDL;
        set => PointLivraison.DescriptionPDL = value ?? string.Empty;
    }

    public string? AdresseLigne1
    {
        get => PointLivraison.AdresseLigne1;
        set => PointLivraison.AdresseLigne1 = value;
    }

    public string? Ville
    {
        get => PointLivraison.Ville;
        set => PointLivraison.Ville = value;
    }

    public string? CodePostal
    {
        get => PointLivraison.CodePostal;
        set => PointLivraison.CodePostal = value;
    }

    public string? Zone
    {
        get => InfosLivreur.Zone;
        set => InfosLivreur.Zone = value;
    }

    public string? ZoneDechargement
    {
        get => InfosLivreur.ZoneDechargement;
        set => InfosLivreur.ZoneDechargement = value;
    }

    public string? Instructions
    {
        get => InfosLivreur.Instructions;
        set => InfosLivreur.Instructions = value;
    }

    public string? CommentaireFiche
    {
        get => InfosLivreur.CommentaireFiche;
        set => InfosLivreur.CommentaireFiche = value;
    }
}

public sealed class ClientDto
{
    public string NumClient { get; set; } = string.Empty;

    public string NomClient { get; set; } = string.Empty;

    public string NomAffiche { get; set; } = string.Empty;
}

public sealed class PointLivraisonDto
{
    public string CodePDL { get; set; } = string.Empty;

    public string DescriptionPDL { get; set; } = string.Empty;

    public string? AdresseLigne1 { get; set; }

    public string? AdresseLigne2 { get; set; }

    public string? AdresseLigne3 { get; set; }

    public string? Ville { get; set; }

    public string? CodePostal { get; set; }
}

public sealed class TourneeInfoDto
{
    public string CodeTournee { get; set; } = string.Empty;

    public string LibelleTournee { get; set; } = string.Empty;

    public int? JourTournee { get; set; }

    public string? SchemaLivraison { get; set; }
}

public sealed class RetourInfoDto
{
    public int? JourTourneeRetour { get; set; }

    public string? JourRetourLibelle { get; set; }

    public string? CodeTourneeRetour { get; set; }

    public string? LibelleTourneeRetour { get; set; }
}

public sealed class InfosLivreurDto
{
    public string? Instructions { get; set; }

    public string? CommentaireFiche { get; set; }

    public string? ZoneDechargement { get; set; }

    public string? Zone { get; set; }

    public string? Precision { get; set; }

    public string? Cle { get; set; }

    public bool EstFerme { get; set; }

    public DateTime? DateFermeture { get; set; }

    public string? MotifFermeture { get; set; }
}

public sealed class SaisieMobileDto
{
    public string? PrecisionLivreur { get; set; }

    public string StatutPassage { get; set; } = StatutPassageConstants.AFaire;

    public string? CommentaireLivreur { get; set; }

    public string? HeureValidation { get; set; }

    public bool EstValidee { get; set; }

    public List<QuantiteSaisieMobileDto> Quantites { get; set; } = new();
}

public sealed class QuantiteSaisieMobileDto
{
    public string CodeArticle { get; set; } = string.Empty;

    public string? Libelle { get; set; }

    public int QuantiteLivree { get; set; }

    public int QuantiteRecuperee { get; set; }
}

public sealed class SynchronisationTourneeRequest
{
    public string SchemaVersion { get; set; } = "1.1";

    public string IdSynchronisation { get; set; } = Guid.NewGuid().ToString();

    public string DateTournee { get; set; } = string.Empty;

    public string CodeTournee { get; set; } = string.Empty;

    public string LibelleTournee { get; set; } = string.Empty;

    public SynchronisationLivreurRequest Livreur { get; set; } = new();

    public SynchronisationMobileRequest Mobile { get; set; } = new();

    public string? CommentaireGlobal { get; set; }

    public List<SynchronisationLigneRequest> Lignes { get; set; } = new();
}

public sealed class SynchronisationLivreurRequest
{
    public string CodeLivreur { get; set; } = string.Empty;

    public string NomLivreur { get; set; } = string.Empty;
}

public sealed class SynchronisationMobileRequest
{
    public string NomAppareil { get; set; } = string.Empty;

    public string VersionApplication { get; set; } = string.Empty;

    public string DateChargementMobile { get; set; } = string.Empty;

    public string DateEnvoiMobile { get; set; } = string.Empty;
}

public sealed class SynchronisationLigneRequest
{
    public string IdLigneSource { get; set; } = string.Empty;

    public int OrdreArret { get; set; }

    public SynchronisationClientRequest Client { get; set; } = new();

    public SynchronisationPointLivraisonRequest PointLivraison { get; set; } = new();

    public SynchronisationSaisieRequest Saisie { get; set; } = new();
}

public sealed class SynchronisationClientRequest
{
    public string NumClient { get; set; } = string.Empty;

    public string NomClient { get; set; } = string.Empty;

    public string NomAffiche { get; set; } = string.Empty;
}

public sealed class SynchronisationPointLivraisonRequest
{
    public string CodePDL { get; set; } = string.Empty;

    public string DescriptionPDL { get; set; } = string.Empty;
}

public sealed class SynchronisationSaisieRequest
{
    public string? PrecisionLivreur { get; set; }

    public string StatutPassage { get; set; } = StatutPassageConstants.AFaire;

    public string? CommentaireLivreur { get; set; }

    public string? HeureValidation { get; set; }

    public bool EstValidee { get; set; }

    public List<SynchronisationQuantiteRequest> Quantites { get; set; } = new();
}

public sealed class SynchronisationQuantiteRequest
{
    public string CodeArticle { get; set; } = string.Empty;

    public string? Libelle { get; set; }

    public int QuantiteLivree { get; set; }

    public int QuantiteRecuperee { get; set; }
}

public sealed class ApiErrorResponse
{
    public string? Error { get; set; }

    public string? Code { get; set; }

    public string? Message { get; set; }
}
