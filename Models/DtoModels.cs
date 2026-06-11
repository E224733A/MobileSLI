using System.Text.Json;
using System.Text.Json.Serialization;
using MobileSLI.Configuration;

namespace MobileSLI.Models;

/// <summary>
/// Livreur renvoyé par l'API et utilisé dans l'écran d'identification mobile.
/// </summary>
public sealed class LivreurDto
{
    public string CodeLivreur { get; set; } = string.Empty;

    public string NomLivreur { get; set; } = string.Empty;

    /// <summary>
    /// Libellé prêt à afficher dans les listes de sélection.
    /// </summary>
    public string NomAffiche =>
        string.IsNullOrWhiteSpace(NomLivreur)
            ? CodeLivreur
            : $"{CodeLivreur} - {NomLivreur}";
}

/// <summary>
/// Enveloppe de réponse API pour la liste des tournées disponibles.
/// Elle porte la date métier décidée par l'API et la version du contrat JSON.
/// </summary>
public sealed class TourneesDisponiblesResponseDto
{
    public string SchemaVersion { get; set; } = AppConfig.SchemaVersion;

    public DateTime DateTournee { get; set; } = DateTime.Today;

    public bool DateModifiable { get; set; }

    public LivreurDto Livreur { get; set; } = new();

    public List<TourneeResumeDto> Tournees { get; set; } = new();
}

/// <summary>
/// Résumé de tournée affiché avant le chargement complet de la tournée.
/// </summary>
public sealed class TourneeResumeDto
{
    public DateTime DateTournee { get; set; } = DateTime.Today;

    public string CodeTournee { get; set; } = string.Empty;

    public string LibelleTournee { get; set; } = string.Empty;

    public int? JourTournee { get; set; }

    public string? JourLibelle { get; set; }

    public int NombrePoints { get; set; }

    /// <summary>
    /// Libellé utilisé dans l'écran de choix de tournée.
    /// </summary>
    public string NomAffiche =>
        string.IsNullOrWhiteSpace(LibelleTournee)
            ? CodeTournee
            : $"{CodeTournee} — {LibelleTournee}";
}

/// <summary>
/// Tournée complète chargée sur le mobile pour permettre la saisie terrain.
/// Ce DTO correspond au contrat de lecture API avant stockage local SQLite.
/// </summary>
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

/// <summary>
/// Informations techniques de génération du chargement côté API.
/// </summary>
public sealed class ChargementDto
{
    public DateTime? DateGenerationApi { get; set; }

    public int NombrePointsEnvoyes { get; set; }
}

/// <summary>
/// Article pour lequel le livreur peut saisir des quantités livrées ou récupérées.
/// </summary>
public sealed class ArticleSaisissableDto
{
    public string CodeArticle { get; set; } = string.Empty;

    public string Libelle { get; set; } = string.Empty;

    public int? OrdreAffichage { get; set; }
}

/// <summary>
/// Ligne de tournée correspondant à un arrêt ou point de livraison.
/// Les propriétés raccourcies conservent la compatibilité avec les anciens écrans
/// tout en déléguant désormais les données aux sous-objets du contrat structuré.
/// </summary>
public sealed class TourneeLigneDto
{
    public string IdLigneSource { get; set; } = string.Empty;

    public int OrdreArret { get; set; }

    /// <summary>
    /// Horaire accepté sous plusieurs formes JSON pour rester compatible avec les données API historiques.
    /// </summary>
    [JsonConverter(typeof(FlexibleStringJsonConverter))]
    public string? Horaire { get; set; }

    public ClientDto Client { get; set; } = new();

    public PointLivraisonDto PointLivraison { get; set; } = new();

    public TourneeInfoDto Tournee { get; set; } = new();

    public RetourInfoDto Retour { get; set; } = new();

    public InfosLivreurDto InfosLivreur { get; set; } = new();

