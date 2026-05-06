using MobileSLI.Models;

namespace MobileSLI.ViewModels;

public sealed class TourneeListItemViewModel : ObservableObject
{
    public TourneeResumeDto Dto { get; }

    public TourneeListItemViewModel(TourneeResumeDto dto)
    {
        Dto = dto;
    }

    public string CodeTournee => Dto.CodeTournee;
    public string LibelleTournee => Dto.LibelleTournee;
    public string NombrePointsText => Dto.NombrePoints <= 0 ? "" : $"{Dto.NombrePoints} points";
}

public sealed class LigneListItemViewModel : ObservableObject
{
    public LocalTourneeLigne Ligne { get; }

    public LigneListItemViewModel(LocalTourneeLigne ligne)
    {
        Ligne = ligne;
    }

    public int Id => Ligne.Id;
    public string OrdreText => $"Arrêt {Ligne.OrdreArret}";
    public string ClientText => $"{Ligne.NumClient} - {Ligne.NomClient}";
    public string DetailText => string.IsNullOrWhiteSpace(Ligne.AdresseLigne1) ? Ligne.DescriptionPDL ?? string.Empty : Ligne.AdresseLigne1;
    public string StatutText => Ligne.StatutPassage switch
    {
        StatutPassageConstants.Fait => "Fait",
        StatutPassageConstants.NonFait => "Non fait",
        StatutPassageConstants.Anomalie => "Anomalie",
        _ => "À faire"
    };
}

public sealed class QuantiteSaisieViewModel : ObservableObject
{
    public LocalTourneeLigneQuantite Entity { get; }

    private string _quantiteLivreeText;
    private string _quantiteRecupereeText;

    public QuantiteSaisieViewModel(LocalTourneeLigneQuantite entity)
    {
        Entity = entity;
        _quantiteLivreeText = entity.QuantiteLivree.ToString();
        _quantiteRecupereeText = entity.QuantiteRecuperee.ToString();
    }

    public string Libelle => Entity.Libelle;

    public string QuantiteLivreeText
    {
        get => _quantiteLivreeText;
        set => SetProperty(ref _quantiteLivreeText, value);
    }

    public string QuantiteRecupereeText
    {
        get => _quantiteRecupereeText;
        set => SetProperty(ref _quantiteRecupereeText, value);
    }

    public bool ApplyToEntity(out string error)
    {
        error = string.Empty;

        if (!int.TryParse(string.IsNullOrWhiteSpace(QuantiteLivreeText) ? "0" : QuantiteLivreeText, out var livree))
        {
            error = $"Quantité livrée invalide pour {Libelle}.";
            return false;
        }

        if (!int.TryParse(string.IsNullOrWhiteSpace(QuantiteRecupereeText) ? "0" : QuantiteRecupereeText, out var recuperee))
        {
            error = $"Quantité récupérée invalide pour {Libelle}.";
            return false;
        }

        if (livree < 0 || recuperee < 0)
        {
            error = $"Quantité négative interdite pour {Libelle}.";
            return false;
        }

        Entity.QuantiteLivree = livree;
        Entity.QuantiteRecuperee = recuperee;
        return true;
    }
}

public sealed class RecapArticleViewModel
{
    public string Libelle { get; set; } = string.Empty;
    public int TotalLivre { get; set; }
    public int TotalRecupere { get; set; }
}

public sealed class DechargementItemViewModel
{
    public string ClientText { get; set; } = string.Empty;
    public string PointText { get; set; } = string.Empty;
    public string ZoneText { get; set; } = string.Empty;
    public string ArticlesText { get; set; } = string.Empty;
}
