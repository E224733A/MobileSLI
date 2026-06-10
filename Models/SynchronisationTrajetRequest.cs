namespace MobileSLI.Models;

/// <summary>
/// Represents a synchronization request for an entire tour along with the associated trip (trajet).
/// Combines the general tour information with driver, device, trip details, and the list of delivery lines.
/// </summary>
public sealed class SynchronisationTourneeAvecTrajetRequest
{
    /// <summary>
    /// Version of the JSON schema used by the API.
    /// </summary>
    public string SchemaVersion { get; set; } = "1.3";

    /// <summary>
    /// Unique identifier for the synchronization request.
    /// </summary>
    public string IdSynchronisation { get; set; } = string.Empty;

    /// <summary>
    /// Date of the tour being synchronized.
    /// </summary>
    public string DateTournee { get; set; } = string.Empty;

    /// <summary>
    /// Code identifying the tour.
    /// </summary>
    public string CodeTournee { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable label for the tour.
    /// </summary>
    public string LibelleTournee { get; set; } = string.Empty;

    /// <summary>
    /// Status of the synchronization request.
    /// </summary>
    public string StatutSynchronisation { get; set; } = "ENVOYEE";

    /// <summary>
    /// Driver information included in the request.
    /// </summary>
    public SynchronisationLivreurRequest Livreur { get; set; } = new();

    /// <summary>
    /// Device information included in the request.
    /// </summary>
    public SynchronisationMobileRequest Mobile { get; set; } = new();

    /// <summary>
    /// Trip details such as mileage and dates.
    /// </summary>
    public SynchronisationTrajetRequest Trajet { get; set; } = new();

    /// <summary>
    /// Optional global comment for the synchronization.
    /// </summary>
    public string? CommentaireGlobal { get; set; }

    /// <summary>
    /// Collection of line-level synchronization requests included in the tour.
    /// </summary>
    public List<SynchronisationLigneRequest> Lignes { get; set; } = new();

    /// <summary>
    /// Creates a new request by combining an existing tour synchronization request with trip details.
    /// </summary>
    /// <param name="request">Base tour synchronization request.</param>
    /// <param name="trajet">Trip details to include.</param>
    /// <returns>A new <see cref="SynchronisationTourneeAvecTrajetRequest"/>.</returns>
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
/// Contains trip-level information included in a synchronization request.
/// Records the truck used and the starting/ending mileage and dates.
/// </summary>
public sealed class SynchronisationTrajetRequest
{
    /// <summary>
    /// Information about the truck used for the trip.
    /// </summary>
    public SynchronisationCamionRequest Camion { get; set; } = new();

    /// <summary>
    /// Starting odometer reading.
    /// </summary>
    public int KilometrageDepart { get; set; }

    /// <summary>
    /// Ending odometer reading.
    /// </summary>
    public int KilometrageArrivee { get; set; }

    /// <summary>
    /// Departure date recorded on the mobile device.
    /// </summary>
    public string DateDepartMobile { get; set; } = string.Empty;

    /// <summary>
    /// Arrival date recorded on the mobile device.
    /// </summary>
    public string DateArriveeMobile { get; set; } = string.Empty;
}

/// <summary>
/// Represents the truck information included in a synchronization request.
/// </summary>
public sealed class SynchronisationCamionRequest
{
    /// <summary>
    /// Unique identifier of the truck.
    /// </summary>
    public string IdCamion { get; set; } = string.Empty;

    /// <summary>
    /// Code assigned to the truck.
    /// </summary>
    public string CodeCamion { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable label for the truck.
    /// </summary>
    public string LibelleCamion { get; set; } = string.Empty;

    /// <summary>
    /// License plate number of the truck.
    /// </summary>
    public string Immatriculation { get; set; } = string.Empty;
}
