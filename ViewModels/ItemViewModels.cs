using Microsoft.Maui.Graphics;
using MobileSLI.Models;

namespace MobileSLI.ViewModels;

public sealed class TourneeListItemViewModel : ObservableObject
{
    private bool _isSelected;

    public TourneeListItemViewModel(TourneeResumeDto dto)
    {
        Dto = dto;
    }

    public TourneeResumeDto Dto { get; }

    public string CodeTournee => Dto.CodeTournee;

    public string LibelleTournee => Dto.LibelleTournee;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (SetProperty(ref _isSelected, value))
            {
                OnPropertyChanged(nameof(CardBackgroundColor));
                OnPropertyChanged(nameof(CardBorderColor));
                OnPropertyChanged(nameof(BadgeBackgroundColor));
                OnPropertyChanged(nameof(BadgeBorderColor));
                OnPropertyChanged(nameof(BadgeTextColor));
                OnPropertyChanged(nameof(ButtonText));
                OnPropertyChanged(nameof(ButtonBackgroundColor));
                OnPropertyChanged(nameof(ButtonTextColor));
                OnPropertyChanged(nameof(SelectionText));
                OnPropertyChanged(nameof(SelectionTextColor));
            }
        }
    }

    public Color CardBackgroundColor => IsSelected
        ? Color.FromArgb("#1E40AF")
        : Color.FromArgb("#1E293B");

    public Color CardBorderColor => IsSelected
        ? Color.FromArgb("#93C5FD")
        : Color.FromArgb("#334155");

    public Color BadgeBackgroundColor => IsSelected
        ? Color.FromArgb("#DBEAFE")
        : Color.FromArgb("#0F172A");

    public Color BadgeBorderColor => IsSelected
        ? Color.FromArgb("#BFDBFE")
        : Color.FromArgb("#334155");

    public Color BadgeTextColor => IsSelected
        ? Color.FromArgb("#1D4ED8")
        : Color.FromArgb("#93C5FD");

    public string ButtonText => IsSelected ? "Sélectionnée" : "Choisir";

    public Color ButtonBackgroundColor => IsSelected
        ? Color.FromArgb("#22C55E")
        : Color.FromArgb("#1E293B");

    public Color ButtonTextColor => IsSelected
        ? Color.FromArgb("#FFFFFF")
        : Color.FromArgb("#93C5FD");

    public string SelectionText => IsSelected
        ? "Tournée actuellement sélectionnée"
        : "Touchez la carte ou le bouton pour sélectionner";

    public Color SelectionTextColor => IsSelected
        ? Color.FromArgb("#BBF7D0")
        : Color.FromArgb("#94A3B8");

    public string NomAffiche =>
        string.IsNullOrWhiteSpace(LibelleTournee)
            ? CodeTournee
            : $"{CodeTournee} - {LibelleTournee}";

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

    public string StatutPassage => Ligne.StatutPassage;

    public bool IsFerme => Ligne.EstFerme;

    public bool IsFermetureVisible => Ligne.EstFerme;

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

    public bool HasInstruction => !string.IsNullOrWhiteSpace(Ligne.Instructions);

    public bool HasCommentaireExceptionnel => !string.IsNullOrWhiteSpace(Ligne.CommentaireExceptionnel);

    public bool HasInformationLivreur => HasInstruction || HasCommentaireExceptionnel;

    public string InstructionText => Ligne.Instructions ?? string.Empty;

    public string CommentaireExceptionnelText => Ligne.CommentaireExceptionnel ?? string.Empty;

    public string StatutText
    {
        get
        {
            if (Ligne.EstFerme)
            {
                return "Fermé";
            }

            return Ligne.StatutPassage switch
            {
                StatutPassageConstants.Fait => "Fait",
                StatutPassageConstants.NonFait => "Non fait",
                StatutPassageConstants.Anomalie => "Anomalie",
                _ => "À faire"
            };
        }
    }

    public string FermetureText
    {
        get
        {
            if (!Ligne.EstFerme)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(Ligne.MotifFermeture))
            {
                return $"Client fermé : {Ligne.MotifFermeture}";
            }

            if (Ligne.DateFermeture.HasValue)
            {
                return $"Client fermé le {Ligne.DateFermeture.Value:dd/MM/yyyy}";
            }

            return "Client fermé.";
        }
    }

    public Color StatutBackgroundColor
    {
        get
        {
            if (Ligne.EstFerme)
            {
                return Color.FromArgb("#450A0A");
            }

            return Ligne.StatutPassage switch
            {
                StatutPassageConstants.Fait => Color.FromArgb("#052E1B"),
                StatutPassageConstants.NonFait => Color.FromArgb("#3B2604"),
                StatutPassageConstants.Anomalie => Color.FromArgb("#450A0A"),
                _ => Color.FromArgb("#0B1120")
            };
        }
    }

    public Color StatutBorderColor
    {
        get
        {
            if (Ligne.EstFerme)
            {
                return Color.FromArgb("#991B1B");
            }

            return Ligne.StatutPassage switch
            {
                StatutPassageConstants.Fait => Color.FromArgb("#166534"),
                StatutPassageConstants.NonFait => Color.FromArgb("#92400E"),
                StatutPassageConstants.Anomalie => Color.FromArgb("#991B1B"),
                _ => Color.FromArgb("#334155")
            };
        }
    }

    public Color StatutTextColor
    {
        get
        {
            if (Ligne.EstFerme)
            {
                return Color.FromArgb("#FCA5A5");
            }

            return Ligne.StatutPassage switch
            {
                StatutPassageConstants.Fait => Color.FromArgb("#86EFAC"),
                StatutPassageConstants.NonFait => Color.FromArgb("#FCD34D"),
                StatutPassageConstants.Anomalie => Color.FromArgb("#FCA5A5"),
                _ => Color.FromArgb("#CBD5E1")
            };
        }
    }
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

    public int? QuantiteLivreePrevue => Entity.QuantiteLivreePrevue;

    public bool HasQuantiteLivreePrevue => Entity.QuantiteLivreePrevue.HasValue;

    public string QuantitePrevueText =>
        Entity.QuantiteLivreePrevue.HasValue
            ? $"Prévu : {Entity.QuantiteLivreePrevue.Value}"
            : "Prévu : non renseigné";

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
