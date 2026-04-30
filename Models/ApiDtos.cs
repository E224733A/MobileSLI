using System.Text.Json.Serialization;

namespace TourneesMobile.Models;

/// <summary>
/// Contrat JSON exact reçu depuis GET /api/tournees/jour.
/// Les noms de propriétés correspondent volontairement au contrat API choisi.
/// </summary>
public sealed class TourneeMobileDto
{
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; set; } = "1.0";

    [JsonPropertyName("dateTournee")]
    public string DateTournee { get; set; } = string.Empty;

    [JsonPropertyName("jourTournee")]
    public int? JourTournee { get; set; }

    [JsonPropertyName("jourLibelle")]
    public string? JourLibelle { get; set; }

    [JsonPropertyName("codeTournee")]
    public string CodeTournee { get; set; } = string.Empty;

    [JsonPropertyName("libelleTournee")]
    public string? LibelleTournee { get; set; }

    [JsonPropertyName("statutSynchronisation")]
    public string StatutSynchronisation { get; set; } = StatutSynchronisation.NonEnvoyee;

    [JsonPropertyName("livreur")]
    public LivreurDto Livreur { get; set; } = new();

    [JsonPropertyName("chargement")]
    public ChargementDto? Chargement { get; set; }

    [JsonPropertyName("lignes")]
    public List<TourneeLigneMobileDto> Lignes { get; set; } = [];
}

public sealed class LivreurDto
{
    [JsonPropertyName("codeLivreur")]
    public string CodeLivreur { get; set; } = string.Empty;

    [JsonPropertyName("nomLivreur")]
    public string NomLivreur { get; set; } = string.Empty;
}

public sealed class ChargementDto
{
    [JsonPropertyName("dateGenerationApi")]
    public DateTimeOffset? DateGenerationApi { get; set; }

    [JsonPropertyName("nombrePointsEnvoyes")]
    public int NombrePointsEnvoyes { get; set; }
}

public sealed class TourneeLigneMobileDto
{
    [JsonPropertyName("idLigneSource")]
    public string IdLigneSource { get; set; } = string.Empty;

    [JsonPropertyName("ordreArret")]
    public int? OrdreArret { get; set; }

    [JsonPropertyName("horaire")]
    public int? Horaire { get; set; }

    [JsonPropertyName("client")]
    public ClientDto Client { get; set; } = new();

    [JsonPropertyName("pointLivraison")]
    public PointLivraisonDto PointLivraison { get; set; } = new();

    [JsonPropertyName("tournee")]
    public TourneeInfoDto? Tournee { get; set; }

    [JsonPropertyName("retour")]
    public RetourInfoDto? Retour { get; set; }

    [JsonPropertyName("infosLivreur")]
    public InfosLivreurDto? InfosLivreur { get; set; }

    [JsonPropertyName("saisie")]
    public SaisieDto? Saisie { get; set; }
}

public sealed class ClientDto
{
    [JsonPropertyName("numClient")]
    public string NumClient { get; set; } = string.Empty;

    [JsonPropertyName("nomClient")]
    public string NomClient { get; set; } = string.Empty;

    [JsonPropertyName("nomAffiche")]
    public string? NomAffiche { get; set; }
}

public sealed class PointLivraisonDto
{
    [JsonPropertyName("codePDL")]
    public string? CodePDL { get; set; }

    [JsonPropertyName("descriptionPDL")]
    public string? DescriptionPDL { get; set; }

    [JsonPropertyName("adresseLigne1")]
    public string? AdresseLigne1 { get; set; }

    [JsonPropertyName("adresseLigne2")]
    public string? AdresseLigne2 { get; set; }

    [JsonPropertyName("adresseLigne3")]
    public string? AdresseLigne3 { get; set; }

    [JsonPropertyName("ville")]
    public string? Ville { get; set; }

    [JsonPropertyName("codePostal")]
    public string? CodePostal { get; set; }
}

public sealed class TourneeInfoDto
{
    [JsonPropertyName("codeTournee")]
    public string? CodeTournee { get; set; }

    [JsonPropertyName("libelleTournee")]
    public string? LibelleTournee { get; set; }

    [JsonPropertyName("jourTournee")]
    public int? JourTournee { get; set; }

    [JsonPropertyName("schemaLivraison")]
    public string? SchemaLivraison { get; set; }
}

public sealed class RetourInfoDto
{
    [JsonPropertyName("jourTourneeRetour")]
    public int? JourTourneeRetour { get; set; }

    [JsonPropertyName("jourRetourLibelle")]
    public string? JourRetourLibelle { get; set; }

    [JsonPropertyName("codeTourneeRetour")]
    public string? CodeTourneeRetour { get; set; }

    [JsonPropertyName("libelleTourneeRetour")]
    public string? LibelleTourneeRetour { get; set; }
}

public sealed class InfosLivreurDto
{
    [JsonPropertyName("instructions")]
    public string? Instructions { get; set; }

    [JsonPropertyName("commentaireFiche")]
    public string? CommentaireFiche { get; set; }

    [JsonPropertyName("zoneDechargement")]
    public string? ZoneDechargement { get; set; }

    [JsonPropertyName("zone")]
    public string? Zone { get; set; }

    [JsonPropertyName("precision")]
    public string? Precision { get; set; }

    [JsonPropertyName("cle")]
    public string? Cle { get; set; }

    [JsonPropertyName("estFerme")]
    public bool EstFerme { get; set; }

    [JsonPropertyName("dateFermeture")]
    public string? DateFermeture { get; set; }

    [JsonPropertyName("motifFermeture")]
    public string? MotifFermeture { get; set; }

    // Tolérance conservée pour les anciennes réponses de test qui plaçaient ces champs ici.
    [JsonPropertyName("schemaLivraison")]
    public string? SchemaLivraison { get; set; }

    [JsonPropertyName("typeLinge")]
    public string? TypeLinge { get; set; }
}

public sealed class SaisieDto
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
    public DateTime? HeureValidation { get; set; }

    [JsonPropertyName("estValidee")]
    public bool EstValidee { get; set; }
}
