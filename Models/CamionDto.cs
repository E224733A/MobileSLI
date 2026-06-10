namespace MobileSLI.Models;

/// <summary>
/// Response DTO containing the list of available trucks and the schema version.
/// </summary>
public sealed class CamionsDisponiblesResponseDto
{
    /// <summary>
    /// Version of the JSON schema used by the API.
    /// </summary>
    public string SchemaVersion { get; set; } = "1.3";

    /// <summary>
    /// List of available trucks returned from the API.
    /// </summary>
    public List<CamionDto> Camions { get; set; } = new();
}

/// <summary>
/// Data Transfer Object representing a truck (camion).
/// </summary>
public sealed class CamionDto
{
    /// <summary>
    /// Unique identifier of the truck.
    /// </summary>
    public string IdCamion { get; set; } = string.Empty;

    /// <summary>
    /// Company code assigned to the truck.
    /// </summary>
    public string CodeCamion { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable label for the truck.
    /// </summary>
    public string LibelleCamion { get; set; } = string.Empty;

    /// <summary>
    /// License plate number.
    /// </summary>
    public string Immatriculation { get; set; } = string.Empty;

    /// <summary>
    /// Indicates whether the truck is active and available.
    /// </summary>
    public bool EstActif { get; set; }

    /// <summary>
    /// Derived display name for the truck. Prefers showing both immatriculation and libelle when both are present
    /// and distinct. Otherwise falls back to immatriculation, libelle, code, or finally the identifier.
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