    public SaisieMobileDto Saisie { get; set; } = new();
/*
    * Propriétés passerelles conservées pour les anciens écrans et services.
    * Elles lisent/écrivent dans les sous-objets du contrat structuré.
    * Ne pas les supprimer sans refactor complet des ViewModels concernés.
*/
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


    public string? LienAdresseLivraison
    {
        get => PointLivraison.LienAdresseLivraison;
        set => PointLivraison.LienAdresseLivraison = value;
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

/// <summary>
/// Informations client affichées au livreur et renvoyées lors de la synchronisation.
/// </summary>
public sealed class ClientDto
{
    public string NumClient { get; set; } = string.Empty;

    public string NomClient { get; set; } = string.Empty;

    public string NomAffiche { get; set; } = string.Empty;
}

/// <summary>
/// Informations du point de livraison.
/// Le lien d'adresse est fourni par l'API ; le mobile ne recalcule pas l'itinéraire dans cette version.
/// </summary>
public sealed class PointLivraisonDto
{
    public string CodePDL { get; set; } = string.Empty;

    public string DescriptionPDL { get; set; } = string.Empty;

    public string? AdresseLigne1 { get; set; }

    public string? AdresseLigne2 { get; set; }

    public string? AdresseLigne3 { get; set; }

    public string? Ville { get; set; }

    public string? CodePostal { get; set; }

    /*
     * Lien Google Maps optionnel fourni par l'API.
     * Le mobile ne construit pas l'itinéraire à partir de coordonnées GPS dans cette version.
     */
    public string? LienAdresseLivraison { get; set; }
}

/// <summary>
/// Informations de tournée associées à une ligne.
/// </summary>
public sealed class TourneeInfoDto
{
    public string CodeTournee { get; set; } = string.Empty;

    public string LibelleTournee { get; set; } = string.Empty;

    public int? JourTournee { get; set; }

    public string? JourLibelle { get; set; }

    public string? SchemaLivraison { get; set; }
}

/// <summary>
/// Informations éventuelles de retour associées à la ligne de livraison.
/// </summary>
public sealed class RetourInfoDto
{
    public int? JourTourneeRetour { get; set; }

    public string? JourRetourLibelle { get; set; }

    public string? CodeTourneeRetour { get; set; }

    public string? LibelleTourneeRetour { get; set; }
}

/// <summary>
/// Informations destinées au livreur : consignes, zones, commentaires exceptionnels et fermeture client.
/// Ces données viennent de l'API et ne correspondent pas au commentaire saisi par le livreur pendant la tournée.
/// </summary>
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

/// <summary>
/// Données de saisie mobile associées à une ligne de tournée.
/// Elles évoluent localement pendant la livraison avant d'être envoyées à l'API.
/// </summary>
public sealed class SaisieMobileDto
{
    public string? PrecisionLivreur { get; set; }

    public string StatutPassage { get; set; } = StatutPassageConstants.AFaire;

    public string? CommentaireLivreur { get; set; }

    public string? HeureValidation { get; set; }

    public bool EstValidee { get; set; }

    public List<QuantiteSaisieMobileDto> Quantites { get; set; } = new();
}

/// <summary>
/// Quantité saisie ou prévue pour un article sur une ligne de tournée.
/// </summary>
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

/// <summary>
/// Socle commun du payload de synchronisation mobile.
/// Le trajet camion est ajouté ensuite par SynchronisationTourneeAvecTrajetRequest
/// pour le contrat final mobile/API 1.3.
/// </summary>
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

/// <summary>
/// Partie livreur du payload envoyé à l'API lors de la synchronisation.
/// </summary>
public sealed class SynchronisationLivreurRequest
{
    public string CodeLivreur { get; set; } = string.Empty;

    public string NomLivreur { get; set; } = string.Empty;
}

/// <summary>
/// Informations techniques du téléphone et de l'application au moment de l'envoi.
/// </summary>
public sealed class SynchronisationMobileRequest
{
    public string NomAppareil { get; set; } = string.Empty;

    public string VersionApplication { get; set; } = string.Empty;

    public string DateChargementMobile { get; set; } = string.Empty;

    public string DateEnvoiMobile { get; set; } = string.Empty;
}

/// <summary>
/// Ligne envoyée dans le payload de synchronisation.
/// Elle contient les informations de contexte API et la saisie effectuée par le livreur.
/// </summary>
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

/// <summary>
/// Partie client du payload de synchronisation.
/// </summary>
public sealed class SynchronisationClientRequest
{
    public string NumClient { get; set; } = string.Empty;

    public string NomClient { get; set; } = string.Empty;

    public string NomAffiche { get; set; } = string.Empty;
}

/// <summary>
/// Partie point de livraison du payload de synchronisation.
/// </summary>
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

/// <summary>
/// Partie tournée du payload de synchronisation.
/// </summary>
public sealed class SynchronisationTourneeInfoRequest
{
    public string CodeTournee { get; set; } = string.Empty;

    public string LibelleTournee { get; set; } = string.Empty;

    public int? JourTournee { get; set; }

    public string? JourLibelle { get; set; }

    public string? SchemaLivraison { get; set; }
}

/// <summary>
/// Partie retour du payload de synchronisation.
/// </summary>
public sealed class SynchronisationRetourInfoRequest
{
    public int? JourTourneeRetour { get; set; }

    public string? JourRetourLibelle { get; set; }

    public string? CodeTourneeRetour { get; set; }

    public string? LibelleTourneeRetour { get; set; }
}

/// <summary>
/// Partie informations livreur du payload de synchronisation.
/// Elle garde les commentaires exceptionnels et la fermeture client reçus de l'API.
/// </summary>
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

    /*
     * Le contrat API mobile attend une date pure pour dateFermeture.
     * On envoie donc yyyy-MM-dd, et non un DateTime ISO avec heure.
     */
    public string? DateFermeture { get; set; }

    public string? MotifFermeture { get; set; }
}

/// <summary>
/// Partie saisie du payload de synchronisation.
/// Elle contient le statut de passage, le commentaire du livreur et les quantités finales.
/// </summary>
public sealed class SynchronisationSaisieRequest
{
    public string? PrecisionLivreur { get; set; }

