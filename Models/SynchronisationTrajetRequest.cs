namespace MobileSLI.Models;

/// <summary>
/// Contrat de synchronisation complet incluant la tournée et le trajet camion.
/// Ce payload est utilisé lorsque l'API attend les informations de camion et de kilométrage
/// en plus des lignes de livraison saisies par le livreur.
/// </summary>
public sealed class SynchronisationTourneeAvecTrajetRequest
{
    /// <summary>
    /// Version du contrat JSON attendue par l'API.
    /// </summary>
    public string SchemaVersion { get; set; } = "1.3";

    /// <summary>
    /// Identifiant unique de la synchronisation.
    /// </summary>
    public string IdSynchronisation { get; set; } = string.Empty;

    /// <summary>
    /// Date métier de la tournée envoyée à l'API au format attendu par le contrat mobile.
    /// </summary>
    public string DateTournee { get; set; } = string.Empty;

    /// <summary>
    /// Code de la tournée synchronisée.
    /// </summary>
    public string CodeTournee { get; set; } = string.Empty;

    /// <summary>
    /// Libellé de la tournée synchronisée.
    /// </summary>
    public string LibelleTournee { get; set; } = string.Empty;

    /// <summary>
    /// Statut envoyé à l'API pour indiquer que la synchronisation est transmise par le mobile.
    /// </summary>
    public string StatutSynchronisation { get; set; } = "ENVOYEE";

    /// <summary>
    /// Informations du livreur associées à la tournée.
    /// </summary>
    public SynchronisationLivreurRequest Livreur { get; set; } = new();

    /// <summary>
    /// Informations du téléphone et de l'application au moment de l'envoi.
    /// </summary>
    public SynchronisationMobileRequest Mobile { get; set; } = new();

    /// <summary>
    /// Informations du camion et des kilométrages de départ et d'arrivée.
    /// </summary>
    public SynchronisationTrajetRequest Trajet { get; set; } = new();

    /// <summary>
    /// Commentaire global optionnel associé à la synchronisation.
    /// </summary>
    public string? CommentaireGlobal { get; set; }

    /// <summary>
    /// Lignes de tournée avec les saisies finales du livreur.
    /// </summary>
    public List<SynchronisationLigneRequest> Lignes { get; set; } = new();

    /// <summary>
    /// Construit le nouveau contrat avec trajet à partir du contrat de tournée existant.
    /// Cette méthode évite de dupliquer tout le mapping des lignes et limite le risque d'écart
    /// entre les deux formats de synchronisation.
    /// </summary>
    public static SynchronisationTourneeAvecTrajetRequest From(
        SynchronisationTourneeRequest request,
        SynchronisationTrajetRequest trajet)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(trajet);

        return new SynchronisationTourneeAvecTrajetRequest
        {
            SchemaVersion = request.SchemaVersion,
            IdSynchronisation = request.IdSynchronisation,
            DateTournee = request.DateTournee,
            CodeTournee = request.CodeTournee,
            LibelleTournee = request.LibelleTournee,
            StatutSynchronisation = request.StatutSynchronisation,
            Livreur = request.Livreur,
            Mobile = request.Mobile,
            Trajet = trajet,
            CommentaireGlobal = request.CommentaireGlobal,
            Lignes = request.Lignes
        };
    }
}

/// <summary>
/// Informations de trajet envoyées lors de la synchronisation.
/// Elles permettent de rattacher la tournée à un camion et aux kilométrages saisis sur le mobile.
/// </summary>
public sealed class SynchronisationTrajetRequest
{
    /// <summary>
    /// Camion sélectionné pour la tournée.
    /// </summary>
    public SynchronisationCamionRequest Camion { get; set; } = new();

    /// <summary>
    /// Kilométrage saisi au départ de la tournée.
    /// </summary>
    public int KilometrageDepart { get; set; }

    /// <summary>
    /// Kilométrage saisi à l'arrivée de la tournée.
    /// </summary>
    public int KilometrageArrivee { get; set; }

    /// <summary>
    /// Date et heure de départ enregistrées par le mobile.
    /// </summary>
    public string DateDepartMobile { get; set; } = string.Empty;

    /// <summary>
    /// Date et heure d'arrivée enregistrées par le mobile.
    /// </summary>
    public string DateArriveeMobile { get; set; } = string.Empty;
}

/// <summary>
/// Informations camion incluses dans le contrat de synchronisation.
/// </summary>
public sealed class SynchronisationCamionRequest
{
    /// <summary>
    /// Identifiant technique du camion.
    /// </summary>
    public string IdCamion { get; set; } = string.Empty;

    /// <summary>
    /// Code fonctionnel du camion.
    /// </summary>
    public string CodeCamion { get; set; } = string.Empty;

    /// <summary>
    /// Libellé métier du camion.
    /// </summary>
    public string LibelleCamion { get; set; } = string.Empty;

    /// <summary>
    /// Immatriculation du camion.
    /// </summary>
    public string Immatriculation { get; set; } = string.Empty;
}
