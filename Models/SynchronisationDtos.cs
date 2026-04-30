using System.Text.Json.Serialization;

namespace TourneesMobile.Models;

/// <summary>
/// Contrat JSON exact envoyé vers POST /api/synchronisations.
/// Ces DTO de sortie sont séparés des DTO de lecture afin de ne pas envoyer des champs inutiles.
/// </summary>
public sealed class SynchronisationTourneeRequest
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = "1.0";

    [JsonPropertyName("idSynchronisation")]
    public string IdSynchronisation { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("dateTournee")]
    public string DateTournee { get; set; } = string.Empty;

    [JsonPropertyName("codeTournee")]
    public string CodeTournee { get; set; } = string.Empty;

    [JsonPropertyName("libelleTournee")]
    public string? LibelleTournee { get; set; }

    [JsonPropertyName("livreur")]
    public LivreurDto Livreur { get; set; } = new();

    [JsonPropertyName("mobile")]
    public MobileDto Mobile { get; set; } = new();

    [JsonPropertyName("commentaireGlobal")]
    public string? CommentaireGlobal { get; set; }

    [JsonPropertyName("lignes")]
    public List<SynchronisationLigneDto> Lignes { get; set; } = [];
}

public sealed class MobileDto
{
    [JsonPropertyName("nomAppareil")]
    public string NomAppareil { get; set; } = string.Empty;

    [JsonPropertyName("versionApplication")]
    public string VersionApplication { get; set; } = string.Empty;

    [JsonPropertyName("dateChargementMobile")]
    public string DateChargementMobile { get; set; } = string.Empty;

    [JsonPropertyName("dateEnvoiMobile")]
    public string DateEnvoiMobile { get; set; } = string.Empty;
}

public sealed class SynchronisationLigneDto
{
    [JsonPropertyName("idLigneSource")]
    public string IdLigneSource { get; set; } = string.Empty;

    [JsonPropertyName("ordreArret")]
    public int? OrdreArret { get; set; }

    [JsonPropertyName("client")]
    public ClientDto Client { get; set; } = new();

    [JsonPropertyName("pointLivraison")]
    public PointLivraisonSynchronisationDto PointLivraison { get; set; } = new();

    [JsonPropertyName("saisie")]
    public SynchronisationSaisieDto Saisie { get; set; } = new();
}

public sealed class PointLivraisonSynchronisationDto
{
    [JsonPropertyName("codePDL")]
    public string? CodePDL { get; set; }

    [JsonPropertyName("descriptionPDL")]
    public string? DescriptionPDL { get; set; }
}

public sealed class SynchronisationSaisieDto
{
    [JsonPropertyName("nbExpes")]
    public int NbExpes { get; set; }

    [JsonPropertyName("nbRolls")]
    public int NbRolls { get; set; }

    [JsonPropertyName("nbVetements")]
    public int NbVetements { get; set; }

    [JsonPropertyName("nbTapis")]
    public int NbTapis { get; set; }

    [JsonPropertyName("nbSacs")]
    public int NbSacs { get; set; }

    [JsonPropertyName("nbRecuperes")]
    public int NbRecuperes { get; set; }

    [JsonPropertyName("precisionLivreur")]
    public string? PrecisionLivreur { get; set; }

    [JsonPropertyName("statutPassage")]
    public string StatutPassage { get; set; } = StatutPassage.AFaire;

    [JsonPropertyName("commentaireLivreur")]
    public string? CommentaireLivreur { get; set; }

    [JsonPropertyName("heureValidation")]
    public string? HeureValidation { get; set; }

    [JsonPropertyName("estValidee")]
    public bool EstValidee { get; set; }
}

public sealed class SynchronisationResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("idSynchronisation")]
    public string? IdSynchronisation { get; set; }

    [JsonPropertyName("dateReceptionApi")]
    public DateTime? DateReceptionApi { get; set; }

    [JsonPropertyName("statutSynchronisation")]
    public string? StatutSynchronisation { get; set; }

    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = [];

    [JsonIgnore]
    public bool EstDoublon =>
        !Success &&
        !string.IsNullOrWhiteSpace(Message) &&
        Message.Contains("déjà", StringComparison.OrdinalIgnoreCase);
}
