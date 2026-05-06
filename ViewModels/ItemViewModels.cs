using MobileSLI.Models;

namespace MobileSLI.ViewModels;

public sealed class TourneeListItemViewModel : ObservableObject
{
    public TourneeListItemViewModel(TourneeResumeDto dto)
    {
        Dto = dto;
    }

    public TourneeResumeDto Dto { get; }

    public string CodeTournee => Dto.CodeTournee;

    public string LibelleTournee => Dto.LibelleTournee;

    public string NomAffiche =>
        string.IsNullOrWhiteSpace(LibelleTournee)
            ? CodeTournee
            : $"{CodeTournee} - {LibelleTournee}";

    /*
     * Conservé pour compatibilité avec d'anciennes versions de ChoixTourneePage.xaml.
     * Le nouvel écran n'a pas besoin d'afficher le nombre de points.
     */
    public string NombrePointsText =>
        Dto.NombrePoints <= 0
            ? string.Empty
            : $"{Dto.NombrePoints} points";
}

public sealed class LigneListItemViewModel : ObservableObject
{
    public LigneListItemViewModel(LocalTourneeLigne ligne)
    {
        Ligne = ligne;
    }

    public LocalTourneeLigne Ligne { get; }

    public int Id => Ligne.Id;

    public string OrdreText => $"Arrêt {Ligne.OrdreArret}";

    public string ClientText =>
        string.IsNullOrWhiteSpace(Ligne.NomClient)
            ? Ligne.NumClient
            : $"{Ligne.NumClient} - {Ligne.NomClient}";

    public string PointText =>
        string.IsNullOrWhiteSpace(Ligne.DescriptionPDL)
            ? Ligne.CodePDL ?? string.Empty
            : Ligne.DescriptionPDL;

    public string DetailText =>
        string.IsNullOrWhiteSpace(Ligne.AdresseLigne1)
            ? PointText
            : Ligne.AdresseLigne1;

    public string AdresseText =>
        string.Join(
            " ",
            new[]
            {
                Ligne.AdresseLigne1,
                Ligne.CodePostal,
                Ligne.Ville
            }.Where(value => !string.IsNullOrWhiteSpace(value)));

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
    private string _quantiteLivreeText;
    private string _quantiteRecupereeText;

    public QuantiteSaisieViewModel(LocalTourneeLigneQuantite entity)
    {
        Entity = entity;
        _quantiteLivreeText = entity.QuantiteLivree.ToString();
        _quantiteRecupereeText = entity.QuantiteRecuperee.ToString();
    }

    public LocalTourneeLigneQuantite Entity { get; }

    public string CodeArticle => Entity.CodeArticle;

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

        if (!int.TryParse(
                string.IsNullOrWhiteSpace(QuantiteLivreeText) ? "0" : QuantiteLivreeText.Trim(),
                out var quantiteLivree))
        {
            error = $"Quantité livrée invalide pour {Libelle}.";
            return false;
        }

        if (!int.TryParse(
                string.IsNullOrWhiteSpace(QuantiteRecupereeText) ? "0" : QuantiteRecupereeText.Trim(),
                out var quantiteRecuperee))
        {
            error = $"Quantité récupérée invalide pour {Libelle}.";
            return false;
        }

        if (quantiteLivree < 0 || quantiteRecuperee < 0)
        {
            error = $"Quantité négative interdite pour {Libelle}.";
            return false;
        }

        Entity.QuantiteLivree = quantiteLivree;
        Entity.QuantiteRecuperee = quantiteRecuperee;

        return true;
    }
}

public sealed class RecapArticleViewModel
{
    public string Libelle { get; set; } = string.Empty;

    public int TotalLivre { get; set; }

    public int TotalRecupere { get; set; }

    public string TotalText => $"Livré : {TotalLivre} · Récupéré : {TotalRecupere}";
}

public sealed class DechargementItemViewModel
{
    public string ClientText { get; set; } = string.Empty;

    public string PointText { get; set; } = string.Empty;

    public string ZoneText { get; set; } = string.Empty;

    public string ArticlesText { get; set; } = string.Empty;
}
