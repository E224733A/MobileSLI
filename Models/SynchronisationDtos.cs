using System.Text.Json.Serialization;

namespace TourneesMobile.Models;

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
    public DateTime DateChargementMobile { get; set; }

    [JsonPropertyName("dateEnvoiMobile")]
    public DateTime DateEnvoiMobile { get; set; }
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
    public PointLivraisonDto PointLivraison { get; set; } = new();

    [JsonPropertyName("saisie")]
    public SaisieDto Saisie { get; set; } = new();
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
