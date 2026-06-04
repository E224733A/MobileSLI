namespace MobileSLI.Models;

public sealed class CamionsDisponiblesResponseDto
{
    public string SchemaVersion { get; set; } = "1.3";

    public List<CamionDto> Camions { get; set; } = new();
}

public sealed class CamionDto
{
    public string IdCamion { get; set; } = string.Empty;

    public string CodeCamion { get; set; } = string.Empty;

    public string LibelleCamion { get; set; } = string.Empty;

    public string Immatriculation { get; set; } = string.Empty;

    public bool EstActif { get; set; }

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
