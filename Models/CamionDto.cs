namespace MobileSLI.Models;

/// <summary>
/// Réponse API contenant la liste des camions disponibles et la version du contrat JSON.
/// </summary>
public sealed class CamionsDisponiblesResponseDto
{
    /// <summary>
    /// Version du schéma JSON utilisée par l'API pour la liste des camions.
    /// </summary>
    public string SchemaVersion { get; set; } = "1.3";

    /// <summary>
    /// Camions actifs disponibles pour rattacher une tournée mobile à un trajet camion.
    /// </summary>
    public List<CamionDto> Camions { get; set; } = new();
}

/// <summary>
/// Camion sélectionnable dans le mobile avant le chargement ou la synchronisation de tournée.
/// </summary>
public sealed class CamionDto
{
    /// <summary>
    /// Identifiant technique du camion fourni par l'API.
    /// </summary>
    public string IdCamion { get; set; } = string.Empty;

    /// <summary>
    /// Code fonctionnel du camion utilisé dans les échanges avec l'API.
    /// </summary>
    public string CodeCamion { get; set; } = string.Empty;

    /// <summary>
    /// Libellé métier du camion.
    /// </summary>
    public string LibelleCamion { get; set; } = string.Empty;

    /// <summary>
    /// Immatriculation affichée en priorité à l'utilisateur lorsqu'elle est disponible.
    /// </summary>
    public string Immatriculation { get; set; } = string.Empty;

    /// <summary>
    /// Indique si le camion peut être proposé dans la liste de sélection mobile.
    /// </summary>
    public bool EstActif { get; set; }

    /// <summary>
    /// Libellé d'affichage robuste pour l'écran de choix camion.
    /// Priorité : immatriculation + libellé, puis immatriculation, libellé, code, identifiant.
    /// </summary>
    public string NomAffiche
    {
        get
        {
            var immatriculation = Immatriculation?.Trim() ?? string.Empty;
            var libelle = LibelleCamion?.Trim() ?? string.Empty;
            var code = CodeCamion?.Trim() ?? string.Empty;
            var id = IdCamion?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(immatriculation)
                && !string.IsNullOrWhiteSpace(libelle)
                && !string.Equals(immatriculation, libelle, StringComparison.OrdinalIgnoreCase))
            {
                return $"{immatriculation} — {libelle}";
            }

            if (!string.IsNullOrWhiteSpace(immatriculation))
            {
                return immatriculation;
            }

            if (!string.IsNullOrWhiteSpace(libelle))
            {
                return libelle;
            }

            if (!string.IsNullOrWhiteSpace(code))
            {
                return code;
            }

            return id;
        }
    }
}
