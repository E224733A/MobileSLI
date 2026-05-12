using MobileSLI.Configuration;
using SQLite;

namespace MobileSLI.Models;

public sealed class LocalTournee
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    public string SchemaVersion { get; set; } = AppConfig.SchemaVersion;

    [Indexed]
    public DateTime DateTournee { get; set; }

    [Indexed]
    public string CodeTournee { get; set; } = string.Empty;

    public string LibelleTournee { get; set; } = string.Empty;
    public string CodeLivreur { get; set; } = string.Empty;
    public string NomLivreur { get; set; } = string.Empty;
    public string StatutLocal { get; set; } = TourneeLocalStatus.Chargee;
    public DateTime DateChargement { get; set; }
    public DateTime? DateEnvoi { get; set; }
    public string IdSynchronisation { get; set; } = Guid.NewGuid().ToString();
    public bool EstVerrouillee { get; set; }
    public string? CommentaireGlobal { get; set; }
}

public sealed class LocalTourneeLigne
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int TourneeId { get; set; }

    [Indexed]
    public string IdLigneSource { get; set; } = string.Empty;

    public int OrdreArret { get; set; }
    public string? Horaire { get; set; }

    public string NumClient { get; set; } = string.Empty;
    public string NomClient { get; set; } = string.Empty;
    public string? NomAffiche { get; set; }

    public string? CodePDL { get; set; }
    public string? DescriptionPDL { get; set; }
    public string? AdresseLigne1 { get; set; }
    public string? AdresseLigne2 { get; set; }
    public string? AdresseLigne3 { get; set; }
    public string? Ville { get; set; }
    public string? CodePostal { get; set; }

    public string? CodeTournee { get; set; }
    public string? LibelleTournee { get; set; }
    public int? JourTournee { get; set; }
    public string? JourLibelle { get; set; }
    public string? SchemaLivraison { get; set; }

    public int? JourTourneeRetour { get; set; }
    public string? JourRetourLibelle { get; set; }
    public string? CodeTourneeRetour { get; set; }
    public string? LibelleTourneeRetour { get; set; }

    public string? Zone { get; set; }
    public string? ZoneDechargement { get; set; }

    /*
     * Valeur finale renvoyée par l'API lorsqu'elle applique déjà la règle métier.
     * La propriété calculée ZoneDechargementAffichee l'utilise en priorité.
     */
    public string? ZoneDechargementAfficheeValeur { get; set; }

    public string? Precision { get; set; }
    public string? Cle { get; set; }

    public bool EstFerme { get; set; }
    public DateTime? DateFermeture { get; set; }
    public string? MotifFermeture { get; set; }

    public string? Instructions { get; set; }

    /*
     * Ancien champ conservé pour compatibilité.
     */
    public string? CommentaireFiche { get; set; }

    /*
     * Commentaire ponctuel venant de l'administration ou de l'expédition.
     * Ce n'est pas le commentaire saisi par le livreur.
     */
    public string? CommentaireExceptionnel { get; set; }

    public string StatutPassage { get; set; } = StatutPassageConstants.AFaire;
    public bool EstValidee { get; set; }
    public DateTime? HeureValidation { get; set; }
    public string? CommentaireLivreur { get; set; }
    public string? PrecisionLivreur { get; set; }

    [Ignore]
    public string? ZoneDechargementAffichee
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(ZoneDechargementAfficheeValeur))
            {
                return ZoneDechargementAfficheeValeur.Trim();
            }

            if (string.IsNullOrWhiteSpace(ZoneDechargement))
            {
                return JourTourneeRetour?.ToString();
            }

            var zone = ZoneDechargement.Trim();

            if (zone.StartsWith("+", StringComparison.Ordinal))
            {
                return JourTourneeRetour.HasValue
                    ? $"{JourTourneeRetour.Value} {zone}"
                    : zone;
            }

            return zone;
        }
    }

    [Ignore]
    public bool IsFermetureVisible => EstFerme;

    [Ignore]
    public string FermetureText
    {
        get
        {
            if (!EstFerme)
            {
                return string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(MotifFermeture))
            {
                return $"Client fermé : {MotifFermeture}";
            }

            if (DateFermeture.HasValue)
            {
                return $"Client fermé le {DateFermeture.Value:dd/MM/yyyy}";
            }

            return "Client fermé.";
        }
    }
}

public sealed class LocalTourneeLigneQuantite
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public int LigneId { get; set; }

    public string CodeArticle { get; set; } = string.Empty;
    public string Libelle { get; set; } = string.Empty;

    /*
     * Pré-remplissage expédition pour la colonne Livré.
     * null = non renseigné ; 0 = zéro volontaire ; > 0 = quantité prévue.
     */
    public int? QuantiteLivreePrevue { get; set; }

    public int QuantiteLivree { get; set; }
    public int QuantiteRecuperee { get; set; }
}