    public string StatutPassage { get; set; } = StatutPassageConstants.AFaire;

    public string? CommentaireLivreur { get; set; }

    public string? HeureValidation { get; set; }

    public bool EstValidee { get; set; }

    public List<SynchronisationQuantiteRequest> Quantites { get; set; } = new();
}

/// <summary>
/// Quantité finale envoyée à l'API pour un article donné.
/// </summary>
public sealed class SynchronisationQuantiteRequest
{
    public string CodeArticle { get; set; } = string.Empty;

    public string? Libelle { get; set; }

    public int? QuantiteLivreePrevue { get; set; }

    public int QuantiteLivree { get; set; }

    public int QuantiteRecuperee { get; set; }
}

/// <summary>
/// Format d'erreur API simple pouvant être renvoyé lors d'un refus ou d'une erreur serveur.
/// </summary>
public sealed class ApiErrorResponse
{
    public string? Error { get; set; }

    public string? Code { get; set; }

    public string? Message { get; set; }
}

/*
 * L'API peut parfois renvoyer horaire sous forme de nombre, de chaîne ou null.
 * Ce convertisseur évite de casser la désérialisation mobile.
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

    /// <summary>
    /// Transforme un token JSON complexe en texte pour ne pas bloquer le chargement mobile
    /// lorsqu'un champ supposé simple arrive sous une forme inattendue.
    /// </summary>
    private static string ReadComplexTokenAsText(ref Utf8JsonReader reader)
    {
        using var document = JsonDocument.ParseValue(ref reader);
        return document.RootElement.ToString();
    }
}
