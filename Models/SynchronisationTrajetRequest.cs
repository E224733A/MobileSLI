namespace MobileSLI.Models;

public sealed class SynchronisationTourneeAvecTrajetRequest
{
    public string SchemaVersion { get; set; } = "1.3";

    public string IdSynchronisation { get; set; } = string.Empty;

    public string DateTournee { get; set; } = string.Empty;

    public string CodeTournee { get; set; } = string.Empty;

    public string LibelleTournee { get; set; } = string.Empty;

    public string StatutSynchronisation { get; set; } = "ENVOYEE";

    public SynchronisationLivreurRequest Livreur { get; set; } = new();

    public SynchronisationMobileRequest Mobile { get; set; } = new();

    public SynchronisationTrajetRequest Trajet { get; set; } = new();

    public string? CommentaireGlobal { get; set; }

    public List<SynchronisationLigneRequest> Lignes { get; set; } = new();

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

public sealed class SynchronisationTrajetRequest
{
    public SynchronisationCamionRequest Camion { get; set; } = new();

    public int KilometrageDepart { get; set; }

    public int KilometrageArrivee { get; set; }

    public string DateDepartMobile { get; set; } = string.Empty;

    public string DateArriveeMobile { get; set; } = string.Empty;
}

public sealed class SynchronisationCamionRequest
{
    public string IdCamion { get; set; } = string.Empty;

    public string CodeCamion { get; set; } = string.Empty;

    public string LibelleCamion { get; set; } = string.Empty;

    public string Immatriculation { get; set; } = string.Empty;
}
