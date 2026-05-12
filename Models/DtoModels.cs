using System.Text.Json;
using System.Text.Json.Serialization;
using MobileSLI.Configuration;

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

public sealed class TourneesDisponiblesResponseDto
{
    public string SchemaVersion { get; set; } = AppConfig.SchemaVersion;

    public DateTime DateTournee { get; set; } = DateTime.Today;

    public bool DateModifiable { get; set; }

    public LivreurDto Livreur { get; set; } = new();

    public List<TourneeResumeDto> Tournees { get; set; } = new();
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
    public string SchemaVersion { get; set; } = AppConfig.SchemaVersion;

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

    public int? OrdreAffichage { get; set; }
}

public sealed class TourneeLigneDto
{
    public string IdLigneSource { get; set; } = string.Empty;

    public int OrdreArret { get; set; }

    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? Horaire { get; set; }

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

    public string? ZoneDechargementAffichee
    {
        get => InfosLivreur.ZoneDechargementAffichee;
        set => InfosLivreur.ZoneDechargementAffichee = value;
    }

    public string? Instructions
    {
        get => InfosLivreur.Instructions;
        set => InfosLivreur.Instructions = value;
    }

    public string? CommentaireExceptionnel
    {
        get => InfosLivreur.CommentaireExceptionnel;
        set => InfosLivreur.CommentaireExceptionnel = value;
    }

    /*
     * Ancien champ conservé uniquement pour compatibilité avec une réponse API plus ancienne.
     * Le champ métier à utiliser désormais est InfosLivreur.CommentaireExceptionnel.
     */
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

    public string? JourLibelle { get; set; }

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

    /*
     * Ancien champ conservé uniquement pour compatibilité.
     * Ne pas l'utiliser comme source principale côté mobile.
     */
    public string? CommentaireFiche { get; set; }

    /*
     * Commentaire ponctuel saisi côté administration ou expédition.
     * Ce n'est pas le commentaire saisi par le livreur.
     */
    public string? CommentaireExceptionnel { get; set; }

    public string? ZoneDechargement { get; set; }

    /*
     * Zone finale prête à afficher si l'API applique déjà la règle métier.
     */
    public string? ZoneDechargementAffichee { get; set; }

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

    /*
     * Valeur prévue par l'expédition pour la colonne Livré.
     * null = non renseigné ; 0 = zéro volontaire ; > 0 = quantité prévue.
     */
    public int? QuantiteLivreePrevue { get; set; }

    public int QuantiteLivree { get; set; }

    public int QuantiteRecuperee { get; set; }
}

public sealed class SynchronisationTourneeRequest
{
    public string SchemaVersion { get; set; } = AppConfig.SchemaVersion;

    public string IdSynchronisation { get; set; } = Guid.NewGuid().ToString();

    public string DateTournee { get; set; } = string.Empty;

    public string CodeTournee { get; set; } = string.Empty;

    public string LibelleTournee { get; set; } = string.Empty;

    public string StatutSynchronisation { get; set; } = "ENVOYEE";

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

    public string? Horaire { get; set; }

    public SynchronisationClientRequest Client { get; set; } = new();

    public SynchronisationPointLivraisonRequest PointLivraison { get; set; } = new();

    public SynchronisationTourneeInfoRequest Tournee { get; set; } = new();

    public SynchronisationRetourInfoRequest Retour { get; set; } = new();

    public SynchronisationInfosLivreurRequest InfosLivreur { get; set; } = new();

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

    public string? AdresseLigne1 { get; set; }

    public string? AdresseLigne2 { get; set; }

    public string? AdresseLigne3 { get; set; }

    public string? Ville { get; set; }

    public string? CodePostal { get; set; }
}

public sealed class SynchronisationTourneeInfoRequest
{
    public string CodeTournee { get; set; } = string.Empty;

    public string LibelleTournee { get; set; } = string.Empty;

    public int? JourTournee { get; set; }

    public string? JourLibelle { get; set; }

    public string? SchemaLivraison { get; set; }
}

public sealed class SynchronisationRetourInfoRequest
{
    public int? JourTourneeRetour { get; set; }

    public string? JourRetourLibelle { get; set; }

    public string? CodeTourneeRetour { get; set; }

    public string? LibelleTourneeRetour { get; set; }
}

public sealed class SynchronisationInfosLivreurRequest
{
    public string? Instructions { get; set; }

    public string? CommentaireExceptionnel { get; set; }

    public string? ZoneDechargement { get; set; }

    public string? ZoneDechargementAffichee { get; set; }

    public string? Zone { get; set; }

    public string? Precision { get; set; }

    public string? Cle { get; set; }

    public bool EstFerme { get; set; }

    public DateTime? DateFermeture { get; set; }

    public string? MotifFermeture { get; set; }
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

    public int? QuantiteLivreePrevue { get; set; }

    public int QuantiteLivree { get; set; }

    public int QuantiteRecuperee { get; set; }
}

public sealed class ApiErrorResponse
{
    public string? Error { get; set; }

    public string? Code { get; set; }

    public string? Message { get; set; }
}

/*
 * L'API peut parfois renvoyer horaire sous forme de nombre, de chaîne ou null.
 * Ce convertisseur évite de casser la désérialisation mobile.
 *
 * Remplacer le bloc :
 *     _ => reader.GetRawText()
 *
 * par :
 *     _ => ReadComplexTokenAsText(ref reader)
 *
 * et ajouter la méthode privée ReadComplexTokenAsText dans FlexibleStringJsonConverter.
 */
public sealed class FlexibleStringJsonConverter : JsonConverter<string?>
{
    public override string? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => reader.GetString(),
            JsonTokenType.Number => reader.TryGetInt64(out var integer)
                ? integer.ToString(System.Globalization.CultureInfo.InvariantCulture)
                : reader.GetDouble().ToString(System.Globalization.CultureInfo.InvariantCulture),
            JsonTokenType.True => "true",
            JsonTokenType.False => "false",
            _ => ReadComplexTokenAsText(ref reader)
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        string? value,
        JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value);
    }

    private static string ReadComplexTokenAsText(ref Utf8JsonReader reader)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.ToString();
    }
}
